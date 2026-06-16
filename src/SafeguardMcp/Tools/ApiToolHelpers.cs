using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tools;

/// <summary>
/// Endpoint context threaded into <see cref="ApiToolHelpers.GetErrorHint(int, string, bool, ErrorContext, ApiSchemaPropertyPath[])"/>
/// so a 70001 / 70002 / 70009 hint can name the rejected token, the
/// endpoint, and the closest valid property pulled from the catalog's
/// already-extracted path graph. <see cref="Path"/> is the swagger
/// template path (e.g. <c>/v4/AssetAccounts/{id}</c>) when one was
/// matched, otherwise the concrete request path -- both are safe to
/// echo back to the agent.
/// </summary>
internal readonly record struct ErrorContext(
    string Service,
    string Method,
    string Path);

/// <summary>
/// Pure helper functions extracted from SafeguardApiTool for testability.
/// All methods are stateless and operate only on their inputs.
/// </summary>
internal static class ApiToolHelpers
{
    // --- Path matching and normalization ---

    internal static string NormalizePath(string path)
    {
        var normalized = path.Trim();
        var queryIndex = normalized.IndexOf('?');
        if (queryIndex >= 0)
            normalized = normalized[..queryIndex];
        if (!normalized.StartsWith('/'))
            normalized = "/" + normalized;
        return normalized;
    }

    /// <summary>Detects a leading /service/{name}/ prefix and returns the bare /v4/... form.</summary>
    internal static bool TryDetectServicePrefix(string rawPath, out string strippedPath, out string serviceName)
    {
        strippedPath = null;
        serviceName = null;
        if (string.IsNullOrWhiteSpace(rawPath))
            return false;

        var p = rawPath.Trim();
        var qIdx = p.IndexOf('?');
        if (qIdx >= 0)
            p = p[..qIdx];

        var segs = p.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segs.Length < 2 || !segs[0].Equals("service", StringComparison.OrdinalIgnoreCase))
            return false;

        var canonical = segs[1].ToLowerInvariant() switch
        {
            "core" => "Core",
            "appliance" => "Appliance",
            "notification" => "Notification",
            "a2a" => "A2A",
            "rsts" => "Rsts",
            _ => null
        };
        if (canonical is null)
            return false;

