namespace SafeguardMcp.Catalog;

/// <summary>
/// Provides agent-friendly hints for schema properties that cannot be inferred from
/// swagger metadata alone. These hints tell an AI agent how to discover valid values,
/// what sentinel values mean, and how to construct nested objects.
/// </summary>
public static class SchemaHints
{
    // A dictionary keyed by property name (case-insensitive).
    // Some entries are "TypeName.PropertyName" for context-specific hints.
    // Falls back to just "PropertyName" for globally applicable hints.
    private static readonly Dictionary<string, string> Hints = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- Identity & Auth ---
        ["PrimaryAuthenticationProvider"] =
            "Nested object reference. Use {\"Id\": -1} for the built-in local provider. "
            + "GET /v4/AuthenticationProviders to list all available providers.",

        ["IdentityProvider"] =
            "Nested object reference. Use {\"Id\": -1} for the built-in local identity provider. "
            + "GET /v4/IdentityProviders to list all.",

        // --- Asset Partitions ---
        ["AssetPartitionId"] =
            "Use -1 for the default asset partition. "
            + "GET /v4/AssetPartitions to list available partitions.",

        // --- Platform Selection ---
        ["PlatformId"] =
            "Integer ID of a platform definition. Platform IDs are appliance-specific. "
            + "GET /v4/Platforms?fields=Id,DisplayName,PlatformType to browse. "
            + "Use filter: GET /v4/Platforms?filter=DisplayName icontains 'Windows Server' "
            + "Prefer the version-independent entries (e.g. 'Windows Server', 'Ubuntu', "
            + "'Active Directory', 'Red Hat Enterprise Linux (RHEL)') over version-specific ones "
            + "(e.g. 'Windows Server 2019', 'Ubuntu 20.04 x86_64') unless targeting a specific OS version. "
            + "Common PlatformType values: Windows, Ubuntu, MicrosoftAD, RedHatEnterprise, "
            + "LinuxOther, AmazonLinux, SqlServer, PostgreSQL, Oracle, CiscoIOS.",

        // --- Nested Object References ---
        ["AssetAccounts.Asset"] =
            "Nested object reference. Use {\"Id\": <assetId>}. "
            + "GET /v4/Assets?fields=Id,Name,NetworkAddress to find asset IDs.",

        ["AccessRequests.Account"] =
            "Nested object reference. Use {\"Id\": <accountId>}. "
            + "GET /v4/AssetAccounts?fields=Id,Name,Asset.Name to find account IDs.",

        // --- Entitlements & Policy ---
        ["AccessPolicies.RoleId"] =
            "Id of an existing Role (called 'Entitlement' in the UI). "
            + "Create one first via POST /v4/Roles, then reference it here.",

        ["AccessRequestType"] =
            "String enum. Allowed values: \"Password\", \"Ssh\", \"RemoteDesktop\", "
            + "\"RdpFile\", \"Telnet\", \"SshKey\", \"RemoteDesktopApplication\", \"ApiKey\", \"File\".",

        ["AccessRequestProperties"] =
            "Nested object describing the type and constraints of access. Required properties: "
            + "AccessRequestType (see enum above). Optional: AllowSimultaneousAccess (bool), "
            + "MaximumDurationDays (int), MaximumDurationHours (int), "
            + "ChangePasswordAfterCheckin (bool).",

        // --- Member/Scope Operations ---
        ["Members"] =
            "Endpoints /{id}/Members/Add or /{id}/Members/Remove accept a JSON array body: "
            + "[{\"Id\": <userId>}, {\"Id\": <userId2>}].",

        ["ScopeItems"] =
            "Body is a JSON array of account or asset group references: "
            + "[{\"Id\": <accountId>}] or [{\"Id\": <assetGroupId>}].",

        // --- Misc ---
        ["NetworkAddress"] =
            "Hostname, IP address, or FQDN of the target system.",

        ["AdminRoles"] =
            "Array of admin role strings. Available roles: GlobalAdmin, Auditor, "
            + "ApplicationAuditor, SystemAuditor, AssetAdmin, ApplianceAdmin, "
            + "PolicyAdmin, UserAdmin, HelpdeskAdmin, OperationsAdmin.",

        // --- Batch Operations ---
        ["BatchOperations"] =
            "For bulk operations, append /BatchCreate, /BatchUpdate, or /BatchDelete to the "
            + "resource path (e.g., POST /v4/Assets/BatchCreate). The request body is a JSON "
            + "array of objects, each with the same format as a single-item POST/PUT. "
            + "Available for: Assets, AssetAccounts, Users, UserGroups, AccountGroups, AssetGroups.",
    };

    /// <summary>
    /// Gets all applicable hints for properties in the given schema.
    /// Checks both "TypeName.PropertyName" (context-specific) and "PropertyName" (global).
    /// Returns null if no hints apply.
    /// </summary>
    public static IReadOnlyList<(string PropertyName, string Hint)> GetHints(ApiSchema schema)
    {
        List<(string, string)> results = null;
        var typeName = schema.TypeName ?? "";

        foreach (var property in schema.Properties)
        {
            // Try context-specific key first: "TypeName.PropertyName"
            var contextKey = $"{typeName}.{property.Name}";
            if (Hints.TryGetValue(contextKey, out var hint))
            {
                results ??= [];
                results.Add((property.Name, hint));
                continue;
            }

            // Fall back to global key: "PropertyName"
            if (Hints.TryGetValue(property.Name, out hint))
            {
                results ??= [];
                results.Add((property.Name, hint));
            }
        }

        return results;
    }
}
