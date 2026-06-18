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
    [Description("Authenticate to the Safeguard appliance and return the principal "
        + "(name, identity provider, token expiry). Optional in HTTP mode, where every tool "
        + "already executes against the appliance using the request bearer.")]
    public async Task<string> Safeguard_Connect(McpServer server, CancellationToken ct = default)
    {
        await session.EnsureReadyAsync(server, ct);
        var info = await session.GetPrincipalInfoAsync(ct);
        return FormatPrincipal(info, "Connected and authenticated");
    }

    [McpServerTool(Name = "Safeguard_Disconnect", Title = "Disconnect from Safeguard",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Log out of the current Safeguard session and invalidate the bearer at the appliance. "
        + "WARNING: any MCP client config still holding that bearer must drop it after this call.")]
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
    [Description("Find Safeguard API endpoints before calling Safeguard_Execute. Requires at least one "
        + "narrower (service=, search=/query=, or method=); a bare call returns usage examples instead of "
        + "the full catalog. Set verbose=true to include per-endpoint query-parameter detail. "
        + "Recipe matches and Batch* siblings are listed alongside results.")]
    public string Safeguard_Discover(
        [Description("Filter by service: 'Appliance', 'Core', or 'Notification'. Omit to search all.")] string service = null,
        [Description("Text to match in endpoint paths and summaries (case-insensitive; synonyms expand the search).")] string search = null,
        [Description("Filter by HTTP method: GET, POST, PUT, PATCH, or DELETE.")] string method = null,
        [Description("Alias for search=.")] string query = null,
        [Description("Include per-endpoint query-parameter detail. Default false; set true after narrowing.")] bool verbose = false)
    {
        var (effectiveSearch, aliasNotice) = ResolveDiscoverSearch(search, query);
        var body = FormatDiscovery(catalogProvider.GetEndpoints().ToArray(), service, effectiveSearch, method, verbose);
        return aliasNotice is null ? body : aliasNotice + "\n\n" + body;
    }

    /// <summary>Reconciles search/query inputs; query= is an alias for search=.</summary>
    internal static (string Search, string Notice) ResolveDiscoverSearch(string search, string query)
    {
        var s = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var q = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        if (s is null && q is null) return (null, null);
        if (s is null) return (q, null);
        if (q is null) return (s, null);
        if (s.Equals(q, StringComparison.OrdinalIgnoreCase)) return (s, null);
        return (s, "Notice: both search= and query= were supplied with different values; using search=. "
                 + "The 'query' alias maps to 'search' — pass only one.");
    }

    internal static string FormatDiscovery(ApiEndpoint[] results, string service, string search, string method)
        => FormatDiscovery(results, service, search, method, verbose: false);

    /// <summary>
    /// Pure formatter for <c>Safeguard_Discover</c>; isolated so unit tests can
    /// exercise the recipe-ranking and synonym-expansion behavior without
    /// having to construct the full DI graph.
    /// </summary>
    /// <remarks>
    /// Two listing modes:
    ///   - <c>verbose=false</c> (default): one line per endpoint, cap
    ///     <see cref="MaxRowsCompact"/>. Designed to fit the host render
    ///     budget for typical broad searches (e.g. "audit log" GET).
    ///   - <c>verbose=true</c>: emits per-endpoint parameter details, cap
    ///     <see cref="MaxRowsVerbose"/>; if matches exceed the cap the
    ///     output short-circuits to a paths-only listing prompting the
    ///     caller to narrow further before requesting details.
    /// A producer-side size guard (<see cref="MaxBytes"/>) trims trailing
    /// rows and prepends a notice naming the available narrowers, so
    /// pathological catalogs can't blow the host render budget.
    /// </remarks>
    internal static string FormatDiscovery(ApiEndpoint[] results, string service, string search, string method, bool verbose)
    {
        const int MaxRowsCompact = 200;
        const int MaxRowsVerbose = 20;
        const int MaxBytes = 18 * 1024;

        // No narrowers: dumping the ~1000-endpoint catalog is unactionable.
        if (string.IsNullOrWhiteSpace(service)
            && string.IsNullOrWhiteSpace(search)
            && string.IsNullOrWhiteSpace(method))
        {
            return BuildNoNarrowerDirective(results.Length);
        }

        var sb = new StringBuilder();
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

        // verbose=true with a wide match set: short-circuit to paths-only
        // and prompt the caller to narrow further. Per-param detail blocks
        // are the dominant byte source on broad searches.
        if (verbose && matches.Count > MaxRowsVerbose)
        {
            sb.Append("Too many matches (").Append(matches.Count)
                .Append(") for verbose=true (cap ").Append(MaxRowsVerbose)
                .AppendLine("). Listing paths only — narrow further before requesting details.");
            int pathsShown = 0;
            foreach (var (_, idx) in matches)
            {
                if (pathsShown >= MaxRowsCompact) break;
                ref readonly var ep = ref results[idx];
                sb.Append(ep.Method.PadRight(7));
                sb.Append(ep.Service.PadRight(13));
                sb.AppendLine(ep.Path);
                pathsShown++;
            }
            if (matches.Count > pathsShown)
                sb.AppendLine().Append("... and ").Append(matches.Count - pathsShown).Append(" more.");
            sb.AppendLine();
            sb.Append("Narrow with service=, method=, or a more specific search= term, then re-run with verbose=true.");
            return ApplySizeGuard(sb, matches.Count, MaxBytes);
        }

        // Shape hint for wide compact results: list the most common 2-segment
        // path prefixes so the agent has a vocabulary for narrowing.
        if (!verbose && matches.Count > 60)
        {
            var prefixes = TopPathPrefixes(results, matches, max: 4);
            if (prefixes.Count > 0)
            {
                sb.Append("Matched ").Append(matches.Count).Append(" endpoints across the {")
                    .Append(string.Join(", ", prefixes))
                    .AppendLine("} path prefixes. Common narrowers: service=Core, method=GET, search='<refined-term>'.");
            }
        }

        int effectiveLimit = verbose ? MaxRowsVerbose : MaxRowsCompact;
        int shown = 0;
        int truncatedAtBytes = -1;
        foreach (var (_, idx) in matches)
        {
            if (shown >= effectiveLimit) break;

            int rowStart = sb.Length;
            ref readonly var ep = ref results[idx];
            sb.Append(ep.Method.PadRight(7));
            sb.Append(ep.Service.PadRight(13));
            sb.Append(ep.Path);
            if (ep.HasBody) sb.Append("  [body]");
            sb.Append("  -- ").AppendLine(ep.Summary);
            if (verbose)
                AppendParameterDetails(sb, ep);

            if (sb.Length > MaxBytes)
            {
                // Roll back the row that pushed us over the budget.
                sb.Length = rowStart;
                truncatedAtBytes = shown;
                break;
            }
            shown++;
        }

        if (truncatedAtBytes < 0 && matches.Count > shown)
            sb.AppendLine().Append("... and ").Append(matches.Count - shown).Append(" more. Narrow your search with service, method, or more specific search text.");

        sb.AppendLine();
        sb.Append("Tip: Use Safeguard_Schema to see request/response body format for POST/PUT endpoints.");

        return ApplySizeGuard(sb, matches.Count, MaxBytes, truncatedAtBytes);
    }

    /// <summary>Directive returned when Safeguard_Discover is called with no narrowers.</summary>
    internal static string BuildNoNarrowerDirective(int totalEndpoints)
    {
        var sb = new StringBuilder();
        sb.Append("Safeguard_Discover requires at least one narrower (service, search/query, or method). ")
          .Append("The catalog has ").Append(totalEndpoints)
          .AppendLine(" endpoints across the Core, Appliance, and Notification services.")
          .AppendLine()
          .AppendLine("Examples:")
          .AppendLine("  Safeguard_Discover service=\"Appliance\"             — appliance-level endpoints (status, time, version, health)")
          .AppendLine("  Safeguard_Discover search=\"users\"                  — user, group, and identity-provider endpoints")
          .AppendLine("  Safeguard_Discover service=\"Core\" method=\"POST\"    — write endpoints in Core")
          .AppendLine("  Safeguard_Discover search=\"audit log\"              — audit log endpoints")
          .AppendLine("  Safeguard_Discover search=\"uptime\"                 — appliance status / time / version / health")
          .AppendLine()
          .AppendLine("For 'tell me about this appliance' style questions, start with service=\"Appliance\".")
          .Append("Tip: query= is accepted as an alias for search=.");
        return sb.ToString();
    }

    /// <summary>
    /// Defense-in-depth size guard: if the rendered buffer still exceeds
    /// <paramref name="maxBytes"/> after the row loop's per-row check
    /// (e.g. the recipe + shape-hint preamble alone is large), trim from
    /// the tail and prepend a single notice naming the available
    /// Safeguard_Discover narrowers.
    /// </summary>
    private static string ApplySizeGuard(StringBuilder sb, int totalMatches, int maxBytes, int truncatedAt = -1)
    {
        bool overBudget = sb.Length > maxBytes;
        if (!overBudget && truncatedAt < 0)
            return sb.ToString();

        if (overBudget)
        {
            // Trim hard to the budget, then back off to the previous line break
            // so the output ends cleanly.
            sb.Length = maxBytes;
            for (int i = sb.Length - 1; i > 0; i--)
            {
                if (sb[i] == '\n')
                {
                    sb.Length = i;
                    break;
                }
            }
        }

        var notice = new StringBuilder();
        notice.Append("Output truncated to fit host render limit. Showing top results of ")
            .Append(totalMatches).AppendLine(" matches.")
            .AppendLine("Narrow with Safeguard_Discover narrowers: search=, method=, or service=. Set verbose=false for the compact one-line-per-endpoint view.")
            .AppendLine();
        sb.Insert(0, notice.ToString());
        return sb.ToString();
    }

    /// <summary>
    /// Returns up to <paramref name="max"/> most-common 2-segment path
    /// prefixes (e.g. "/v4/Users") across the matched endpoints, ranked
    /// by frequency.
    /// </summary>
    private static List<string> TopPathPrefixes(ApiEndpoint[] results, List<(int Score, int Index)> matches, int max)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, idx) in matches)
        {
            var path = results[idx].Path;
            if (string.IsNullOrEmpty(path)) continue;
            // Path looks like "/v4/Users/{id}" — take the first two non-empty segments.
            int first = path.IndexOf('/', 1);
            if (first <= 0) continue;
            int second = path.IndexOf('/', first + 1);
            string prefix = second < 0 ? path : path.Substring(0, second);
            counts.TryGetValue(prefix, out var c);
            counts[prefix] = c + 1;
        }
        return counts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(max)
            .Select(kv => kv.Key)
            .ToList();
    }

    private static void AppendRecipeBlock(StringBuilder sb, IReadOnlyList<RecipeMatch> matches, bool includeStrongCallout)
    {
        var strong = matches.FirstOrDefault(m => m.Strong);
        if (includeStrongCallout && strong != null)
        {
            sb.Append("Strong recipe match: ").Append(strong.Recipe.Id)
                .Append(" -- ").AppendLine(strong.Recipe.Name);
            sb.Append("  Call: Safeguard_Reference topic=workflows id=\"").Append(strong.Recipe.Id).AppendLine("\"");
            if (!string.IsNullOrWhiteSpace(strong.Recipe.Tool))
            {
                sb.Append("  Or call the composite tool directly: ")
                    .AppendLine(strong.Recipe.Tool);
            }
            sb.AppendLine();
        }

        sb.AppendLine("Recipes that match (call Safeguard_Reference topic=workflows id=\"<id>\"):");
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
    [Description("Call any Safeguard API endpoint. Path must start with /v4/...; the service "
        + "(Core/Appliance/Notification) is auto-detected — do not add a /service/{name}/ prefix. "
        + "Use Safeguard_Discover to find endpoints and Safeguard_Schema for request-body shape. "
        + "JSON responses are a { data, meta } envelope (meta carries notices, paging, truncation); "
        + "responses are capped (~30 KB), so project with fields= or page via meta.paging.next. "
        + "For repeated writes to one collection, check for a Batch* sibling (Discover search='Batch'). "
        + "Sensitive credential endpoints are not callable here — they refuse-and-redirect to "
        + "Safeguard_RetrieveCredential.")]
    public async Task<string> Safeguard_Execute(McpServer server,
        [Description("HTTP method: GET, POST, PUT, PATCH, or DELETE.")] string method,
        [Description("API path, e.g. '/v4/Users'. Must start with /v4/...; service auto-detected (no /service/{name}/ prefix).")] string path,
        [Description("Query parameters (e.g. 'fields=Id,Name&filter=Name eq \"x\"'). Omit if none.")] string query = null,
        [Description("JSON request body for POST/PUT/PATCH. Omit for GET/DELETE.")] string body = null,
        [Description("Response format: 'json' (default) or 'csv' (GET-only, tabular).")]
        string format = "json",
        CancellationToken ct = default)
        => await DispatchAsync(server, method, path, query, body, format, ct);

    [McpServerTool(Name = "Safeguard_Schema", Title = "Get API Schema",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get the request-body and response schema (property names, types, required fields) for an "
        + "endpoint. Call before POST or PUT. Set depth (max 3) to expand nested object/array properties.")]
    public string Safeguard_Schema(
        [Description("API path, e.g. '/v4/AssetAccounts'. Must start with /v4/... (no /service/{name}/ prefix).")] string path,
        [Description("HTTP method: POST, PUT, or GET (for response schema). Default: POST")] string method = "POST",
        [Description("Levels to expand nested object/array properties. Default 1, max 3.")] int depth = 1)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new McpException("The 'path' parameter is required (e.g. '/v4/AssetAccounts').");

        if (ApiToolHelpers.TryDetectServicePrefix(path, out var strippedPath, out var detectedService))
            throw new McpException(ApiToolHelpers.BuildServicePrefixDirective(path, strippedPath, detectedService));

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

    [McpServerTool(Name = "Safeguard_Reference", Title = "Safeguard Reference",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("On-demand Safeguard reference. topic = query-syntax | workflows | enum | "
        + "terminology | overview | common-patterns (omit to list them). For doc topics, pass "
        + "search= to return only the matching section instead of the whole document.")]
    public string Safeguard_Reference(
        [Description("Reference source (see tool description). Omit to list topics.")]
        string topic = null,
        [Description("Keyword filter. Doc topics return just the matching section; workflows filters recipes.")]
        string search = null,
        [Description("workflows: exact recipe id (e.g. 'health-check').")]
        string id = null,
        [Description("enum: schema name (e.g. 'AccessRequestType'). Omit to list enum names.")]
        string name = null,
        [Description("enum: case-insensitive substring filter for long value lists.")]
        string pattern = null,
        [Description("query-syntax: endpoint path to list its live filterable fields.")]
        string path = null,
        [Description("enum: maximum values to return. Default 50.")]
        int limit = 50)
    {
        switch (topic?.Trim().ToLowerInvariant())
        {
            case null or "":
                return ReferenceTopicIndex();
            case "query-syntax" or "query" or "filter":
                return QuerySyntaxReference(search, path);
            case "workflows" or "workflow" or "recipes":
                return SafeguardWorkflows.Lookup(search, id);
            case "enum" or "enums":
                return EnumLookup(name, pattern, limit);
            case "terminology" or "terms":
                return MarkdownSections.Select("terminology.md", "terminology", search);
            case "overview" or "api-overview":
                return MarkdownSections.Select("api-overview.md", "overview", search);
            case "common-patterns" or "patterns":
                return MarkdownSections.Select("common-patterns.md", "common-patterns", search);
            default:
                return $"Unknown topic '{topic}'.\n\n" + ReferenceTopicIndex();
        }
    }

    private static string ReferenceTopicIndex() =>
        "Safeguard_Reference topics (set topic=):\n"
        + "  query-syntax     filter/field/order/paging rules (path= for an endpoint's live fields)\n"
        + "  workflows        multi-step recipes (search= to find, id= for one)\n"
        + "  enum             allowed values for an enum schema (name=, pattern=, limit=)\n"
        + "  terminology      UI/product term -> API endpoint\n"
        + "  overview         API surface overview\n"
        + "  common-patterns  lookup-by-name, bulk ops, access-request lifecycle, sensitive material\n"
        + "Pass search=\"<keyword>\" on a doc topic to get only the matching section.";

    private string QuerySyntaxReference(string search, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return MarkdownSections.Select("query-syntax.md", "query-syntax", search);

        var sb = new StringBuilder();
        if (ApiToolHelpers.TryDetectServicePrefix(path, out var strippedPath, out var detectedService))
        {
            sb.AppendLine(ApiToolHelpers.BuildServicePrefixDirective(path, strippedPath, detectedService));
        }
        else
        {
            var normalizedPath = NormalizePath(path);
            var service = ResolveService(normalizedPath);
            var serviceName = GetServiceName(service);
            var schema = catalogProvider.GetResponseSchema("GET", serviceName, normalizedPath)
                ?? catalogProvider.GetSchema("GET", serviceName, normalizedPath);

            if (schema != null && schema.Value.Properties.Length > 0)
            {
                var propertyNames = schema.Value.Properties.Select(p => p.Name).ToArray();
                sb.Append("Top-level fields for ").Append(normalizedPath).Append(" (").Append(serviceName).AppendLine("):")
                    .Append("  ").AppendLine(string.Join(", ", propertyNames));
            }
            else
            {
                sb.Append("No schema field list is available for ").Append(normalizedPath).AppendLine(".");
            }
        }

        sb.AppendLine()
            .Append("For filter/field/order/paging rules call Safeguard_Reference topic=query-syntax search=\"<keyword>\".");
        return sb.ToString().TrimEnd();
    }

    private string EnumLookup(string name, string pattern, int limit)
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
            sb0.AppendLine().AppendLine("Call Safeguard_Reference topic=enum name=\"<Name>\" to see allowed values.");
            return sb0.ToString().TrimEnd();
        }

        var values = catalogProvider.GetEnum(name);
        if (values == null)
        {
            return $"No enum named '{name}' is available. "
                + "Call Safeguard_Reference topic=enum (no name) to list known enum names. "
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

        if (ApiToolHelpers.TryDetectServicePrefix(path, out var strippedPath, out var detectedService))
            throw new McpException(ApiToolHelpers.BuildServicePrefixDirective(path, strippedPath, detectedService));

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

        var offerNotice = BuildSessionLaunchOfferNotice(method, path);
        if (offerNotice != null)
            notices.Add(offerNotice);

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
            : "Use Safeguard_Reference topic=workflows id=\"session-access-request\" for the full launch workflow.";

        return new Notice(
            NoticeKinds.WorkflowRecipeSuggested,
            "Related workflow recipe: session-access-request (composite tools: "
            + "Safeguard_OpenAccessRequest, Safeguard_CloseAccessRequest).",
            suggestion);
    }

    /// <summary>
    /// Emits the session_token_issued_offer_to_launch notice ONLY when the caller
    /// hits POST /v4/AccessRequests/{id}/InitializeSession. The notice names the
    /// "present + offer + ask, never auto-launch" convention so the agent renders
    /// the SessionsLaunchData block consistently. Credential-checkout leaves
    /// (CheckOutPassword / CheckOutSshKey / CheckOutApiKeys / CheckOutFile) flow
    /// through Safeguard_RetrieveCredential, which tags the plaintext for the
    /// user audience and is meant to be handed to the human rather than re-used
    /// as input to a connection step, so this notice intentionally does NOT fire
    /// on those paths.
    /// </summary>
    internal static Notice BuildSessionLaunchOfferNotice(string method, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var verb = string.IsNullOrWhiteSpace(method) ? "POST" : method.ToUpperInvariant();
        if (verb != "POST")
            return null;

        var segments = GetPathSegments(path);
        if (segments.Length < 4)
            return null;

        if (!segments.Any(s => s.Equals("AccessRequests", StringComparison.OrdinalIgnoreCase)))
            return null;

        if (!segments[^1].Equals("InitializeSession", StringComparison.OrdinalIgnoreCase))
            return null;

        var requestId = segments[^2];

        var message =
            "Session token issued for request " + requestId + ". The credential never enters this "
            + "agent context — Safeguard injects it at the proxy. Present the user BOTH the manual "
            + "launch command (and/or ConnectionUri) AND an explicit offer to launch the session on "
            + "their behalf. Do not auto-launch without explicit consent. Use "
            + "Safeguard_CloseAccessRequest requestId=" + requestId + " when the user is done (or "
            + "accept the policy-driven idle timeout).";

        var suggestion =
            "Format: short connection block + \"Want me to launch it for you?\" + the request id ("
            + requestId + ") for later check-in. On Windows use Start-Process ssh / mstsc; on POSIX "
            + "use ssh / open / xfreerdp / Microsoft Remote Desktop / the SCALUS handler. RDP types: "
            + "save RdpConnectionFile to disk and run mstsc against the saved file — do not hand-build "
            + "the .rdp file.";

        return new Notice(NoticeKinds.SessionTokenIssuedOfferToLaunch, message, suggestion);
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

    private (string TemplatePath, bool TemplateMatched, ApiSchemaPropertyPath[] Paths) ResolveErrorContext(
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

        var matched = templatePath != null;
        templatePath ??= requestPath;

        var response = catalogProvider.GetResponseSchema(method, serviceName, templatePath)
            ?? catalogProvider.GetResponseSchema("GET", serviceName, templatePath);
        var request = catalogProvider.GetSchema(method, serviceName, templatePath);

        ApiSchemaPropertyPath[] paths = null;
        if (response != null && response.Value.Paths != null && response.Value.Paths.Length > 0)
            paths = response.Value.Paths;
        else if (request != null && request.Value.Paths != null && request.Value.Paths.Length > 0)
            paths = request.Value.Paths;

        return (templatePath, matched, paths ?? Array.Empty<ApiSchemaPropertyPath>());
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

    internal static bool TryParseErrorBody(string message, out string apiMessage, out string apiCode, out string innerError)
    {
        apiMessage = null;
        apiCode = null;
        innerError = null;

        if (string.IsNullOrWhiteSpace(message))
            return false;

        // Belt-and-suspenders: SafeguardInvoker now prefers
        // SafeguardDotNetException.Response so the body usually
        // arrives bare. If a future SDK (or a code path we missed)
        // hands us wrapper prose like "Error returned from Safeguard
        // API, Error: BadRequest {…}", scan to the first '{' instead
        // of giving up because position 0 isn't JSON.
        var braceIndex = message.IndexOf('{');
        if (braceIndex < 0)
            return false;

        var jsonCandidate = braceIndex == 0 ? message : message[braceIndex..];

        try
        {
            using var document = JsonDocument.Parse(jsonCandidate);
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
        var (templatePath, templateMatched, paths) = ResolveErrorContext(method, serviceName, requestPath);
        var ctx = new ErrorContext(serviceName, method, templatePath);

        string[] pathSuggestions = Array.Empty<string>();
        string[] supportedMethods = Array.Empty<string>();
        if (statusCode == 404 && !templateMatched)
        {
            pathSuggestions = EndpointPathSuggester.Suggest(method, requestPath, catalogProvider.GetEndpoints());
        }
        else if (statusCode == 405 && templateMatched)
        {
            supportedMethods = CollectSupportedMethods(serviceName, templatePath);
        }

        var hint = ApiToolHelpers.GetErrorHint(
            statusCode, apiMessage, hasModelState, ctx, paths,
            requestPath, templateMatched, pathSuggestions, supportedMethods);
        if (!string.IsNullOrWhiteSpace(hint))
            return hint;

        if (rawMessage != null
            && rawMessage.Contains("Authentication expired", StringComparison.OrdinalIgnoreCase))
        {
            return "Token expired. Call Safeguard_Connect to re-authenticate.";
        }

        return null;
    }

    private string[] CollectSupportedMethods(string serviceName, string templatePath)
    {
        var endpoints = catalogProvider.GetEndpoints();
        var methods = new List<string>(4);
        for (int i = 0; i < endpoints.Length; i++)
        {
            ref readonly var endpoint = ref endpoints[i];
            if (!endpoint.Service.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!endpoint.Path.Equals(templatePath, StringComparison.OrdinalIgnoreCase))
                continue;
            var upper = endpoint.Method.ToUpperInvariant();
            if (!methods.Contains(upper))
                methods.Add(upper);
        }
        methods.Sort(StringComparer.Ordinal);
        return methods.ToArray();
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
    // Safeguard_Reference topic=enum so the agent can fetch the full list explicitly. Matches the threshold
    // recommended by the live-swagger inventory (134 enums; 8 exceed 30 values).
    private const int InlineEnumValueThreshold = 30;

    private static void AppendSchemaProperty(StringBuilder sb, SchemaProperty property, int indent, int depth, CatalogProvider catalog)
    {
        sb.Append(' ', indent).Append(property.Name).Append(" (").Append(property.Type).Append(')');
        if (!string.IsNullOrWhiteSpace(property.Description))
            sb.Append(" - ").Append(property.Description.Trim());
        sb.AppendLine();

        // Enum vocabularies: inline up to N values, otherwise point at Safeguard_Reference topic=enum.
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
                        .Append(" values — call Safeguard_Reference topic=enum name=\"").Append(property.EnumName).AppendLine("\".");
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