        serviceName = canonical;
        strippedPath = segs.Length > 2 ? "/" + string.Join('/', segs[2..]) : "/";
        return true;
    }

    /// <summary>Directive shown when a request includes a /service/{name}/ prefix.</summary>
    internal static string BuildServicePrefixDirective(string rawPath, string strippedPath, string serviceName)
        => $"Path '{rawPath}' includes a '/service/{{name}}/' prefix. "
         + $"Use path=\"{strippedPath}\" instead — Safeguard_Execute auto-routes to the correct service "
         + $"(here: {serviceName}) from the bare /v4/... path. Appliance URLs include /service/{{Name}}/v4/, "
         + "but the tool prepends that for you.";

    internal static string[] GetPathSegments(string path) => NormalizePath(path)
        .Trim('/')
        .Split('/', StringSplitOptions.RemoveEmptyEntries);

    internal static bool PathsMatch(string catalogPath, string actualPath)
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

    internal static bool IsPlaceholder(string segment) => segment.StartsWith('{') && segment.EndsWith('}');

    internal static bool EndsWithPlaceholder(string path)
    {
        var segments = GetPathSegments(path);
        return segments.Length > 0 && IsPlaceholder(segments[^1]);
    }

    internal static bool LooksLikeId(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || IsPlaceholder(segment))
            return true;
        if (int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return true;
        if (long.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return true;
        return Guid.TryParse(segment, out _);
    }

    // --- Service routing ---

    internal static Service ResolveServiceHeuristic(string path)
    {
        // Check Notification (Status) first — /v4/Status sub-paths should not be
        // caught by Appliance keyword matching (e.g. /v4/Status/ClusterPatch contains "Patch")
        if (path.Equals("/v4/Status", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/v4/Status/", StringComparison.OrdinalIgnoreCase))
        {
            return Service.Notification;
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

        return Service.Core;
    }

    internal static Service ParseServiceName(string service) => service.ToUpperInvariant() switch
    {
        "APPLIANCE" => Service.Appliance,
        "CORE" => Service.Core,
        "NOTIFICATION" => Service.Notification,
        _ => Service.Core
    };

    internal static string GetServiceName(Service service) => service switch
    {
        Service.Appliance => "Appliance",
        Service.Notification => "Notification",
        _ => "Core"
    };

    // --- Response truncation ---

    internal static bool TryTruncateJsonArray(string body, int maxItems, out string truncatedBody, out int totalItems)
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

            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartArray();
                foreach (var item in items)
                    item.WriteTo(writer);
                writer.WriteEndArray();
            }
            truncatedBody = Encoding.UTF8.GetString(buffer.ToArray());
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    internal enum TruncationOutcome
    {
        /// <summary>Body was not a JSON array; caller should leave it intact.</summary>
        NotArray,
        /// <summary>Array fit under both budgets unchanged.</summary>
        WithinBudget,
        /// <summary>Records were dropped (either by maxItems or by maxBytes); JSON remains valid.</summary>
        RecordsDropped,
        /// <summary>A single record alone exceeds maxBytes; returned intact (no mid-record cut).</summary>
        RecordTooLarge
    }

    /// <summary>
    /// Whole-record-boundary truncation for JSON-array responses, enforcing both an
    /// item-count budget (<paramref name="maxItems"/>) and a byte budget (<paramref name="maxBytes"/>)
    /// while never producing a mid-object cut. The JSON output is always a valid array.
    /// </summary>
    /// <param name="body">Raw JSON response body.</param>
    /// <param name="maxItems">Hard cap on the number of items kept.</param>
    /// <param name="maxBytes">Soft cap on the serialized byte length; records are dropped from the tail until the budget is met (with a one-record floor).</param>
    /// <param name="resultBody">When the outcome is anything other than <see cref="TruncationOutcome.NotArray"/>, the (possibly trimmed) JSON array body. Otherwise null.</param>
    /// <param name="totalItems">Total number of items the upstream response contained.</param>
    /// <param name="keptItems">Number of items present in <paramref name="resultBody"/> after truncation.</param>
    internal static TruncationOutcome TryTruncateJsonArrayWithBudget(
        string body,
        int maxItems,
        int maxBytes,
        out string resultBody,
        out int totalItems,
        out int keptItems)
    {
        resultBody = null;
        totalItems = 0;
        keptItems = 0;

        if (string.IsNullOrWhiteSpace(body))
            return TruncationOutcome.NotArray;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return TruncationOutcome.NotArray;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return TruncationOutcome.NotArray;

            var items = new List<JsonElement>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                totalItems++;
                items.Add(item.Clone());
            }

            var droppedByItemCap = false;
            if (items.Count > maxItems)
            {
                items.RemoveRange(maxItems, items.Count - maxItems);
                droppedByItemCap = true;
            }

            // Drop from the tail until the serialized array fits the byte budget.
            // Always keep at least one record so we never emit "[]" when the upstream had data.
            var droppedByByteCap = false;
            string serialized = SerializeArray(items);
            while (items.Count > 1 && serialized.Length > maxBytes)
            {
                items.RemoveAt(items.Count - 1);
                droppedByByteCap = true;
                serialized = SerializeArray(items);
            }

            keptItems = items.Count;
            resultBody = serialized;

            if (items.Count == 1 && serialized.Length > maxBytes)
                return TruncationOutcome.RecordTooLarge;

            if (droppedByItemCap || droppedByByteCap)
                return TruncationOutcome.RecordsDropped;

            return TruncationOutcome.WithinBudget;
        }
    }

    private static string SerializeArray(List<JsonElement> items)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            foreach (var item in items)
                item.WriteTo(writer);
            writer.WriteEndArray();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    // --- Error parsing ---

    internal static int ExtractStatusCode(string message)
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

    internal static string ExtractErrorDetail(string message)
    {
        var separatorIndex = message.IndexOf("):", StringComparison.Ordinal);
        return separatorIndex >= 0 ? message[(separatorIndex + 2)..].Trim() : message.Trim();
    }

    internal static string GetErrorHint(int statusCode) => statusCode switch
    {
        400 => "Check request body format. Use Safeguard_Schema to see required fields.",
        401 => "Token expired. Call Safeguard_Connect to re-authenticate.",
        403 => "Insufficient permissions for this operation.",
        404 => "Resource not found. Verify the ID exists using a GET call.",
        409 => "Conflict. GET the current state first, then retry.",
        422 => "Validation failed. Check property types match the schema.",
        _ => null
    };

    /// <summary>
    /// Context-aware hint that inspects the parsed Safeguard error message and surfaced
    /// validation state to return more specific guidance for known failure shapes.
    /// </summary>
    internal static string GetErrorHint(int statusCode, string apiMessage, bool hasModelState)
    {
        if (statusCode == 400
            && !string.IsNullOrWhiteSpace(apiMessage)
            && apiMessage.Contains("Invalid order by property", StringComparison.OrdinalIgnoreCase))
        {
            return "Safeguard orderby uses a leading minus for descending (orderby=-Field), not OData ('Field desc'/'Field asc').";
        }

        if (statusCode == 400
            && !string.IsNullOrWhiteSpace(apiMessage)
            && apiMessage.Contains("Invalid field property", StringComparison.OrdinalIgnoreCase))
        {
            var badField = ExtractQuotedToken(apiMessage);
            if (!string.IsNullOrWhiteSpace(badField) && badField.Contains('.'))
            {
                return "Dotted field selection only works for to-one nav properties (e.g. Asset.Name). "
                    + "For child collections, call the sub-resource endpoint (GET /v4/<parent>/{id}/<collection>) instead of dotting into them.";
            }

            return "Run Safeguard_Schema for this endpoint to see valid property paths. "
                + "Safeguard exposes parent relationships as nested objects (e.g. Asset.Id, Asset.Name), not flat foreign-key columns.";
        }

        if (statusCode == 400 && hasModelState)
            return "Fix the fields listed under 'Validation errors' and retry.";

        return GetErrorHint(statusCode);
    }

    /// <summary>
    /// Context- and graph-aware hint for the 70001 / 70002 / 70009 family of
    /// filter / orderby / fields property-name errors. The suggester pulls
    /// candidates only from <paramref name="paths"/> (the endpoint's schema
    /// property-path graph) and from the operator vocabulary on disk -- it
    /// never invents a path. When no candidate scores above threshold the
    /// caller's hint falls back to "use Safeguard_Schema" rather than guessing.
    /// </summary>
    internal static string GetErrorHint(
        int statusCode,
        string apiMessage,
        bool hasModelState,
        ErrorContext ctx,
        ApiSchemaPropertyPath[] paths,
        string requestPath = null,
        bool templateMatched = true,
        string[] pathSuggestions = null,
        string[] supportedMethods = null)
    {
        if (statusCode == 404 && !templateMatched && !string.IsNullOrWhiteSpace(requestPath))
        {
            var sb404 = new StringBuilder();
            sb404.Append("No endpoint at ").Append(requestPath).Append('.');
            if (pathSuggestions != null && pathSuggestions.Length > 0)
            {
                sb404.Append(" Did you mean ");
                for (int i = 0; i < pathSuggestions.Length; i++)
                {
                    if (i > 0) sb404.Append(", ");
                    sb404.Append('`').Append(pathSuggestions[i]).Append('`');
                }
                sb404.Append("? Use Safeguard_Discover to browse other endpoints.");
            }
            else
            {
                sb404.Append(" Use Safeguard_Discover to find the right path.");
            }
            return sb404.ToString();
        }

        if (statusCode == 405 && !string.IsNullOrWhiteSpace(requestPath))
        {
            var sb405 = new StringBuilder();
            sb405.Append("Method ").Append(ctx.Method).Append(" not supported at ")
                 .Append(requestPath).Append('.');
            if (supportedMethods != null && supportedMethods.Length > 0)
            {
                sb405.Append(" Supported: ");
                for (int i = 0; i < supportedMethods.Length; i++)
                {
                    if (i > 0) sb405.Append(", ");
                    sb405.Append(supportedMethods[i]);
                }
                sb405.Append('.');
            }
            else
            {
                sb405.Append(" Use Safeguard_Discover for endpoints that accept this method.");
            }
            return sb405.ToString();
        }

        if (statusCode == 400 && !string.IsNullOrWhiteSpace(apiMessage))
        {
            if (apiMessage.Contains("Invalid order by property", StringComparison.OrdinalIgnoreCase))
                return BuildPropertyHint(apiMessage, paths, QueryParamKind.OrderBy, ctx);
            if (apiMessage.Contains("Invalid filter property", StringComparison.OrdinalIgnoreCase))
                return BuildPropertyHint(apiMessage, paths, QueryParamKind.Filter, ctx);
            if (apiMessage.Contains("Invalid field property", StringComparison.OrdinalIgnoreCase))
                return BuildPropertyHint(apiMessage, paths, QueryParamKind.Fields, ctx);
        }

        // Access-request 90408: "not authorized to use this request type" — wire-accurate but
        // misleading. The real cause is almost always (account, asset) selected ≠ asset the
        // policy is scoped to. Surface the entitlements check that resolves it, and point at
        // the composite tool that pre-validates the same combination.
        if (!string.IsNullOrWhiteSpace(apiMessage)
            && (apiMessage.Contains("not authorized to use this request type", StringComparison.OrdinalIgnoreCase)
                || apiMessage.Contains("90408", StringComparison.Ordinal)))
        {
            return "Appliance error 90408 ('not authorized to use this request type') usually means "
                + "the (AccountId, AssetId) pair you posted does not have a policy of the requested "
                + "AccessRequestType — the entitlement exists for that account but on a different asset. "
                + "Run Safeguard_Execute method=GET path=/v4/Me/RequestEntitlements "
                + "query=accountIds=<id>&accessRequestType=<type> to see which asset the entitlement is "
                + "scoped to, then re-POST with that AssetId. Or use Safeguard_OpenAccessRequest, "
                + "which performs this pre-flight check automatically.";
        }

        // Dependency-blocked delete (50104): "This object is referenced by ...". When the
        // blocker is an active AccessRequest, the wire message embeds the request id and state
        // (verified at PangaeaAppliance/src/Data/Middleware/Core/V4/System/AssetLogic.cs and
        // /Accounts/AssetAccountLogic.cs). Surface the id so the agent can close it via
        // Safeguard_CloseAccessRequest, and nudge the multi-row case toward BatchDelete to
        // avoid a second N-call fan-out.
        if (!string.IsNullOrWhiteSpace(apiMessage)
            && apiMessage.Contains("This object is referenced by AccessRequest", StringComparison.OrdinalIgnoreCase))
        {
            var requestId = ExtractAccessRequestId(apiMessage);
            var sb50104 = new StringBuilder();
            sb50104.Append("Appliance error 50104: this object is referenced by ");
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                sb50104.Append("AccessRequest ").Append(requestId).Append(". Close (or wait for) the access request first ")
                    .Append("(Safeguard_CloseAccessRequest requestId=").Append(requestId).Append("), then retry. ");
            }
            else
            {
                sb50104.Append("an active AccessRequest. Close (or wait for) the named access request first ")
                    .Append("(Safeguard_CloseAccessRequest requestId=<id>), then retry. ");
            }
            sb50104.Append("For multi-row cleanups, batch the unblocked ids via POST /v4/")
                .Append(GetBulkResourceFromContext(ctx))
                .Append("/BatchDelete to avoid a second N-call fan-out.");
            return sb50104.ToString();
        }

        return GetErrorHint(statusCode, apiMessage, hasModelState);
    }

    /// <summary>
    /// Pulls the AccessRequest id out of a 50104 wire message of the form
    /// "This object is referenced by AccessRequest &lt;id&gt;  (&lt;State&gt;)." The id is
    /// the request's GUID-ish identifier (digits and dashes); the trailing state in
    /// parens is dropped. Returns null if no id can be located.
    /// </summary>
    internal static string ExtractAccessRequestId(string apiMessage)
    {
        if (string.IsNullOrWhiteSpace(apiMessage))
            return null;

        const string marker = "AccessRequest";
        var idx = apiMessage.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var cursor = idx + marker.Length;
        while (cursor < apiMessage.Length && char.IsWhiteSpace(apiMessage[cursor]))
            cursor++;

        var start = cursor;
        while (cursor < apiMessage.Length)
        {
            var ch = apiMessage[cursor];
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
            {
                cursor++;
                continue;
            }
            break;
        }

        if (cursor == start)
            return null;

        return apiMessage[start..cursor];
    }

    /// <summary>
    /// Maps the request endpoint context to the bulk-delete resource segment used in
    /// the 50104 hint. Falls back to a generic placeholder when the request path doesn't
    /// match a known Batch*-bearing collection so the hint still reads cleanly.
    /// </summary>
    private static string GetBulkResourceFromContext(ErrorContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.Path))
            return "<Resource>";

        var segments = ctx.Path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            return "<Resource>";

        var resource = segments[1];
        return resource switch
        {
            "Assets" or "AssetAccounts" or "Users" or "UserGroups" or "AccountGroups" or "AssetGroups" => resource,
            _ => "<Resource>",
        };
    }

    private static string BuildPropertyHint(
        string apiMessage,
        ApiSchemaPropertyPath[] paths,
        QueryParamKind kind,
        ErrorContext ctx)
    {
        var badName = ExtractQuotedToken(apiMessage);
        var label = kind switch
        {
            QueryParamKind.OrderBy => "orderby",
            QueryParamKind.Fields => "field",
            QueryParamKind.Filter => "filter",
            _ => "query"
        };
        var pathLabel = string.IsNullOrWhiteSpace(ctx.Path) ? null : ctx.Path;

        if (string.IsNullOrWhiteSpace(badName))
        {
            var sbFallback = new StringBuilder();
            sbFallback.Append("Rejected ").Append(label).Append(" property.");
            sbFallback.Append(" Use Safeguard_Schema");
            if (pathLabel != null)
                sbFallback.Append(" path=").Append(pathLabel);
            sbFallback.Append(" to see valid properties.");
            return sbFallback.ToString();
        }

        // Operator-shape hint: only meaningful for filter, where the parser
        // mistook an operator token for an identifier.
        if (kind == QueryParamKind.Filter)
        {
            var opHint = TryGetOperatorSuggestion(badName);
            if (!string.IsNullOrWhiteSpace(opHint))
            {
                var opSb = new StringBuilder();
                opSb.Append('\'').Append(badName).Append("' is not a Safeguard filter operator. ");
                opSb.Append(opHint);
                return opSb.ToString();
            }
        }

        var suggestions = (paths != null && paths.Length > 0)
            ? PropertyPathSuggester.Suggest(badName, paths, kind)
            : Array.Empty<string>();

        var sb = new StringBuilder();
        sb.Append('\'').Append(badName).Append("' is not a valid ").Append(label).Append(" property");
        if (pathLabel != null)
            sb.Append(" on ").Append(pathLabel);
        sb.Append('.');

        if (suggestions.Length > 0)
        {
            var best = suggestions[0];
            if (kind == QueryParamKind.OrderBy)
                sb.Append(" Try `").Append(best).Append("` (descending: `-").Append(best).Append("`).");
            else
                sb.Append(" Try `").Append(best).Append("`.");

            if (suggestions.Length > 1)
            {
                sb.Append(" Other close matches: ");
                for (int i = 1; i < suggestions.Length; i++)
                {
                    if (i > 1) sb.Append(", ");
                    sb.Append('`').Append(suggestions[i]).Append('`');
                }
                sb.Append('.');
            }
        }
        else
        {
            sb.Append(" No close match found; use Safeguard_Schema");
            if (pathLabel != null)
                sb.Append(" path=").Append(pathLabel);
            sb.Append(" to see valid properties.");
        }

        if (kind == QueryParamKind.Filter || kind == QueryParamKind.OrderBy)
        {
            sb.Append(" (Note: filter/orderby sometimes use flattened forms like `Account.AssetName`")
              .Append(" instead of the nested `Account.Asset.Name` shown in fields= and the response body;")
              .Append(" if this also fails the field exists in the response but is not filterable -- pick a sibling.)");
        }
        else if (kind == QueryParamKind.Fields)
        {
            sb.Append(" For child collections, call `GET ");
            sb.Append(pathLabel ?? "<endpoint>");
            sb.Append("/{id}/<collection>` instead of dotting into them.");
        }

        return sb.ToString();
    }

    private static string TryGetOperatorSuggestion(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            using var doc = QueryOperatorsResource.Parse();
            if (!doc.RootElement.TryGetProperty("rules", out var rules))
                return null;
            if (!rules.TryGetProperty("unsupported", out var unsupported)
                || unsupported.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var item in unsupported.EnumerateArray())
            {
                if (!item.TryGetProperty("token", out var tokenEl)
                    || tokenEl.ValueKind != JsonValueKind.String)
                    continue;
                var candidate = tokenEl.GetString();
                if (string.IsNullOrEmpty(candidate)) continue;
                if (!candidate.Equals(token, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (item.TryGetProperty("reason", out var reasonEl)
                    && reasonEl.ValueKind == JsonValueKind.String)
                {
                    return reasonEl.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Resource is embedded; a parse failure is a build-time bug, not
            // an agent-time concern. Fall through to the generic hint.
        }
        return null;
    }

    /// <summary>
    /// Extract the first single-quoted token from a Safeguard error message.
    /// Safeguard formats invalid-field/orderby messages as: ... 'BadName' is not a valid property name.
    /// Returns null when no single-quoted token is present.
    /// </summary>
    private static string ExtractQuotedToken(string message)
    {
        if (string.IsNullOrEmpty(message)) return null;
        var start = message.IndexOf('\'');
        if (start < 0) return null;
        var end = message.IndexOf('\'', start + 1);
        if (end <= start) return null;
        return message.Substring(start + 1, end - start - 1);
    }

    /// <summary>
    /// Extracts and formats the Safeguard ASP.NET ModelState dictionary from a JSON error body.
    /// ModelState shape: {"path.to.field":["error message", ...], ...}
    /// Returns null when the body is not JSON, has no ModelState property, or ModelState is empty.
    /// </summary>
    internal static string FormatModelState(string errorBody)
    {
        if (string.IsNullOrWhiteSpace(errorBody) || !errorBody.TrimStart().StartsWith('{'))
            return null;

        try
        {
            using var document = JsonDocument.Parse(errorBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            JsonElement modelState = default;
            var found = false;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.Equals("ModelState", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.Object)
                {
                    modelState = property.Value;
                    found = true;
                    break;
                }
            }

            if (!found)
                return null;

            var lines = new List<string>();
            foreach (var field in modelState.EnumerateObject())
            {
                var messages = new List<string>();
                if (field.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var msg in field.Value.EnumerateArray())
                    {
                        if (msg.ValueKind == JsonValueKind.String)
                        {
                            var text = msg.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                                messages.Add(text);
                        }
                    }
                }
                else if (field.Value.ValueKind == JsonValueKind.String)
                {
                    var text = field.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        messages.Add(text);
                }

                if (messages.Count == 0)
                    lines.Add($"- {field.Name}");
                else
                    lines.Add($"- {field.Name}: {string.Join(" ", messages)}");
            }

            return lines.Count == 0 ? null : string.Join("\n", lines);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // --- Query parameter parsing ---

    internal static IDictionary<string, string> ParseQueryParameters(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
            return result;

        var normalized = query.TrimStart('?');
        foreach (var part in normalized.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equalsIndex = part.IndexOf('=');
            if (equalsIndex > 0)
                result[Uri.UnescapeDataString(part[..equalsIndex])] = Uri.UnescapeDataString(part[(equalsIndex + 1)..]);
            else
                result[Uri.UnescapeDataString(part)] = string.Empty;
        }

        return result;
    }
}
