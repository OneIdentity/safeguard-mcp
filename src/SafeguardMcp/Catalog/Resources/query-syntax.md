# Safeguard API Query Syntax Reference

All GET collection endpoints support these query parameters passed via the `query` parameter in Safeguard_Execute.

## Filter Operators

| Operator | Meaning | Example |
|----------|---------|---------|
| eq | Equals | `filter=Name eq 'Admin'` |
| ne | Not equals | `filter=Disabled ne true` |
| gt | Greater than | `filter=Id gt 100` |
| ge | Greater or equal | `filter=CreatedDate ge '2024-01-01'` |
| lt | Less than | `filter=Id lt 50` |
| le | Less or equal | `filter=CreatedDate le '2024-12-31'` |
| contains | Substring (case-sensitive) | `filter=Name contains 'srv'` |
| icontains | Substring (case-insensitive) | `filter=Name icontains 'admin'` |
| ieq | Equals (case-insensitive) | `filter=Name ieq 'administrator'` |
| sw | Starts with (case-sensitive) | `filter=Name sw 'DC'` |
| isw | Starts with (case-insensitive) | `filter=Name isw 'dc'` |
| ew | Ends with (case-sensitive) | `filter=Name ew '-prod'` |
| iew | Ends with (case-insensitive) | `filter=Name iew '-PROD'` |
| in | In list | `filter=Id in [1,2,3]` |

> **Operators are case-insensitive** (`EQ`, `Eq`, `eq` all work) but **string literals are case-sensitive** —
> `filter=AccessRequestType eq 'RemoteDesktop'` matches; `'remotedesktop'` does not. For case-insensitive
> string matching use `ieq` / `icontains` / `isw` / `iew`.
>
> See `Safeguard_Reference topic=enum` for the exact spelling of enum-typed values used with `eq` / `ne` / `in`.

## Logical Operators

- `and` — both conditions must match: `filter=Disabled eq false and Name contains 'admin'`
- `or` — either condition: `filter=State eq 'Available' or State eq 'Pending'`
- `not` — negate (unary): `filter=not (Disabled eq true)`
- Parentheses for grouping: `filter=(Name sw 'DC') and (Platform.DisplayName eq 'Windows')`

### Negating a list membership

There is **no `not_in` operator**. Combine the unary `not` with `in`:

- `filter=not (Id in [4,5,6])` ✅
- `filter=Id not_in [4,5,6]` ❌ — parses as an unknown identifier (HTTP 400, code 70009).
- `filter=Id not in [4,5,6]` ❌ — space-separated form is not recognized.

### Null comparisons

`null` is a literal. Use it with `eq` / `ne` to test for missing values:

- `filter=Description eq null`
- `filter=Description ne null`

### Synthetic `.Count` on collections

To-many relationships expose a synthetic `<Collection>.Count` for `filter` and `orderby` (not `fields`):

- `filter=ScopeItems.Count gt 0`
- `orderby=-Members.Count`

## Nested Properties (Relationships)

Safeguard exposes parent relationships as **nested objects**, not flat foreign-key columns.
Run `Safeguard_Schema` on a path to see the nested shape — complex properties show their child names on a `Fields:` line.

**To-one navigations are dottable** in `filter`, `fields`, and `orderby`:
- `fields=Id,Name,Asset.Id,Asset.Name,Asset.NetworkAddress` ✅
- `filter=Asset.Name icontains 'prod'` ✅
- `filter=TaskProperties.HasAccountTaskFailure eq true` ✅
- `orderby=Asset.Name` ✅

**Don't guess flat foreign-key column names** — they don't exist:
- `fields=AssetId,AssetName` ❌ → use `Asset.Id,Asset.Name`
- `filter=PartitionId eq 5` ❌ → use `AssetPartition.Id eq 5` (or whatever the schema names the parent)
- HTTP 400 (Code 70002): `Invalid field property - 'AssetId' is not a valid property name.`

**To-many (collection) navigations are NOT dottable** — call the child sub-resource endpoint instead:
- `fields=Id,Name,Profiles.Id,Profiles.Name` on `/v4/AssetPartitions` ❌
- Instead: `GET /v4/AssetPartitions/{id}/Profiles` with `fields=Id,Name,...` ✅
- Same pattern for `Members`, `Policies`, `Roles`, `Accounts`, `Tags`, etc.
- HTTP 400 (Code 70002): `Invalid field property - 'Profiles.Id' is not a valid property name.`

Rule of thumb: if the schema shows a property as `array<Type>` it's to-many — use a child endpoint. If it shows `object<Type>` it's to-one — dot into it freely.

## Field Selection

- Include specific fields: `fields=Id,Name,Description`
- Exclude verbose fields: `fields=-TaskProperties,-Platform,-ConnectionProperties`
- Reduces response size and improves performance

## Ordering

- Ascending: `orderby=Name`
- Descending: `orderby=-CreatedDate`
- Multiple fields: `orderby=Asset.Name,-CreatedDate`

> **Not OData.** Safeguard does **not** accept OData-style direction keywords. 
> Use the leading-minus convention (`-Field`) for descending. 
> `orderby=Name desc` or `orderby=Name asc` will be rejected with HTTP 400 
> (`Invalid order by property - 'Name desc' is not a valid property name`).

