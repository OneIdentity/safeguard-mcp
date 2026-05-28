# Agent Simulation Integration Tests

## Goal

Validate that the Safeguard MCP server is **discoverable, schema-guided, and callable** by an AI agent that has no prior knowledge of the Safeguard API — only the MCP tool descriptions and responses.

## Motivation: Why This Matters

The Safeguard MCP server exposes Safeguard for Privileged Passwords (SPP) functionality
to AI agents that work on behalf of their human owner. It uses an **OAuth2-style
interaction** — the agent never receives the user's credentials directly. Instead, a
browser-based login flow produces a token that the MCP server holds on behalf of the
agent. This means:

1. The agent **cannot read documentation or login screens** — it can only use what the
   MCP tools expose.
2. The agent's only interface is: discover endpoints → understand schemas → execute calls.
3. If our tools are ambiguous, incomplete, or use undocumented magic values, the agent
   will fail — and the user won't understand why.

The unique **cataloging and dispatching system** (TerminologyMap + StaticCatalog +
DynamicCatalog + SchemaHints + Workflows) is the bridge between natural language
("check out a password for the Linux admin account") and the correct API call sequence
(`GET /v4/Me/RequestEntitlements` → `POST /v4/AccessRequests` → `POST .../CheckOutPassword`).

**These tests validate that bridge.** Even though we cannot predict exactly what words
an agent will search for, we can establish confidence that the most common PAM
operations are reachable through reasonable natural-language queries.

## What We're Testing

We're not testing the Safeguard API itself. We're testing whether our MCP tool layer provides enough guidance for an agent to succeed. Specifically:

1. **Discoverability** — Can an agent find the right endpoint using natural-language search terms?
2. **Schema Usefulness** — Does `Safeguard_Schema` return enough information to construct a valid request body without guessing?
3. **End-to-End Callability** — Does a request shaped entirely from schema output actually succeed?
4. **Workflow Guidance** — Do workflow recipes guide multi-step operations that actually work?
5. **Error Recoverability** — When something fails, does the error message help the agent correct course?

## What We Cannot Test

- Whether a specific LLM correctly interprets our tool descriptions
- Whether an LLM picks optimal search terms
- Whether an LLM correctly maps types to JSON values

## Design Principles

### The "Blind Agent" Rule

Tests must not hard-code knowledge about Safeguard's API that wouldn't be available through our tools. For example:
- ❌ `body: { PlatformId = 188 }` — where did 188 come from?
- ✅ First discover platforms, pick one from the list, then use its ID

### The 3-Step Pipeline

Every scenario follows this pattern:
```
DISCOVER → SCHEMA → EXECUTE
```

Some scenarios add a 4th step (VERIFY) where we read back what we created to confirm it worked.

### Setup/Execute/Cleanup Lifecycle

Following the SafeguardDotNet TestFramework pattern:
- **Setup**: Create prerequisite objects using a privileged admin (direct API calls OK here — this simulates "the environment exists before the agent arrives")
- **Execute**: Simulate agent behavior using ONLY the MCP tool methods (discover, schema, execute, workflow)
- **Cleanup**: LIFO deletion of all created objects (always runs)

---

## Test Infrastructure

### `AgentSimulationFixture` (new shared fixture)

Extends `ApplianceFixture` with:
- Creates a test admin user with ALL AdminRoles
- Authenticates the MCP `ConnectionManager` as that user
- Provides `Discover()`, `Schema()`, `Execute()`, `QueryHelp()`, `Workflows()` helper methods that invoke the MCP tool C# methods directly (not HTTP — testing the tool logic, not transport)
- Provides `RegisterCleanup(method, path)` for LIFO teardown
- Pre-cleans stale objects from prior failed runs (by prefix)

### Helper: `SchemaBodyBuilder`

A deliberately naive body builder that simulates what an agent would do:
- Takes an `ApiSchema` response
- For each **required** field, generates a test value based solely on the `Type` and `Name`:
  - `string` → `"Test_{FieldName}_{random}"`
  - `integer` → attempts discovery of valid IDs (e.g., looks up PlatformId by calling GET)
  - `boolean` → `false`
  - `object` (nested) → `{ "Id": <discovered_value> }`
