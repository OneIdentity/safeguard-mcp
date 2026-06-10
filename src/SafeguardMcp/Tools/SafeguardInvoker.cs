using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ModelContextProtocol;
using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.Tools;

/// <summary>
/// Static helpers that execute Safeguard API calls through an
/// <see cref="ISafeguardSession"/>. Kept separate from the session so
/// the session abstraction itself stays narrow (it only owns the
/// connection lifetime); HTTP-verb policy, PATCH-via-raw, and the
/// unauthenticated Notification path live here.
/// </summary>
internal static class SafeguardInvoker
{
    public static string NormalizeMethod(string method) => method.Trim().ToUpperInvariant() switch
    {
        "GET" => "GET",
        "POST" => "POST",
        "PUT" => "PUT",
        "PATCH" => "PATCH",
        "DELETE" => "DELETE",
        _ => throw new McpException($"Unsupported HTTP method: '{method}'. Use GET, POST, PUT, PATCH, or DELETE.")
    };

    public static Method ParseSdkMethod(string method) => method switch
    {
        "GET" => Method.Get,
        "POST" => Method.Post,
        "PUT" => Method.Put,
        "DELETE" => Method.Delete,
        _ => throw new McpException($"HTTP method '{method}' is not supported by the Safeguard SDK.")
    };

    /// <summary>
    /// Authenticated call routed through the session. PATCH is sent via
    /// raw <see cref="HttpClient"/> because the SDK doesn't expose it;
    /// the bearer for the raw request is read from the session's
    /// connection. Notification calls without a session are handled by
    /// <see cref="InvokeUnauthenticatedAsync"/>.
    /// </summary>
    public static async Task<FullResponse> InvokeAsync(
        ISafeguardSession session,
        Service service,
        string method,
        string relativeUrl,
        string body,
        IDictionary<string, string> parameters,
        CancellationToken ct)
    {
        method = NormalizeMethod(method);

        if (method == "PATCH")
        {
            return await session.ExecuteWithConnectionAsync(async connection =>
            {
                var bearer = GetBearerToken(connection);
                return await InvokeRawAsync(
                    session.Host, session.IgnoreSsl, service, method, relativeUrl,
                    body, parameters, bearer, ct);
            }, ct);
        }

        try
        {
            return await session.ExecuteWithConnectionAsync(connection =>
                Task.Run(() => connection.InvokeMethodFull(service, ParseSdkMethod(method), relativeUrl, body, parameters, null, null), ct),
                ct);
        }
        catch (SafeguardDotNetException ex)
        {
            throw new McpException(
                $"Safeguard API error (HTTP {(int?)ex.HttpStatusCode ?? 0}): {ex.Message}");
        }
    }

    public static async Task<string> InvokeCsvAsync(
        ISafeguardSession session,
        Service service,
        string relativeUrl,
        IDictionary<string, string> parameters,
        CancellationToken ct)
    {
        try
        {
            return await session.ExecuteWithConnectionAsync(connection =>
                Task.Run(() => connection.InvokeMethodCsv(service, Method.Get, relativeUrl, null, parameters, null, null), ct),
                ct);
        }
        catch (SafeguardDotNetException ex)
        {
            throw new McpException(
                $"Safeguard API error (HTTP {(int?)ex.HttpStatusCode ?? 0}): {ex.Message}");
        }
    }

    /// <summary>
    /// Unauthenticated raw call (used for anonymous Notification
    /// endpoints). Does not touch the session's connection.
    /// </summary>
    public static Task<FullResponse> InvokeUnauthenticatedAsync(
        string host,
        bool ignoreSsl,
        Service service,
        string method,
        string relativeUrl,
        string body,
        IDictionary<string, string> parameters,
        CancellationToken ct)
        => InvokeRawAsync(host, ignoreSsl, service, NormalizeMethod(method), relativeUrl,
            body, parameters, bearerToken: null, ct);

    private static async Task<FullResponse> InvokeRawAsync(
        string host,
        bool ignoreSsl,
        Service service,
        string method,
        string relativeUrl,
        string body,
        IDictionary<string, string> parameters,
        string bearerToken,
        CancellationToken ct)
    {
        using var handler = new HttpClientHandler();
        if (ignoreSsl)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(new HttpMethod(method), BuildUrl(host, service, relativeUrl, parameters));

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        if (body != null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, ct);
        var responseBody = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new McpException(
                "Safeguard token has expired or been revoked. Re-acquire "
                + "(`safeguard-mcp login` or your MCP client's OAuth flow) and retry.");
        }

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
        // NOTE: GetAccessToken() hands back the SDK's internal bearer
        // SecureString by reference. Do NOT wrap in `using` — disposing
        // it zeroes the SDK's own copy and forces a re-login on the
        // next call.
        var token = connection.GetAccessToken();
        return SecureStringExtensions.ReadInsecure(token) ?? string.Empty;
    }
}