## Pagination

- `page=0&limit=50` — page is 0-indexed, limit is items per page
- Default limit varies by endpoint (typically 100)
- `count=true` — returns only the count, not the data

## Quick Search

- `q=searchterm` — searches across multiple text fields (like a global search)
- Simpler than filter but less precise

## Aggregation and Summarization

- `count=true` returns just the row count for any collection endpoint — the response body is a bare
  JSON integer (e.g. `52`), not an object or array. Pair with `filter=` for scoped counts (per
  partition, per requester, per time window). See workflow recipe `count-with-filter`.
- There is **no server-side group-by / distinct**: no `groupBy=`, no `distinct=`, no `aggregate=`
  parameter exists. For per-group counts, page filtered rows and tally in agent context (recipe
  `summarize-audit-log`). For per-time-bucket counts, issue N `count=true` calls, one per bucket
  (recipe `time-bucketed-counts`).

## Combined Examples

```
# Find disabled Windows accounts with failures, sorted by asset name
fields=Id,Name,Asset.Name&filter=(Disabled eq false) and (Platform.DisplayName eq 'Windows') and (TaskProperties.HasAccountTaskFailure eq true)&orderby=Asset.Name&limit=50

# Recent access requests for a specific user, newest first
fields=Id,AccessRequestType,State,AccountName,AssetName,CreatedDate&filter=RequesterName eq 'john.smith'&orderby=-CreatedDate&limit=20

# Count assets in a partition
filter=AssetPartitionId eq 1&count=true

# Accounts with passwords not changed in 90+ days
fields=Id,Name,Asset.Name,TaskProperties.LastSuccessPasswordChangeDate&filter=TaskProperties.LastSuccessPasswordChangeDate lt '2024-01-01'&orderby=TaskProperties.LastSuccessPasswordChangeDate&limit=50
```

## Reports vs Direct Queries

`/v4/Reports/*` endpoints aggregate across the whole estate and can take a long time to generate on large deployments. 
Prefer direct entity queries for narrow questions; reach for Reports only when you genuinely need an estate-wide aggregate.

**Use direct queries for narrow questions:**

| Question | Direct query | Avoid |
|----------|--------------|-------|
| Who is in role X? | `GET /v4/Roles/{id}/Members` | `/v4/Reports/Entitlements/UserEntitlements` |
| What policies does role X have? | `GET /v4/Roles/{id}/Policies` | `/v4/Reports/Entitlements/UserEntitlements` |
| Which accounts can user Y request? | `GET /v4/Users/{id}/Roles` then `GET /v4/Roles/{id}/Policies` | `/v4/Reports/Entitlements/UserEntitlements` |
| Compare two users' access | Two queries on `/v4/Users/{id}/Roles` | `/v4/Reports/Entitlements/UserEntitlements/Summary` |
| Owners of a single asset | `GET /v4/Reports/Ownership/Asset/{id}/Owners` (already scoped) | n/a |

**Use Reports only for estate-wide aggregates:**

- "How many users have access to anything?" → `/v4/Reports/Entitlements/UserEntitlements/Summary`
- "Generate a CSV of every user-account pair" → `/v4/Reports/Entitlements/UserEntitlements`
- "All accounts whose secrets changed last month" → `/v4/Reports/Tasks/AccountSecretsChanged`

Reports endpoints typically have their own field schemas that do not match the underlying entity schemas — call `Safeguard_Schema` on the specific report path before selecting fields.

## Sensitive Credential Material

Treat the following as sensitive — **do not echo, log, or include in summaries, tables, or follow-up tool calls**:
- Account passwords (any value returned from `/v4/AssetAccounts/{id}/Password`, `/Passwords`, or `/GeneratePassword`)
- SSH private keys (any value from `/v4/AssetAccounts/{id}/SshKey`)
- A2A registration secrets and API keys
- Certificate private keys
- Anything Safeguard explicitly calls out as a secret in its response body

Reference these by account id (or registration id), not by value. If a user asks for confirmation, reply with status ("rotated", "set", "verified") — never the value itself.

**Prefer server-side password operations so plaintext never enters your context:**

| Goal | Right call | Notes |
|------|------------|-------|
| Set initial password on a new managed account | `POST /v4/AssetAccounts/{id}/ChangePassword` (no body) | Safeguard generates per partition rule, pushes to asset, returns activity log only — **no plaintext returned**. |
| Rotate a managed account's password | `POST /v4/AssetAccounts/{id}/ChangePassword` (no body) | Same. There is no batch endpoint — call in parallel by id. |
| Generate a rule-compliant value out of band | `POST /v4/AssetAccounts/{id}/GeneratePassword` (no body) | Returns one sample string. Treat as sensitive. |
| Set a known value (e.g. import) | `PUT /v4/AssetAccounts/{id}/Password` (body = value) | Use only when you already control the value. |

Do **not** mint passwords client-side (local pwgen, `Get-Random`, LLM-generated strings) — they bypass the partition's password rule and leak plaintext into your transcript. The `set-initial-account-password` workflow recipe walks through the full flow.
