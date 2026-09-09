namespace SafeguardMcp.Catalog;

/// <summary>
/// Builds "did you mean" path suggestions for a request whose URL did
/// not match any cataloged endpoint. Sibling of
/// <see cref="PropertyPathSuggester"/>: pure data-driven, never invents
/// a path. The leaf segment of the request path drives ranking so that
/// a top-level guess like <c>/v4/Tags</c> finds
/// <c>/v4/AssetPartitions/Tags</c> first.
/// </summary>
/// <remarks>
/// Strategy:
/// <list type="number">
/// <item>Filter the catalog by HTTP method (case-insensitive). A 404
/// on POST never suggests a GET-only path.</item>
/// <item>Bucket 0 — endpoint's last non-placeholder segment matches
/// the request's last non-placeholder segment exactly.</item>
/// <item>Bucket 1 — the request leaf is a known domain concept whose
/// canonical Safeguard resource matches the endpoint leaf (e.g.
/// <c>Entitlements</c> -> <c>Roles</c>). These synonyms are not
/// string-derivable, so a small curated map carries them.</item>
/// <item>Bucket 2 — Damerau-Levenshtein distance between leaf segments
/// is within <c>max(1, len/3)</c> (covers <c>Account</c> -> <c>Accounts</c>).</item>
/// <item>Bucket 3 — one leaf contains the other as a substring (covers
/// <c>Entitlements</c> -> <c>RequestEntitlements</c>, which is too far
/// for the edit-distance bucket).</item>
/// </list>
/// Within a bucket: smaller distance wins, then shorter path wins so
/// the canonical resource (e.g. <c>/v4/AssetPartitions/Tags</c>) ranks
/// above its <c>{id}</c>-scoped sibling.
/// </remarks>
internal static class EndpointPathSuggester
{
    private const int MaxSuggestions = 3;

    // Minimum leaf length for the containment bucket, so a short guess like
    // "User" does not match every "...UserGroups"/"...Users..." path by
    // substring. Below this length only exact / synonym / edit-distance apply.
    private const int MinContainmentLeafLength = 4;

    // Curated concept -> canonical resource-leaf synonyms. Safeguard surfaces
    // "entitlements" through the Roles resource, a name an agent is unlikely to
    // guess and that string distance cannot recover. Keys are lowercase; values
    // are matched against endpoint leaves case-insensitively.
    private static readonly Dictionary<string, string[]> ConceptSynonyms =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["entitlement"] = new[] { "Roles" },
            ["entitlements"] = new[] { "Roles" },
        };

    public static string[] Suggest(string method, string requestPath, ReadOnlySpan<ApiEndpoint> endpoints)
    {
        if (string.IsNullOrWhiteSpace(requestPath) || endpoints.Length == 0)
            return Array.Empty<string>();

        var requestLeaf = LastNonPlaceholderSegment(requestPath);
        if (string.IsNullOrEmpty(requestLeaf))
            return Array.Empty<string>();

        var threshold = Math.Max(1, requestLeaf.Length / 3);
        ConceptSynonyms.TryGetValue(requestLeaf, out var synonymTargets);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scored = new List<(string Path, int Bucket, int Distance)>();

        foreach (var ep in endpoints)
        {
            if (!string.IsNullOrEmpty(method)
                && !ep.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrEmpty(ep.Path) || !seen.Add(ep.Path))
                continue;

            var epLeaf = LastNonPlaceholderSegment(ep.Path);
            if (string.IsNullOrEmpty(epLeaf))
                continue;

            int bucket;
            int dist = PropertyPathSuggester.Distance(requestLeaf, epLeaf);
            if (epLeaf.Equals(requestLeaf, StringComparison.OrdinalIgnoreCase))
                bucket = 0;
            else if (IsSynonymMatch(synonymTargets, epLeaf))
                bucket = 1;
            else if (dist <= threshold)
                bucket = 2;
            else if (IsContainmentMatch(requestLeaf, epLeaf))
                bucket = 3;
            else
                continue;

            scored.Add((ep.Path, bucket, dist));
        }

        var ordered = scored
            .OrderBy(s => s.Bucket)
            .ThenBy(s => s.Distance)
            .ThenBy(s => s.Path.Length)
            .ThenBy(s => s.Path, StringComparer.OrdinalIgnoreCase);

        var result = new List<string>(MaxSuggestions);
        foreach (var s in ordered)
        {
            result.Add(s.Path);
            if (result.Count >= MaxSuggestions) break;
        }
        return result.ToArray();
    }

    private static bool IsSynonymMatch(string[] synonymTargets, string epLeaf)
    {
        if (synonymTargets == null)
            return false;
        foreach (var target in synonymTargets)
        {
            if (epLeaf.Equals(target, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool IsContainmentMatch(string requestLeaf, string epLeaf)
    {
        if (requestLeaf.Length < MinContainmentLeafLength || epLeaf.Length < MinContainmentLeafLength)
            return false;
        return epLeaf.Contains(requestLeaf, StringComparison.OrdinalIgnoreCase)
            || requestLeaf.Contains(epLeaf, StringComparison.OrdinalIgnoreCase);
    }

    private static string LastNonPlaceholderSegment(string path)
    {
        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = segments.Length - 1; i >= 0; i--)
        {
            var s = segments[i];
            if (s.Length > 0 && s[0] != '{')
                return s;
        }
        return string.Empty;
    }
}
