using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Catalog;

/// <summary>
/// MCP Resource providing common Safeguard API usage patterns.
/// Helps agents construct correct API calls for frequent operations.
/// </summary>
[McpServerResourceType]
internal sealed class CommonPatternsResource
{
    private CommonPatternsResource() { }

    [McpServerResource(UriTemplate = "safeguard://common-patterns")]
    [Description("Common Safeguard API patterns — lookup by name, create with dependencies, "
        + "bulk operations, audit queries, and error handling. Preload to write correct API calls faster.")]
    public static string GetCommonPatterns()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Common Safeguard API Patterns");
        sb.AppendLine();
        sb.AppendLine("## Lookup by Name (then use ID)");
        sb.AppendLine();
        sb.AppendLine("Most write operations require integer IDs. The pattern is: search by name, extract ID, use in subsequent calls.");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("# Find an asset by name");
        sb.AppendLine("GET /v4/Assets  query: filter=Name ieq 'web-server-01'&fields=Id,Name,NetworkAddress");
        sb.AppendLine();
        sb.AppendLine("# Find a user by username");
        sb.AppendLine("GET /v4/Users  query: filter=UserName ieq 'john.smith'&fields=Id,UserName,DisplayName");
        sb.AppendLine();
        sb.AppendLine("# Find an entitlement (role) by name");
        sb.AppendLine("GET /v4/Roles  query: filter=Name icontains 'help desk'&fields=Id,Name,MemberCount");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Create with Dependencies");
        sb.AppendLine();
        sb.AppendLine("Many objects have required relationships. Create in order:");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("# Creating a managed account requires: Asset exists, Platform assigned");
        sb.AppendLine("1. GET /v4/Assets?filter=Name eq 'myserver'&fields=Id  → get assetId");
        sb.AppendLine("2. POST /v4/AssetAccounts  body: {\"AssetId\": <assetId>, \"Name\": \"sa_admin\"}");
        sb.AppendLine();
        sb.AppendLine("# Creating an entitlement with policy requires: Role → Policy → Scope");
        sb.AppendLine("1. POST /v4/Roles  body: {\"Name\": \"DB Admins\"}  → get roleId");
        sb.AppendLine("2. POST /v4/Roles/{roleId}/Members/Add  body: [{\"Id\": <userId>}]");
        sb.AppendLine("3. POST /v4/AccessPolicies  body: {\"RoleId\": <roleId>, \"Name\": \"DB Password Access\", ...}  → get policyId");
        sb.AppendLine("4. POST /v4/AccessPolicies/{policyId}/ScopeItems/Add  body: [{\"Id\": <accountId>}]");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Bulk Operations");
        sb.AppendLine();
        sb.AppendLine("Use collection add/remove endpoints instead of individual calls:");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("# Add multiple members to a role at once");
        sb.AppendLine("POST /v4/Roles/{roleId}/Members/Add  body: [{\"Id\": 1}, {\"Id\": 2}, {\"Id\": 3}]");
        sb.AppendLine();
        sb.AppendLine("# Add multiple accounts to a policy scope");
        sb.AppendLine("POST /v4/AccessPolicies/{policyId}/ScopeItems/Add  body: [{\"Id\": 10}, {\"Id\": 11}]");
        sb.AppendLine();
        sb.AppendLine("# Remove members");
        sb.AppendLine("POST /v4/Roles/{roleId}/Members/Remove  body: [{\"Id\": 1}]");
        sb.AppendLine();
        sb.AppendLine("# Batch asset creation — use POST /v4/Assets/BatchCreate (if available in your version)");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Audit & Activity Queries");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("# Recent password check/change activity");
        sb.AppendLine("GET /v4/AuditLog/Passwords/Activities  query: orderby=-LogTime&limit=50");
        sb.AppendLine();
        sb.AppendLine("# Failed password changes");
        sb.AppendLine("GET /v4/AuditLog/Passwords/Activities  query: filter=EventName eq 'PasswordChangeFailed'&orderby=-LogTime&limit=20");
        sb.AppendLine();
        sb.AppendLine("# Access request history for a user");
        sb.AppendLine("GET /v4/AuditLog/AccessRequests/Activities  query: filter=RequesterName eq 'john.smith'&orderby=-LogTime&limit=50");
        sb.AppendLine();
        sb.AppendLine("# Login audit for a specific date range");
        sb.AppendLine("GET /v4/AuditLog/Logins/Activities  query: filter=LogTime ge '2024-06-01' and LogTime lt '2024-07-01'&orderby=-LogTime");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Error Handling Patterns");
        sb.AppendLine();
        sb.AppendLine("| Status | Meaning | Action |");
        sb.AppendLine("|--------|---------|--------|");
        sb.AppendLine("| 400 | Validation error | Read the error body — it explains which field/value is wrong |");
        sb.AppendLine("| 401 | Token expired or invalid | Reconnect with Safeguard_Connect |");
        sb.AppendLine("| 403 | Insufficient permissions | The authenticated user lacks the required admin role |");
        sb.AppendLine("| 404 | Object not found | Verify the ID exists; check for typos in the path |");
        sb.AppendLine("| 409 | Conflict (duplicate, locked) | Object already exists or is being modified by another operation |");
        sb.AppendLine("| 503 | Appliance in maintenance | Wait for maintenance to complete, then retry |");
        sb.AppendLine();
        sb.AppendLine("## Permissions Model");
        sb.AppendLine();
        sb.AppendLine("Safeguard uses role-based admin permissions (distinct from entitlements):");
        sb.AppendLine();
        sb.AppendLine("| Admin Role | Can Manage |");
        sb.AppendLine("|-----------|-----------|");
        sb.AppendLine("| Appliance Administrator | Appliance settings, certificates, cluster, backup, patches |");
        sb.AppendLine("| Asset Administrator | Assets, accounts, partitions, profiles, platforms |");
        sb.AppendLine("| Security Policy Administrator | Roles (entitlements), access policies, user groups |");
        sb.AppendLine("| User Administrator | Users, user groups, identity providers |");
        sb.AppendLine("| Auditor | Read-only access to audit logs and reports |");
        sb.AppendLine("| Operations Administrator | Running tasks, platform task management |");
        sb.AppendLine();
        sb.AppendLine("## Useful /v4/Me Endpoints");
        sb.AppendLine();
        sb.AppendLine("These return data scoped to the currently authenticated user:");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("GET /v4/Me                        → current user info and permissions");
        sb.AppendLine("GET /v4/Me/RequestableAccounts    → what accounts the user can request access to");
        sb.AppendLine("GET /v4/Me/ActionableRequests     → requests waiting for this user's approval");
        sb.AppendLine("GET /v4/Me/AccessRequests         → this user's own access requests");
        sb.AppendLine("GET /v4/Me/PersonalPasswords      → personal vault entries");
        sb.AppendLine("```");

        return sb.ToString();
    }
}
