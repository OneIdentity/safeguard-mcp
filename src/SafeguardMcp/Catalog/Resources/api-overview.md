# Safeguard API Service Map

Safeguard exposes three services, each with its own base URL path.
The Safeguard_Execute tool auto-routes to the correct service, but understanding
the split helps when browsing the catalog or diagnosing routing issues.

## Path format

Pass bare `/v4/...` paths to `Safeguard_Execute` and `Safeguard_Schema`.
The appliance's actual URLs are `https://{host}/service/{Name}/v4/...`,
but the tool prepends `/service/{Name}/` for you based on the path.
Calls that include `/service/{name}/` themselves are rejected with a
directive — they would otherwise hit the wire as
`/service/{name}/service/{name}/v4/...` and 404.

## Core Service (requires authentication)

The primary service — contains all business objects and policy.

| Category | Key Endpoints | Purpose |
|----------|--------------|---------|
| Assets & Accounts | /v4/Assets, /v4/AssetAccounts, /v4/AssetGroups, /v4/AccountGroups | Managed systems and their privileged accounts |
| Users & Identity | /v4/Users, /v4/UserGroups, /v4/IdentityProviders | Safeguard users and directory integration |
| Policy & Access | /v4/Roles, /v4/AccessPolicies, /v4/AccessRequests | Entitlements, policies, and access request workflow |
| Partitions & Profiles | /v4/AssetPartitions, /v4/PasswordProfiles, /v4/SshKeyProfiles | Organizational units and credential management rules |
| Audit & Reporting | /v4/AuditLog, /v4/AuditLog/Passwords/Activities, /v4/AuditLog/AccessRequests/Activities | Compliance and activity history |
| Discovery | /v4/AssetPartitions/{id}/Profiles/{id}/AccountDiscoverySchedules | Find unmanaged accounts on assets |
| Cluster | /v4/Cluster, /v4/Cluster/Members | Cluster join, unjoin, failover operations |
| A2A | /v4/A2ARegistrations | Application-to-Application credential retrieval |
| Configuration | /v4/Settings, /v4/Licenses, /v4/Platforms | System settings and platform definitions |
| Tasks | /v4/RunningTasks, /v4/PlatformTaskLogs | Background task monitoring |
| Certificates | /v4/TrustedCertificates, /v4/SslCertificates | Certificate management |
| Personal Vault | /v4/Me/PersonalPasswords | User personal password storage |
| Current User | /v4/Me, /v4/Me/RequestableAccounts, /v4/Me/ActionableRequests | Context for the authenticated user |

## Appliance Service (requires authentication)

Hardware/infrastructure management for the appliance itself.

| Category | Key Endpoints | Purpose |
|----------|--------------|---------|
| Health | /v4/ApplianceStatus, /v4/ApplianceStatus/Health | CPU, memory, disk, overall state |
| Identity | /v4/Version, /v4/SystemTime | Software version and current appliance time (uptime is derived from these) |
| Backup & Restore | /v4/Backups, /v4/BackupSettings | Create, schedule, restore backups |
| Networking | /v4/NetworkInterfaces, /v4/NetworkDiagnostics | Network configuration and diagnostics |
| Patch | /v4/Patches | Upload, distribute, install patches |
| Diagnostics | /v4/DiagnosticPackage | Generate support bundles |
| Time & NTP | /v4/TimeSettings | Time synchronization configuration |

## Notification Service (no authentication required)

Lightweight status checks — useful for health monitoring without credentials.

| Category | Key Endpoints | Purpose |
|----------|--------------|---------|
| Status | /v4/Status, /v4/Status/Availability | Appliance online state and service availability |
| Cluster | /v4/Status/Cluster, /v4/Status/ClusterPatch | Cluster member states and patch progress |
| Maintenance | /v4/Status/Maintenance | Whether appliance is in maintenance mode |

## Key Object Relationships

```
Asset (managed system)
  └── AssetAccount (privileged account on that system)
        └── TaskProperties (rotation status, last check/change dates)

AssetPartition (organizational container)
  ├── Assets (belong to one partition)
  ├── PasswordProfiles (check/change schedules)
  └── SshKeyProfiles (SSH key rotation schedules)

Role (what the UI calls 'Entitlement')
  ├── Members (Users/UserGroups who can request access)
  └── AccessPolicies (rules governing access)
        ├── ScopeItems (which accounts/assets are covered)
        ├── ApproverSets (who must approve)
        └── AccessRequestProperties (type, duration, emergency)

AccessRequest (a user's request for access)
  States: Pending → Approved → Available → CheckedIn/Expired
  Types: Password, SSH Key, RDP, SSH, Telnet, API Key
```

## Common ID Lookups

Most Safeguard objects are referenced by integer ID. To find an ID by name:
- `GET /v4/Assets?filter=Name ieq 'myserver'&fields=Id,Name`
- `GET /v4/Users?filter=UserName ieq 'john.smith'&fields=Id,UserName`
- `GET /v4/Roles?filter=Name icontains 'help desk'&fields=Id,Name`
