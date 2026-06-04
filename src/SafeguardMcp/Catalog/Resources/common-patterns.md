# Common Safeguard API Patterns

## Lookup by Name (then use ID)

Most write operations require integer IDs. The pattern is: search by name, extract ID, use in subsequent calls.

```
# Find an asset by name
GET /v4/Assets  query: filter=Name ieq 'web-server-01'&fields=Id,Name,NetworkAddress

# Find a user by username
GET /v4/Users  query: filter=UserName ieq 'john.smith'&fields=Id,UserName,DisplayName

# Find an entitlement (role) by name
GET /v4/Roles  query: filter=Name icontains 'help desk'&fields=Id,Name,MemberCount
```

## Create with Dependencies

Many objects have required relationships. Create in order:

```
# Creating a managed account requires: Asset exists, Platform assigned
1. GET /v4/Assets?filter=Name eq 'myserver'&fields=Id  → get assetId
2. POST /v4/AssetAccounts  body: {"AssetId": <assetId>, "Name": "sa_admin"}

# Creating an entitlement with policy requires: Role → Policy → Scope
1. POST /v4/Roles  body: {"Name": "DB Admins"}  → get roleId
2. POST /v4/Roles/{roleId}/Members/Add  body: [{"Id": <userId>}]
3. POST /v4/AccessPolicies  body: {"RoleId": <roleId>, "Name": "DB Password Access", ...}  → get policyId
4. POST /v4/AccessPolicies/{policyId}/ScopeItems/Add  body: [{"Id": <accountId>}]
```

## Bulk Operations

Use collection add/remove endpoints instead of individual calls:

```
# Add multiple members to a role at once
POST /v4/Roles/{roleId}/Members/Add  body: [{"Id": 1}, {"Id": 2}, {"Id": 3}]

# Add multiple accounts to a policy scope
POST /v4/AccessPolicies/{policyId}/ScopeItems/Add  body: [{"Id": 10}, {"Id": 11}]

# Remove members
POST /v4/Roles/{roleId}/Members/Remove  body: [{"Id": 1}]

# Batch asset creation — use POST /v4/Assets/BatchCreate (if available in your version)
```

## Audit & Activity Queries

```
# Recent password check/change activity
GET /v4/AuditLog/Passwords/Activities  query: orderby=-LogTime&limit=50

# Failed password changes
GET /v4/AuditLog/Passwords/Activities  query: filter=EventName eq 'PasswordChangeFailed'&orderby=-LogTime&limit=20

# Access request history for a user
GET /v4/AuditLog/AccessRequests/Activities  query: filter=RequesterName eq 'john.smith'&orderby=-LogTime&limit=50

# Login audit for a specific date range
GET /v4/AuditLog/Logins/Activities  query: filter=LogTime ge '2024-06-01' and LogTime lt '2024-07-01'&orderby=-LogTime
```

## Error Handling Patterns

| Status | Meaning | Action |
|--------|---------|--------|
| 400 | Validation error | Read the error body — it explains which field/value is wrong |
| 401 | Token expired or invalid | Reconnect with Safeguard_Connect |
| 403 | Insufficient permissions | The authenticated user lacks the required admin role |
| 404 | Object not found | Verify the ID exists; check for typos in the path |
| 409 | Conflict (duplicate, locked) | Object already exists or is being modified by another operation |
| 503 | Appliance in maintenance | Wait for maintenance to complete, then retry |

## Permissions Model

Safeguard uses role-based admin permissions (distinct from entitlements):

| Admin Role | Can Manage |
|-----------|-----------|
| Appliance Administrator | Appliance settings, certificates, cluster, backup, patches |
| Asset Administrator | Assets, accounts, partitions, profiles, platforms |
| Security Policy Administrator | Roles (entitlements), access policies, user groups |
| User Administrator | Users, user groups, identity providers |
| Auditor | Read-only access to audit logs and reports |
| Operations Administrator | Running tasks, platform task management |

## Useful /v4/Me Endpoints

These return data scoped to the currently authenticated user:

```
GET /v4/Me                        → current user info and permissions
GET /v4/Me/RequestableAccounts    → what accounts the user can request access to
GET /v4/Me/ActionableRequests     → requests waiting for this user's approval
GET /v4/Me/AccessRequests         → this user's own access requests
GET /v4/Me/PersonalPasswords      → personal vault entries
```
