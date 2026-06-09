namespace SafeguardMcp.Catalog;

/// <summary>
/// Which Safeguard query parameter is being validated. Drives both the
/// suggester's pool (synthetic <c>&lt;Collection&gt;.Count</c> entries
/// are valid in <c>filter</c>/<c>orderby</c> but never in <c>fields</c>)
/// and the wording of the hint shown to the agent.
/// </summary>
internal enum QueryParamKind
{
    Unknown = 0,
    Filter,
    OrderBy,
    Fields
}

/// <summary>
/// Builds "did you mean" suggestions for a rejected property name from
/// the path graph already present on the catalog. Pure data-driven: the
/// suggester never invents a path. If the bad name does not match any
/// real path under the priority strategy below, it returns an empty
/// array and the caller falls back to a "use Safeguard_Schema" hint.
/// </summary>
/// <remarks>
/// Strategy, in priority order:
/// <list type="number">
/// <item>Exact case-insensitive match on the full path or its leaf.</item>
/// <item>Flat-FK pattern: <c>XId</c> -> <c>X.Id</c>, <c>XName</c> -> <c>X.Name</c>.</item>
/// <item>Substring match either direction (covers
/// <c>Account.Asset.Name</c> vs. <c>Account.AssetName</c> flattening).</item>
/// <item>Damerau-Levenshtein distance with cutoff
/// <c>min(3, len(badName)/3)</c>.</item>
/// </list>
/// Once a higher-priority strategy produces hits the lower ones are
/// skipped -- this avoids drowning a high-confidence answer in noisy
/// fuzzy matches.
/// </remarks>
internal static class PropertyPathSuggester
{
    private const int MaxSuggestions = 3;

    public static string[] Suggest(
        string badName,
        ApiSchemaPropertyPath[] paths,
        QueryParamKind kind)
    {
        if (string.IsNullOrWhiteSpace(badName) || paths == null || paths.Length == 0)
            return Array.Empty<string>();

        var pool = BuildPool(paths, kind);
        if (pool.Length == 0)
            return Array.Empty<string>();

        var results = new List<string>();

        // 1. Exact case-insensitive match on the full path.
        foreach (var path in pool)
        {
            if (path.Equals(badName, StringComparison.OrdinalIgnoreCase))
                AddUnique(results, path);
        }
        if (results.Count > 0)
            return Trim(results);

        // 1b. Match by leaf segment (agent typed `name` instead of `Name`,
        // or `Id` against a graph whose leaf form is the right answer).
        foreach (var path in pool)
        {
            if (LeafSegment(path).Equals(badName, StringComparison.OrdinalIgnoreCase))
                AddUnique(results, path);
        }
        if (results.Count > 0)
            return Trim(results);

        // 2. Flat-FK pattern (high-confidence): AssetId -> Asset.Id,
        // UserName -> User.Name. We try only the qualified form here
        // because the bare-suffix fallback (UserName -> Name when no
        // User.Name exists) can otherwise steal a result that fuzzy
        // would have produced more accurately (DisplyName -> DisplayName).
        var fkStripped = TryStripFkSuffix(badName, out var prefix, out var suffix);
        if (fkStripped)
        {
            var flat = prefix + "." + suffix;
            foreach (var path in pool)
            {
                if (path.Equals(flat, StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith("." + flat, StringComparison.OrdinalIgnoreCase))
                {
                    AddUnique(results, path);
                }
            }
        }
        if (results.Count > 0)
            return Trim(results);

        // 3. Substring / collapsed-form match. We require the shorter
        // side to be at least 60% of the longer to avoid a 4-char
        // path (`Name`) latching onto a 10-char bad name (`DisplyName`).
        // The collapsed-form equality is the headline rule here -- it
        // catches flattening misses like `Account.AssetName` vs.
        // `Account.Asset.Name` without inventing anything.
        var badCollapsed = badName.Replace(".", "", StringComparison.Ordinal);
        foreach (var path in pool)
        {
            var pathCollapsed = path.Replace(".", "", StringComparison.Ordinal);
            if (pathCollapsed.Equals(badCollapsed, StringComparison.OrdinalIgnoreCase))
            {
                AddUnique(results, path);
                continue;
            }

            if (!SubstringRatioOk(path, badName) && !SubstringRatioOk(pathCollapsed, badCollapsed))
                continue;

            if (path.Contains(badName, StringComparison.OrdinalIgnoreCase)
                || badName.Contains(path, StringComparison.OrdinalIgnoreCase)
                || pathCollapsed.Contains(badCollapsed, StringComparison.OrdinalIgnoreCase)
                || badCollapsed.Contains(pathCollapsed, StringComparison.OrdinalIgnoreCase))
            {
                AddUnique(results, path);
            }
        }
        if (results.Count > 0)
            return Trim(results);

        // 4. Damerau-Levenshtein with conservative cutoff.
        var threshold = Math.Max(1, Math.Min(3, badName.Length / 3));
        var scored = new List<(string path, int dist)>();
        foreach (var path in pool)
        {
            var dist = Math.Min(
                Distance(badName, path),
                Distance(badName, LeafSegment(path)));
            if (dist <= threshold)
                scored.Add((path, dist));
        }
        foreach (var (path, _) in scored.OrderBy(x => x.dist))
            AddUnique(results, path);

        if (results.Count > 0)
            return Trim(results);

        // 5. Last resort: FK bare-suffix fallback. When the bad name
        // was XId / XName but the entity has neither a nav `X` nor a
        // close fuzzy match, the bare suffix is often what the agent
        // wanted (`UserName` on an entity whose owner column is just
        // `Name`).
        if (fkStripped)
        {
            foreach (var path in pool)
            {
                if (LeafSegment(path).Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    AddUnique(results, path);
            }
        }

        return Trim(results);
    }

    private static string[] BuildPool(ApiSchemaPropertyPath[] paths, QueryParamKind kind)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pool = new List<string>(paths.Length);
        foreach (var p in paths)
        {
            if (p.IsSynthetic && kind == QueryParamKind.Fields)
                continue;
            if (string.IsNullOrEmpty(p.Path))
                continue;
            if (seen.Add(p.Path))
                pool.Add(p.Path);
        }
        return pool.ToArray();
    }

    private static void AddUnique(List<string> list, string value)
    {
        if (list.Count >= MaxSuggestions) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                return;
        }
        list.Add(value);
    }

