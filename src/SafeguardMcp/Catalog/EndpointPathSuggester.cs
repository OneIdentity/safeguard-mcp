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
/// <item>Bucket 1 — Damerau-Levenshtein distance between leaf segments
/// is within <c>max(1, len/3)</c> (covers <c>Account</c> -> <c>Accounts</c>).</item>
/// </list>
/// Within a bucket: smaller distance wins, then shorter path wins so
/// the canonical resource (e.g. <c>/v4/AssetPartitions/Tags</c>) ranks
/// above its <c>{id}</c>-scoped sibling.
/// </remarks>
internal static class EndpointPathSuggester
{
    private const int MaxSuggestions = 3;

    public static string[] Suggest(string method, string requestPath, ReadOnlySpan<ApiEndpoint> endpoints)
    {
        if (string.IsNullOrWhiteSpace(requestPath) || endpoints.Length == 0)
            return Array.Empty<string>();

        var requestLeaf = LastNonPlaceholderSegment(requestPath);
        if (string.IsNullOrEmpty(requestLeaf))
            return Array.Empty<string>();

        var threshold = Math.Max(1, requestLeaf.Length / 3);
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
            else if (dist <= threshold)
                bucket = 1;
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
