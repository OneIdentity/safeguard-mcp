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

        // Password operations — agents say "checkout" or "check out a password"
        ["checkout", "check out", "password release", "password checkout", "checkoutpassword"],

        // Directory / AD / LDAP — agents may use any of these interchangeably
        ["directory", "directories", "active directory", "ad", "ldap", "identityprovider", "identityproviders"],

        // User groups
        ["user group", "user groups", "usergroups", "usergroup"],

        // Account groups
        ["account group", "account groups", "accountgroups", "accountgroup"],

        // Asset groups
        ["asset group", "asset groups", "assetgroups", "assetgroup"],

        // Audit / logging — agents looking for activity history
        ["audit", "audit log", "auditlog", "activity", "activities", "event log"],

        // Access requests — the full lifecycle term set. Also folds in the
        // umbrella PAM vocabulary ("privileged access", "pam") so that agents
        // who search for the broad concept land on the AccessRequests family
        // of endpoints, which is what they almost always mean.
        ["access request", "access requests", "accessrequests", "request", "requests",
         "privilege", "privileged", "pam", "privileged access"],

        // Service accounts / dependent accounts
        ["service account", "service accounts", "dependent account", "dependent accounts"],

        // Cluster & appliance — infrastructure management terms
        ["cluster", "cluster members", "node", "nodes", "appliance", "appliances"],

        // Discovery — finding unmanaged assets/accounts
        ["discovery", "discover", "discoveredassets", "discoveredaccounts", "discovery job"],

        // Password management tasks — rotation, change, check
        ["password change", "password check", "change password", "check password", "rotation"],

        // SSH keys
        ["ssh key", "ssh keys", "sshkey", "sshkeys"],

        // Certificate management
        ["certificate", "certificates", "cert", "certs", "ssl", "tls"],

        // Backup / restore
        ["backup", "backups", "restore", "archive"],

        // Common PAM vocabulary is folded into the access-request group
        // above (single source of truth) — see comment there.

        // --- Verb / intent groups -----------------------------------------
        // These groups pair common verbs the agent will type with one or
        // more API-shaped anchors so ScoreMatch can hit endpoint paths or
        // summaries directly. They also drive recipe ranking in
        // Safeguard_Discover via RecipeIndex.

        // Reassigning ownership of an asset/account to a different partition
        ["move", "transfer", "migrate", "reassign",
         "partition reassignment", "partition transfer"],

        // Discovering soft-deleted accounts/assets/users and audit history
        ["deleted", "removed", "archived", "trashed", "restore",
         "deletedaccounts", "deletedassets", "auditlog/objectchanges"],

        // "Show me recent activity" — last-login + audit log queries
        ["recent", "latest", "last activity", "last login",
         "lastlogindate", "auditlog/logins"],

        // Stale credentials / expiring secrets
        ["expired", "expiring", "stale", "rotated",
         "lastsuccesspasswordchangedate"],

        // Failure-state diagnostics
        ["broken", "failing", "unhealthy", "failure",
         "taskproperties.hasfailure", "reports/tasks"],

        // "Who can access X?" entitlement lookups
        ["who has access", "authorized", "allowed", "entitled",
         "requestentitlements", "accountentitlements",
         "roles/{id}/members"],

        // Acknowledge / remediate failed tasks
        ["acknowledge", "retry", "remediate", "fix", "repair"],

        // Bulk operations vocabulary
        ["bulk", "batch", "many", "import"],
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
