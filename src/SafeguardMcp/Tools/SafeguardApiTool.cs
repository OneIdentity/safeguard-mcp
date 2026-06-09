using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tools;

[McpServerToolType]
internal sealed class SafeguardApiTool
{
    private readonly ISafeguardSession session;
    private readonly CatalogProvider catalogProvider;
    private readonly IConfiguration configuration;
    private readonly ILogger<SafeguardApiTool> logger;

    public SafeguardApiTool(
        ISafeguardSession session,
        CatalogProvider catalogProvider,
        IConfiguration configuration,
        ILogger<SafeguardApiTool> logger = null)
    {
        this.session = session;
        this.catalogProvider = catalogProvider;
        this.configuration = configuration;
        this.logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardApiTool>.Instance;
    }

    private int MaxResultsBeforeTruncation => ParseInt(configuration["Safeguard:MaxResultsBeforeTruncation"], 100);
    private int MaxResponseChars => ParseInt(configuration["Safeguard:MaxResponseChars"], 30000);
    private int DefaultLimit => ParseInt(configuration["Safeguard:DefaultLimit"], 50);
    private bool AutoInjectLimit => ParseBool(configuration["Safeguard:AutoInjectLimit"], true);

    private static int ParseInt(string value, int defaultValue)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;

    private static bool ParseBool(string value, bool defaultValue)
        => bool.TryParse(value, out var v) ? v : defaultValue;

    [McpServerTool(Name = "Safeguard_Connect", Title = "Connect to Safeguard",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Authenticate to the configured Safeguard appliance. "
        + "In stdio mode this runs a device-code (or PKCE) login the first time and caches the connection. "
        + "In HTTP mode this is OPTIONAL — every tool already executes against the appliance using the request "
        + "`Authorization: Bearer` header; calling this tool just verifies the bearer and returns the principal "
        + "(display name, identity provider, token expiry).")]
    public async Task<string> Safeguard_Connect(McpServer server, CancellationToken ct = default)
    {
        await session.EnsureReadyAsync(server, ct);
        var info = await session.GetPrincipalInfoAsync(ct);
        return FormatPrincipal(info, "Connected and authenticated");
    }

    [McpServerTool(Name = "Safeguard_Status", Title = "Safeguard Connection Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Report the current Safeguard authentication state. In stdio mode reports the cached "
        + "connection's token lifetime. In HTTP mode inspects the request bearer (decoding the JWT body "
        + "WITHOUT signature verification, display only) and reports the principal and expiry; the "
        + "Safeguard appliance is the authority on token validity.")]
    public async Task<string> Safeguard_Status(McpServer server, CancellationToken ct = default)
    {
        if (!session.HasCredentials)
        {
            return string.IsNullOrWhiteSpace(session.Host)
                ? "No Safeguard appliance is configured. Set SAFEGUARD_HOST and restart the MCP server."
                : "Not authenticated against Safeguard. Acquire a Safeguard user token (e.g., "
                    + "`safeguard-mcp login`) and configure your client to send `Authorization: Bearer <token>`.";
        }

        try
        {
            await session.EnsureReadyAsync(server, ct);
            var info = await session.GetPrincipalInfoAsync(ct);
            return FormatPrincipal(info, "Authenticated")
                + "\nNote: The Safeguard appliance is the authority on token validity; this output is display-only.";
        }
        catch (McpException ex)
        {
            return ex.Message;
        }
    }

    [McpServerTool(Name = "Safeguard_Disconnect", Title = "Disconnect from Safeguard",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Log out of the current Safeguard session. In stdio mode drops the cached connection. "
        + "In HTTP mode posts to /service/core/v4/Token/Logout with the request bearer. "
        + "WARNING: This invalidates the bearer for any client config still holding it; remove the token "
        + "from your MCP client configuration after calling this.")]
    public async Task<string> Safeguard_Disconnect(CancellationToken ct = default)
    {
        if (!session.HasCredentials)
            return "No active Safeguard session to disconnect.";
        try
        {
            await session.LogoutAsync(ct);
            return "Safeguard session ended. The bearer (if any) has been invalidated at the appliance.";
        }
        catch (McpException ex)
        {
            return ex.Message;
        }
    }

    private static string FormatPrincipal(PrincipalInfo info, string verb)
    {
        var sb = new StringBuilder();
        sb.Append(verb).Append(" to Safeguard appliance at ").Append(info.ApplianceHost ?? "<unknown>").Append('.');
        if (!string.IsNullOrWhiteSpace(info.DisplayName) || !string.IsNullOrWhiteSpace(info.Name))
        {
            sb.Append(" Principal: ").Append(info.DisplayName ?? info.Name);
            if (!string.IsNullOrWhiteSpace(info.IdentityProvider))
                sb.Append(" (").Append(info.IdentityProvider).Append(')');
            sb.Append('.');
        }
        if (info.TokenLifetimeMinutes > 0)
            sb.Append(" Token expires in ").Append(info.TokenLifetimeMinutes).Append(" minutes");
        if (info.TokenExpiresAt.HasValue)
            sb.Append(" (at ").Append(info.TokenExpiresAt.Value.ToString("u")).Append(')');
        if (info.TokenLifetimeMinutes > 0 || info.TokenExpiresAt.HasValue)
            sb.Append('.');
        return sb.ToString();
    }

    [McpServerTool(Name = "Safeguard_Discover", Title = "Discover Safeguard API Endpoints",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Search the Safeguard API catalog to find available endpoints. "
        + "Returns matching endpoints with their HTTP method, path, summary, query parameters, and whether they accept a request body. "
        + "When the search matches a workflow recipe, the recipe is listed first so multi-step operations don't get bypassed in favor of a single endpoint. "
        + "Use this to find the right endpoint before calling Safeguard_Execute.")]
    public string Safeguard_Discover(
        [Description("Filter by service: 'Appliance', 'Core', or 'Notification'. Omit to search all services.")] string service = null,
        [Description("Text to search for in endpoint paths and summaries (case-insensitive).")] string search = null,
        [Description("Filter by HTTP method: GET, POST, PUT, PATCH, or DELETE.")] string method = null)
        => FormatDiscovery(catalogProvider.GetEndpoints().ToArray(), service, search, method);

