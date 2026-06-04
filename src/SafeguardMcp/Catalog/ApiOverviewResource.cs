using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Catalog;

/// <summary>
/// MCP Resource providing a high-level map of Safeguard API services and their endpoints.
/// Helps agents orient quickly without needing to call Safeguard_Discover first.
/// </summary>
[McpServerResourceType]
internal sealed class ApiOverviewResource
{
    private ApiOverviewResource() { }

    [McpServerResource(UriTemplate = "safeguard://api-overview")]
    [Description("High-level map of Safeguard services, endpoint categories, and key object relationships. "
        + "Preload this to understand the API landscape before navigating specific endpoints.")]
    public static string GetApiOverview()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Safeguard API Service Map");
        sb.AppendLine();
        sb.AppendLine("Safeguard exposes three services, each with its own base URL path.");
        sb.AppendLine("The Safeguard_Execute tool auto-routes to the correct service, but understanding");
        sb.AppendLine("the split helps when browsing the catalog or diagnosing routing issues.");
        sb.AppendLine();
        sb.AppendLine("## Core Service (requires authentication)");
        sb.AppendLine();
        sb.AppendLine("The primary service — contains all business objects and policy.");
        sb.AppendLine();
        sb.AppendLine("| Category | Key Endpoints | Purpose |");
        sb.AppendLine("|----------|--------------|---------|");
        sb.AppendLine("| Assets & Accounts | /v4/Assets, /v4/AssetAccounts, /v4/AssetGroups, /v4/AccountGroups | Managed systems and their privileged accounts |");
        sb.AppendLine("| Users & Identity | /v4/Users, /v4/UserGroups, /v4/IdentityProviders | Safeguard users and directory integration |");
        sb.AppendLine("| Policy & Access | /v4/Roles, /v4/AccessPolicies, /v4/AccessRequests | Entitlements, policies, and access request workflow |");
        sb.AppendLine("| Partitions & Profiles | /v4/AssetPartitions, /v4/PasswordProfiles, /v4/SshKeyProfiles | Organizational units and credential management rules |");
        sb.AppendLine("| Audit & Reporting | /v4/AuditLog, /v4/AuditLog/Passwords/Activities, /v4/AuditLog/AccessRequests/Activities | Compliance and activity history |");
        sb.AppendLine("| Discovery | /v4/AssetPartitions/{id}/Profiles/{id}/AccountDiscoverySchedules | Find unmanaged accounts on assets |");
        sb.AppendLine("| Cluster | /v4/Cluster, /v4/Cluster/Members | Cluster join, unjoin, failover operations |");
        sb.AppendLine("| A2A | /v4/A2ARegistrations | Application-to-Application credential retrieval |");
        sb.AppendLine("| Configuration | /v4/Settings, /v4/Licenses, /v4/Platforms | System settings and platform definitions |");
        sb.AppendLine("| Tasks | /v4/RunningTasks, /v4/PlatformTaskLogs | Background task monitoring |");
        sb.AppendLine("| Certificates | /v4/TrustedCertificates, /v4/SslCertificates | Certificate management |");
        sb.AppendLine("| Personal Vault | /v4/Me/PersonalPasswords | User personal password storage |");
        sb.AppendLine("| Current User | /v4/Me, /v4/Me/RequestableAccounts, /v4/Me/ActionableRequests | Context for the authenticated user |");
        sb.AppendLine();
        sb.AppendLine("## Appliance Service (requires authentication)");
        sb.AppendLine();
        sb.AppendLine("Hardware/infrastructure management for the appliance itself.");
        sb.AppendLine();
        sb.AppendLine("| Category | Key Endpoints | Purpose |");
        sb.AppendLine("|----------|--------------|---------|");
        sb.AppendLine("| Health | /v4/ApplianceStatus, /v4/ApplianceStatus/Health | CPU, memory, disk, overall state |");
        sb.AppendLine("| Backup & Restore | /v4/Backups, /v4/BackupSettings | Create, schedule, restore backups |");
        sb.AppendLine("| Networking | /v4/NetworkInterfaces, /v4/NetworkDiagnostics | Network configuration and diagnostics |");
        sb.AppendLine("| Patch | /v4/Patches | Upload, distribute, install patches |");
        sb.AppendLine("| Diagnostics | /v4/DiagnosticPackage | Generate support bundles |");
        sb.AppendLine("| Time & NTP | /v4/TimeSettings | Time synchronization configuration |");
        sb.AppendLine();
        sb.AppendLine("## Notification Service (no authentication required)");
        sb.AppendLine();
        sb.AppendLine("Lightweight status checks — useful for health monitoring without credentials.");
        sb.AppendLine();
        sb.AppendLine("| Category | Key Endpoints | Purpose |");
        sb.AppendLine("|----------|--------------|---------|");
        sb.AppendLine("| Status | /v4/Status, /v4/Status/Availability | Appliance online state and service availability |");
        sb.AppendLine("| Cluster | /v4/Status/Cluster, /v4/Status/ClusterPatch | Cluster member states and patch progress |");
        sb.AppendLine("| Maintenance | /v4/Status/Maintenance | Whether appliance is in maintenance mode |");
        sb.AppendLine();
        sb.AppendLine("## Key Object Relationships");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("Asset (managed system)");
        sb.AppendLine("  └── AssetAccount (privileged account on that system)");
        sb.AppendLine("        └── TaskProperties (rotation status, last check/change dates)");
        sb.AppendLine();
        sb.AppendLine("AssetPartition (organizational container)");
        sb.AppendLine("  ├── Assets (belong to one partition)");
        sb.AppendLine("  ├── PasswordProfiles (check/change schedules)");
        sb.AppendLine("  └── SshKeyProfiles (SSH key rotation schedules)");
        sb.AppendLine();
        sb.AppendLine("Role (what the UI calls 'Entitlement')");
        sb.AppendLine("  ├── Members (Users/UserGroups who can request access)");
        sb.AppendLine("  └── AccessPolicies (rules governing access)");
        sb.AppendLine("        ├── ScopeItems (which accounts/assets are covered)");
        sb.AppendLine("        ├── ApproverSets (who must approve)");
        sb.AppendLine("        └── AccessRequestProperties (type, duration, emergency)");
        sb.AppendLine();
        sb.AppendLine("AccessRequest (a user's request for access)");
        sb.AppendLine("  States: Pending → Approved → Available → CheckedIn/Expired");
        sb.AppendLine("  Types: Password, SSH Key, RDP, SSH, Telnet, API Key");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Common ID Lookups");
        sb.AppendLine();
        sb.AppendLine("Most Safeguard objects are referenced by integer ID. To find an ID by name:");
        sb.AppendLine("- `GET /v4/Assets?filter=Name ieq 'myserver'&fields=Id,Name`");
        sb.AppendLine("- `GET /v4/Users?filter=UserName ieq 'john.smith'&fields=Id,UserName`");
        sb.AppendLine("- `GET /v4/Roles?filter=Name icontains 'help desk'&fields=Id,Name`");

        return sb.ToString();
    }
}
