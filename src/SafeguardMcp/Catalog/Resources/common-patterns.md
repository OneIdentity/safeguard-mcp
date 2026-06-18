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

# Bulk asset / account operations — use the Batch* endpoints (POST /v4/{Resource}/BatchCreate,
# /BatchUpdate, /BatchDelete) on Assets, AssetAccounts, Users, UserGroups, AccountGroups, AssetGroups.
# Body is a JSON array; partial failures return per-row detail in one envelope.
# See workflow recipe: bulk-asset-operations.
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

## Response Envelope, Paging & Formats

`Safeguard_Execute` returns JSON as a `{ data, meta }` envelope:

- `data` — the actual API payload (read this, not the envelope root).
- `meta.notices[]` — applied auto-limit, paging hints, truncation events, workflow suggestions.
- `meta.paging` — `{ page, limit, returned, more, next }`; follow `meta.paging.next` for the next page.

Responses are capped at ~30 KB. For fat endpoints (audit logs, Assets, AssetAccounts) project with
`fields=` or page via `meta.paging.next` rather than fetching everything.

`format=csv` is GET-only (tabular, smaller for large reads); non-GET methods must use `format=json`
(the tool rejects `format=csv` on writes with an error).

## Access Requests — Open Lifecycle

`Safeguard_OpenAccessRequest` pre-validates the (account, asset, type) combo against
`/v4/Me/RequestEntitlements`, then waits up to 5s for auto-approval. Exactly one `meta.notices[0].kind`
names the next step (branch on this, not the raw `State`):

| Notice kind | Meaning / next step |
|---|---|
| `auto_approved_ready` | Ready now — call Safeguard_RetrieveCredential, or POST .../InitializeSession |
| `pending_approval_check_back` | Human approval needed (can take hours); poll GET /v4/AccessRequests/{id} |
| `pending_scheduled` | Approved but waiting for the RequestedFor time |
| `pending_account_action` | Appliance is elevating/restoring the account (usually < 1 min) |
| `terminated_before_ready` | Terminated/Expired/Closed/Complete — submit a fresh request if still needed |

After a successful POST .../InitializeSession the response carries a
`session_token_issued_offer_to_launch` notice: **present** the manual launch command and ConnectionUri
AND explicitly offer to launch on the user's behalf (with the request id for later check-in). Never
auto-launch; never omit the offer. The credential is injected at the proxy and never enters agent context.

## Access Requests — Close Dispatch

`Safeguard_CloseAccessRequest` looks up the request first (a 404 means you neither own it nor are a
policy admin), then dispatches automatically by `State`. Requester-path action per state:

| State | Requester action |
|---|---|
| New, PendingApproval, Approved, PendingTimeRequested, RequestAvailable, PendingAccountRestored, PendingPasswordReset | Cancel |
| PasswordCheckedOut, SshKeyCheckedOut, SessionInitialized | CheckIn |
| Expired, PendingAcknowledgment | Acknowledge |
| RequestCheckedIn, Terminated, PendingReview, PendingAccountSuspended, PendingAccountDemoted | needs PolicyAdmin (requester gets a diagnostic) |
| Closed, Complete, Reclaimed | no-op (terminal) |

A PolicyAdmin acting on someone else's request (or on a needs-admin row) dispatches **Close** from any
non-terminal state. `comment` is attached to Cancel/Close/Acknowledge (ignored for CheckIn, truncated to
255 chars with a notice). Response is `{ ok, action, request, notices }`; `allFields=true` returns the
appliance body verbatim.

## Sensitive Credential Material

Passwords, SSH private keys, API client/secret history, TOTP codes, generated passwords,
personal-account passwords/history, and secure-file content are **not** callable via `Safeguard_Execute`
— it refuses and redirects (a `sensitive_endpoint_redirected` envelope naming the matching kind).
Use `Safeguard_RetrieveCredential` (kinds: access-request-password, access-request-ssh-key,
access-request-api-key, access-request-totp, access-request-file, personal-account-password,
personal-account-password-history, personal-account-totp, generated-password,
asset-account-api-secret-history). It returns a two-block response: block 1 (assistant) is metadata
only; block 2 (user) carries the plaintext.

For managed-account passwords without retrieving plaintext:
- Rotate per partition rule and push to the asset: `POST /v4/AssetAccounts/{id}/ChangePassword` (no body).
- Generate a rule-compliant value out of band: `POST /v4/AssetAccounts/{id}/GeneratePassword` (no body).
- Set a known value: `PUT /v4/AssetAccounts/{id}/Password` (body = value).
Never mint passwords client-side — it bypasses the password rule and leaks plaintext.
