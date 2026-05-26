# Safeguard MCP Server

An [MCP](https://modelcontextprotocol.io/) server that enables AI agents to interact with
One Identity Safeguard for Privileged Passwords (SPP) appliances.

MCP (Model Context Protocol) is a standard that lets AI assistants — Claude, GitHub Copilot,
and others — call external "tools" to take actions in the real world. This server exposes
Safeguard's REST API as MCP tools, so an AI agent can manage privileged access, look up
accounts, create assets, run reports, diagnose appliance health, and perform cross-server
operations like migrations.

## Architecture & Design

### The Problem: Large API Surfaces and AI Agents

Safeguard has over 500 REST endpoints across three services (Core, Appliance, Notification).
The naive approach to exposing this to AI would be to register one MCP tool per endpoint —
`Get_Users`, `Create_AssetAccount`, `Delete_AccessRequest`, and so on. This fails in
practice: MCP clients choke on hundreds of tools, agents can't reason about which tool to
pick, and every Safeguard version change requires regenerating the tool surface.

### The Solution: Navigate → Understand → Execute

Instead of 500 individual tools, this server provides a small, stable set of **meta-tools**
that give the agent the ability to navigate, understand, and execute against any endpoint
dynamically. Think of it as providing a map and a car rather than building 500 individual
roads.

The complete tool surface is **7 tools**:

| Tool | Purpose |
|------|---------|
| `Safeguard_Connect` | Authenticate to one or more appliances (browser or headless) |
| `Safeguard_Discover` | Search the API catalog by keyword, service, or HTTP method |
| `Safeguard_Schema` | Get the request/response shape for a specific endpoint |
| `Safeguard_QueryHelp` | Learn Safeguard's filter, field selection, and pagination syntax |
| `Safeguard_Workflows` | Get step-by-step recipes for common multi-step operations |
| `Safeguard_Execute` | Call any endpoint on any service (auto-routes to the correct service) |
| `RandomPassword` | Generate a secure random password |

An agent working through a task follows this pattern:

1. **Discover** — "find me endpoints related to password change failures"
2. **Schema** — "what fields does a POST to /v4/AssetAccounts require?"
3. **QueryHelp** — "how do I filter by name and sort by date?"
4. **Execute** — call the endpoint with the correct parameters and body

### Dynamic Catalog: Version-Resilient Discovery

The API catalog is loaded from the appliance's own Swagger/OpenAPI spec at connect time,
so the tool surface always matches the actual API of the connected appliance — whether it's
running Safeguard 7.5, 8.2, or a future release. A compiled static catalog is included as a
fallback for airgapped environments where Swagger isn't reachable.

#### How it works

When `Safeguard_Connect` authenticates to an appliance, it triggers a background catalog
load. The loader fetches three OpenAPI specs in parallel:

```
https://{host}/service/Core/swagger/v4/swagger.json
https://{host}/service/Appliance/swagger/v4/swagger.json
https://{host}/service/Notification/swagger/v4/swagger.json
```

These are public endpoints (no auth required on most versions). The loader parses each spec
to extract:

- **Endpoints** — method, path, service, summary, query parameters, whether it accepts a body
- **Schemas** — request and response body structures with property names, types, required
  flags, and descriptions (extracted from `components/schemas` and `requestBody` refs)

The result is cached per-host in memory. Each connection gets its own catalog, so connecting
to two appliances running different Safeguard versions gives each one the correct endpoint
list and schemas for its version.

The SSL policy used for swagger fetch matches the connection's SSL policy — if you set
`SAFEGUARD_IGNORE_SSL=true` for self-signed lab certs, the catalog loader respects that too.

**Fallback**: If swagger fetch fails (airgapped network, firewall rules, older appliance
version), the server falls back to a compiled static catalog (~1,070 endpoints from a
reference installation). The static catalog has no schemas — `Safeguard_Schema` will report
"no schema available" but discovery and execution still work.

### Schema Tool: Enabling Write Operations

For read operations (GET), an agent just needs Discover and Execute. But **write operations**
(POST, PUT, PATCH) are where most AI integrations fall apart — the agent doesn't know what
fields are required, what the valid values are, or how objects relate to each other.

The Schema tool solves this by extracting structured field information from the OpenAPI spec:
required fields, types, descriptions, nested object shapes, and a minimal working example.
An agent can call `Safeguard_Schema` for `POST /v4/AssetAccounts`, learn that `Name` and
`Asset.Id` are required, and construct a valid request body without trial and error.

### Workflow Recipes: Domain Expertise for Agents

Safeguard operations rarely involve a single API call. Diagnosing password rotation failures
requires checking task properties, pulling change request logs, testing connectivity, and
inspecting service account profiles. A health check spans appliance status, hardware metrics,
cluster state, and audit logs.

Without domain guidance, an agent would need dozens of exploratory calls to figure out these
sequences — or worse, it guesses wrong and misses critical steps.

Workflow recipes encode the operational expertise of a Safeguard administrator into
step-by-step instructions the agent follows. They specify which endpoints to call, what
parameters to use, how to interpret results, and what to do next. Pre-built recipes cover:

- Appliance & cluster health assessment
- Password/SSH key task failure triage
- Access request activity audit
- Bulk asset and account onboarding
- User permission audit
- Entitlement review
- Appliance diagnostics

### Multi-Server Support

The server maintains connections to multiple Safeguard appliances simultaneously. This
enables cross-server workflows — comparing configurations between production and DR,
migrating assets from one appliance to another, or auditing multiple sites from a single
agent conversation.

### Unified Dispatcher with Service Auto-Routing

Safeguard's three services (Core, Appliance, Notification) each handle different endpoint
families. Agents and users often don't know — or shouldn't need to know — which service owns
a given endpoint. The unified `Safeguard_Execute` tool resolves the correct service
automatically by looking up the path in the API catalog. One tool handles everything.

#### Service resolution

When you call `Safeguard_Execute` with a path like `/v4/AssetAccounts`, the dispatcher:

1. **Catalog lookup** — scans the dynamic catalog (or static fallback) for an endpoint
   whose path template matches the request path, accounting for `{id}` placeholders.
   If found, uses that endpoint's service.

2. **Heuristic fallback** — if no catalog match (e.g., a new endpoint on a newer appliance
   not yet in the static catalog), applies keyword-based routing:
   - `ApplianceStatus`, `Backup`, `Network`, `DiagnosticPackage` → Appliance service
   - `/v4/Status` → Notification service
   - Everything else → Core service (handles ~80% of endpoints)

#### Response intelligence

The dispatcher adds guardrails to prevent context window overflow and guide the agent:

- **Auto-limit injection** — collection GETs without an explicit `limit` parameter get
  `limit=50` injected automatically (configurable). This prevents accidental retrieval of
  10,000+ records that would overwhelm an agent's context.

- **Array truncation** — if a response contains a JSON array with more items than
  `MaxResultsBeforeTruncation` (default 100), only the first N items are returned, with a
  note explaining how to filter or paginate for more.

- **Character truncation** — responses exceeding `MaxResponseChars` (default 30,000) are
  cut with a note suggesting `fields` selection or `format=csv` to reduce payload.

- **Error enrichment** — HTTP errors get contextual hints:
  - 400 → "Use Safeguard_Schema to see required fields"
  - 401 → "Call Safeguard_Connect to re-authenticate"
  - 404 → "Verify the ID exists using a GET call"

#### Configuration

All response thresholds are configurable in `appsettings.json`:

```json
{
  "Safeguard": {
    "MaxResultsBeforeTruncation": 100,
    "MaxResponseChars": 30000,
    "DefaultLimit": 50,
    "AutoInjectLimit": true
  }
}
```

## Status

Under active development.

## License

Apache 2.0 — see [LICENSE](LICENSE)