    private static string[] Trim(List<string> list)
        => list.Count <= MaxSuggestions
            ? list.ToArray()
            : list.GetRange(0, MaxSuggestions).ToArray();

    private static string LeafSegment(string path)
    {
        var idx = path.LastIndexOf('.');
        return idx >= 0 ? path[(idx + 1)..] : path;
    }

    internal static bool TryStripFkSuffix(string badName, out string prefix, out string suffix)
    {
        prefix = string.Empty;
        suffix = string.Empty;
        if (string.IsNullOrEmpty(badName))
            return false;
        if (badName.Length > 2 && badName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
        {
            prefix = badName[..^2];
            suffix = "Id";
            return true;
        }
        if (badName.Length > 4 && badName.EndsWith("Name", StringComparison.OrdinalIgnoreCase))
        {
            prefix = badName[..^4];
            suffix = "Name";
            return true;
        }
        return false;
    }

    private static bool SubstringRatioOk(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;
        var min = Math.Min(a.Length, b.Length);
        var max = Math.Max(a.Length, b.Length);
        return max == 0 || (min * 10) >= (max * 6); // shorter >= 60% of longer
    }

    /// <summary>Damerau-Levenshtein distance, case-insensitive.</summary>
    internal static int Distance(string a, string b)
    {
        a ??= string.Empty;
        b ??= string.Empty;
        var x = a.ToLowerInvariant();
        var y = b.ToLowerInvariant();
        var n = x.Length;
        var m = y.Length;
        if (n == 0) return m;
        if (m == 0) return n;

        var d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = x[i - 1] == y[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && x[i - 1] == y[j - 2] && x[i - 2] == y[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
            }
        }

        return d[n, m];
    }
}
