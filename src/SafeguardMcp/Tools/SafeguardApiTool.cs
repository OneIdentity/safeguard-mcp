using System.ComponentModel;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tools;

[McpServerToolType]
public class SafeguardApiTool(SafeguardConnectionManager connectionManager, CatalogProvider catalogProvider)
{
    [McpServerTool(Name = "Safeguard_Connect", Title = "Connect to Safeguard",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Connect and authenticate to a Safeguard appliance. "
        + "You will be prompted for the server DNS name or IP address, then a browser window will open for OAuth2 login. "
        + "Call this once per server. You can connect to multiple servers simultaneously for cross-server operations. "
        + "Call without parameters to check the current connection status.")]
    public async Task<string> Safeguard_Connect(McpServer server,
        [Description("DNS name or IP address of the Safeguard appliance to connect to. If omitted, you will be prompted.")] string host = null)
    {
        if (string.IsNullOrWhiteSpace(host) && connectionManager.ConnectedHosts.Count > 0)
            return connectionManager.GetStatusSummary();

        var resolvedHost = await connectionManager.EnsureAuthenticatedAsync(server, host, CancellationToken.None);
        var allHosts = connectionManager.ConnectedHosts;
        var minutes = connectionManager.GetTokenLifetimeMinutes(resolvedHost);
        var msg = $"Connected and authenticated to Safeguard appliance at {resolvedHost}. Token expires in {minutes} minutes.";
        if (allHosts.Count > 1)
            msg += $"\nActive connections: {string.Join(", ", allHosts)}.";
        return msg;
    }

