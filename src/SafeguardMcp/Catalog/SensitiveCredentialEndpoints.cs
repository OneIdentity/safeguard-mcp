using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Catalog of Safeguard API endpoints that return plaintext credential
/// material (passwords, SSH private keys, TOTP codes, API client secrets,
/// secure-file content, etc.). This is the single source of truth shared
/// by two callers:
///
///   1. <c>Safeguard_RetrieveCredential</c> uses it to dispatch from a
///      typed <c>kind</c> to the concrete endpoint (kind → path).
///   2. <c>Safeguard_Execute</c> uses it to refuse-and-redirect any call
///      that targets one of these paths (path → kind), so a generic Execute
///      against a sensitive route can never reach the appliance.
///
/// Loaded once from the embedded <c>sensitive-credential-endpoints.json</c>
/// resource. Path patterns are anchored regex strings; <c>requires</c>
/// names the typed parameters the kind exposes (used for argument
/// validation and to assemble the refuse-and-redirect <c>next_call</c>).
/// </summary>
internal static class SensitiveCredentialEndpoints
{
    internal sealed record EndpointBinding(
        string Method,
        Regex PathPattern,
        IReadOnlyList<string> Args);

    internal sealed record KindEntry(
        string Kind,
        IReadOnlyList<string> Requires,
        IReadOnlyList<EndpointBinding> Endpoints);

    /// <summary>Result of a refuse-and-redirect lookup against (method, path).</summary>
    internal sealed record Match(
        KindEntry Entry,
        EndpointBinding Endpoint,
        IReadOnlyList<KeyValuePair<string, string>> ExtractedArguments);

    private static readonly IReadOnlyList<KindEntry> _entries = Load();
    private static readonly IReadOnlyDictionary<string, KindEntry> _byKind = BuildKindIndex(_entries);

    public static IReadOnlyList<KindEntry> All => _entries;

    public static KindEntry GetByKind(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
            return null;
        return _byKind.TryGetValue(kind, out var entry) ? entry : null;
    }

    /// <summary>
    /// Returns the catalog entry whose method+path pattern matches the
    /// caller's request, along with the named path parameters extracted
    /// from the matched regex. Returns null when no entry matches.
    /// </summary>
    public static Match TryMatch(string method, string path)
    {
        if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(path))
            return null;

        foreach (var entry in _entries)
        {
            foreach (var endpoint in entry.Endpoints)
            {
                if (!endpoint.Method.Equals(method, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                var m = endpoint.PathPattern.Match(path);
                if (!m.Success)
                    continue;

                // Extract integer ids from the matched path segments in order.
                // The path patterns capture ids implicitly via \d+; we re-scan
                // the literal path here to populate the named args slot-wise
                // because the JSON catalog is the source of truth for argument
                // names, not the regex group names.
                var ids = ExtractIntIds(path);
                var args = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < endpoint.Args.Count && i < ids.Count; i++)
                {
                    args.Add(new KeyValuePair<string, string>(endpoint.Args[i], ids[i]));
                }
                return new Match(entry, endpoint, args);
            }
        }
        return null;
    }

    private static List<string> ExtractIntIds(string path)
    {
        var ids = new List<string>();
        var segments = path.Trim('/').Split('/', System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var seg in segments)
        {
            if (int.TryParse(seg, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                ids.Add(seg);
        }
        return ids;
    }

    private static IReadOnlyList<KindEntry> Load()
    {
        var raw = EmbeddedResources.Load("sensitive-credential-endpoints.json");
        using var doc = JsonDocument.Parse(raw);
        var list = new List<KindEntry>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var kind = el.GetProperty("kind").GetString();
            var requires = new List<string>();
            if (el.TryGetProperty("requires", out var req) && req.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in req.EnumerateArray())
                    if (r.ValueKind == JsonValueKind.String) requires.Add(r.GetString());
            }

            var endpoints = new List<EndpointBinding>();
            foreach (var epEl in el.GetProperty("endpoints").EnumerateArray())
            {
                var method = epEl.GetProperty("method").GetString();
                var pattern = epEl.GetProperty("pathPattern").GetString();
                var args = new List<string>();
                if (epEl.TryGetProperty("args", out var argsEl) && argsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in argsEl.EnumerateArray())
                        if (a.ValueKind == JsonValueKind.String) args.Add(a.GetString());
                }
                else if (epEl.TryGetProperty("arg", out var argEl) && argEl.ValueKind == JsonValueKind.String)
                {
                    args.Add(argEl.GetString());
                }
                endpoints.Add(new EndpointBinding(
                    method,
                    new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled),
                    args));
            }

            list.Add(new KindEntry(kind, requires, endpoints));
        }
        return list;
    }

    private static IReadOnlyDictionary<string, KindEntry> BuildKindIndex(IReadOnlyList<KindEntry> entries)
    {
        var d = new Dictionary<string, KindEntry>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
            d[e.Kind] = e;
        return d;
    }
}
