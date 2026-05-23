using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

/// <summary>
/// Singleton service that manages Safeguard appliance connection and authentication.
/// Uses MCP form-mode elicitation to obtain the server address and URL-mode elicitation
/// for OAuth2/PKCE browser-based login.
/// </summary>
public class SafeguardAuth(ILogger<SafeguardAuth> logger)
{
    private readonly ConcurrentDictionary<string, ServerConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _authLock = new(1, 1);

    private static readonly HttpClientHandler IgnoreSsl = new()
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    };

    private static readonly HttpClient httpClient = new(IgnoreSsl);
    private static readonly string StsAuthRedirectUri = "urn:InstalledApplicationTcpListener";

    /// <summary>Gets all currently connected host names.</summary>
    public IReadOnlyList<string> ConnectedHosts =>
        _connections.Keys.ToList();

    /// <summary>Builds a full URL for a Safeguard API request.</summary>
    public string BuildUrl(string host, string servicePath, string apiPath) =>
        $"https://{host}/{servicePath}{apiPath}";

    /// <summary>
    /// Resolves which host to target. Returns the given host if specified,
    /// the sole active host if only one connection exists, or null if none exist.
    /// Throws if multiple connections exist and host is not specified.
    /// </summary>
    public string ResolveHost(string host)
    {
        if (!string.IsNullOrWhiteSpace(host))
            return host.Trim();

        CleanupExpired();

        if (_connections.Count == 1)
            return _connections.Values.First().Host;
        if (_connections.IsEmpty)
            return null;

        throw new McpException(
            $"Multiple Safeguard servers are connected ({string.Join(", ", _connections.Keys)}). "
            + "Specify the 'host' parameter to indicate which server to use.");
    }

    /// <summary>Removes connections whose tokens have expired.</summary>
    private void CleanupExpired()
    {
        foreach (var kvp in _connections)
        {
            if (kvp.Value.TokenInfo != null && kvp.Value.TokenInfo.IsExpired())
            {
                logger.LogInformation("Removing expired connection to '{Host}'.", kvp.Key);
                _connections.TryRemove(kvp.Key, out _);
            }
        }
    }

    /// <summary>
    /// Ensures the Safeguard server host is configured, eliciting from the user if needed.
    /// Does not require authentication — suitable for public status endpoints.
    /// Returns the resolved host.
    /// </summary>
    public async Task<string> EnsureHostConfiguredAsync(McpServer server, string host, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(host))
        {
            host = host.Trim();
            _connections.GetOrAdd(host, h => new ServerConnection { Host = h });
            return host;
        }

        logger.LogDebug("EnsureHostConfiguredAsync: no host specified, acquiring lock to elicit...");
        await _authLock.WaitAsync(cancellationToken);
        try
        {
            host = await ElicitHostAsync(server, cancellationToken);
            _connections.GetOrAdd(host, h => new ServerConnection { Host = h });
            return host;
        }
        finally
        {
            _authLock.Release();
        }
    }

    /// <summary>
    /// Ensures the user is authenticated with a valid, non-expired token for the given host.
    /// Will elicit the server address (form-mode) and browser login (URL-mode) as needed.
    /// Returns the resolved host.
    /// </summary>
    public async Task<string> EnsureAuthenticatedAsync(McpServer server, string host, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(host))
        {
            host = host.Trim();
            if (_connections.TryGetValue(host, out var existing) && existing.IsAuthenticated)
                return host;
        }

        logger.LogDebug("EnsureAuthenticatedAsync: token missing or expired for '{Host}', acquiring lock...", host);
        await _authLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring the lock
            if (!string.IsNullOrWhiteSpace(host))
            {
                if (_connections.TryGetValue(host, out var rechecked) && rechecked.IsAuthenticated)
                {
                    logger.LogDebug("EnsureAuthenticatedAsync: token set by another caller for '{Host}'.", host);
                    return host;
                }
            }
            else
            {
                logger.LogInformation("No host specified — eliciting from user.");
                host = await ElicitHostAsync(server, cancellationToken);

                // The newly elicited host may already have a valid connection
                if (_connections.TryGetValue(host, out var elicited) && elicited.IsAuthenticated)
                    return host;
            }

            logger.LogInformation("Host is '{Host}' — starting browser login.", host);
            await BrowserLoginAsync(server, host, cancellationToken);
            return host;
        }
        finally
        {
            _authLock.Release();
        }
    }

    /// <summary>
    /// Sends an HTTP request to the Safeguard API. Includes the Bearer token if authenticated.
    /// </summary>
    public async Task<string> RequestAsync(
        string host, HttpMethod method, string url, string body = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("RequestAsync: {Method} {Url} (host={Host})", method, url, host);

        using var request = new HttpRequestMessage(method, url);

        if (_connections.TryGetValue(host, out var conn) && conn.UserToken != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", conn.UserToken);

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body != null)
            request.Content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        logger.LogDebug("RequestAsync: {Method} {Url} -> HTTP {StatusCode}", method, url, (int)response.StatusCode);

        if (response.StatusCode == HttpStatusCode.NoContent)
            return string.Empty;

        var data = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("RequestAsync failed: HTTP {StatusCode} — {Body}", (int)response.StatusCode, data);
            throw new McpException(
                $"Safeguard API {method} request failed with HTTP {(int)response.StatusCode}: {data}");
        }

        return data;
    }

    // ─── Elicitation ───────────────────────────────────────────────────

    private async Task<string> ElicitHostAsync(McpServer server, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending form-mode elicitation for Safeguard server address...");

        var result = await server.ElicitAsync(new ElicitRequestParams
        {
            Mode = "form",
            Message = "Enter the DNS name or IP address of the Safeguard server to connect to.",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                {
                    ["host"] = new ElicitRequestParams.StringSchema
                    {
                        Title = "Safeguard Server",
                        Description = "DNS name or IP address of the Safeguard appliance"
                    }
                },
                Required = ["host"]
            }
        }, cancellationToken);

        logger.LogInformation(
            "Elicit host result — Action: '{Action}', IsAccepted: {IsAccepted}, Content is null: {ContentNull}",
            result.Action, result.IsAccepted, result.Content == null);

        if (result.Content != null)
        {
            foreach (var kvp in result.Content)
                logger.LogInformation("  Elicit content key='{Key}', value='{Value}'", kvp.Key, kvp.Value);
        }

        // NOTE: VS MCP client sends "accepted" rather than "accept", so we check Action directly
        // instead of using result.IsAccepted which only matches "accept".
        var isAccepted = result.Action != null
            && result.Action.StartsWith("accept", StringComparison.OrdinalIgnoreCase);

        if (!isAccepted || result.Content == null
            || !result.Content.TryGetValue("host", out var hostElement))
        {
            logger.LogError("Host elicitation failed — IsAccepted={IsAccepted}, ContentKeys=[{Keys}]",
                isAccepted,
                result.Content != null ? string.Join(", ", result.Content.Keys) : "<null>");
            throw new McpException("Safeguard server address is required to proceed.");
        }

        var resolvedHost = hostElement.GetString();
        logger.LogInformation("Host configured: '{Host}'", resolvedHost);
        return resolvedHost;
    }

    // ─── Browser Login (OAuth2 / PKCE via URL-mode elicitation) ────────

    private async Task BrowserLoginAsync(McpServer server, string host, CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        var authData = new AuthData();

        try
        {
            listener.Start();
            authData.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            logger.LogInformation("TCP listener started on loopback port {Port}", authData.Port);

            var codeVerifier = OAuthCodeVerifier();
            var url = $"https://{host}/RSTS/Login?response_type=code";
            url += "&code_challenge_method=S256&code_challenge=" + OAuthCodeChallenge(codeVerifier);
            url += "&redirect_uri=" + Uri.EscapeDataString(StsAuthRedirectUri);
            url += $"&port={authData.Port}";

            logger.LogInformation("Login URL: {Url}", url);

            // Start TCP listener in background to capture the OAuth redirect
            _ = TcpListenAndRespondAsync(listener, authData, cancellationToken);

            // Open the login URL in the user's default browser
            logger.LogInformation("Launching browser for Safeguard login...");
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            // Wait for the TCP listener to capture the OAuth code via browser redirect
            logger.LogInformation("Awaiting OAuth redirect via TCP listener...");

            await authData.TaskCompletionSource.Task;

            logger.LogInformation("TCP listener completed. OAuthCode is null: {IsNull}",
                string.IsNullOrEmpty(authData.OAuthCode));

            if (string.IsNullOrEmpty(authData.OAuthCode))
            {
                logger.LogError("No OAuth authorization code was captured by TCP listener.");
                throw new McpException("Login failed — no authorization code was received.");
            }

            logger.LogInformation("OAuth code received: {Snippet}... Exchanging for tokens...",
                authData.OAuthCode.Substring(0, 10));

            await ExchangeCodeForTokenAsync(host, authData.OAuthCode, codeVerifier);

            var connInfo = _connections.GetValueOrDefault(host);

            logger.LogInformation("Authentication complete. User: {Upn}, Expires: {Expiry}",
                connInfo?.TokenInfo?.upn, connInfo?.TokenInfo?.Expiration);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BrowserLoginAsync failed.");
            throw;
        }
        finally
        {
            listener.Stop();
            logger.LogDebug("TCP listener stopped.");
        }
    }

    // ─── Token Exchange ────────────────────────────────────────────────

    private async Task ExchangeCodeForTokenAsync(string host, string code, string codeVerifier)
    {
        // Step 1: Exchange OAuth authorization code for RSTS access token
        var tokenUrl = $"https://{host}/RSTS/oauth2/token";
        var formData = HttpUtility.ParseQueryString(string.Empty);
        formData["code"] = code;
        formData["grant_type"] = "authorization_code";
        formData["code_verifier"] = codeVerifier;

        logger.LogInformation("Step 1: Exchanging OAuth code for RSTS token at {Url}...", tokenUrl);
        var token = await InternalHttpRequestAsync<TokenResponse>(
            HttpMethod.Post, tokenUrl, formData.ToString(),
            MediaTypeNames.Application.FormUrlEncoded);
        logger.LogInformation("Step 1 complete. AccessToken is null: {IsNull}", token?.AccessToken == null);

        // Step 2: Exchange RSTS access token for Safeguard UserToken
        var loginUrl = $"https://{host}/service/Core/v4/Token/LoginResponse";
        var loginReq = new LoginResponseRequestData { StsAccessToken = token.AccessToken };
        var loginJson = JsonSerializer.Serialize(loginReq, SafeguardJsonContext.Default.LoginResponseRequestData);

        logger.LogInformation("Step 2: Exchanging RSTS token for Safeguard UserToken at {Url}...", loginUrl);
        var user = await InternalHttpRequestAsync<LoginResponse>(
            HttpMethod.Post, loginUrl, loginJson, MediaTypeNames.Application.Json);
        logger.LogInformation("Step 2 complete. UserToken is null: {IsNull}", user?.UserToken == null);

        var tokenInfo = DecodeAccessToken(user.UserToken);
        _connections[host] = new ServerConnection
        {
            Host = host,
            UserToken = user.UserToken,
            TokenInfo = tokenInfo
        };
    }

    private async Task<T> InternalHttpRequestAsync<T>(
        HttpMethod method, string url, string content, string contentType) where T : class
    {
        using var request = new HttpRequestMessage(method, url);
        request.Content = new StringContent(content, Encoding.UTF8, contentType);

        logger.LogDebug("InternalHttpRequest: {Method} {Url}", method, url);
        using var response = await httpClient.SendAsync(request);
        var data = await response.Content.ReadAsStringAsync();
        logger.LogDebug("InternalHttpRequest: {Method} {Url} -> HTTP {StatusCode}", method, url, (int)response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("InternalHttpRequest failed: HTTP {StatusCode} — {Body}", (int)response.StatusCode, data);
            throw new McpException($"Request to {url} failed with HTTP {response.StatusCode}: {data}");
        }

        if (string.IsNullOrEmpty(data))
            return default;

        if (typeof(T) == typeof(LoginResponse))
            return JsonSerializer.Deserialize(data, SafeguardJsonContext.Default.LoginResponse) as T;

        return JsonSerializer.Deserialize(data, SafeguardJsonContext.Default.TokenResponse) as T;
    }

    // ─── TCP Listener (captures OAuth redirect from browser) ───────────

    private async Task TcpListenAndRespondAsync(
        TcpListener tcpListener, AuthData tcs, CancellationToken token)
    {
        var registration = token.Register(() =>
        {
            tcpListener.Stop();
            tcs.TaskCompletionSource.TrySetCanceled();
        });

        try
        {
            while (!token.IsCancellationRequested)
            {
                logger.LogDebug("TCP listener: waiting for connection...");
                var tcpClient = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                logger.LogDebug("TCP listener: connection accepted from {Remote}", tcpClient.Client.RemoteEndPoint);
                _ = HandleTcpConnectionAsync(tcpClient, tcs, token);
            }
        }
        catch (InvalidOperationException ex)
        {
            logger.LogDebug(ex, "TCP listener stopped (expected on cancellation/completion).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TCP listener unexpected error.");
        }
        finally
        {
            registration.Dispose();
        }
    }

    private static readonly string HtmlWrapper =
        "<!doctype html><html><head><title>{0}</title>"
        + "<meta name=\"color-scheme\" content=\"dark\">"
        + "<script>var prefDark = window.matchMedia(\"(prefers-color-scheme: dark)\");"
        + "if (!prefDark.matches){{document.head.querySelector('meta[name=\"color-scheme\"]')"
        + ".setAttribute(\"content\", \"light\");}}</script></head><body>{1}</body></html>";

    private static readonly string SuccessMessage = "Login success.";
    private static readonly string FailureMessage = "Login failed.";
    private static readonly string CloseMessage = "You may close this web browser tab or window.";

    private async Task HandleTcpConnectionAsync(
        TcpClient client, AuthData tcs, CancellationToken token)
    {
        try
        {
            var req = await GetTcpResponseAsync(client, token).ConfigureAwait(false);
            logger.LogDebug("TCP received request path: {Request}", req);

            if (req.Contains("/favicon.ico"))
            {
                logger.LogDebug("TCP: favicon request — releasing semaphore.");
                tcs.FavIcon.Release();
                await SendFavIconAsync(client.GetStream(), token);
                return;
            }

            var html = GetResponse(req, tcs);
            logger.LogDebug("TCP: OAuthCode captured: {HasCode}", !string.IsNullOrEmpty(tcs.OAuthCode));

            await SendResponseMessageAsync(client.GetStream(), html, token);

            _ = await tcs.FavIcon.WaitAsync(1000);
            tcs.TaskCompletionSource.TrySetResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HandleTcpConnectionAsync error.");
            tcs.TaskCompletionSource.TrySetException(ex);
        }
        finally
        {
            client.Close();
        }
    }

    private static string GetResponse(string req, AuthData tcs)
    {
        var uri = new Uri(req, UriKind.RelativeOrAbsolute);
        var queryString = uri.IsAbsoluteUri ? uri.PathAndQuery : req;
        queryString = queryString.Trim('/', '?');
        var query = HttpUtility.ParseQueryString(queryString);

        if (HasOAuthCode(query, out var code))
        {
            tcs.OAuthCode = code;
            return string.Format(HtmlWrapper, SuccessMessage, $"{SuccessMessage} {CloseMessage}");
        }

        if (HasOAuthError(query, out var error, out var desc))
        {
            var msg = $"{FailureMessage} {CloseMessage}<br/>{HttpUtility.HtmlEncode(error)}<br/>{HttpUtility.HtmlEncode(desc)}";
            return string.Format(HtmlWrapper, FailureMessage, msg);
        }

        return string.Format(HtmlWrapper, FailureMessage,
            $"Unexpected response. {CloseMessage}<br/>{HttpUtility.HtmlEncode(req)}");
    }

    private static bool HasOAuthCode(System.Collections.Specialized.NameValueCollection query, out string code)
    {
        code = query["code"] ?? query["oauth"];
        return !string.IsNullOrEmpty(code);
    }

    private static bool HasOAuthError(
        System.Collections.Specialized.NameValueCollection query, out string error, out string desc)
    {
        error = query["error"];
        desc = query["error_description"];
        return !string.IsNullOrEmpty(error);
    }

    private static async Task SendResponseMessageAsync(NetworkStream stream, string html, CancellationToken token)
    {
        var fullResponse =
            $"HTTP/1.1 200 OK\r\nCache-Control: private, no-cache, must-revalidate\r\nContent-Type: text/html\r\nContent-Length: {html.Length}\r\nConnection: close\r\n\r\n{html}";
        var bytes = Encoding.ASCII.GetBytes(fullResponse);

        await stream.WriteAsync(bytes, token).ConfigureAwait(false);
    }

    private static async Task SendFavIconAsync(NetworkStream stream, CancellationToken token)
    {
        const string safeguardIcon = "AAABAAEAEBAAAAEAIABoBAAAFgAAACgAAAAQAAAAIAAAAAEAIAAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAA"
                + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA1"
                + "6YJGtqpBH7aqQOZ2qkDmdqpA5naqQOZ2qkDmdqpA5naqQR+16YJGgAAAAAAAAAAAAAAAAAAAAAAAAAAv78ABNqqBZTbqgb/3a4R/92vE"
                + "//drxP/3a8T/92vE//drhH/26oG/9qqBZS/vwAEAAAAAAAAAAAAAAAAAAAAANyrA0PaqQLu4709//bqw//47cv/9+3L//fty//47cv/9"
                + "urD/+O9Pf/aqQLu3KsDQwAAAAAAAAAAAAAAANqjAA7aqgS13K0O//Pkr//////////////////////////////////z5K//3K0O/9qqB"
                + "LXaowAOAAAAAAAAAADZqgVg2qgC+efHW//+/Pf//fvz//HfoP/58NL/+fDS//HfoP/9+/P//vz3/+fHW//aqAL52aoFYAAAAADYqQch2"
                + "qkCzd6yG//37cn///////XovP/drg//5L9C/+S/Qv/drg//9ei8///////37cn/3rIb/9qpAs3YqQch26kFitqqBP3s0nv////+/////"
                + "//79uP/474//9qpAf/aqQH/474///v24//////////+/+zSe//aqgT926kFitqqBK/cqwn/8uGm///////8+Or/5sRS/9ytDv/bqgT/2"
                + "6oE/9ytDv/mxFL//Pjq///////y4ab/3KsJ/9qqBK/cqQZR2qkC7uS/Q//8+e3//fv1/+fHWv/jvj//3bAV/92wFf/jvj//58da//379"
                + "f/8+e3/5L9D/9qpAu7cqQZR1KoABtqqA5rbqwf/8NuV///////9+vD/+/bj/+G5Mv/huTL/+/bj//368P//////8NuV/9urB//aqgOa1"
                + "KoABgAAAADdqwUu2qgC4eG3K//689z///////79+v/26b//9um///79+v//////+vPc/+G3K//aqALh3asFLgAAAAAAAAAAAAAAANupB"
                + "HvaqAP+69B2///+/f////////////////////////79/+vQdv/aqAP+26kEewAAAAAAAAAAAAAAAAAAAADXpgka2qkCy92wFf/nx1v/6"
                + "Mpi/+jKYv/oymL/6Mpi/+fHW//dsBX/2qkCy9emCRoAAAAAAAAAAAAAAAAAAAAAAAAAANypBlHbqgPc2qgB7dqnAOzapwDs2qcA7NqnA"
                + "OzaqAHt26oD3NypBlEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD/qgAD3acGJtqoBDjZqwQ32asEN9mrBDfZqwQ32qgEON2nBib/qgADA"
                + "AAAAAAAAAAAAAAA//8AAPgfAADgBwAA4AcAAMADAADAAwAAgAEAAAAAAAAAAAAAgAEAAIABAADAAwAA4AcAAOAHAADwDwAA//8AAA==";

        var iconData = Convert.FromBase64String(safeguardIcon);

        var headers = $"HTTP/1.1 200 OK\r\nCache-Control: private, no-cache, must-revalidate\r\nContent-Type: image/x-icon\r\nContent-Length: {iconData.Length}\r\nConnection: close\r\n\r\n";
        var headerData = Encoding.ASCII.GetBytes(headers);

        await stream.WriteAsync(headerData, token).ConfigureAwait(false);
        await stream.WriteAsync(iconData, token).ConfigureAwait(false);
    }

    private static async Task<string> GetTcpResponseAsync(TcpClient client, CancellationToken token)
    {
        const string EndOfHttpHeader = "\r\n\r\n";

        var networkStream = client.GetStream();
        var buffer = new byte[1024];
        var sb = new StringBuilder();
        var foundEnd = false;
        var maxBytesAllowed = 32 * 1024;

        while (sb.Length < EndOfHttpHeader.Length || !foundEnd)
        {
            var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
            if (bytesRead == 0) break;

            var packetString = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            if (packetString.EndsWith(EndOfHttpHeader) || packetString.Contains(EndOfHttpHeader))
            {
                foundEnd = true;
            }
            else if (sb.Length > 0)
            {
                var startIndex = sb.Length <= EndOfHttpHeader.Length ? 0 : sb.Length - EndOfHttpHeader.Length;
                var previous = sb.ToString(startIndex, Math.Min(EndOfHttpHeader.Length, sb.Length));
                foundEnd = (previous + packetString).Contains(EndOfHttpHeader);
            }

            sb.Append(packetString);
            maxBytesAllowed -= bytesRead;

            if (maxBytesAllowed < 0)
                throw new Exception("Received an unexpected amount of data while logging in.");
        }

        var str = sb.ToString();
        var requestLine = str.Split('\r').First();
        var parts = requestLine.Split(' ');

        if (parts.Length == 3)
            return parts[1];

        throw new Exception($"Malformed request: {str}");
    }

    // ─── PKCE / JWT Helpers ────────────────────────────────────────────

    private static string OAuthCodeVerifier()
    {
        var rnd = new byte[60];
        Random.Shared.NextBytes(rnd);
        return ToBase64Url(rnd);
    }

    private static string OAuthCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return ToBase64Url(hash);
    }

    private static string ToBase64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static SafeguardAuthToken DecodeAccessToken(string accessToken)
    {
        var parts = accessToken.Split('.');
        var claimData = parts[1];

        while (claimData.Length % 4 != 0)
            claimData += "=";

        claimData = claimData.Replace('-', '+').Replace('_', '/');
        claimData = Encoding.UTF8.GetString(Convert.FromBase64String(claimData));

        return JsonSerializer.Deserialize(claimData, SafeguardJsonContext.Default.SafeguardAuthToken);
    }
}

