using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using SafeguardMcp.Catalog;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;
using BrowserLogin = OneIdentity.SafeguardDotNet.BrowserLogin.DefaultBrowserLogin;
using PkceLogin = OneIdentity.SafeguardDotNet.PkceNoninteractiveLogin.PkceNoninteractiveLogin;

namespace SafeguardMcp.Tools;

/// <summary>
/// Manages connections to one or more Safeguard appliances using the SafeguardDotNet SDK.
/// Handles interactive (browser) and non-interactive (headless) authentication,
/// multi-server connection tracking, token refresh, and SSL trust.
/// </summary>
public class SafeguardConnectionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, ISafeguardConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private readonly ILogger<SafeguardConnectionManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly CatalogProvider _catalogProvider;

    public SafeguardConnectionManager(
        ILogger<SafeguardConnectionManager> logger,
        IConfiguration configuration,
        CatalogProvider catalogProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _catalogProvider = catalogProvider;
    }

    /// <summary>Gets all currently connected host names.</summary>
    public IReadOnlyList<string> ConnectedHosts => _connections.Keys.ToList();

    /// <summary>
    /// Resolves which host to target. Returns the given host if specified,
    /// the sole active host if only one connection exists, or null if none exist.
    /// </summary>
    public string ResolveHost(string host)
    {
        if (!string.IsNullOrWhiteSpace(host))
            return host.Trim();

        CleanupExpired();

        if (_connections.Count == 1)
            return _connections.Keys.First();
        if (_connections.IsEmpty)
            return null;

        throw new McpException(
            $"Multiple Safeguard servers are connected ({string.Join(", ", _connections.Keys)}). "
            + "Specify the 'host' parameter to indicate which server to use.");
    }

    /// <summary>
    /// Ensures a connection exists and is authenticated for the given host.
    /// Uses MCP elicitation to prompt for host if not provided.
    /// </summary>
    public async Task<string> EnsureAuthenticatedAsync(McpServer server, string host, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(host))
        {
            host = host.Trim();
            if (_connections.ContainsKey(host))
            {
                EnsureTokenFresh(host);
                return host;
            }
        }

        await _authLock.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(host) && _connections.ContainsKey(host))
            {
                EnsureTokenFresh(host);
                return host;
            }

            host = ResolveConfiguredHost(host);
            if (string.IsNullOrWhiteSpace(host))
                host = await ElicitHostAsync(server, ct);

            if (_connections.ContainsKey(host))
            {
                EnsureTokenFresh(host);
                return host;
            }

            await ConnectAsync(host, ct);
            return host;
        }
        finally
        {
            _authLock.Release();
        }
    }

    /// <summary>
    /// Ensures host is configured (for unauthenticated endpoints like Notification service).
    /// </summary>
    public async Task<string> EnsureHostConfiguredAsync(McpServer server, string host, CancellationToken ct)
    {
        host = ResolveConfiguredHost(host);
        if (!string.IsNullOrWhiteSpace(host))
            return host;

        await _authLock.WaitAsync(ct);
        try
        {
            host = ResolveConfiguredHost(host);
            return !string.IsNullOrWhiteSpace(host) ? host : await ElicitHostAsync(server, ct);
        }
        finally
        {
            _authLock.Release();
        }
    }

    /// <summary>
    /// Invokes a Safeguard API method via the SDK when possible.
    /// PATCH requests and unauthenticated Notification requests fall back to direct HTTP.
    /// </summary>
    public async Task<FullResponse> InvokeAsync(
        string host, Service service, string method, string relativeUrl,
        string body = null, IDictionary<string, string> parameters = null, CancellationToken ct = default)
    {
        method = NormalizeMethod(method);

        if (!_connections.TryGetValue(host, out var connection))
        {
            if (service == Service.Notification)
                return await InvokeRawAsync(host, service, method, relativeUrl, body, parameters, bearerToken: null, ct);

            throw new McpException($"Not connected to '{host}'. Call Safeguard_Connect first.");
        }

        EnsureTokenFresh(host);

        if (method == "PATCH")
            return await InvokeWithRefreshAsync(host, connection, service, method, relativeUrl, body, parameters, ct);

        try
        {
            return await Task.Run(() =>
                connection.InvokeMethodFull(service, ParseSdkMethod(method), relativeUrl, body, parameters, null, null), ct);
        }
        catch (SafeguardDotNetException ex)
        {
            if (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                try
                {
                    await Task.Run(connection.RefreshAccessToken, ct);
                    return await Task.Run(() =>
                        connection.InvokeMethodFull(service, ParseSdkMethod(method), relativeUrl, body, parameters, null, null), ct);
                }
                catch
                {
                    RemoveConnection(host);
                    throw new McpException(
                        $"Authentication expired for '{host}'. Call Safeguard_Connect to re-authenticate.");
                }
            }

            throw new McpException(
                $"Safeguard API error (HTTP {(int?)ex.HttpStatusCode ?? 0}): {ex.Message}");
        }
    }

    /// <summary>
    /// Invokes a Safeguard API method and returns CSV format.
    /// </summary>
    public async Task<string> InvokeCsvAsync(
        string host, Service service, Method method, string relativeUrl,
        IDictionary<string, string> parameters = null, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(host, out var connection))
            throw new McpException($"Not connected to '{host}'. Call Safeguard_Connect first.");

        EnsureTokenFresh(host);

        try
        {
            return await Task.Run(() =>
                connection.InvokeMethodCsv(service, method, relativeUrl, null, parameters, null, null), ct);
        }
        catch (SafeguardDotNetException ex)
        {
            throw new McpException(
                $"Safeguard API error (HTTP {(int?)ex.HttpStatusCode ?? 0}): {ex.Message}");
        }
    }

    /// <summary>Gets the token lifetime remaining for a host, or -1 if not connected.</summary>
    public int GetTokenLifetimeMinutes(string host)
    {
        if (_connections.TryGetValue(host, out var connection))
        {
            try { return connection.GetAccessTokenLifetimeRemaining(); }
            catch { return -1; }
        }

        return -1;
    }

    /// <summary>Gets the connection status summary for all hosts.</summary>
    public string GetStatusSummary()
    {
        CleanupExpired();

        if (_connections.IsEmpty)
            return "No active connections. Call Safeguard_Connect to authenticate.";

        var lines = new List<string>();
        foreach (var kvp in _connections.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            var minutes = GetTokenLifetimeMinutes(kvp.Key);
            lines.Add($"  {kvp.Key}: token expires in {minutes} minutes");
        }

        return "Active connections:\n" + string.Join("\n", lines);
    }

    public void Dispose()
    {
        foreach (var kvp in _connections)
        {
            try { kvp.Value.LogOut(); } catch { }
            try { kvp.Value.Dispose(); } catch { }
        }

        _connections.Clear();
        _authLock.Dispose();
    }

    private async Task ConnectAsync(string host, CancellationToken ct)
    {
        var ignoreSsl = ResolveSslPolicy();
        ISafeguardConnection connection;

        var envProvider = Environment.GetEnvironmentVariable("SAFEGUARD_PROVIDER");
        var envUser = Environment.GetEnvironmentVariable("SAFEGUARD_USER");
        var envPassword = Environment.GetEnvironmentVariable("SAFEGUARD_PASSWORD");

        if (!string.IsNullOrEmpty(envProvider) && !string.IsNullOrEmpty(envUser) && !string.IsNullOrEmpty(envPassword))
        {
            _logger.LogInformation("Using non-interactive PKCE authentication for '{Host}'.", host);
            connection = await Task.Run(() =>
            {
                using var securePassword = ToSecureString(envPassword);
                return PkceLogin.Connect(host, envProvider, envUser, securePassword, ignoreSsl: ignoreSsl);
            }, ct);
        }
        else
        {
            _logger.LogInformation("Using interactive browser login for '{Host}'.", host);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(120));

            try
            {
                connection = await Task.Run(() =>
                    BrowserLogin.Connect(host, ignoreSsl: ignoreSsl), cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new McpException(
                    "Browser login timed out after 120 seconds. Please try again and complete the login promptly.");
            }
        }

        _connections[host] = connection;
        _logger.LogInformation(
            "Successfully connected to '{Host}'. Token expires in {Minutes} minutes.",
            host, connection.GetAccessTokenLifetimeRemaining());

        _ = Task.Run(() => _catalogProvider.LoadCatalogForHostAsync(host), CancellationToken.None);
    }

    private bool ResolveSslPolicy()
    {
        var envVal = Environment.GetEnvironmentVariable("SAFEGUARD_IGNORE_SSL");
        if (bool.TryParse(envVal, out var ignore))
            return ignore;

        return _configuration.GetValue<bool>("Safeguard:IgnoreSsl");
    }

    private void EnsureTokenFresh(string host)
    {
        if (_connections.TryGetValue(host, out var connection))
        {
            try
            {
                var minutesLeft = connection.GetAccessTokenLifetimeRemaining();
                if (minutesLeft < 5)
                {
                    _logger.LogInformation("Token for '{Host}' expires in {Minutes} min — refreshing.", host, minutesLeft);
                    connection.RefreshAccessToken();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check/refresh token for '{Host}'.", host);
            }
        }
    }

    private void CleanupExpired()
    {
        foreach (var kvp in _connections)
        {
            try
            {
                var minutes = kvp.Value.GetAccessTokenLifetimeRemaining();
                if (minutes <= 0)
                {
                    _logger.LogInformation("Removing expired connection to '{Host}'.", kvp.Key);
                    RemoveConnection(kvp.Key, logOut: false);
                }
            }
            catch
            {
                RemoveConnection(kvp.Key, logOut: false);
            }
        }
    }

    private async Task<string> ElicitHostAsync(McpServer server, CancellationToken ct)
    {
        _logger.LogInformation("Eliciting Safeguard server address from user...");

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
        }, ct);

        var isAccepted = result.Action != null
            && result.Action.StartsWith("accept", StringComparison.OrdinalIgnoreCase);

        if (!isAccepted || result.Content == null
            || !result.Content.TryGetValue("host", out var hostElement))
        {
            throw new McpException("Safeguard server address is required to proceed.");
        }

        var resolvedHost = hostElement.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(resolvedHost))
            throw new McpException("Safeguard server address is required to proceed.");

        _logger.LogInformation("Host configured: '{Host}'", resolvedHost);
        return resolvedHost;
    }

    private static string NormalizeMethod(string method) => method.Trim().ToUpperInvariant() switch
    {
        "GET" => "GET",
        "POST" => "POST",
        "PUT" => "PUT",
        "PATCH" => "PATCH",
        "DELETE" => "DELETE",
        _ => throw new McpException($"Unsupported HTTP method: '{method}'. Use GET, POST, PUT, PATCH, or DELETE.")
    };

    private static Method ParseSdkMethod(string method) => method switch
    {
        "GET" => Method.Get,
        "POST" => Method.Post,
        "PUT" => Method.Put,
        "DELETE" => Method.Delete,
        _ => throw new McpException($"HTTP method '{method}' is not supported by the Safeguard SDK.")
    };

    private async Task<FullResponse> InvokeWithRefreshAsync(
        string host,
        ISafeguardConnection connection,
        Service service,
        string method,
        string relativeUrl,
        string body,
        IDictionary<string, string> parameters,
        CancellationToken ct)
    {
        try
        {
            return await InvokeRawAsync(host, service, method, relativeUrl, body, parameters, GetBearerToken(connection), ct);
        }
        catch (RawRequestUnauthorizedException)
        {
            try
            {
                await Task.Run(connection.RefreshAccessToken, ct);
                return await InvokeRawAsync(host, service, method, relativeUrl, body, parameters, GetBearerToken(connection), ct);
            }
            catch (RawRequestUnauthorizedException)
            {
                RemoveConnection(host);
                throw new McpException($"Authentication expired for '{host}'. Call Safeguard_Connect to re-authenticate.");
            }
        }
    }

    private async Task<FullResponse> InvokeRawAsync(
        string host,
        Service service,
        string method,
        string relativeUrl,
        string body,
        IDictionary<string, string> parameters,
        string bearerToken,
        CancellationToken ct)
    {
        using var handler = new HttpClientHandler();
        if (ResolveSslPolicy())
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(new HttpMethod(method), BuildUrl(host, service, relativeUrl, parameters));

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        if (body != null)
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, ct);
        var responseBody = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new RawRequestUnauthorizedException();

        if (!response.IsSuccessStatusCode)
        {
            var message = string.IsNullOrWhiteSpace(responseBody) ? response.ReasonPhrase : responseBody;
            throw new McpException($"Safeguard API error (HTTP {(int)response.StatusCode}): {message}");
        }

        var headers = response.Headers
            .Concat(response.Content == null
                ? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()
                : response.Content.Headers)
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value), StringComparer.OrdinalIgnoreCase);

        return new FullResponse
        {
            StatusCode = response.StatusCode,
            Headers = headers,
            Body = responseBody
        };
    }

    private static string BuildUrl(string host, Service service, string relativeUrl, IDictionary<string, string> parameters)
    {
        var path = relativeUrl.TrimStart('/');
        var url = $"https://{host}/{GetServicePath(service)}/{path}";
        if (parameters == null || parameters.Count == 0)
            return url;

        var query = string.Join("&", parameters.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}"));
        return string.IsNullOrEmpty(query) ? url : $"{url}?{query}";
    }

    private static string GetServicePath(Service service) => service switch
    {
        Service.Appliance => "service/Appliance",
        Service.Core => "service/Core",
        Service.Notification => "service/Notification",
        Service.A2A => "service/A2A",
        Service.Management => "service/Management",
        _ => throw new McpException($"Unsupported Safeguard service: {service}.")
    };

    private static string GetBearerToken(ISafeguardConnection connection)
    {
        using var token = connection.GetAccessToken();
        return new NetworkCredential(string.Empty, token).Password;
    }

    private string ResolveConfiguredHost(string host)
    {
        if (!string.IsNullOrWhiteSpace(host))
            return host.Trim();

        var envHost = Environment.GetEnvironmentVariable("SAFEGUARD_HOST");
        return string.IsNullOrWhiteSpace(envHost) ? ResolveHost(null) : envHost.Trim();
    }

    private void RemoveConnection(string host, bool logOut = true)
    {
        if (_connections.TryRemove(host, out var connection))
        {
            try
            {
                if (logOut)
                    connection.LogOut();
            }
            catch { }

            try { connection.Dispose(); } catch { }
        }

        _catalogProvider.RemoveCatalog(host);
    }

    private static SecureString ToSecureString(string str)
    {
        var secure = new SecureString();
        foreach (var c in str)
            secure.AppendChar(c);
        secure.MakeReadOnly();
        return secure;
    }

    private sealed class RawRequestUnauthorizedException : Exception;
}
