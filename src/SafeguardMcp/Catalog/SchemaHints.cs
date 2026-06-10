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
            "Nested object reference. Use {\"Id\": -1} for the built-in local provider (confirmed constant). "
            + "GET /v4/AuthenticationProviders?fields=Id,Name to list all available providers.",

        ["IdentityProvider"] =
            "Nested object reference. Use {\"Id\": -1} for the built-in local identity provider (confirmed constant). "
            + "GET /v4/IdentityProviders?fields=Id,Name to list all.",

        // --- Asset Partitions ---
        ["AssetPartitionId"] =
            "Integer ID of an asset partition. Use -1 for the default partition (confirmed constant). "
            + "GET /v4/AssetPartitions?fields=Id,Name to list all partitions.",

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
        ["AssetAccount.Asset"] =
            "Nested object reference. Use {\"Id\": <assetId>}. "
            + "GET /v4/Assets?fields=Id,Name,NetworkAddress to find asset IDs.",

        ["AccessRequests.Account"] =
            "Nested object reference. Use {\"Id\": <accountId>}. "
            + "GET /v4/AssetAccounts?fields=Id,Name,Asset.Name to find account IDs.",
        ["AccessRequest.Account"] =
            "Nested object reference. Use {\"Id\": <accountId>}. "
            + "GET /v4/AssetAccounts?fields=Id,Name,Asset.Name to find account IDs.",

        // --- Entitlements & Policy ---
        ["AccessPolicies.RoleId"] =
            "Id of an existing Role (called 'Entitlement' in the UI). "
            + "Create one first via POST /v4/Roles, then reference it here.",
        ["AccessPolicy.RoleId"] =
            "Id of an existing Role (called 'Entitlement' in the UI). "
            + "Create one first via POST /v4/Roles, then reference it here.",

        ["AccessRequestProperties"] =
            "Nested object describing the type and constraints of access. Required properties: "
            + "AccessRequestType (call Safeguard_Enum name=\"AccessRequestType\" for allowed values). "
            + "Optional: AllowSimultaneousAccess (bool), MaximumDurationDays (int), "
            + "MaximumDurationHours (int), ChangePasswordAfterCheckin (bool).",

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
    };

    /// <summary>
    /// Fields that are effectively required for creation (POST) but are either:
    /// - marked readOnly in swagger (so filtered from schema), or
    /// - listed as Optional when they should be Required.
    /// Keyed by schema TypeName; values are property names whose hints should carry
    /// the "(Required for creation)" prefix so that SchemaBodyBuilder includes them.
    /// </summary>
    private static readonly Dictionary<string, string[]> ImplicitRequiredFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["User"] = ["PrimaryAuthenticationProvider"],
        ["NewUser"] = ["PrimaryAuthenticationProvider"],
        ["Asset"] = ["AssetPartitionId"],
    };

    /// <summary>
    /// Gets all applicable hints for properties in the given schema.
    /// Checks both "TypeName.PropertyName" (context-specific) and "PropertyName" (global).
    /// Also includes hints for implicit required fields that swagger marks as optional/readOnly
    /// but are actually needed for creation.
    /// Returns null if no hints apply.
    /// </summary>
    public static IReadOnlyList<(string PropertyName, string Hint)> GetHints(ApiSchema schema)
    {
        List<(string, string)> results = null;
        var typeName = schema.TypeName ?? "";

        // Determine which fields are implicit-required for this type
        HashSet<string> implicitSet = null;
        if (ImplicitRequiredFields.TryGetValue(typeName, out var implicitFields))
        {
            implicitSet = new HashSet<string>(implicitFields, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var property in schema.Properties)
        {
            string hint = null;

            // Try context-specific key first: "TypeName.PropertyName"
            var contextKey = $"{typeName}.{property.Name}";
            if (!Hints.TryGetValue(contextKey, out hint))
            {
                // Fall back to global key: "PropertyName"
                Hints.TryGetValue(property.Name, out hint);
            }

            if (hint != null)
            {
                // If this is an implicit-required field that's Optional in the schema,
                // prefix the hint so SchemaBodyBuilder knows to include it in the body.
                if (!property.Required && implicitSet != null && implicitSet.Contains(property.Name))
                {
                    hint = "(Required for creation) " + hint;
                    implicitSet.Remove(property.Name); // Don't add again below
                }

                results ??= [];
                results.Add((property.Name, hint));
            }
        }

        // Include implicit required fields that are completely missing from the schema
        // (e.g., filtered out by readOnly)
        if (implicitSet != null && implicitSet.Count > 0)
        {
            foreach (var fieldName in implicitSet)
            {
                if (Hints.TryGetValue(fieldName, out var hint))
                {
                    results ??= [];
                    results.Add((fieldName, "(Required for creation) " + hint));
                }
            }
        }

        return results;
    }
}