// ─── Supporting Types ──────────────────────────────────────────────────

public class ServerConnection
{
    public string Host { get; init; }
    public string UserToken { get; set; }
    public SafeguardAuthToken TokenInfo { get; set; }
    public bool IsAuthenticated => UserToken != null && TokenInfo != null && !TokenInfo.IsExpired();
}

public class AuthData
{
    public int Port { get; set; }
    public string OAuthCode { get; set; }
    public SemaphoreSlim FavIcon { get; } = new(0);
    public TaskCompletionSource<bool> TaskCompletionSource { get; } = new();
}

[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(LoginResponseRequestData))]
[JsonSerializable(typeof(SafeguardAuthToken))]
public partial class SafeguardJsonContext : JsonSerializerContext
{
}

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
}

public class LoginResponse
{
    public string UserToken { get; set; }
}

public class LoginResponseRequestData
{
    public string StsAccessToken { get; set; }
}

public class SafeguardAuthToken
{
    public string ActualUserId { get; set; }
    public string TimeZone { get; set; }
    public string Culture { get; set; }
    public string AuthTokenId { get; set; }
    public string upn { get; set; }
    public long nbf { get; set; }
    public long exp { get; set; }

    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DateTime Expiration => UnixEpoch.AddSeconds(exp);

    public bool IsExpired() => DateTime.UtcNow > Expiration;
}
