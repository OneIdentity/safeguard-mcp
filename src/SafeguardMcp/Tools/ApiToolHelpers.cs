using System.Globalization;
using System.Text.Json;
using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.Tools;

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

            truncatedBody = JsonSerializer.Serialize(items);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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
