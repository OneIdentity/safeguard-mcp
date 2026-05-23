using System.ComponentModel;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

[McpServerToolType]
public class SafeguardApiTool(SafeguardAuth auth)
{
    // ─── Connect ───────────────────────────────────────────────────────

    [McpServerTool(Name = "Safeguard_Connect", Title = "Connect to Safeguard",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Connect and authenticate to a Safeguard appliance. "
        + "You will be prompted for the server DNS name or IP address, then a browser window will open for OAuth2 login. "
        + "Call this once per server. You can connect to multiple servers simultaneously for cross-server operations (e.g. copying an asset between servers). "
        + "Call without parameters to check the current connection status.")]
    public async Task<string> Safeguard_Connect(McpServer server,
        [Description("DNS name or IP address of the Safeguard appliance to connect to. If omitted, you will be prompted.")] string host = null)
    {
        var resolvedHost = await auth.EnsureAuthenticatedAsync(server, host, CancellationToken.None);
        var allHosts = auth.ConnectedHosts;
        var msg = $"Connected and authenticated to Safeguard appliance at {resolvedHost}.";
        if (allHosts.Count > 1)
            msg += $" Active connections: {string.Join(", ", allHosts)}.";
        return msg;
    }

    // ─── Discover ──────────────────────────────────────────────────────

    [McpServerTool(Name = "Safeguard_Discover", Title = "Discover Safeguard API Endpoints",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Search the Safeguard API catalog to find available endpoints. "
        + "Returns matching endpoints with their HTTP method, path, summary, query parameters, and whether they accept a request body. "
        + "Use this to find the right endpoint before calling one of the service dispatcher tools.")]
    public string Safeguard_Discover(
        [Description("Filter by service: 'Appliance', 'Core', or 'Notification'. Omit to search all services.")] string service = null,
        [Description("Text to search for in endpoint paths and summaries (case-insensitive).")] string search = null,
        [Description("Filter by HTTP method: GET, POST, PUT, or DELETE.")] string method = null)
    {
        var results = SafeguardCatalog.Endpoints.AsSpan();
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

    // ─── Appliance Service Dispatcher ──────────────────────────────────

    [McpServerTool(Name = "Safeguard_Appliance", Title = "Safeguard Appliance API",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Execute any Safeguard Appliance API endpoint. Authentication is handled automatically (or use Safeguard_Connect first). "
        + "Use Safeguard_Discover to find available endpoints. "
        + "Covers: " + SafeguardCatalog.ApplianceSegments)]
    public async Task<string> Safeguard_Appliance(McpServer server,
        [Description("HTTP method: GET, POST, PUT, or DELETE.")] string method,
        [Description("API path starting with /v4/ (e.g. '/v4/ApplianceStatus'). Substitute any path parameters.")] string path,
        [Description("Query parameters without leading '?' (e.g. 'fields=Id,Name&filter=Name eq \"x\"'). Omit if none.")] string query = null,
        [Description("JSON request body for POST/PUT. Omit for GET/DELETE.")] string body = null,
        [Description("DNS name or IP address of the target Safeguard server. Required when multiple servers are connected.")] string host = null)
        => await DispatchAsync(server, "service/Appliance", method, path, query, body, host, requiresAuth: true);

    // ─── Core Service Dispatcher ───────────────────────────────────────

    [McpServerTool(Name = "Safeguard_Core", Title = "Safeguard Core API",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Execute any Safeguard Core API endpoint. Authentication is handled automatically (or use Safeguard_Connect first). "
        + "Use Safeguard_Discover to find available endpoints. "
        + "Covers: " + SafeguardCatalog.CoreSegments)]
    public async Task<string> Safeguard_Core(McpServer server,
        [Description("HTTP method: GET, POST, PUT, or DELETE.")] string method,
        [Description("API path starting with /v4/ (e.g. '/v4/Users' or '/v4/Users/123'). Substitute any path parameters.")] string path,
        [Description("Query parameters without leading '?' (e.g. 'fields=Id,Name&filter=Name eq \"x\"'). Omit if none.")] string query = null,
        [Description("JSON request body for POST/PUT. Omit for GET/DELETE.")] string body = null,
        [Description("DNS name or IP address of the target Safeguard server. Required when multiple servers are connected.")] string host = null)
        => await DispatchAsync(server, "service/Core", method, path, query, body, host, requiresAuth: true);

    // ─── Notification Service Dispatcher ───────────────────────────────

    [McpServerTool(Name = "Safeguard_Notification", Title = "Safeguard Notification API",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Execute any Safeguard Notification API endpoint (no authentication required, but server address is needed). "
        + "Use Safeguard_Discover to find available endpoints. "
        + "Covers: " + SafeguardCatalog.NotificationSegments)]
    public async Task<string> Safeguard_Notification(McpServer server,
        [Description("HTTP method: GET.")] string method,
        [Description("API path starting with /v4/ (e.g. '/v4/Status').")] string path,
        [Description("Query parameters without leading '?'. Omit if none.")] string query = null,
        [Description("DNS name or IP address of the target Safeguard server. Required when multiple servers are connected.")] string host = null)
        => await DispatchAsync(server, "service/Notification", method, path, query, body: null, host, requiresAuth: false);

    // ─── Shared Dispatcher ─────────────────────────────────────────────

    private async Task<string> DispatchAsync(
        McpServer server, string servicePath, string method, string path,
        string query, string body, string host, bool requiresAuth)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new McpException("The 'method' parameter is required (GET, POST, PUT, or DELETE).");
        if (string.IsNullOrWhiteSpace(path))
            throw new McpException("The 'path' parameter is required (e.g. '/v4/Users').");

        var httpMethod = method.Trim().ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            _ => throw new McpException($"Unsupported HTTP method: '{method}'. Use GET, POST, PUT, or DELETE.")
        };

        var fullPath = string.IsNullOrWhiteSpace(query) ? path : path + "?" + query;

        // Resolve the target host (auto-selects when only one connection exists)
        var resolvedHost = auth.ResolveHost(host);

        if (requiresAuth)
            resolvedHost = await auth.EnsureAuthenticatedAsync(server, resolvedHost, CancellationToken.None);
        else
            resolvedHost = await auth.EnsureHostConfiguredAsync(server, resolvedHost, CancellationToken.None);

        return await auth.RequestAsync(resolvedHost, httpMethod, auth.BuildUrl(resolvedHost, servicePath, fullPath), body);
    }
}
