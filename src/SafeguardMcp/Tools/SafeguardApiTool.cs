using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tools;

[McpServerToolType]
public class SafeguardApiTool(
    SafeguardConnectionManager connectionManager,
    CatalogProvider catalogProvider,
    IConfiguration configuration)
{
    private int MaxResultsBeforeTruncation => configuration.GetValue("Safeguard:MaxResultsBeforeTruncation", 100);
    private int MaxResponseChars => configuration.GetValue("Safeguard:MaxResponseChars", 30000);
    private int DefaultLimit => configuration.GetValue("Safeguard:DefaultLimit", 50);
    private bool AutoInjectLimit => configuration.GetValue("Safeguard:AutoInjectLimit", true);

    [McpServerTool(Name = "Safeguard_Connect", Title = "Connect to Safeguard",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Connect and authenticate to a Safeguard appliance. "
        + "You will be prompted for the server DNS name or IP address, then a browser window will open for OAuth2 login. "
        + "Call this once per server. You can connect to multiple servers simultaneously for cross-server operations. "
        + "Call without parameters to check the current connection status. "
        + "IMPORTANT: Only set ignoreSsl to true after the user has explicitly confirmed they want to skip certificate validation.")]
    public async Task<string> Safeguard_Connect(McpServer server,
        [Description("DNS name or IP address of the Safeguard appliance to connect to. If omitted, you will be prompted.")] string host = null,
        [Description("Skip SSL certificate validation for this connection. Only use when the user has explicitly confirmed — never set this silently.")] bool? ignoreSsl = null)
    {
        if (string.IsNullOrWhiteSpace(host) && ignoreSsl == null && connectionManager.ConnectedHosts.Count > 0)
            return connectionManager.GetStatusSummary();

        var resolvedHost = await connectionManager.EnsureAuthenticatedAsync(server, host, CancellationToken.None, ignoreSsl);
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
        const int limit = 80;
        var searchTerms = TerminologyMap.ExpandSearchTerms(search);

        // Collect matching endpoints with relevance scores so that exact path-segment
        // matches appear before substring/summary-only matches.
        var matches = new List<(int Score, int Index)>();

        for (int i = 0; i < results.Length; i++)
        {
            ref readonly var ep = ref results[i];

            if (service != null && !ep.Service.Equals(service, StringComparison.OrdinalIgnoreCase))
                continue;
            if (method != null && !ep.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                continue;

            int score = ScoreMatch(searchTerms, ep.Path, ep.Summary);
            if (search != null && score == 0)
                continue;

            matches.Add((score, i));
        }

        if (matches.Count == 0)
            return "No endpoints matched the search criteria. Try broader search terms.\nTip: Use Safeguard_Schema to see request/response body format for POST/PUT endpoints.";

        // Sort by descending relevance; ties keep catalog order (stable sort).
        matches.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : a.Index.CompareTo(b.Index));

        int shown = 0;
        foreach (var (_, idx) in matches)
        {
            if (shown >= limit) break;
            ref readonly var ep = ref results[idx];
            sb.Append(ep.Method.PadRight(7));
            sb.Append(ep.Service.PadRight(13));
            sb.Append(ep.Path);
            if (ep.HasBody) sb.Append("  [body]");
            if (!string.IsNullOrEmpty(ep.Params)) sb.Append("  params: ").Append(ep.Params);
            sb.Append("  -- ").AppendLine(ep.Summary);
            shown++;
        }

        if (matches.Count > limit)
            sb.AppendLine().Append("... and ").Append(matches.Count - limit).Append(" more. Narrow your search with service, method, or more specific search text.");

        sb.AppendLine();
        sb.Append("Tip: Use Safeguard_Schema to see request/response body format for POST/PUT endpoints.");
        return sb.ToString();
    }

    [McpServerTool(Name = "Safeguard_Execute", Title = "Execute Safeguard API",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Execute any Safeguard API endpoint. The service (Core, Appliance, Notification) "
        + "is automatically determined from the endpoint path. "
        + "Use Safeguard_Discover to find endpoints, Safeguard_Schema for request body format.")]
    public async Task<string> Safeguard_Execute(McpServer server,
        [Description("HTTP method: GET, POST, PUT, PATCH, or DELETE.")] string method,
        [Description("API path (e.g. '/v4/Users', '/v4/ApplianceStatus/Health'). The correct service is auto-detected from the path.")] string path,
        [Description("Query parameters (e.g. 'fields=Id,Name&filter=Name eq \"x\"'). Omit if none.")] string query = null,
        [Description("JSON request body for POST/PUT/PATCH. Omit for GET/DELETE.")] string body = null,
        [Description("Response format: 'json' (default) or 'csv' (tabular, smaller for large datasets).")]
        string format = "json",
        [Description("Target server (required only with multiple connections).")]
        string host = null)
        => await DispatchAsync(server, method, path, query, body, format, host);

    [McpServerTool(Name = "Safeguard_Schema", Title = "Get API Schema",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get the request body schema and response schema for a Safeguard API endpoint. "
        + "Returns property names, types, required fields, and descriptions. "
        + "Use this before POST or PUT calls to understand the JSON body format.")]
    public string Safeguard_Schema(
        [Description("The API path (e.g. '/v4/AssetAccounts', '/v4/Users').")] string path,
        [Description("HTTP method: POST, PUT, or GET (for response schema). Default: POST")] string method = "POST",
        [Description("Target server for dynamic schema lookup.")] string host = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new McpException("The 'path' parameter is required (e.g. '/v4/AssetAccounts').");

        var normalizedPath = NormalizePath(path);
        var normalizedMethod = ParseMethod(method);
        var catalogHost = TryResolveCatalogHost(host);
        var service = ResolveService(normalizedPath, catalogHost);
        var serviceName = GetServiceName(service);

        var requestSchema = catalogProvider.GetSchema(normalizedMethod, serviceName, normalizedPath, catalogHost);
        var responseSchema = catalogProvider.GetResponseSchema("GET", serviceName, normalizedPath, catalogHost);

        if (requestSchema == null && responseSchema == null)
        {
            return $"No schema is available for {normalizedMethod} {normalizedPath} ({serviceName}). "
                + "Dynamic schemas are available after connecting with Safeguard_Connect. "
                + "Use Safeguard_Discover to verify the endpoint path if needed.";
        }

        var sb = new StringBuilder();
        sb.Append("Endpoint: ").Append(normalizedMethod).Append(' ').Append(normalizedPath)
            .Append(" (").Append(serviceName).AppendLine(")");

        if (requestSchema != null)
            AppendSchemaSection(sb, "REQUEST BODY", requestSchema.Value);
        else
            sb.AppendLine().AppendLine("REQUEST BODY:").AppendLine("  No request schema available.");

        if (responseSchema != null)
            AppendSchemaSection(sb, "RESPONSE BODY (GET)", responseSchema.Value);
        else
            sb.AppendLine().AppendLine("RESPONSE BODY (GET):").AppendLine("  No response schema available.");

        return sb.ToString().TrimEnd();
    }

    [McpServerTool(Name = "Safeguard_QueryHelp", Title = "Safeguard Query Syntax",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get help with Safeguard API query parameters: filter syntax, operators, "
        + "field selection, ordering, and pagination.")]
    public string Safeguard_QueryHelp(
        [Description("Optional endpoint path to show filterable fields for.")] string path = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Safeguard query syntax:")
            .AppendLine("  Filter operators: eq, ne, gt, ge, lt, le, contains, ieq, icontains, sw, isw, ew, iew, in [...], not_in [...]")
            .AppendLine("  String values use single quotes: filter=Name eq 'Admin'")
            .AppendLine("  Combine expressions with and, or, not, and parentheses")
            .AppendLine("  Nested properties: filter=TaskProperties.HasAccountTaskFailure eq true")
            .AppendLine("  Field selection: fields=Id,Name,Description")
            .AppendLine("  Exclude fields: fields=-TaskProperties,-Platform")
            .AppendLine("  Ordering: orderby=Name or orderby=-CreatedDate")
            .AppendLine("  Multiple order fields: orderby=Name,-CreatedDate")
            .AppendLine("  Pagination: page=0&limit=50 (page is 0-indexed)")
            .AppendLine("  Quick search: q=searchterm")
            .AppendLine("  Count only: count=true")
            .AppendLine()
            .AppendLine("Examples:")
            .AppendLine("  fields=Id,Name&filter=Name icontains 'admin'&orderby=Name&page=0&limit=25")
            .AppendLine("  filter=(Disabled eq false) and (Platform.DisplayName eq 'Windows')")
            .AppendLine("  filter=Id in [1,2,3]");

        if (string.IsNullOrWhiteSpace(path))
            return sb.ToString().TrimEnd();

        var normalizedPath = NormalizePath(path);
        var catalogHost = TryResolveCatalogHost(null);
        var service = ResolveService(normalizedPath, catalogHost);
        var serviceName = GetServiceName(service);
        var schema = catalogProvider.GetResponseSchema("GET", serviceName, normalizedPath, catalogHost)
            ?? catalogProvider.GetSchema("GET", serviceName, normalizedPath, catalogHost);

        if (schema != null && schema.Value.Properties.Length > 0)
        {
            var propertyNames = schema.Value.Properties.Select(p => p.Name).ToArray();
            sb.AppendLine()
                .Append("Top-level fields for ").Append(normalizedPath).Append(" (").Append(serviceName).AppendLine("):")
                .Append("  ").Append(string.Join(", ", propertyNames));
        }
        else
        {
            sb.AppendLine()
                .Append("No schema field list is available for ").Append(normalizedPath).Append('.');
        }

        return sb.ToString().TrimEnd();
    }

    private async Task<string> DispatchAsync(
        McpServer server,
        string method,
        string path,
        string query,
        string body,
        string format,
        string host)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new McpException("The 'method' parameter is required (GET, POST, PUT, PATCH, or DELETE).");
        if (string.IsNullOrWhiteSpace(path))
            throw new McpException("The 'path' parameter is required (e.g. '/v4/Users').");

        var normalizedMethod = ParseMethod(method);
        var normalizedPath = NormalizePath(path);
        var requestedFormat = string.IsNullOrWhiteSpace(format) ? "json" : format.Trim().ToLowerInvariant();
        if (requestedFormat != "json" && requestedFormat != "csv")
            throw new McpException("Unsupported response format. Use 'json' or 'csv'.");
        if (requestedFormat == "csv" && normalizedMethod != "GET")
            throw new McpException("CSV format is only supported for GET requests.");

        var catalogHost = TryResolveCatalogHost(host);
        var service = ResolveService(normalizedPath, catalogHost);
        var relativeUrl = ToSdkRelativeUrl(normalizedPath);
        var parameters = ParseQueryParameters(query);
        parameters = MaybeInjectLimit(normalizedMethod, normalizedPath, service, parameters, catalogHost, out var injectedLimit);

        try
        {
            var requiresAuthentication = service != Service.Notification || requestedFormat == "csv";
            var resolvedHost = requiresAuthentication
                ? await connectionManager.EnsureAuthenticatedAsync(server, host, CancellationToken.None)
                : await connectionManager.EnsureHostConfiguredAsync(server, host, CancellationToken.None);

            if (requestedFormat == "csv")
            {
                var csv = await connectionManager.InvokeCsvAsync(
                    resolvedHost,
                    service,
                    Method.Get,
                    relativeUrl,
                    parameters,
                    CancellationToken.None);
                return FormatResponse(csv ?? string.Empty, requestedFormat, injectedLimit);
            }

            var response = await connectionManager.InvokeAsync(
                resolvedHost,
                service,
                normalizedMethod,
                relativeUrl,
                body,
                parameters,
                CancellationToken.None);

            return FormatResponse(response.Body ?? string.Empty, requestedFormat, injectedLimit);
        }
        catch (McpException ex)
        {
            throw new McpException(FormatErrorResponse(ex));
        }
    }

    private string FormatResponse(string body, string format, int injectedLimit)
    {
        var notes = new List<string>();
        var formattedBody = body ?? string.Empty;

        if (injectedLimit > 0)
        {
            notes.Add($"Note: Auto-applied limit={injectedLimit}. Specify 'limit' in the query to override it.");
        }

        if (format == "json" && TryTruncateJsonArray(formattedBody, MaxResultsBeforeTruncation, out var truncatedBody, out var totalItems))
        {
            formattedBody = truncatedBody;
            notes.Add($"Returned {totalItems} items. Showing the first {MaxResultsBeforeTruncation}. Use filter, fields, page, or limit to narrow the result.");
        }

        if (formattedBody.Length > MaxResponseChars)
        {
            formattedBody = formattedBody[..MaxResponseChars];
            notes.Add($"Response truncated to {MaxResponseChars} characters. Use fields, filter, page/limit, or format='csv' to reduce payload size.");
        }

        if (notes.Count == 0)
            return formattedBody;

        return string.Join("\n", notes) + "\n\n" + formattedBody;
    }

    private static bool TryTruncateJsonArray(string body, int maxItems, out string truncatedBody, out int totalItems)
    {
        truncatedBody = null;
        totalItems = 0;

        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return false;

            var items = new List<JsonElement>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                totalItems++;
                if (items.Count < maxItems)
                    items.Add(item.Clone());
            }

            if (totalItems <= maxItems)
                return false;

            truncatedBody = JsonSerializer.Serialize(items);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private string FormatErrorResponse(McpException ex)
    {
        var rawMessage = ex.Message ?? "Safeguard API request failed.";
        var statusCode = ExtractStatusCode(rawMessage);
        if (statusCode == 0 && rawMessage.Contains("Authentication expired", StringComparison.OrdinalIgnoreCase))
            statusCode = 401;

        var errorDetail = ExtractErrorDetail(rawMessage);
        TryParseErrorBody(errorDetail, out var apiMessage, out var apiCode, out var innerError);

        var lines = new List<string>();
        if (statusCode > 0)
            lines.Add($"Safeguard API error (HTTP {statusCode}).");
        else
            lines.Add("Safeguard API error.");

        var displayMessage = !string.IsNullOrWhiteSpace(apiMessage) ? apiMessage : errorDetail;
        if (!string.IsNullOrWhiteSpace(displayMessage))
            lines.Add($"Message: {displayMessage}");
        if (!string.IsNullOrWhiteSpace(apiCode))
            lines.Add($"Code: {apiCode}");
        if (!string.IsNullOrWhiteSpace(innerError))
            lines.Add($"InnerError: {innerError}");

        var hint = GetErrorHint(statusCode, rawMessage);
        if (!string.IsNullOrWhiteSpace(hint))
            lines.Add($"Hint: {hint}");

        return string.Join("\n", lines);
    }

    private static int ExtractStatusCode(string message)
    {
        var marker = "HTTP ";
        var start = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return 0;

        start += marker.Length;
        var end = start;
        while (end < message.Length && char.IsDigit(message[end]))
            end++;

        return int.TryParse(message[start..end], out var statusCode) ? statusCode : 0;
    }

    private static string ExtractErrorDetail(string message)
    {
        var separatorIndex = message.IndexOf("):", StringComparison.Ordinal);
        return separatorIndex >= 0 ? message[(separatorIndex + 2)..].Trim() : message.Trim();
    }

    private static bool TryParseErrorBody(string message, out string apiMessage, out string apiCode, out string innerError)
    {
        apiMessage = null;
        apiCode = null;
        innerError = null;

        if (string.IsNullOrWhiteSpace(message) || !message.TrimStart().StartsWith('{'))
            return false;

        try
        {
            using var document = JsonDocument.Parse(message);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            if (TryGetPropertyIgnoreCase(document.RootElement, "Message", out var messageElement))
                apiMessage = JsonElementToString(messageElement);
            if (TryGetPropertyIgnoreCase(document.RootElement, "Code", out var codeElement))
                apiCode = JsonElementToString(codeElement);
            if (TryGetPropertyIgnoreCase(document.RootElement, "InnerError", out var innerElement))
                innerError = JsonElementToString(innerElement);

            return !string.IsNullOrWhiteSpace(apiMessage)
                || !string.IsNullOrWhiteSpace(apiCode)
                || !string.IsNullOrWhiteSpace(innerError);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string JsonElementToString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Object or JsonValueKind.Array => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => null,
        _ => element.ToString()
    };

    private static string GetErrorHint(int statusCode, string rawMessage) => statusCode switch
    {
        400 => "Check request body format. Use Safeguard_Schema to see required fields.",
        401 => "Token expired. Call Safeguard_Connect to re-authenticate.",
        403 => "Insufficient permissions for this operation.",
        404 => "Resource not found. Verify the ID exists using a GET call.",
        409 => "Conflict. GET the current state first, then retry.",
        422 => "Validation failed. Check property types match the schema.",
        _ when rawMessage.Contains("Authentication expired", StringComparison.OrdinalIgnoreCase)
            => "Token expired. Call Safeguard_Connect to re-authenticate.",
        _ => null
    };

    private IDictionary<string, string> MaybeInjectLimit(
        string method,
        string path,
        Service service,
        IDictionary<string, string> parameters,
        string host,
        out int injectedLimit)
    {
        injectedLimit = 0;

        if (!AutoInjectLimit || !method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            return parameters;
        if (parameters != null && parameters.ContainsKey("limit"))
            return parameters;
        if (!ShouldInjectLimit(path, service, host))
            return parameters;

        var updated = parameters == null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);

        injectedLimit = DefaultLimit;
        updated["limit"] = DefaultLimit.ToString(CultureInfo.InvariantCulture);
        return updated;
    }

    private bool ShouldInjectLimit(string path, Service service, string host)
    {
        var endpoints = catalogProvider.GetEndpoints(host);
        var serviceName = GetServiceName(service);

        for (int i = 0; i < endpoints.Length; i++)
        {
            ref readonly var endpoint = ref endpoints[i];
            if (!endpoint.Service.Equals(serviceName, StringComparison.OrdinalIgnoreCase)
                || !endpoint.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                || !PathsMatch(endpoint.Path, path))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(endpoint.Params)
                && endpoint.Params.Contains("limit", StringComparison.OrdinalIgnoreCase)
                && !EndsWithPlaceholder(endpoint.Path))
            {
                return true;
            }

            return false;
        }

        var segments = GetPathSegments(path);
        if (segments.Length == 0)
            return false;

        var lastSegment = segments[^1];
        return !LooksLikeId(lastSegment);
    }

    private Service ResolveService(string path, string host)
    {
        var endpoints = catalogProvider.GetEndpoints(host);
        for (int i = 0; i < endpoints.Length; i++)
        {
            ref readonly var endpoint = ref endpoints[i];
            if (PathsMatch(endpoint.Path, path))
                return ParseServiceName(endpoint.Service);
        }

        if (path.Contains("ApplianceStatus", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Backup", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Network", StringComparison.OrdinalIgnoreCase)
            || path.Contains("DiagnosticPackage", StringComparison.OrdinalIgnoreCase)
            || path.Contains("Appliance", StringComparison.OrdinalIgnoreCase)
            || (path.Contains("Patch", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("PatchPolicies", StringComparison.OrdinalIgnoreCase)))
        {
            return Service.Appliance;
        }

        if (path.Equals("/v4/Status", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/v4/Status/", StringComparison.OrdinalIgnoreCase))
        {
            return Service.Notification;
        }

        return Service.Core;
    }

    private static Service ParseServiceName(string service) => service.ToUpperInvariant() switch
    {
        "APPLIANCE" => Service.Appliance,
        "CORE" => Service.Core,
        "NOTIFICATION" => Service.Notification,
        _ => Service.Core
    };

    private static string GetServiceName(Service service) => service switch
    {
        Service.Appliance => "Appliance",
        Service.Notification => "Notification",
        _ => "Core"
    };

    private static bool PathsMatch(string catalogPath, string actualPath)
    {
        var templateSegments = GetPathSegments(catalogPath);
        var actualSegments = GetPathSegments(actualPath);
        if (templateSegments.Length != actualSegments.Length)
            return false;

        for (int i = 0; i < templateSegments.Length; i++)
        {
            if (IsPlaceholder(templateSegments[i]))
                continue;
            if (!templateSegments[i].Equals(actualSegments[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static string[] GetPathSegments(string path) => NormalizePath(path)
        .Trim('/')
        .Split('/', StringSplitOptions.RemoveEmptyEntries);

    private static bool EndsWithPlaceholder(string path)
    {
        var segments = GetPathSegments(path);
        return segments.Length > 0 && IsPlaceholder(segments[^1]);
    }

    private static bool IsPlaceholder(string segment) => segment.StartsWith('{') && segment.EndsWith('}');

    private static bool LooksLikeId(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || IsPlaceholder(segment))
            return true;
        if (int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return true;
        if (long.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return true;
        return Guid.TryParse(segment, out _);
    }

    /// <summary>
    /// Scores how well an endpoint matches the search terms.
    /// Higher score = better relevance. Returns 0 if no match.
    /// 3 = a search term matches an entire path segment exactly (e.g. "assets" → /v4/Assets)
    /// 2 = a search term is a substring of the path
    /// 1 = a search term matches only in the summary
    /// </summary>
    private static int ScoreMatch(IReadOnlyList<string> terms, string path, string summary)
    {
        if (terms == null || terms.Count == 0)
            return 1; // No filter means everything matches equally

        int best = 0;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < terms.Count; i++)
        {
            var term = terms[i];
            // Check for exact path-segment match (highest relevance)
            foreach (var seg in segments)
            {
                if (seg.Equals(term, StringComparison.OrdinalIgnoreCase)
                    || seg.Equals($"v4/{term}", StringComparison.OrdinalIgnoreCase))
                {
                    return 3; // Can't do better
                }
            }
            // Check for path substring match
            if (path.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                if (best < 2) best = 2;
            }
            // Check for summary match
            else if (summary.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                if (best < 1) best = 1;
            }
        }
        return best;
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Trim();
        var queryIndex = normalized.IndexOf('?');
        if (queryIndex >= 0)
            normalized = normalized[..queryIndex];
        if (!normalized.StartsWith('/'))
            normalized = "/" + normalized;
        return normalized;
    }

    private string TryResolveCatalogHost(string host)
    {
        if (!string.IsNullOrWhiteSpace(host))
            return host.Trim();

        try
        {
            return connectionManager.ResolveHost(null);
        }
        catch (McpException)
        {
            return null;
        }
    }

    private static void AppendSchemaSection(StringBuilder sb, string heading, ApiSchema schema)
    {
        sb.AppendLine().Append(heading).AppendLine(":");
        if (!string.IsNullOrWhiteSpace(schema.TypeName))
            sb.Append("  Type: ").AppendLine(schema.TypeName);

        var required = schema.Properties
            .Where(p => p.Required)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var optional = schema.Properties
            .Where(p => !p.Required)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        sb.AppendLine("  Required:");
        if (required.Length == 0)
            sb.AppendLine("    None");
        else
            foreach (var property in required)
                AppendSchemaProperty(sb, property);

        sb.AppendLine("  Optional:");
        if (optional.Length == 0)
            sb.AppendLine("    None");
        else
            foreach (var property in optional)
                AppendSchemaProperty(sb, property);

        var hints = SchemaHints.GetHints(schema);
        if (hints != null)
        {
            sb.AppendLine().AppendLine("  AGENT HINTS:");
            foreach (var (propertyName, hint) in hints)
                sb.Append("    ").Append(propertyName).Append(": ").AppendLine(hint);
        }
    }

    private static void AppendSchemaProperty(StringBuilder sb, SchemaProperty property)
    {
        sb.Append("    ").Append(property.Name).Append(" (").Append(property.Type).Append(')');
        if (!string.IsNullOrWhiteSpace(property.Description))
            sb.Append(" - ").Append(property.Description.Trim());
        sb.AppendLine();
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

        query = query.Trim();
        if (query.StartsWith('?'))
            query = query[1..];

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