- Returns a JSON body ready for Execute
- This is the key validation: **if SchemaBodyBuilder can construct a working request from schema alone, the schema is useful**

### Helper: `DiscoverAssertions`

Validates discovery results:
- `AssertFindsEndpoint(results, method, pathContains)` — the endpoint appears
- `AssertHasBodyIndicator(results, path)` — `[body]` marker present for POST/PUT
- `AssertHasParams(results, path)` — query params listed

---

## Test Suites

### Suite 1: Discovery Quality

Tests that reasonable search terms lead to correct endpoints.

| # | Agent Intent | Search Terms | Expected to Find |
|---|---|---|---|
| 1 | "I want to list users" | `search: "users"` | GET /v4/Users |
| 2 | "Create an asset" | `search: "assets", method: "POST"` | POST /v4/Assets |
| 3 | "Manage entitlements" | `search: "entitlements"` | GET/POST /v4/Roles (terminology mapping!) |
| 4 | "Check health" | `search: "health"` | GET /v4/ApplianceStatus/Health |
| 5 | "Password checkout" | `search: "password checkout"` | POST .../CheckOutPassword |
| 6 | "Managed accounts" | `search: "managed accounts"` | /v4/AssetAccounts (terminology mapping!) |
| 7 | "Access request" | `search: "access request"` | /v4/AccessRequests |
| 8 | "Cluster status" | `search: "cluster"` | /v4/Cluster/Members |
| 9 | "Filter by service" | `service: "Appliance"` | Only Appliance results |
| 10 | "What can I DELETE?" | `method: "DELETE"` | DELETE endpoints only |
| 11 | "No results" | `search: "xyzzy_nonexistent"` | Helpful "no results" message |
| 12 | "Narrow from broad" | `search: "users", method: "POST"` | POST /v4/Users only |
| 13 | "Bulk import" | `search: "batch"` | BatchCreate endpoints |
| 14 | "Audit trail" | `search: "audit"` | /v4/AuditLog/* endpoints |
| 15 | "PAM" | `search: "privileged access"` | AccessRequests (terminology!) |
| 16 | "User groups" | `search: "user group"` | /v4/UserGroups |
| 17 | "Check out a password" | `search: "checkout"` | CheckOutPassword |
| 18 | "Discovery jobs" | `search: "discovery"` | DiscoveredAccounts/DiscoveredAssets |

### Suite 2: Schema Quality

Tests that schemas provide enough to build valid requests, **including that SchemaHints
augment raw swagger data with agent-actionable guidance**.

| # | Endpoint | Validates |
|---|---|---|
| 1 | POST /v4/Users | Has Name (required), PrimaryAuthenticationProvider (required + nested) |
| 2 | POST /v4/Assets | Has Name (required), NetworkAddress, PlatformId |
| 3 | POST /v4/AssetAccounts | Has Name (required), Asset (nested, has Id) |
| 4 | POST /v4/Roles | Has Name (required) |
| 5 | POST /v4/AccessPolicies | Has RoleId, AccessRequestProperties (nested) |
| 6 | GET /v4/Users (response) | Has Id, Name, AdminRoles, Disabled |
| 7 | No schema available | Returns clear "connect first" guidance |
| 8 | Path with parameters | GET /v4/Users/{id} — still returns field info |
| 9 | SchemaHints present | POST /v4/Assets schema includes "AGENT HINTS:" section with PlatformId guidance |
| 10 | Hint says discover-by-name | PlatformId hint says to GET /v4/Platforms by DisplayName, NOT hard-code an ID |
| 11 | Sentinel value hint | AssetPartitionId hint mentions `-1` for default partition |
| 12 | Nested object hint | PrimaryAuthenticationProvider hint shows `{"Id": -1}` pattern |

### Suite 3: Schema-Guided Execution

Each test follows the full blind-agent pipeline. **No hard-coded bodies.** The `SchemaBodyBuilder` constructs requests from schema output + schema hints + discovery calls. If a test fails, it means the agent experience is broken at that point — which is exactly what we want to know.

#### 3a: Create a User
```
1. Discover: search="users", method="POST" → finds POST /v4/Users
2. Schema: path="/v4/Users", method="POST" → gets required fields + hints
3. SchemaBodyBuilder reads hints for PrimaryAuthenticationProvider → {Id: -1}
4. Execute: POST /v4/Users with built body
5. Verify: GET /v4/Users/{id} returns the created user
6. Cleanup: DELETE /v4/Users/{id}
```

#### 3b: Create an Asset (Platform Discovery by Name)
```
1. Schema: path="/v4/Assets", method="POST" → gets fields + hints
2. SchemaBodyBuilder reads PlatformId hint → told to GET /v4/Platforms by DisplayName
3. SchemaBodyBuilder calls: GET /v4/Platforms?filter=DisplayName eq 'Windows Server'&fields=Id,DisplayName
4. Uses returned Id (NOT a hard-coded number — validates hint-driven discovery)
5. SchemaBodyBuilder reads hint for AssetPartitionId → uses -1 (sentinel)
6. Discover: search="assets", method="POST" → finds POST /v4/Assets
7. Execute: POST /v4/Assets with built body
8. Verify: GET /v4/Assets/{id} → Name, PlatformId, NetworkAddress all match
9. Cleanup: DELETE /v4/Assets/{id}
```

#### 3c: Create an Account on an Asset
```
1. (Setup provides an asset)
2. Discover: search="asset accounts", method="POST"
3. Schema: path="/v4/AssetAccounts", method="POST" → hints say Asset is {Id: X}
4. SchemaBodyBuilder uses setup's AssetId
5. Execute: POST /v4/AssetAccounts
6. Verify: GET /v4/AssetAccounts/{id}
7. Cleanup: DELETE /v4/AssetAccounts/{id}
```

#### 3d: Query with Filters (Schema-guided parameters)
```
1. QueryHelp: path="/v4/Users" → get available fields and syntax
2. Execute: GET /v4/Users with query "filter=Name icontains 'Test'&fields=Id,Name&orderby=Name&limit=5"
3. Verify: Response is valid JSON array, each item has only Id and Name
```

#### 3e: Update an Object (PUT)
```
1. (Setup creates a user)
2. Schema: path="/v4/Users", method="PUT" → what fields for update?
3. Execute: GET /v4/Users/{id} → get current state
4. Modify one field (Description)
5. Execute: PUT /v4/Users/{id} with modified body
6. Verify: GET /v4/Users/{id} shows new Description
```

#### 3f: Delete an Object
```
1. (Setup creates a throwaway user)
2. Discover: search="users", method="DELETE" → finds DELETE /v4/Users/{id}
3. Execute: DELETE /v4/Users/{id}
4. Verify: GET /v4/Users/{id} → 404
```

### Suite 4: Workflow-Guided Multi-Step Operations

Tests that workflow recipes produce working API call sequences.

#### 4a: Health Check Workflow
```
1. Workflows: search="health" → gets health-check recipe
2. Follow Step 1: GET /v4/Status → State == "Online" (Notification service, no auth)
3. Follow Step 2: GET /v4/ApplianceStatus/Health → has CPU/memory data (Appliance service)
4. Follow Step 3: GET /v4/Cluster/Members → at least one member (Core service)
```

#### 4b: Create Entitlement Workflow
```
1. Workflows: id="create-entitlement" → gets recipe
2. Follow Step 1: POST /v4/Roles with {Name: "Test_Entitlement_..."}
3. Follow Step 2: POST /v4/Roles/{id}/Members/Add with test user
4. Follow Step 3: POST /v4/AccessPolicies with recipe-guided body
5. Verify: GET /v4/Roles/{id} → MemberCount > 0
6. Cleanup: DELETE policy, DELETE role
```

#### 4c: Task Triage Workflow
```
1. Workflows: id="task-triage" → gets recipe
2. Follow: GET /v4/AssetAccounts?filter=TaskProperties.HasAccountTaskFailure eq true&count=true
3. Verify: Returns a count (might be 0 — that's fine, validates the query works)
```

#### 4d: Access Request Lifecycle Workflow (Password Checkout)
```
1. Workflows: search="password access request" → gets recipe
2. (Setup: create asset + account + entitlement + policy scoped to that account)
3. Follow Step 1: GET /v4/Me/RequestEntitlements → find available entitlements
4. Follow Step 2: POST /v4/AccessRequests with {AccountId, AccessRequestType: "Password"}
5. Follow Step 3: POST /v4/AccessRequests/{id}/Approve (test admin is also approver)
6. Follow Step 4: POST /v4/AccessRequests/{id}/CheckOutPassword
7. Verify: Response contains a credential string
8. Follow Step 5: POST /v4/AccessRequests/{id}/CheckIn
9. Cleanup: Entitlement & policy cleaned up by fixture
```

### Suite 5: Error Guidance & Recovery

Tests that error messages help an agent self-correct.

| # | Scenario | Expected Guidance |
|---|---|---|
| 1 | POST to wrong path | 404 with "NotFound" — agent should re-discover |
| 2 | Missing required field | 400 with field name in error message |
| 3 | Invalid field name in filter | 400 with "Invalid field property" and the bad name |
| 4 | Wrong type (string where int expected) | 400 with type-related error |
| 5 | Insufficient permissions | 403 — tells agent they need different role |
| 6 | Invalid field in `fields` projection | 400 with field name |

### Suite 6: Edge Cases & Resilience

| # | Test | What it validates |
|---|---|---|
| 1 | Empty search results | `filter=Name eq 'IMPOSSIBLE_VALUE_12345'` returns `[]` not error |
| 2 | Special characters in filter | `filter=Name eq 'O''Brien'` (escaped single quote) |
| 3 | Count returns integer | `count=true` returns parseable integer (not JSON array!) |
| 4 | Large limit gets capped | `limit=99999` doesn't crash, returns data |
| 5 | All query params at once | fields + filter + orderby + limit + page combined |
| 6 | CSV format works | `format=csv` on a GET returns comma-separated data |
| 7 | Schema before connect | Returns "connect first" message, not crash |
| 8 | Execute before connect | Returns clear auth-required error |
| 9 | Quick search `q=` param | `q=admin` returns matching results |
| 10 | Batch endpoint discovery | Agent finds `/v4/Assets/BatchCreate` from "bulk" or "batch" |

---

## Phase 0: MCP Server Gaps to Close Before Testing

The following MCP server improvements must be implemented first. Without them, the agent
simulation tests cannot pass — the tools simply don't provide enough information for a
blind agent to succeed.

### 0A: Schema Hints (`src/SafeguardMcp/Catalog/SchemaHints.cs`)

**Problem**: The swagger-derived schema reports property names and types but says nothing
about Safeguard conventions (sentinel values like `-1`, nested object patterns, enum
values, or how to discover valid IDs). An agent seeing `PlatformId (integer)` has no
idea what to put there.

**Solution**: A static dictionary keyed by `TypeName.PropertyName` (or just `PropertyName`
for globally-applicable patterns) that appends agent-friendly guidance to `Safeguard_Schema`
output as an `AGENT HINTS:` section.

**Location**: `src/SafeguardMcp/Catalog/SchemaHints.cs`
**Wiring**: Modify `SafeguardApiTool.AppendSchemaSection()` to call `SchemaHints.GetHints()`
and append them after the property list.

#### Hints to implement:

```csharp
// --- Identity & Auth ---
"PrimaryAuthenticationProvider" =>
    "Nested object reference. Use {\"Id\": -1} for the built-in local provider. "
    + "GET /v4/AuthenticationProviders to list all available providers."

"IdentityProvider" =>
    "Nested object reference. Use {\"Id\": -1} for the built-in local identity provider. "
    + "GET /v4/IdentityProviders to list all."

// --- Asset Partitions ---
"AssetPartitionId" =>
    "Use -1 for the default asset partition. "
    + "GET /v4/AssetPartitions to list available partitions."

// --- Platform Selection ---
"PlatformId" =>
    "Integer ID of a platform definition. Platform IDs are appliance-specific. "
    + "GET /v4/Platforms?fields=Id,DisplayName,PlatformType to browse. "
    + "Use filter: GET /v4/Platforms?filter=DisplayName icontains 'Windows Server' "
    + "Prefer the version-independent entries (e.g. 'Windows Server', 'Ubuntu', "
    + "'Active Directory', 'Red Hat Enterprise Linux (RHEL)') over version-specific ones "
    + "(e.g. 'Windows Server 2019', 'Ubuntu 20.04 x86_64') unless targeting a specific OS version. "
    + "Common PlatformType values: Windows, Ubuntu, MicrosoftAD, RedHatEnterprise, "
    + "LinuxOther, AmazonLinux, SqlServer, PostgreSQL, Oracle, CiscoIOS."

// --- Nested Object References ---
"Asset" (on AssetAccounts) =>
    "Nested object reference. Use {\"Id\": <assetId>}. "
    + "GET /v4/Assets?fields=Id,Name,NetworkAddress to find asset IDs."

"Account" (on AccessRequests) =>
    "Nested object reference. Use {\"Id\": <accountId>}. "
    + "GET /v4/AssetAccounts?fields=Id,Name,Asset.Name to find account IDs."

// --- Entitlements & Policy ---
"RoleId" (on AccessPolicies) =>
    "Id of an existing Role (called 'Entitlement' in the UI). "
    + "Create one first via POST /v4/Roles, then reference it here."

"AccessRequestType" =>
    "String enum. Allowed values: \"Password\", \"Ssh\", \"RemoteDesktop\", "
    + "\"RdpFile\", \"Telnet\", \"SshKey\", \"RemoteDesktopApplication\", \"ApiKey\", \"File\"."

"AccessRequestProperties" (on AccessPolicies) =>
    "Nested object describing the type and constraints of access. Required properties: "
    + "AccessRequestType (see enum above). Optional: AllowSimultaneousAccess (bool), "
    + "MaximumDurationDays (int), MaximumDurationHours (int), "
    + "ChangePasswordAfterCheckin (bool)."

// --- Member/Scope Operations ---
"Members/Add, Members/Remove pattern" =>
    "Endpoints ending in /{operation} use Add or Remove as the URL segment. "
    + "Body is a JSON array of objects: [{\"Id\": <userId>}, {\"Id\": <userId2>}]."

"ScopeItems/Add" =>
    "Body is a JSON array of account or asset group references: "
    + "[{\"Id\": <accountId>}] or [{\"Id\": <assetGroupId>}]."

// --- Misc ---
"NetworkAddress" =>
    "Hostname, IP address, or FQDN of the target system."

"AdminRoles" =>
    "Array of admin role strings. Available roles: GlobalAdmin, Auditor, "
    + "ApplicationAuditor, SystemAuditor, AssetAdmin, ApplianceAdmin, "
    + "PolicyAdmin, UserAdmin, HelpdeskAdmin, OperationsAdmin."
```

#### Platform Hint Design Note

Platform IDs are **database-assigned and differ between appliances and versions**.
The `PlatformType` enum (defined in `Pangaea.Data.Transfer.V2.PlatformTasks.PlatformType`)
is stable across releases, but the numeric ID that the API returns is not. For example:
- `Windows Server` = Id 547 on one appliance, could differ on another
- `Ubuntu` = Id 545 on this appliance
- `Active Directory` = Id 522 on this appliance

The hint MUST tell the agent to **discover platforms by name/type**, not hard-code IDs.
Newer "version-independent" platform entries (e.g., "Windows Server" without a year suffix,
"Ubuntu" without a version number) are preferred because they automatically inherit
the latest connection logic for that platform family.

### 0B: Terminology Map Expansion (`src/SafeguardMcp/Catalog/TerminologyMap.cs`)

**Problem**: The current terminology map has 9 alias groups. Testing against a live
appliance reveals many common PAM terms that agents will use but that currently return
no results or wrong results.

**New alias groups to add**:

```csharp
// Password operations
["checkout", "check out", "password release", "password checkout", "checkoutpassword"],

// Directory / AD / LDAP
["directory", "directories", "active directory", "ad", "ldap", "identityprovider", "identityproviders"],

// User groups
["user group", "user groups", "usergroups", "usergroup"],

// Account groups
["account group", "account groups", "accountgroups", "accountgroup"],

// Asset groups
["asset group", "asset groups", "assetgroups", "assetgroup"],

// Audit / logging
["audit", "audit log", "auditlog", "activity", "activities", "event log"],

// Access requests
["access request", "access requests", "accessrequests", "request", "requests"],

// Service accounts / dependent accounts
["service account", "service accounts", "dependent account", "dependent accounts"],

// Cluster & appliance
["cluster", "cluster members", "node", "nodes", "appliance", "appliances"],

// Discovery
["discovery", "discover", "discoveredassets", "discoveredaccounts", "discovery job"],

// Password management tasks
["password change", "password check", "change password", "check password", "rotation"],

// SSH keys
["ssh key", "ssh keys", "sshkey", "sshkeys"],

// Certificate management
["certificate", "certificates", "cert", "certs", "ssl", "tls"],

// Backup / restore
["backup", "backups", "restore", "archive"],

// Common PAM vocabulary (umbrella terms agents will try)
["privilege", "privileged", "pam", "privileged access"],
```

### 0C: Batch Endpoint Documentation (Workflow or Hint)

**Problem**: Safeguard provides batch endpoints for bulk operations that agents handling
large imports will need. These are not discoverable by searching "create" because they're
at a sub-path like `/v4/Assets/BatchCreate`. An agent that knows `POST /v4/Assets` exists
won't know that `POST /v4/Assets/BatchCreate` exists for bulk operations unless explicitly
guided.

**Available batch endpoints** (all `POST`, all in Core service, all accept a JSON array body):

| Resource | BatchCreate | BatchUpdate | BatchDelete |
|----------|-------------|-------------|-------------|
| `/v4/Assets` | ✅ | ✅ | ✅ |
| `/v4/AssetAccounts` | ✅ | ✅ | ✅ |
| `/v4/Users` | ✅ | ✅ | ✅ |
| `/v4/UserGroups` | ✅ | ✅ | ✅ |
| `/v4/AccountGroups` | ✅ | ✅ | ✅ |
| `/v4/AssetGroups` | ✅ | ✅ | ✅ |
| `/v4/AccessRequests` | ✅ (BatchCreate) | — | — |
| `/v4/AccessRequests` | BatchApprove | BatchDeny | BatchReview |

**Solution**: Add a `bulk-import` workflow recipe that documents the batch pattern, AND
add a schema hint for batch endpoints:

```
"Batch operations" =>
    "For bulk operations, append /BatchCreate, /BatchUpdate, or /BatchDelete to the "
    + "resource path (e.g., POST /v4/Assets/BatchCreate). The request body is a JSON "
    + "array of objects, each with the same format as a single-item POST/PUT. "
    + "Available for: Assets, AssetAccounts, Users, UserGroups, AccountGroups, AssetGroups."
```

Also update the existing `bulk-asset-import.md` workflow to reference these endpoints
explicitly and note the array-body pattern.

### 0D: Access Request Lifecycle Workflow

**Problem**: The most common agent use case — requesting, approving, and checking out
a password — has no dedicated workflow recipe. The `password-access-request.md` may
exist but needs validation against the actual API paths.

**Solution**: Ensure there is a complete `password-access-request.md` workflow that covers:
1. Find available entitlements: `GET /v4/Me/RequestEntitlements`
2. Create the request: `POST /v4/AccessRequests`
3. Approve (if the agent also has approver role): `POST /v4/AccessRequests/{id}/Approve`
4. Check out: `POST /v4/AccessRequests/{id}/CheckOutPassword`
5. Check in: `POST /v4/AccessRequests/{id}/CheckIn`

---

## Phase 1: Test Infrastructure

### 1A: `AgentSimulationFixture` (new shared fixture)

Extends `ApplianceFixture` with:
- Authenticates as bootstrap admin (`admin`, lowercase — note: this is the default
  bootstrap username, NOT "Admin")
- Creates a test admin user with ALL AdminRoles (bootstrap admin has UserAdmin,
  which is sufficient to create other admins)
- Re-authenticates as the newly created test admin for actual test execution
- Provides `Discover()`, `Schema()`, `Execute()`, `QueryHelp()`, `Workflows()` helper
  methods that invoke the MCP tool C# methods directly
- Provides `RegisterCleanup(method, path)` for LIFO teardown
- Pre-cleans stale objects from prior failed runs (by naming prefix)
- Waits for dynamic catalog load to complete (3-5 seconds after connection)

### 1B: `SchemaBodyBuilder`

A deliberately naive body builder that simulates what an agent would do:
- Takes an `ApiSchema` response + the AGENT HINTS section text
- For each **required** field, generates a test value based on `Type`, `Name`, and hints:
  - `string` → `"Test_{FieldName}_{random}"`
  - `integer` with platform hint → calls `GET /v4/Platforms?filter=DisplayName eq 'Windows Server'`
  - `integer` with sentinel hint (e.g., `-1`) → uses the sentinel
  - `boolean` → `false`
  - `object` (nested) with hint → follows the hint (e.g., `{"Id": -1}`)
  - `object` (nested) without hint → **FAIL** (this is a discoverability bug)
- Returns a JSON body ready for Execute
- **If SchemaBodyBuilder cannot construct a valid body from schema + hints alone, the
  test fails. This is by design — it surfaces gaps.**

### 1C: `DiscoverAssertions`

Validates discovery results:
- `AssertFindsEndpoint(results, method, pathContains)`
- `AssertHasBodyIndicator(results, path)` — `[body]` marker present
- `AssertHasParams(results, path)` — query params listed
- `AssertTerminologyWorks(searchTerm, expectedPath)` — synonym resolution

---

## Implementation Order

### Phase 0 — MCP Server Improvements (prerequisite for tests)

| Step | Deliverable | Location |
|------|-------------|----------|
| 0A | `SchemaHints.cs` + wiring into `AppendSchemaSection` | `src/SafeguardMcp/Catalog/SchemaHints.cs` |
| 0B | Terminology map expansion (add ~15 alias groups) | `src/SafeguardMcp/Catalog/TerminologyMap.cs` |
| 0C | Update `bulk-asset-import.md` workflow with batch patterns | `src/SafeguardMcp/Workflows/bulk-asset-import.md` |
| 0D | Validate/update `password-access-request.md` workflow | `src/SafeguardMcp/Workflows/password-access-request.md` |

### Phase 1 — Test Infrastructure

| Step | Deliverable | Location |
|------|-------------|----------|
| 1A | `AgentSimulationFixture` | `tests/SafeguardMcp.IntegrationTests/` |
| 1B | `SchemaBodyBuilder` helper | `tests/SafeguardMcp.IntegrationTests/` |
| 1C | `DiscoverAssertions` helper | `tests/SafeguardMcp.IntegrationTests/` |

### Phase 2 — Test Suites

| Step | Suite | Test Count |
|------|-------|------------|
| 2A | Suite 1: Discovery Quality | 18 tests |
| 2B | Suite 2: Schema Quality | 12 tests |
| 2C | Suite 3: Schema-Guided Execution | 6 scenarios (~18 assertions) |
| 2D | Suite 4: Workflow-Guided Operations | 4 scenarios (~16 assertions) |
| 2E | Suite 5: Error Guidance | 6 tests |
| 2F | Suite 6: Edge Cases & Resilience | 10 tests |

**Total: ~80 tests**

Phase 0 is the MCP server work. Phase 1 is test infrastructure. Phase 2 is the test
suites themselves, written serially (each may surface further gaps).

---

## Success Criteria

- Phase 0 deliverables are implemented and the existing unit tests still pass
- All Phase 2 tests pass against a live appliance with a bootstrap admin (`admin`/password)
- Suite 3 tests succeed **without any hard-coded Safeguard knowledge in the test body
  construction** (except what the tools themselves reveal via schema + hints)
- Any discoverability gap found is documented as a known limitation or filed as an
  enhancement issue
- The test suite itself serves as documentation of "how an agent would use this server"

---

## Notes & Decisions

### Bootstrap Admin

The Safeguard bootstrap admin username is **`admin`** (lowercase, id=-2). It has
the following admin roles: `GlobalAdmin, ApplianceAdmin, UserAdmin, HelpdeskAdmin,
OperationsAdmin, SystemAuditor`. It is **missing**: `AssetAdmin, PolicyAdmin, Auditor,
ApplicationAuditor`.

This means:
- ✅ Bootstrap admin CAN create test users with all roles (has UserAdmin)
- ❌ Bootstrap admin CANNOT directly manage assets, partitions, or policies
- → The fixture must create a test admin with all roles and re-authenticate as them

### Platform IDs Are Not Portable

Platform IDs (e.g., 547 for "Windows Server") are database-assigned and differ between
appliances and Safeguard versions. The `PlatformType` string enum (Windows, Ubuntu,
MicrosoftAD, etc.) is stable, but the numeric `Id` is not.

**SchemaHints must tell agents to discover platforms by name/type, never hard-code IDs.**

Prefer the version-independent platform entries (introduced in newer releases):
- "Windows Server" (Id varies) over "Windows Server 2019" (Id varies)
- "Ubuntu" over "Ubuntu 20.04 x86_64"
- "Red Hat Enterprise Linux (RHEL)" over "RHEL 8 x86_64"

### Batch Endpoints

No `Safeguard_Batch` tool exists. Batch operations are regular `POST` calls to
`/v4/{Resource}/BatchCreate` (or `BatchUpdate`, `BatchDelete`). The body is a JSON
array of objects in the same format as the single-item endpoint. An agent discovers
these via `Safeguard_Discover` searching "batch" or by following the `bulk-asset-import`
workflow recipe.

### The `Safeguard_Connect` Flow

Cannot be fully tested in this harness (requires interactive browser or env vars).
The agent simulation starts post-connection. The fixture authenticates using
environment variables (`SPP_HOST`, `SPP_USERNAME`, `SPP_PASSWORD`).

### SchemaBodyBuilder Strictness

If a required field has no hint and no discoverable value, the test **fails**. This is
intentional — it surfaces a gap in our schema/hints that would cause a real agent to
struggle. Every failure is a bug to fix in Phase 0 deliverables.

---

## How We Work Together

### Workflow

This plan is implemented **one step at a time** using an orchestrator session that
delegates to background agents. The human reviews each step before committing.

1. **Orchestrator picks the next step** from the Status Tracker (below)
2. **Background agent implements the step** — writes code, runs build, runs tests
3. **Orchestrator reviews the diff** and proposes a one-liner commit message
4. **Human reviews** — approves, requests changes, or rejects
5. **On approval**: commit with the proposed message, mark step ✅, move to next
6. **On rejection**: agent revises, re-presents for review

### Rules

- One logical commit per step (don't bundle unrelated changes)
- Every commit must build cleanly (`dotnet build safeguard-mcp.sln --nologo`)
- After Phase 0 steps, existing tests must still pass
- After Phase 1+2 steps, new tests must compile (may not pass until Phase 0 is done)
- Background agents get full context from this plan — they don't need conversation history
- The orchestrator preserves its context by delegating; it doesn't write code itself

### Commit Message Convention

```
<type>: <one-liner description>

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

Types: `feat` (new functionality), `test` (test code), `docs` (workflow .md files)

---

## Status Tracker

| Step | Description | Status | Commit |
|------|-------------|--------|--------|
| 0A | SchemaHints.cs + wiring | ✅ Done | 74171bc |
| 0B | TerminologyMap expansion | ✅ Done | bd6176c |
| 0C | bulk-asset-import.md update | ✅ Done | b1151d0 |
| 0D | password-access-request.md update | ✅ Done | f22976a |
| 1A | AgentSimulationFixture | ✅ Done | 327cd25 |
| 1B | SchemaBodyBuilder | ✅ Done | 6b618bd |
| 1C | DiscoverAssertions | ✅ Done | 910babc |
| 2A | Suite 1: Discovery Quality | ✅ Done | b1a5da9 |
| 2B | Suite 2: Schema Quality | ✅ Done | edf86ec |
| 2C | Suite 3: Schema-Guided Execution | ✅ Done | 562be98 |
| 2D | Suite 4: Workflow-Guided Operations | ✅ Done | c98760e |
| 2E | Suite 5: Error Guidance | ✅ Done | 309f063 |
| 2F | Suite 6: Edge Cases & Resilience | ✅ Done | 81cefdc |