    [McpServerTool(Name = "Safeguard_Discover", Title = "Discover Safeguard API Endpoints",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Search the Safeguard API catalog to find available endpoints. "
        + "Returns matching endpoints with their HTTP method, path, summary, query parameters, and whether they accept a request body. "
        + "Use this to find the right endpoint before calling Safeguard_Execute.")]
    public string Safeguard_Discover(
        [Description("Filter by service: 'Appliance', 'Core', or 'Notification'. Omit to search all services.")] string service = null,
        [Description("Text to search for in endpoint paths and summaries (case-insensitive).")] string search = null,
        [Description("Filter by HTTP method: GET, POST, PUT, PATCH, or DELETE.")] string method = null)
    {
        string host = null;
        try
        {
            host = connectionManager.ResolveHost(null);
        }
        catch (McpException)
        {
        }

        var results = catalogProvider.GetEndpoints(host);
        var sb = new StringBuilder();
        int matched = 0;
        const int limit = 80;

        for (int i = 0; i < results.Length; i++)
        {
            ref readonly var ep = ref results[i];

            if (service != null && !ep.Service.Equals(service, StringComparison.OrdinalIgnoreCase))
                continue;
            if (method != null && !ep.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                continue;
            if (search != null
                && !ep.Path.Contains(search, StringComparison.OrdinalIgnoreCase)
                && !ep.Summary.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            matched++;
            if (matched <= limit)
            {
                sb.Append(ep.Method.PadRight(7));
                sb.Append(ep.Service.PadRight(13));
                sb.Append(ep.Path);
                if (ep.HasBody) sb.Append("  [body]");
                if (!string.IsNullOrEmpty(ep.Params)) sb.Append("  params: ").Append(ep.Params);
                sb.Append("  -- ").AppendLine(ep.Summary);
            }
        }

        if (matched == 0)
            return "No endpoints matched the search criteria. Try broader search terms.";

        if (matched > limit)
            sb.AppendLine().Append("... and ").Append(matched - limit).Append(" more. Narrow your search with service, method, or more specific search text.");

        return sb.ToString();
    }

    [McpServerTool(Name = "Safeguard_Appliance", Title = "Safeguard Appliance API",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Execute any Safeguard Appliance API endpoint. Authentication is handled automatically. "
        + "Use Safeguard_Discover to find available endpoints. "
        + "Covers: " + SafeguardCatalog.ApplianceSegments)]
    public async Task<string> Safeguard_Appliance(McpServer server,
        [Description("HTTP method: GET, POST, PUT, PATCH, or DELETE.")] string method,
        [Description("API path starting with /v4/ (e.g. '/v4/ApplianceStatus'). Substitute any path parameters.")] string path,
        [Description("Query parameters without leading '?' (e.g. 'fields=Id,Name&filter=Name eq \"x\"'). Omit if none.")] string query = null,
        [Description("JSON request body for POST/PUT/PATCH. Omit for GET/DELETE.")] string body = null,
        [Description("Target server. Required when multiple servers are connected.")] string host = null)
        => await DispatchAsync(server, Service.Appliance, method, path, query, body, host);

    [McpServerTool(Name = "Safeguard_Core", Title = "Safeguard Core API",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Execute any Safeguard Core API endpoint. Authentication is handled automatically. "
        + "Use Safeguard_Discover to find available endpoints. "
        + "Covers: " + SafeguardCatalog.CoreSegments)]
    public async Task<string> Safeguard_Core(McpServer server,
        [Description("HTTP method: GET, POST, PUT, PATCH, or DELETE.")] string method,
        [Description("API path starting with /v4/ (e.g. '/v4/Users'). Substitute any path parameters.")] string path,
        [Description("Query parameters without leading '?' (e.g. 'fields=Id,Name&filter=Name eq \"x\"'). Omit if none.")] string query = null,
        [Description("JSON request body for POST/PUT/PATCH. Omit for GET/DELETE.")] string body = null,
        [Description("Target server. Required when multiple servers are connected.")] string host = null)
        => await DispatchAsync(server, Service.Core, method, path, query, body, host);

    [McpServerTool(Name = "Safeguard_Notification", Title = "Safeguard Notification API",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Execute any Safeguard Notification API endpoint (no authentication required). "
        + "Use Safeguard_Discover to find available endpoints. "
        + "Covers: " + SafeguardCatalog.NotificationSegments)]
    public async Task<string> Safeguard_Notification(McpServer server,
        [Description("HTTP method: GET.")] string method,
        [Description("API path starting with /v4/ (e.g. '/v4/Status').")] string path,
        [Description("Query parameters without leading '?'. Omit if none.")] string query = null,
        [Description("Target server. Required when multiple servers are connected.")] string host = null)
        => await DispatchNotificationAsync(server, method, path, query, host);

    private async Task<string> DispatchAsync(
        McpServer server, Service service, string method, string path,
        string query, string body, string host)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new McpException("The 'method' parameter is required (GET, POST, PUT, PATCH, or DELETE).");
        if (string.IsNullOrWhiteSpace(path))
            throw new McpException("The 'path' parameter is required (e.g. '/v4/Users').");

        var normalizedMethod = ParseMethod(method);
        var relativeUrl = ToSdkRelativeUrl(path);
        var parameters = ParseQueryParameters(query);

        var resolvedHost = await connectionManager.EnsureAuthenticatedAsync(server, host, CancellationToken.None);
        var response = await connectionManager.InvokeAsync(resolvedHost, service, normalizedMethod, relativeUrl, body, parameters);

        return response.Body ?? string.Empty;
    }

    private async Task<string> DispatchNotificationAsync(
        McpServer server, string method, string path, string query, string host)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new McpException("The 'method' parameter is required.");
        if (string.IsNullOrWhiteSpace(path))
            throw new McpException("The 'path' parameter is required.");

        var normalizedMethod = ParseMethod(method);
        var relativeUrl = ToSdkRelativeUrl(path);
        var parameters = ParseQueryParameters(query);

        var resolvedHost = await connectionManager.EnsureHostConfiguredAsync(server, host, CancellationToken.None);
        var response = await connectionManager.InvokeAsync(resolvedHost, Service.Notification, normalizedMethod, relativeUrl, null, parameters);
        return response.Body ?? string.Empty;
    }

    private static string ParseMethod(string method) => method.Trim().ToUpperInvariant() switch
    {
        "GET" => "GET",
        "POST" => "POST",
        "PUT" => "PUT",
        "PATCH" => "PATCH",
        "DELETE" => "DELETE",
        _ => throw new McpException($"Unsupported HTTP method: '{method}'. Use GET, POST, PUT, PATCH, or DELETE.")
    };

    private static string ToSdkRelativeUrl(string path)
    {
        var trimmed = path.TrimStart('/');
        if (trimmed.StartsWith("v4/", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[3..];
        else if (trimmed.StartsWith("v3/", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[3..];
        return trimmed;
    }

    private static IDictionary<string, string> ParseQueryParameters(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIdx = pair.IndexOf('=');
            if (eqIdx > 0)
            {
                var key = Uri.UnescapeDataString(pair[..eqIdx]);
                var value = Uri.UnescapeDataString(pair[(eqIdx + 1)..]);
                parameters[key] = value;
            }
        }

        return parameters.Count > 0 ? parameters : null;
    }
}
