# Safeguard Terminology Map

The Safeguard product UI and documentation use different terminology than the REST API.
When a user asks about a concept, use this mapping to find the correct API endpoint.

| Product/UI Term | API Endpoint | Notes |
|-----------------|--------------|-------|
| Entitlement | /v4/Roles | 'Roles' in the API are what the UI calls 'Entitlements' — they group users and define what they can request access to |
| Access Request Policy | /v4/AccessPolicies | Defines rules for how access requests are handled (approval, time limits, etc.) |
| Partition | /v4/AssetPartitions | Logical grouping of assets that share password management settings |
| Managed System, Managed Asset | /v4/Assets | Any system (server, network device, etc.) managed by Safeguard |
| Managed Account | /v4/AssetAccounts | A privileged account on a managed asset |
| Password Profile, Change Profile | /v4/PasswordProfiles | Rules for password generation and rotation |
| Platform, Connection Template | /v4/Platforms | Defines how Safeguard connects to a type of system |
| Linked Account, Personal Account | /v4/PersonalAccounts | Accounts linked to a user for personal credential access |
| Session Recording | /v4/Sessions | Recorded privileged sessions (RDP, SSH, etc.) |
| Uptime, Boot time, System time | /v4/ApplianceStatus, /v4/ApplianceStatus/Health, /v4/SystemTime, /v4/Version | Appliance status/health endpoints; uptime is derived from these |

## Tips

- Use Safeguard_Discover with either the product term or the API term — both will work.
- The Roles endpoint manages entitlements: members (who can request) and policies (what they can request).
- An 'Entitlement' (Role) contains Access Policies (AccessPolicies), which define scope and approval rules.
- 'Asset' and 'AssetAccount' are the foundational objects — most workflows start by finding these.