    /// <summary>
    /// Pure formatter for <c>Safeguard_Discover</c>; isolated so unit tests can
    /// exercise the recipe-ranking and synonym-expansion behavior without
    /// having to construct the full DI graph.
    /// </summary>
    internal static string FormatDiscovery(ApiEndpoint[] results, string service, string search, string method)
    {
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

        // Recipe matching only runs when there is a search; an empty search keeps
        // the existing "everything in catalog order" behavior unchanged.
        var recipeMatches = string.IsNullOrWhiteSpace(search)
            ? Array.Empty<RecipeMatch>()
            : RecipeIndex.Score(searchTerms).ToArray();

        // Don't surface description-only recipe hits unless there's at least one
        // strong (id or tag) match — otherwise "list assets" would prepend a
        // noisy recipe block on every word-cloud search.
        var hasStrongRecipe = recipeMatches.Any(m => m.Strong);
        var emitRecipes = hasStrongRecipe;

        var batchSearched = !string.IsNullOrWhiteSpace(search)
            && search.Contains("batch", StringComparison.OrdinalIgnoreCase);
        var anyBatchMatch = false;
        if (batchSearched)
        {
            foreach (var (_, idx) in matches)
            {
                if (results[idx].Path.Contains("/Batch", StringComparison.OrdinalIgnoreCase))
                {
                    anyBatchMatch = true;
                    break;
                }
            }
        }

        if (matches.Count == 0)
        {
            // Empty-result fallback: if no endpoints matched but a recipe did,
            // steer the agent toward the recipe rather than blanking out.
            if (emitRecipes)
            {
                sb.Append("No endpoints matched '").Append(search).AppendLine("', but a related workflow recipe exists:");
                AppendRecipeBlock(sb, recipeMatches, includeStrongCallout: true);
                return sb.ToString().TrimEnd();
            }

            if (batchSearched)
                return BuildNoBatchHint() + "\n\nNo endpoints matched the search criteria. Try broader search terms.";
            return "No endpoints matched the search criteria. Try broader search terms.\nTip: Use Safeguard_Schema to see request/response body format for POST/PUT endpoints.";
        }

        if (batchSearched && !anyBatchMatch)
            sb.AppendLine(BuildNoBatchHint()).AppendLine();

        // Recipe block sits ABOVE the endpoint listing. Workflows describe
        // multi-step compositions the agent is about to attempt one endpoint
        // at a time; ranking them first prevents the "right URL, wrong
        // workflow" failure mode.
        if (emitRecipes)
        {
            AppendRecipeBlock(sb, recipeMatches, includeStrongCallout: true);
            sb.AppendLine("----");
            sb.Append("Endpoints (").Append(matches.Count).AppendLine("):");
        }

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
            sb.Append("  -- ").AppendLine(ep.Summary);
            AppendParameterDetails(sb, ep);
            shown++;
        }

        if (matches.Count > limit)
            sb.AppendLine().Append("... and ").Append(matches.Count - limit).Append(" more. Narrow your search with service, method, or more specific search text.");

