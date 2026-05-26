namespace SafeguardMcp.Catalog;

/// <summary>
/// Maps Safeguard product/UI terminology to API terminology and vice versa.
/// The Safeguard API uses different names than the product UI in several places.
/// This map allows discovery searches using either product or API terms to find
/// the correct endpoints.
///
/// To add a new mapping: add an entry to the Aliases array below.
/// Each entry is a set of terms that are synonymous — searching for any one
/// of them will also match the others.
/// </summary>
public static class TerminologyMap
{
    /// <summary>
    /// Groups of synonymous terms. Each array entry contains terms that refer to the
    /// same concept — one is the API name, others are product/UI names or common
    /// shorthand used by administrators.
    /// </summary>
    private static readonly string[][] Aliases =
    [
        // Product UI calls them "Entitlements"; API endpoint is /v4/Roles
        ["entitlement", "entitlements", "role", "roles"],

        // UI sometimes says "Access Request Policy"; API is "AccessPolicies"
        ["access policy", "access policies", "access request policy", "accesspolicies"],

        // "Partition" is the UI term; API uses "AssetPartitions"
        ["partition", "partitions", "asset partition", "assetpartitions"],

        // "Managed System" / "Managed Asset" vs API "Assets"
        ["managed system", "managed asset", "asset", "assets"],

        // "Managed Account" vs API "AssetAccounts"
        ["managed account", "managed accounts", "asset account", "assetaccounts"],

        // "Profile" can mean several things but commonly refers to password change settings
        ["password profile", "change profile", "passwordprofiles"],

        // "Platform" was historically called "Connection Template" or "Profile"
        ["platform", "platforms", "connection template"],

        // "Linked Account" vs API "LinkedAccounts" or "PersonalAccounts"
        ["linked account", "linked accounts", "personal account", "personalaccounts"],

        // "Session Recording" vs API "SessionRecordings" / "Sessions"
        ["session recording", "session recordings", "sessions", "sessionrecordings"],
    ];

    /// <summary>
    /// Given a search term, returns all related terms that should also be searched.
    /// Returns the original term plus any aliases from the same group.
    /// </summary>
    public static IReadOnlyList<string> ExpandSearchTerms(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return [];

        var trimmed = search.Trim();
        var expanded = new List<string> { trimmed };

        foreach (var group in Aliases)
        {
            var found = false;
            foreach (var term in group)
            {
                if (trimmed.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                foreach (var term in group)
                {
                    if (!expanded.Contains(term, StringComparer.OrdinalIgnoreCase))
                        expanded.Add(term);
                }
            }
        }

        return expanded;
    }

    /// <summary>
    /// Checks whether any of the expanded terms match the given text.
    /// </summary>
    public static bool MatchesAny(IReadOnlyList<string> terms, string text)
    {
        for (int i = 0; i < terms.Count; i++)
        {
            if (text.Contains(terms[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