        sb.AppendLine();
        sb.Append("Tip: Use Safeguard_Schema to see request/response body format for POST/PUT endpoints.");
        return sb.ToString();
    }

    private static void AppendRecipeBlock(StringBuilder sb, IReadOnlyList<RecipeMatch> matches, bool includeStrongCallout)
    {
        var strong = matches.FirstOrDefault(m => m.Strong);
        if (includeStrongCallout && strong != null)
        {
            sb.Append("Strong recipe match: ").Append(strong.Recipe.Id)
                .Append(" -- ").AppendLine(strong.Recipe.Name);
            sb.Append("  Call: Safeguard_Workflows id=\"").Append(strong.Recipe.Id).AppendLine("\"");
            if (!string.IsNullOrWhiteSpace(strong.Recipe.Tool))
            {
                sb.Append("  Or call the composite tool directly: ")
                    .AppendLine(strong.Recipe.Tool);
            }
            sb.AppendLine();
        }

        sb.AppendLine("Recipes that match (call Safeguard_Workflows id=\"<id>\"):");
        foreach (var match in matches)
        {
            sb.Append("  ").Append(match.Recipe.Id.PadRight(28))
                .Append("  ").AppendLine(match.Recipe.Name);
            if (!string.IsNullOrWhiteSpace(match.Recipe.Tool))
            {
                sb.Append("    composite tool: ").AppendLine(match.Recipe.Tool);
            }
            if (match.Recipe.Tags.Count > 0)
            {
                sb.Append("    tags: ").AppendLine(string.Join(", ", match.Recipe.Tags));
            }
        }
        sb.AppendLine();
    }

    [McpServerTool(Name = "Safeguard_Execute", Title = "Execute Safeguard API",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Execute any Safeguard API endpoint. The service (Core, Appliance, Notification) "
        + "is automatically determined from the endpoint path. "
        + "Use Safeguard_Discover to find endpoints, Safeguard_Schema for request body format. "
        + "Note: format=csv is only valid for GET requests; POST/PUT/PATCH/DELETE must use format=json. "
        + "JSON responses are returned as a structured envelope: { data: <body>, meta: { notices: [...], paging?: { page, limit, returned, more, next }, truncation?: {...} } } — "
        + "always read the data field for the actual API payload, and meta.notices for applied auto-limit / paging hints / truncation events. "
        + "Responses are capped at ~30 KB; for fat endpoints (audit logs, Assets, AssetAccounts) "
        + "use fields= to project, or follow meta.paging.next to fetch the next page.")]
    public async Task<string> Safeguard_Execute(McpServer server,
        [Description("HTTP method: GET, POST, PUT, PATCH, or DELETE.")] string method,
        [Description("API path (e.g. '/v4/Users', '/v4/ApplianceStatus/Health'). The correct service is auto-detected from the path.")] string path,
        [Description("Query parameters (e.g. 'fields=Id,Name&filter=Name eq \"x\"'). Omit if none.")] string query = null,
        [Description("JSON request body for POST/PUT/PATCH. Omit for GET/DELETE.")] string body = null,
        [Description("Response format: 'json' (default) or 'csv' (tabular, smaller for large datasets). GET-only — non-GET methods must use 'json'.")]
        string format = "json",
        CancellationToken ct = default)
        => await DispatchAsync(server, method, path, query, body, format, ct);

    [McpServerTool(Name = "Safeguard_Schema", Title = "Get API Schema",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get the request body schema and response schema for a Safeguard API endpoint. "
        + "Returns property names, types, required fields, and descriptions. "
        + "Use this before POST or PUT calls to understand the JSON body format. "
        + "Set depth>1 (max 3) to expand nested object/array properties one or more levels.")]
    public string Safeguard_Schema(
        [Description("The API path (e.g. '/v4/AssetAccounts', '/v4/Users').")] string path,
        [Description("HTTP method: POST, PUT, or GET (for response schema). Default: POST")] string method = "POST",
        [Description("How many levels deep to expand nested object/array properties. Default 1, max 3.")] int depth = 1)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new McpException("The 'path' parameter is required (e.g. '/v4/AssetAccounts').");

        var normalizedPath = NormalizePath(path);
        var normalizedMethod = ParseMethod(method);
        var service = ResolveService(normalizedPath);
        var serviceName = GetServiceName(service);
        var clampedDepth = Math.Max(1, Math.Min(depth, 3));

        var requestSchema = catalogProvider.GetSchema(normalizedMethod, serviceName, normalizedPath);

        // Prefer the response schema for the requested method (PUT/POST often return the
        // updated entity); fall back to GET-on-same-path so query-only callers still get a
        // useful response shape.
        var responseSchema = catalogProvider.GetResponseSchema(normalizedMethod, serviceName, normalizedPath);
        var responseHeading = $"RESPONSE BODY ({normalizedMethod})";
        if (responseSchema == null && normalizedMethod != "GET")
        {
            responseSchema = catalogProvider.GetResponseSchema("GET", serviceName, normalizedPath);
            responseHeading = "RESPONSE BODY (GET)";
        }
        else if (normalizedMethod == "GET")
        {
            responseHeading = "RESPONSE BODY (GET)";
        }

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
        {
            AppendSchemaSection(sb, "REQUEST BODY", requestSchema.Value, clampedDepth, catalogProvider);
        }
        else if (HasRequestBody(normalizedMethod, serviceName, normalizedPath))
        {
            sb.AppendLine().AppendLine("REQUEST BODY:").AppendLine("  No request schema available.");
        }
        else if (normalizedMethod is "POST" or "PUT" or "PATCH")
        {
            sb.AppendLine().AppendLine("REQUEST BODY:").AppendLine("  No request body required.");
        }

        if (responseSchema != null)
            AppendSchemaSection(sb, responseHeading, responseSchema.Value, clampedDepth, catalogProvider);
        else
            sb.AppendLine().Append(responseHeading).AppendLine(":").AppendLine("  No response schema available.");

        AppendPropertyPaths(sb, responseSchema, requestSchema);

        return sb.ToString().TrimEnd();
    }

    private static void AppendPropertyPaths(StringBuilder sb, ApiSchema? responseSchema, ApiSchema? requestSchema)
    {
        // Prefer the response graph (matches `fields=` and the appliance's serializer ~95%);
        // fall back to the request body graph for write-only endpoints.
        var primary = responseSchema?.Paths;
        var primaryHeading = "Field paths (for fields=; ~90%+ also valid for filter/orderby)";
        if (primary == null || primary.Length == 0)
        {
            primary = requestSchema?.Paths;
            primaryHeading = "Body field paths (for request-body construction)";
        }
        if (primary == null || primary.Length == 0)
            return;

        var fieldPaths = primary.Where(p => !p.IsSynthetic).ToArray();
        var syntheticPaths = primary.Where(p => p.IsSynthetic).ToArray();

        sb.AppendLine().Append("Property paths:").AppendLine();
        sb.Append("  ").Append(primaryHeading).AppendLine(":");
        if (fieldPaths.Length == 0)
        {
            sb.AppendLine("    (none)");
        }
        else
        {
            foreach (var p in fieldPaths)
                sb.Append("    ").Append(p.Path).Append("  ").AppendLine(p.Type);
        }

        if (syntheticPaths.Length > 0)
        {
            sb.AppendLine("  Synthetic count paths (orderby/filter only):");
            foreach (var p in syntheticPaths)
                sb.Append("    ").Append(p.Path).Append("  ").AppendLine(p.Type);
        }

        sb.AppendLine("  Notes:")
            .AppendLine("    - Paths are case-insensitive.")
            .AppendLine("    - filter/orderby may use flattened forms (e.g. Account.AssetName) where")
            .AppendLine("      fields walks the full graph (Account.Asset.Name). When the appliance")
            .AppendLine("      returns 70001 / 70009, try the flattened sibling at the parent level.")
            .AppendLine("    - Enum properties cannot be sorted in the pre-filter (HTTP 70019); they")
            .AppendLine("      sort post-fetch only.");

        if (primary.Length >= 500)
        {
            sb.AppendLine("    - Path closure was capped at 500 entries; deeply nested branches may")
                .AppendLine("      not appear above. Use Safeguard_Execute and inspect a sample row.");
        }
    }

    private bool HasRequestBody(string method, string service, string path)
    {
        foreach (var endpoint in catalogProvider.GetEndpoints())
        {
            if (string.Equals(endpoint.Method, method, StringComparison.OrdinalIgnoreCase)
                && string.Equals(endpoint.Service, service, StringComparison.OrdinalIgnoreCase)
                && string.Equals(endpoint.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                return endpoint.HasBody;
            }
        }
        return false;
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
            .AppendLine("  Filter operators: eq, ne, gt, ge, lt, le, contains, icontains, ieq, sw, isw, ew, iew, in [...]")
            .AppendLine("  Operators are case-insensitive; string literals are case-sensitive (use ieq/icontains/isw/iew for case-insensitive text)")
            .AppendLine("  String values use single quotes: filter=Name eq 'Admin'")
            .AppendLine("  Null literal: filter=Description eq null  /  filter=Description ne null")
            .AppendLine("  Combine expressions with and, or, not, and parentheses")
            .AppendLine("  No 'not_in' / 'not in' operator: use filter=not (Id in [1,2,3])")
            .AppendLine("  Collections expose synthetic .Count for filter/orderby: filter=ScopeItems.Count gt 0")
            .AppendLine("  Nested properties: filter=TaskProperties.HasAccountTaskFailure eq true")
            .AppendLine("  Relationships: parents are nested objects, NOT flat FK columns.")
            .AppendLine("    To-one is dottable: fields=Asset.Id,Asset.Name (NOT AssetId/AssetName).")
            .AppendLine("    To-many is NOT dottable: use child endpoint GET /v4/<parent>/{id}/<collection>")
            .AppendLine("      (e.g. /v4/AssetPartitions/{id}/Profiles, NOT fields=Profiles.Id).")
            .AppendLine("    Schema labels: object<Type> = to-one (dottable); array<Type> = to-many (use child path).")
            .AppendLine("  Field selection: fields=Id,Name,Description")
            .AppendLine("  Exclude fields: fields=-TaskProperties,-Platform")
            .AppendLine("  ObjectChanges default: on /v4/AuditLog/ObjectChanges list-style routes "
                + "(collection, by-type, by-type+by-id) fields= defaults to '-OldValue,-NewValue' so the "
                + "full-entity JSON-string snapshots are dropped and the structured Changes[] diff is "
                + "retained; add OldValue or NewValue to your own fields= list to include the snapshots, "
                + "or fetch /v4/AuditLog/ObjectChanges/{objectType}/{objectId}/{logId} for the singleton view.")
            .AppendLine("  Ordering: orderby=Name or orderby=-CreatedDate")
            .AppendLine("  Multiple order fields: orderby=Name,-CreatedDate")
            .AppendLine("  NOTE: Not OData. Use -Field for descending; 'Name desc' or 'Name asc' returns HTTP 400.")
            .AppendLine("  Pagination: page=0&limit=50 (page is 0-indexed)")
            .AppendLine("  Quick search: q=searchterm")
            .AppendLine("  Count only: count=true")
            .AppendLine()
            .AppendLine("Examples:")
            .AppendLine("  fields=Id,Name&filter=Name icontains 'admin'&orderby=Name&page=0&limit=25")
            .AppendLine("  filter=(Disabled eq false) and (Platform.DisplayName eq 'Windows')")
            .AppendLine("  filter=Id in [1,2,3]")
            .AppendLine()
            .AppendLine("Reports vs direct queries:")
            .AppendLine("  /v4/Reports/* aggregates across the whole estate and can be slow on large deployments.")
            .AppendLine("  Prefer direct entity queries for narrow questions:")
            .AppendLine("    Who is in role X?            -> GET /v4/Roles/{id}/Members")
            .AppendLine("    What policies does role X have?-> GET /v4/Roles/{id}/Policies")
            .AppendLine("    Which roles is user Y in?    -> GET /v4/Users/{id}/Roles")
            .AppendLine("  Use Reports only for estate-wide aggregates (e.g. \"every user-account pair\").")
            .AppendLine("  Reports endpoints have their own field schemas - call Safeguard_Schema on the report path first.")
            .AppendLine()
            .AppendLine("Sensitive credential material:")
            .AppendLine("  Passwords, SSH private keys, A2A secrets, API client secrets, API key history, TOTP codes,")
            .AppendLine("  generated passwords, personal-account password history, and secure-file content are all sensitive.")
            .AppendLine("  These endpoints are NOT callable via Safeguard_Execute. Use Safeguard_RetrieveCredential")
            .AppendLine("  (kinds: access-request-password, access-request-ssh-key, access-request-api-key,")
            .AppendLine("  access-request-totp, access-request-file, personal-account-password,")
            .AppendLine("  personal-account-password-history, personal-account-totp, generated-password,")
            .AppendLine("  asset-account-api-secret-history). Calling those paths via Safeguard_Execute returns a")
            .AppendLine("  structured sensitive_endpoint_redirected envelope naming the matching Safeguard_RetrieveCredential")
            .AppendLine("  kind and arguments — lift data.next_call into a follow-up tool invocation.")
            .AppendLine("  Safeguard_RetrieveCredential emits a two-block MCP response: block 1 (audience=assistant) is")
            .AppendLine("  metadata only; block 2 (audience=user) carries the plaintext. Hosts that honor audience")
            .AppendLine("  annotations route the user block to a secure pane without exposing it to the LLM.")
            .AppendLine("  Setting/rotating a password on a managed account: POST /v4/AssetAccounts/{id}/ChangePassword (no body)")
            .AppendLine("    -> Safeguard generates per partition rule, pushes to asset, NO plaintext returned. Parallelize by id.")
            .AppendLine("  Generating a rule-compliant value out of band: POST /v4/AssetAccounts/{id}/GeneratePassword (no body)")
            .AppendLine("  Setting a known value: PUT /v4/AssetAccounts/{id}/Password (body = value)")
            .AppendLine("  Do NOT mint passwords client-side (Get-Random, pwgen, LLM) - bypasses the password rule and leaks plaintext.")
            .AppendLine("  See workflow recipe: set-initial-account-password.");

        if (string.IsNullOrWhiteSpace(path))
            return sb.ToString().TrimEnd();

        var normalizedPath = NormalizePath(path);
        var service = ResolveService(normalizedPath);
        var serviceName = GetServiceName(service);
        var schema = catalogProvider.GetResponseSchema("GET", serviceName, normalizedPath)
            ?? catalogProvider.GetSchema("GET", serviceName, normalizedPath);

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

    [McpServerTool(Name = "Safeguard_Enum", Title = "Get Enum Values",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("List the allowed values for a Safeguard enum schema (e.g. 'AccessRequestType', "
        + "'EventName', 'ScheduleType'). Call without arguments to list all known enum names. "
        + "Use this to resolve enum<...>-typed properties surfaced by Safeguard_Schema, especially "
        + "for the high-cardinality ones (EventName has 600+ values) that are not inlined.")]
    public string Safeguard_Enum(
        [Description("Enum schema name (case-insensitive). Omit to list all known enum names.")]
        string name = null,
        [Description("Optional case-insensitive substring filter to narrow long lists.")]
        string pattern = null,
        [Description("Maximum values to return. Default 50.")] int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var names = catalogProvider.GetEnumNames();
            if (names.Count == 0)
            {
                return "No enums are available. Connect first with Safeguard_Connect to load "
                    + "the dynamic catalog from the appliance swagger.";
            }
            var sb0 = new StringBuilder();
            sb0.Append(names.Count).AppendLine(" enum(s) available:");
            foreach (var n in names)
                sb0.Append("  ").AppendLine(n);
            sb0.AppendLine().AppendLine("Call Safeguard_Enum name=\"<Name>\" to see allowed values.");
            return sb0.ToString().TrimEnd();
        }

        var values = catalogProvider.GetEnum(name);
        if (values == null)
        {
            return $"No enum named '{name}' is available. "
                + "Call Safeguard_Enum (no args) to list known enum names. "
                + "Names are case-insensitive but must match a swagger schema name exactly.";
        }

        IEnumerable<string> filtered = values;
        if (!string.IsNullOrWhiteSpace(pattern))
        {
            filtered = values.Where(v => v.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
        var clampedLimit = Math.Max(1, limit);
        var matched = filtered.ToArray();
        var shown = matched.Take(clampedLimit).ToArray();

        var sb = new StringBuilder();
        sb.Append("Enum: ").AppendLine(name);
        sb.Append("Total values: ").Append(values.Length);
        if (!string.IsNullOrWhiteSpace(pattern))
            sb.Append("  (filter: '").Append(pattern).Append("' matched ").Append(matched.Length).Append(')');
        sb.AppendLine();
        if (shown.Length == 0)
        {
            sb.AppendLine("  (no matches)");
        }
        else
        {
            foreach (var v in shown)
                sb.Append("  ").AppendLine(v);
        }
        if (matched.Length > shown.Length)
            sb.Append("  ... showing ").Append(shown.Length).Append(" of ").Append(matched.Length).AppendLine(".");
        return sb.ToString().TrimEnd();
    }

    private async Task<string> DispatchAsync(
        McpServer server,
        string method,
        string path,
        string query,
        string body,
        string format,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new McpException("The 'method' parameter is required (GET, POST, PUT, PATCH, or DELETE).");
        if (string.IsNullOrWhiteSpace(path))
            throw new McpException("The 'path' parameter is required (e.g. '/v4/Users').");

        var normalizedMethod = ParseMethod(method);
        var normalizedPath = NormalizePath(path);

        // Refuse-and-redirect for sensitive credential endpoints BEFORE any I/O.
        // The single source of truth lives in
        // Catalog/Resources/sensitive-credential-endpoints.json. Anything in that
        // catalog is owned by Safeguard_RetrieveCredential; Safeguard_Execute
        // never touches the wire for those paths. The agent receives a
        // structured next_call envelope so it can lift the suggested tool +
        // arguments without re-deriving them.
        var sensitiveMatch = SensitiveCredentialEndpoints.TryMatch(normalizedMethod, normalizedPath);
        if (sensitiveMatch != null)
        {
            logger.LogInformation(
                "Safeguard_Execute refused sensitive endpoint: refused_path={Path} redirected_to=Safeguard_RetrieveCredential kind={Kind}",
                normalizedPath,
                sensitiveMatch.Entry.Kind);
            return BuildSensitiveEndpointRedirectEnvelope(normalizedMethod, normalizedPath, sensitiveMatch);
        }

        var requestedFormat = string.IsNullOrWhiteSpace(format) ? "json" : format.Trim().ToLowerInvariant();
        if (requestedFormat != "json" && requestedFormat != "csv")
            throw new McpException("Unsupported response format. Use 'json' or 'csv'.");
        if (requestedFormat == "csv" && normalizedMethod != "GET")
            throw new McpException("CSV format is only supported for GET requests.");

        var service = ResolveService(normalizedPath);
        var relativeUrl = ToSdkRelativeUrl(normalizedPath);
        var parameters = ParseQueryParameters(query);
        parameters = MaybeInjectLimit(normalizedMethod, normalizedPath, service, parameters, out var injectedLimit);
        parameters = MaybeInjectDefaultFields(normalizedMethod, normalizedPath, parameters, out var injectedDefaultFields);

        try
        {
            var requiresAuthentication = service != Service.Notification || requestedFormat == "csv";
            if (requiresAuthentication)
            {
                await session.EnsureReadyAsync(server, ct);
            }
            else if (string.IsNullOrWhiteSpace(session.Host))
            {
                throw new McpException(
                    "Safeguard appliance host is not configured. Set SAFEGUARD_HOST and restart the MCP server.");
            }

            if (requestedFormat == "csv")
            {
                var csv = await SafeguardInvoker.InvokeCsvAsync(session, service, relativeUrl, parameters, ct);
                return FormatResponse(csv ?? string.Empty, requestedFormat, injectedLimit, injectedDefaultFields, parameters, normalizedMethod, normalizedPath);
            }

            FullResponse response;
            if (!requiresAuthentication)
            {
                response = await SafeguardInvoker.InvokeUnauthenticatedAsync(
                    session.Host, session.IgnoreSsl, service, normalizedMethod, relativeUrl, body, parameters, ct);
            }
            else
            {
                response = await SafeguardInvoker.InvokeAsync(
                    session, service, normalizedMethod, relativeUrl, body, parameters, ct);
            }

            return FormatResponse(response.Body ?? string.Empty, requestedFormat, injectedLimit, injectedDefaultFields, parameters, normalizedMethod, normalizedPath);
        }
        catch (McpException ex)
        {
            throw new McpException(FormatErrorResponse(ex, normalizedMethod, GetServiceName(service), normalizedPath));
        }
    }

    private string FormatResponse(string body, string format, int injectedLimit, bool injectedDefaultFields, IDictionary<string, string> parameters, string method = null, string path = null)
    {
        var safeBody = body ?? string.Empty;
        var notices = new List<Notice>();

        if (injectedLimit > 0)
        {
            notices.Add(new Notice(
                NoticeKinds.AutoLimitApplied,
                $"Auto-applied limit={injectedLimit}.",
                "Specify 'limit' in the query to override; pass page=N for further pages."));
        }

        if (injectedDefaultFields)
        {
            notices.Add(new Notice(
                NoticeKinds.DefaultFieldsApplied,
                "Default fields= projection applied to drop OldValue/NewValue (full-entity JSON-string snapshots). "
                + "The structured per-property diff in Changes[] is retained.",
                "Add 'OldValue' or 'NewValue' to your own fields= list to include the snapshots; "
                + "or fetch /v4/AuditLog/ObjectChanges/{objectType}/{objectId}/{logId} for the singleton view."));
        }

        var recipeNotice = BuildRecipeCrossLinkNotice(method, path);
        if (recipeNotice != null)
            notices.Add(recipeNotice);

        // Heuristic backstop: a path NOT in the sensitive-credential catalog
        // that returns a top-level (or first-array-element) field literally
        // named Password / PrivateKey / ClientSecret / Secret / ApiKey /
        // Passphrase / Code with a non-empty string value gets flagged for
        // maintainers. Does not redact (the catalog is the contract); the
        // notice surfaces the inventory gap so the next reader closes it.
        if (format != "csv")
        {
            var heuristicNotice = BuildUncatalogedSensitiveShapeNotice(safeBody, method, path);
            if (heuristicNotice != null)
                notices.Add(heuristicNotice);
        }

        // Determine effective limit/page for the paging block.
        int? effectiveLimit = null;
        var limitSource = "none";
        if (parameters != null && parameters.TryGetValue("limit", out var limitText)
            && int.TryParse(limitText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLimit)
            && parsedLimit > 0)
        {
            effectiveLimit = parsedLimit;
            limitSource = injectedLimit > 0 ? "auto" : "explicit";
        }

        var currentPage = 0;
        if (parameters != null && parameters.TryGetValue("page", out var pageText)
            && int.TryParse(pageText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPage)
            && parsedPage >= 0)
        {
            currentPage = parsedPage;
        }

        if (format == "csv")
        {
            // CSV: never prepend or interleave prose into the data. Append meta as a
            // single trailing comment line only when there is something to surface.
            // Paging info for CSV is best-effort: we don't parse the CSV to count rows here,
            // so we surface only what we already know from the request.
            PagingInfo csvPaging = null;
            if (effectiveLimit.HasValue)
            {
                csvPaging = new PagingInfo
                {
                    Page = currentPage,
                    Limit = effectiveLimit,
                    Returned = -1,
                    LimitSource = limitSource,
                    More = false,
                    Next = ResponseEnvelopeBuilder.BuildNextQueryString(parameters, currentPage, effectiveLimit)
                };
            }

            return ResponseEnvelopeBuilder.BuildCsvWithMeta(safeBody, notices, csvPaging, truncation: null);
        }

        // JSON path.
        var truncationOutcome = ApiToolHelpers.TryTruncateJsonArrayWithBudget(
            safeBody,
            MaxResultsBeforeTruncation,
            MaxResponseChars,
            out var truncatedBody,
            out var totalItems,
            out var keptItems);

        var isArray = truncationOutcome != ApiToolHelpers.TruncationOutcome.NotArray;
        var dataBody = isArray ? truncatedBody : safeBody;
        TruncationInfo truncationInfo = null;

        switch (truncationOutcome)
        {
            case ApiToolHelpers.TruncationOutcome.RecordsDropped:
                truncationInfo = new TruncationInfo
                {
                    Applied = true,
                    Kind = "records_dropped",
                    ReturnedRecords = keptItems,
                    TotalRecords = totalItems
                };
                notices.Add(new Notice(
                    NoticeKinds.BodyTruncatedRecords,
                    $"Returned {totalItems} items; trimmed to the first {keptItems} to fit the response cap.",
                    "Use filter=, fields=, or page=/limit= to narrow the result."));
                break;
            case ApiToolHelpers.TruncationOutcome.RecordTooLarge:
                truncationInfo = new TruncationInfo
                {
                    Applied = true,
                    Kind = "record_too_large",
                    ReturnedRecords = keptItems,
                    TotalRecords = totalItems
                };
                notices.Add(new Notice(
                    NoticeKinds.RecordTooLargeForCap,
                    $"A single record exceeds the response cap (~{MaxResponseChars} chars). Returned intact to preserve JSON validity.",
                    injectedDefaultFields
                        ? "OldValue/NewValue snapshots were already excluded by the default projection; narrow with filter= "
                            + "(e.g. by EventName, ObjectType, or LogTime range) or fetch the singleton "
                            + "/v4/AuditLog/ObjectChanges/{objectType}/{objectId}/{logId} for one record at a time."
                        : "Use fields= to project (e.g. fields=Id,ObjectName,Action,LogTime to drop OldValue/NewValue), or query the per-record path if available."));
                break;
        }

        // Non-array: never mid-character truncate. Surface a notice if oversize.
        if (!isArray && dataBody.Length > MaxResponseChars)
        {
            notices.Add(new Notice(
                NoticeKinds.BodyTruncatedChars,
                $"Response exceeds the {MaxResponseChars}-character cap. Body returned intact to preserve JSON validity.",
                "Use fields= to project, or call a more specific endpoint to reduce payload size."));
        }

        PagingInfo paging = null;
        if (isArray)
        {
            var truncationApplied = truncationInfo != null && truncationInfo.Applied;
            var more = truncationApplied
                || (effectiveLimit.HasValue && keptItems >= effectiveLimit.Value);
            string next = null;
            if (effectiveLimit.HasValue && more)
                next = ResponseEnvelopeBuilder.BuildNextQueryString(parameters, currentPage, effectiveLimit);

            paging = new PagingInfo
            {
                Page = currentPage,
                Limit = effectiveLimit,
                Returned = keptItems,
                LimitSource = limitSource,
                More = more,
                Next = next
            };

            if (more && next != null && truncationInfo == null)
            {
                notices.Add(new Notice(
                    NoticeKinds.PagingMoreAvailable,
                    "More records may be available.",
                    $"Fetch the next page with query='{next}'."));
            }
        }

        return ResponseEnvelopeBuilder.BuildJsonEnvelope(dataBody, notices, paging, truncationInfo);
    }

    internal static Notice BuildRecipeCrossLinkNotice(string method, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Only surface composite-tool hints for the highest-leverage launch / close paths;
        // broader path-to-recipe matching could become noisy and would belong in RecipeIndex
        // if expanded. Currently the composite tools are Safeguard_OpenAccessRequest and
        // Safeguard_CloseAccessRequest.
        var segments = GetPathSegments(path);
        if (segments.Length == 0)
            return null;

        // /v4/AccessRequests (POST creates) and /v4/AccessRequests/{id}/InitializeSession,
        // /CheckOutPassword, /CheckOutSshKey, /CheckOutApiKeys, /CheckOutFile are all part
        // of the launch workflow. /Cancel, /CheckIn, /Close, /Acknowledge map to the close
        // composite tool.
        if (!segments.Any(s => s.Equals("AccessRequests", StringComparison.OrdinalIgnoreCase)))
            return null;

        var verb = string.IsNullOrWhiteSpace(method) ? "POST" : method.ToUpperInvariant();

        if (verb == "POST" && segments.Length >= 4)
        {
            var leaf = segments[^1];
            if (leaf.Equals("Cancel", StringComparison.OrdinalIgnoreCase)
                || leaf.Equals("CheckIn", StringComparison.OrdinalIgnoreCase)
                || leaf.Equals("Close", StringComparison.OrdinalIgnoreCase)
                || leaf.Equals("Acknowledge", StringComparison.OrdinalIgnoreCase))
            {
                return new Notice(
                    NoticeKinds.WorkflowRecipeSuggested,
                    "Related composite tool: Safeguard_CloseAccessRequest picks the correct sub-endpoint "
                    + "(Cancel / CheckIn / Close / Acknowledge) for you based on the request's current State "
                    + "and your role, so you don't have to guess which of the four to POST.",
                    "Call Safeguard_CloseAccessRequest requestId=<id> [comment=...] [allFields=...]; "
                    + "the tool returns a refusal envelope with a diagnostic naming the state when no "
                    + "sub-endpoint is appropriate.");
            }
        }

        var suggestion = verb == "POST" && segments.Length == 2
            ? "Safeguard_OpenAccessRequest performs the pre-flight entitlement check and submits the request in one call."
            : "Use Safeguard_Workflows id=\"session-access-request\" for the full launch workflow.";

        return new Notice(
            NoticeKinds.WorkflowRecipeSuggested,
            "Related workflow recipe: session-access-request (composite tools: "
            + "Safeguard_OpenAccessRequest, Safeguard_CloseAccessRequest).",
            suggestion);
    }

    // Field names whose presence in a top-level response body or first-array-element
    // commonly indicates credential material. Match is literal and case-insensitive
    // on the JSON property name; value must be a non-empty string. Numeric / null /
    // boolean / object values do NOT trigger the notice (audit-log "Password event"
    // records, schema descriptions that mention these names, etc. all skip naturally).
    private static readonly string[] SensitiveFieldNames =
    {
        "Password", "PrivateKey", "ClientSecret", "Secret", "ApiKey", "Passphrase", "Code"
    };

    internal static Notice BuildUncatalogedSensitiveShapeNotice(string body, string method, string path)
    {
        if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(path))
            return null;
        // Catalog wins: any cataloged sensitive endpoint is unreachable via Execute,
        // so we never expect to see one here — but the guard above keeps the heuristic
        // honest when paths drift.
        if (SensitiveCredentialEndpoints.TryMatch(method, path) != null)
            return null;

        string firedField = null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            JsonElement scope = root;
            if (root.ValueKind == JsonValueKind.Array)
            {
                var first = default(JsonElement);
                var any = false;
                foreach (var el in root.EnumerateArray()) { first = el; any = true; break; }
                if (!any) return null;
                scope = first;
            }
            if (scope.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var name in SensitiveFieldNames)
            {
                if (!scope.TryGetProperty(name, out var prop)) continue;
                if (prop.ValueKind != JsonValueKind.String) continue;
                var v = prop.GetString();
                if (string.IsNullOrEmpty(v)) continue;
                firedField = name;
                break;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        if (firedField == null)
            return null;

        return new Notice(
            NoticeKinds.UncatalogedSensitiveShape,
            $"Response contains a field commonly used for credential material ('{firedField}') at an endpoint not "
            + "in the sensitive-credential catalog. The catalog (Catalog/Resources/sensitive-credential-endpoints.json) "
            + "is the authoritative boundary for Safeguard_RetrieveCredential; this notice flags a possible inventory gap. "
            + "No redaction was applied.",
            "If this endpoint does return live credential material, add it to the catalog so Safeguard_Execute "
            + "refuses-and-redirects through Safeguard_RetrieveCredential instead of returning plaintext.");
    }

    internal static string BuildSensitiveEndpointRedirectEnvelope(
        string method, string path, SensitiveCredentialEndpoints.Match match)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("data");
            writer.WriteStartObject();
            writer.WriteString("error", "sensitive_endpoint_redirected");
            writer.WriteString("message",
                "This endpoint returns plaintext credential material and is not callable via Safeguard_Execute. "
                + "Use Safeguard_RetrieveCredential, which returns a two-block MCP response that splits "
                + "the assistant-audience metadata from the user-audience plaintext.");
            writer.WriteString("refusedMethod", method);
            writer.WriteString("refusedPath", path);

            writer.WritePropertyName("next_call");
            writer.WriteStartObject();
            writer.WriteString("tool", "Safeguard_RetrieveCredential");
            writer.WritePropertyName("arguments");
            writer.WriteStartObject();
            writer.WriteString("kind", match.Entry.Kind);
            foreach (var kv in match.ExtractedArguments)
            {
                if (int.TryParse(kv.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    writer.WriteNumber(kv.Key, n);
                else
                    writer.WriteString(kv.Key, kv.Value);
            }
            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.WriteEndObject(); // data

            writer.WritePropertyName("meta");
            writer.WriteStartObject();
            writer.WritePropertyName("notices");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("kind", NoticeKinds.SensitiveEndpointRedirected);
            writer.WriteString("message",
                "Safeguard_Execute refused this endpoint because it returns plaintext credential material. "
                + "Lift data.next_call into the suggested tool invocation.");
            writer.WriteString("suggestion",
                $"Call {match.Entry.Kind} via Safeguard_RetrieveCredential with the arguments in data.next_call.arguments.");
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject(); // meta
            writer.WriteEndObject(); // root
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private string FormatErrorResponse(McpException ex, string method, string serviceName, string requestPath)
    {
        var rawMessage = ex.Message ?? "Safeguard API request failed.";
        var statusCode = ExtractStatusCode(rawMessage);
        if (statusCode == 0 && rawMessage.Contains("Authentication expired", StringComparison.OrdinalIgnoreCase))
            statusCode = 401;

        var errorDetail = ExtractErrorDetail(rawMessage);
        TryParseErrorBody(errorDetail, out var apiMessage, out var apiCode, out var innerError);
        var modelStateSummary = ApiToolHelpers.FormatModelState(errorDetail);

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
        if (!string.IsNullOrWhiteSpace(modelStateSummary))
        {
            lines.Add("Validation errors:");
            lines.Add(modelStateSummary);
        }

        var hint = GetErrorHint(
            statusCode,
            rawMessage,
            apiMessage,
            !string.IsNullOrWhiteSpace(modelStateSummary),
            method,
            serviceName,
            requestPath);
        if (!string.IsNullOrWhiteSpace(hint))
            lines.Add($"Hint: {hint}");

        return string.Join("\n", lines);
    }

    private (string TemplatePath, ApiSchemaPropertyPath[] Paths) ResolveErrorContext(
        string method, string serviceName, string requestPath)
    {
        var endpoints = catalogProvider.GetEndpoints();
        string templatePath = null;
        for (int i = 0; i < endpoints.Length; i++)
        {
            ref readonly var endpoint = ref endpoints[i];
            if (!endpoint.Service.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!PathsMatch(endpoint.Path, requestPath))
                continue;
            // Prefer the endpoint matching the actual method; otherwise keep
            // looking but remember the first path match as a fallback.
            if (endpoint.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
            {
                templatePath = endpoint.Path;
                break;
            }
            templatePath ??= endpoint.Path;
        }

        templatePath ??= requestPath;

        var response = catalogProvider.GetResponseSchema(method, serviceName, templatePath)
            ?? catalogProvider.GetResponseSchema("GET", serviceName, templatePath);
        var request = catalogProvider.GetSchema(method, serviceName, templatePath);

        ApiSchemaPropertyPath[] paths = null;
        if (response != null && response.Value.Paths != null && response.Value.Paths.Length > 0)
            paths = response.Value.Paths;
        else if (request != null && request.Value.Paths != null && request.Value.Paths.Length > 0)
            paths = request.Value.Paths;

        return (templatePath, paths ?? Array.Empty<ApiSchemaPropertyPath>());
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

    private string GetErrorHint(
        int statusCode,
        string rawMessage,
        string apiMessage,
        bool hasModelState,
        string method,
        string serviceName,
        string requestPath)
    {
        var (templatePath, paths) = ResolveErrorContext(method, serviceName, requestPath);
        var ctx = new ErrorContext(serviceName, method, templatePath);

        var hint = ApiToolHelpers.GetErrorHint(statusCode, apiMessage, hasModelState, ctx, paths);
        if (!string.IsNullOrWhiteSpace(hint))
            return hint;

        if (rawMessage != null
            && rawMessage.Contains("Authentication expired", StringComparison.OrdinalIgnoreCase))
        {
            return "Token expired. Call Safeguard_Connect to re-authenticate.";
        }

        return null;
    }

    private static IDictionary<string, string> MaybeInjectDefaultFields(
        string method,
        string normalizedPath,
        IDictionary<string, string> parameters,
        out bool injected)
    {
        injected = false;

        var callerSuppliedFields = parameters != null && parameters.ContainsKey("fields");
        if (!DefaultProjections.TryGetDefaultFields(method, normalizedPath, callerSuppliedFields, out var defaultFields))
            return parameters;

        var updated = parameters == null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);

        updated["fields"] = defaultFields;
        injected = true;
        return updated;
    }

    private IDictionary<string, string> MaybeInjectLimit(
        string method,
        string path,
        Service service,
        IDictionary<string, string> parameters,
        out int injectedLimit)
    {
        injectedLimit = 0;

        if (!AutoInjectLimit || !method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            return parameters;
        if (parameters != null && parameters.ContainsKey("limit"))
            return parameters;
        if (!ShouldInjectLimit(path, service))
            return parameters;

        var updated = parameters == null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);

        injectedLimit = DefaultLimit;
        updated["limit"] = DefaultLimit.ToString(CultureInfo.InvariantCulture);
        return updated;
    }

    private bool ShouldInjectLimit(string path, Service service)
    {
        var endpoints = catalogProvider.GetEndpoints();
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

    private Service ResolveService(string path)
    {
        var endpoints = catalogProvider.GetEndpoints();
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
    private static string BuildNoBatchHint()
    {
        return "Note: Safeguard exposes batch endpoints only on a handful of top-level entity collections "
            + "(look for paths ending in '/Batch' on entities like Users, Assets, AssetAccounts). "
            + "Per-id actions (e.g. ChangePassword, CheckPassword, Disable, Enable) have NO batch counterpart — "
            + "call them in parallel by id from the client.";
    }

    // Renders the parameter contract for one endpoint underneath its header line. Preferred
    // parameters (the audit-endpoint scoping params marked "(Preferred over 'filter')" in the
    // controller XML docs) are listed first with a "[preferred]" tag and short description.
    // Path parameters appear separately so the agent doesn't try to send them as query strings.
    // The Defaults: line is built straight from the preferred parameter descriptions (e.g. the
    // "Default 1 day before endDate" sentence on startDate) — no hand-maintained hint table.
    internal static void AppendParameterDetails(StringBuilder sb, ApiEndpoint endpoint)
    {
        var infos = endpoint.ParamInfos;
        if (infos == null || infos.Length == 0)
            return;

        var preferred = new List<ParamInfo>();
        var otherQuery = new List<ParamInfo>();
        var pathParams = new List<ParamInfo>();

        foreach (var p in infos)
        {
            if (string.Equals(p.In, "path", StringComparison.OrdinalIgnoreCase))
                pathParams.Add(p);
            else if (p.PreferredOverFilter)
                preferred.Add(p);
            else
                otherQuery.Add(p);
        }

        if (preferred.Count > 0)
        {
            sb.Append("  Preferred params: ");
            for (int i = 0; i < preferred.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(preferred[i].Name);
                if (!string.IsNullOrEmpty(preferred[i].Type))
                    sb.Append(" (").Append(preferred[i].Type).Append(')');
                sb.Append(" [preferred]");
            }
            sb.AppendLine();
        }

        if (otherQuery.Count > 0)
        {
            sb.Append("  Other params:     ");
            for (int i = 0; i < otherQuery.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(otherQuery[i].Name);
                if (otherQuery[i].Required)
                    sb.Append('*');
            }
            sb.AppendLine();
        }

        if (pathParams.Count > 0)
        {
            sb.Append("  Path params:      ");
            for (int i = 0; i < pathParams.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('{').Append(pathParams[i].Name).Append('}');
            }
            sb.AppendLine();
        }

        var defaults = BuildDefaultsLine(preferred);
        if (defaults != null)
            sb.Append("  Defaults:         ").AppendLine(defaults);

        foreach (var p in preferred)
        {
            if (!string.IsNullOrWhiteSpace(p.Description))
                sb.Append("    - ").Append(p.Name).Append(": ").AppendLine(p.Description);
        }
    }

    private static string BuildDefaultsLine(List<ParamInfo> preferred)
    {
        var parts = new List<string>();
        foreach (var p in preferred)
        {
            if (string.IsNullOrWhiteSpace(p.Description))
                continue;
            // Extract any "Default ..." sentence from the controller XML doc, e.g.
            // "Log time range start. Default 1 day before endDate. (Preferred over 'filter')."
            var match = DefaultSentenceRegex.Match(p.Description);
            if (match.Success)
            {
                var text = match.Groups[1].Value.Trim().TrimEnd('.');
                parts.Add($"{p.Name} = {text}");
            }
        }
        return parts.Count == 0 ? null : string.Join("; ", parts);
    }

    private static readonly System.Text.RegularExpressions.Regex DefaultSentenceRegex =
        new(@"Default\s+([^.]+)\.",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.Compiled);

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

    private static void AppendSchemaSection(StringBuilder sb, string heading, ApiSchema schema, int depth, CatalogProvider catalog)
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
                AppendSchemaProperty(sb, property, indent: 4, depth: depth, catalog);

        sb.AppendLine("  Optional:");
        if (optional.Length == 0)
            sb.AppendLine("    None");
        else
            foreach (var property in optional)
                AppendSchemaProperty(sb, property, indent: 4, depth: depth, catalog);

        var hints = SchemaHints.GetHints(schema);
        if (hints != null)
        {
            sb.AppendLine().AppendLine("  AGENT HINTS:");
            foreach (var (propertyName, hint) in hints)
                sb.Append("    ").Append(propertyName).Append(": ").AppendLine(hint);
        }
    }

    // Inlines enum vocabularies up to this many values; beyond that, prints a pointer to
    // Safeguard_Enum so the agent can fetch the full list explicitly. Matches the threshold
    // recommended by the live-swagger inventory (134 enums; 8 exceed 30 values).
    private const int InlineEnumValueThreshold = 30;

    private static void AppendSchemaProperty(StringBuilder sb, SchemaProperty property, int indent, int depth, CatalogProvider catalog)
    {
        sb.Append(' ', indent).Append(property.Name).Append(" (").Append(property.Type).Append(')');
        if (!string.IsNullOrWhiteSpace(property.Description))
            sb.Append(" - ").Append(property.Description.Trim());
        sb.AppendLine();

        // Enum vocabularies: inline up to N values, otherwise point at Safeguard_Enum.
        if (!string.IsNullOrEmpty(property.EnumName) && catalog != null)
        {
            var values = catalog.GetEnum(property.EnumName);
            if (values != null && values.Length > 0)
            {
                if (values.Length <= InlineEnumValueThreshold)
                {
                    sb.Append(' ', indent + 2).Append("Allowed: ").AppendLine(string.Join(", ", values));
                }
                else
                {
                    sb.Append(' ', indent + 2)
                        .Append("Allowed: ").Append(values.Length)
                        .Append(" values — call Safeguard_Enum name=\"").Append(property.EnumName).AppendLine("\".");
                }
            }
        }

        // depth>1: walk parsed-time NestedProperties recursively. Fall back to the legacy
        // immediate-name list when no full child schemas were captured (e.g. unresolvable refs).
        if (depth > 1 && property.NestedProperties != null && property.NestedProperties.Length > 0)
        {
            foreach (var child in property.NestedProperties)
                AppendSchemaProperty(sb, child, indent + 2, depth - 1, catalog);
        }
        else if (property.NestedFields != null && property.NestedFields.Length > 0)
        {
            sb.Append(' ', indent + 2).Append("Fields: ").AppendLine(string.Join(", ", property.NestedFields));
        }
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
