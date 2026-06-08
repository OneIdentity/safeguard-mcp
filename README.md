# Safeguard MCP Server

An [MCP](https://modelcontextprotocol.io/) server that enables AI agents to interact with
One Identity Safeguard for Privileged Passwords (SPP) appliances.

MCP (Model Context Protocol) is a standard that lets AI assistants — Claude, GitHub Copilot,
and others — call external "tools" to take actions in the real world. This server exposes
Safeguard's REST API as MCP tools, so an AI agent can manage privileged access, look up
accounts, create assets, run reports, diagnose appliance health, and perform cross-server
operations like migrations.

## Installation

Install via npm (no .NET SDK required):

```bash
npx @oneidentity/safeguard-mcp
```

Or install globally:

```bash
npm install -g @oneidentity/safeguard-mcp
safeguard-mcp
```

### Docker

The container is built for shared deployments and defaults to the
**streamable-HTTP** MCP transport on port 8080. For per-user stdio use,
prefer the npm package or the standalone binary instead.

```bash
docker run -d --rm \
  -p 8080:8080 \
  -e SAFEGUARD_HOST=safeguard.corp.example.com \
  -e SAFEGUARD_IGNORE_SSL=true \
  ghcr.io/oneidentity/safeguard-mcp
```

The container's `ENTRYPOINT` is pinned to `--http`; to run it in
stdio mode (e.g. wired into an MCP client that launches its own
subprocess), override the entrypoint and pass `-i` so stdin stays
attached:

```bash
docker run -i --rm \
  --entrypoint /app/SafeguardMcp \
  -e SAFEGUARD_HOST=safeguard.corp.example.com \
  ghcr.io/oneidentity/safeguard-mcp
```

The container runs as a built-in nonroot user (`app`, UID 1654).
For production HTTP deployments, see the
[reference deployment shapes](deploy/README.md).

### Binary Downloads

Self-contained binaries (no runtime needed) are available from
[GitHub Releases](https://github.com/OneIdentity/safeguard-mcp/releases)
for Linux x64, Linux arm64, Windows x64, and macOS ARM64.

### Verifying Downloads

Each release publishes a `SHA256SUMS` file alongside the archives, plus a
[cosign](https://github.com/sigstore/cosign) public key (`cosign.pub`) for
verifying container image signatures. Verify before extracting.

**Checksums:**

```bash
sha256sum -c SHA256SUMS --ignore-missing
```

The `SHA256SUMS` file covers the release archives, the per-platform SPDX
SBOMs (`sbom-linux-amd64.spdx.json`, `sbom-linux-arm64.spdx.json`), and
the `cosign.pub` key itself.

**Container image signature** — the multi-arch image is signed with a key
held in an Azure Key Vault HSM. Download `cosign.pub` from the Release
assets and verify the image digest:

```bash
cosign verify --key cosign.pub ghcr.io/oneidentity/safeguard-mcp:<tag>
```

## Quick Start

### Claude Desktop

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "safeguard": {
      "command": "npx",
      "args": ["-y", "@oneidentity/safeguard-mcp"],
      "env": {
        "SAFEGUARD_HOST": "safeguard.corp.example.com"
      }
    }
  }
}
```

On first use, the server displays a verification URL and one-time code; complete the
sign-in from any browser to authorize the connection.

### VS Code (GitHub Copilot)

Add to `.vscode/mcp.json` in your workspace:

```json
{
  "servers": {
    "safeguard": {
      "command": "npx",
      "args": ["-y", "@oneidentity/safeguard-mcp"],
      "env": {
        "SAFEGUARD_HOST": "safeguard.corp.example.com"
      }
    }
  }
}
```

### HTTP Mode (Shared Server Deployment)

`safeguard-mcp` can also run as a long-lived HTTP server that multiple users share.
A single server process is bound to one appliance via `SAFEGUARD_HOST`; the bearer
rides on each MCP request, so the server holds no per-user state.

**OAuth-bridge setup (recommended)** — point your MCP client at
`https://<your-mcp-host>/mcp` and let it discover OAuth via the
server's `/.well-known/*` metadata. The client opens a browser-based
PKCE login against the appliance's rSTS, and the resulting bearer is
sent on each request automatically. The bridge is on by default in
HTTP mode, holds no long-lived secrets, and never retains a Safeguard
token. Set `BRIDGE_DISABLED=true` if you front the deployment with a
separate OAuth gateway.

**Paste-bearer setup (manual fallback)** — for scripting or MCP
clients that don't speak OAuth discovery. Each user runs
`safeguard-mcp login` locally to acquire a Safeguard user token and
drops it into their MCP client config:

```bash
safeguard-mcp login --host safeguard.corp.example.com --print-bearer-only > token.txt
```

```jsonc
{
  "mcpServers": {
    "safeguard": {
      "url": "https://mcp.corp.example.com/mcp",
      "headers": { "Authorization": "Bearer <paste-token-here>" }
    }
  }
}
```

For production HTTP deployments, see [`deploy/README.md`](deploy/README.md).

## Authentication

The Safeguard appliance is the **sole authentication authority** in every deployment
shape. No service-account credentials are stored in MCP config and no Safeguard tokens
ever live in the MCP server's persistent state.

### Stdio mode

When `safeguard-mcp` runs as a child process of an MCP client (the default), it uses
the **OAuth 2.0 Device Authorization Grant**
([RFC 8628](https://datatracker.ietf.org/doc/html/rfc8628)). On first use the server
prints a verification URL and a short one-time code; you complete sign-in from any
browser — same machine or a different one — and the token flows back automatically.
This works in containers and headless environments without needing a local browser,
and supports any authentication method your appliance allows (LDAP, RADIUS, SAML, etc.).

> Device code grant must be enabled on the Safeguard appliance (Safeguard Access settings).

### HTTP mode

When the server runs as a shared HTTP service, each MCP request carries its own
`Authorization: Bearer <token>` header and the server forwards that bearer verbatim
to the appliance. The server has no ambient identity of its own; the appliance
validates the bearer on every call, so a revoked or expired token stops working the
moment the appliance says so.

Bearers reach the client one of two ways — see [HTTP Mode](#http-mode-shared-server-deployment)
above for the paste-bearer and OAuth-bridge flows.

### Typical Setup

Most stdio users simply set `SAFEGUARD_HOST` to tell the server which appliance to
connect to. On first use, the server displays a verification URL and code — no
passwords stored in config files:

```json
{
  "env": {
    "SAFEGUARD_HOST": "safeguard.corp.example.com"
  }
}
```

You can also omit `SAFEGUARD_HOST` entirely in stdio mode. The server then starts
with no connection and the agent will prompt you for the appliance address at
runtime via `Safeguard_Connect`. See [One appliance per process](#one-appliance-per-process)
for details.

### SSL Certificate Validation

For appliances using self-signed or internal CA certificates, set `SAFEGUARD_IGNORE_SSL`:

```json
{
  "env": {
    "SAFEGUARD_HOST": "safeguard.corp.example.com",
    "SAFEGUARD_IGNORE_SSL": "true"
  }
}
```

The agent can also pass `ignoreSsl` per-connection at runtime via `Safeguard_Connect`, but the
server will always prompt you for confirmation before disabling SSL validation.

## Architecture & Design

### The Problem: Large API Surfaces and AI Agents

Safeguard has over 1,000 REST endpoints across three services (Core, Appliance, Notification).
The naive approach to exposing this to AI would be to register one MCP tool per endpoint —
`Get_Users`, `Create_AssetAccount`, `Delete_AccessRequest`, and so on. This fails in
practice: MCP clients choke on hundreds of tools, agents can't reason about which tool to
pick, and every Safeguard version change requires regenerating the tool surface.

### The Solution: Navigate → Understand → Execute

Instead of 1,000+ individual tools, this server provides a small, stable set of **meta-tools**
that give the agent the ability to navigate, understand, and execute against any endpoint
dynamically. Think of it as providing a map and a car rather than building 1,000 individual
roads.

The complete tool surface is **7 tools**:

| Tool | Purpose |
|------|---------|
| `Safeguard_Connect` | Authenticate to one or more appliances via device code |
| `Safeguard_Discover` | Search the API catalog by keyword, service, or HTTP method |
| `Safeguard_Schema` | Get the request/response shape for a specific endpoint |
| `Safeguard_QueryHelp` | Learn Safeguard's filter, field selection, and pagination syntax |
| `Safeguard_Workflows` | Get step-by-step recipes for common multi-step operations |
| `Safeguard_Execute` | Call any endpoint on any service (auto-routes to the correct service) |

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

### Terminology Mapping: Bridging Product and API Language

Safeguard's REST API uses different names than the product UI in several important places.
The most significant: what administrators know as **Entitlements** is the `/v4/Roles`
endpoint in the API. An agent (or user) asking to "create an entitlement" would never find
the right endpoint by searching the API literally — the word "entitlement" doesn't appear
in the Roles endpoint path or summary.

This is a common problem with enterprise products that evolved over many releases — UI
terminology shifts but API paths remain stable for backward compatibility.

The server addresses this with two layers:

1. **Search-time expansion** — When `Safeguard_Discover` receives a search term, it checks
   a terminology alias map and expands the search to include related API terms. Searching
   "entitlement" automatically also searches for "roles." This works transparently — the
   agent doesn't need to know the mapping exists.

2. **MCP Resource** — The server exposes a `safeguard://terminology` resource that AI agents
   can read for full context on product-to-API naming differences. This gives agents
   proactive awareness of the terminology landscape before they even start searching.

Current mappings include Entitlement→Roles, Managed Account→AssetAccounts,
Partition→AssetPartitions, Platform→Platforms, and others. The map is designed to grow as
real-world usage reveals additional gaps.

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
- Password/SSH key rotation status and task failure triage
- Access request workflows (password checkout, RDP/SSH sessions, emergency breakglass)
- Bulk asset and account onboarding
- User permission and entitlement audit
- Backup/restore and patch/upgrade procedures
- Directory integration (LDAP/AD import)
- Certificate and SSH key management
- A2A credential retrieval setup
- Personal password vault configuration

### MCP Resources: Preloadable Context

MCP Resources are URI-addressable reference documents that clients can load into the
agent's context at the start of a session — before any tool calls happen. This gives the
agent "day-one knowledge" of Safeguard's API without consuming tool-call round trips.

| Resource URI | Content |
|---|---|
| `safeguard://api-overview` | Service map — what endpoints exist, how objects relate |
| `safeguard://query-syntax` | Complete filter, field, ordering, and pagination syntax |
| `safeguard://common-patterns` | Lookup-by-name, create-with-deps, bulk ops, error handling |
| `safeguard://terminology` | Product UI terms → API endpoint name mappings |

Clients that support MCP resource preloading can inject all four at session start. Clients that don't can still access the same content
via tool calls (`Safeguard_QueryHelp`, `Safeguard_Discover`).

### One appliance per process

A single `safeguard-mcp` process binds to a single appliance. To address more than
one appliance from a single agent conversation, run more than one server and wire
the agent to all of them — each gets its own MCP server entry in your client's
config.

In **stdio mode** this falls out naturally: most MCP clients let you declare
multiple servers in one config file, each launched with its own `SAFEGUARD_HOST`.

In **HTTP mode**, the same applies: deploy each appliance behind its own URL. Same
image, same chart, same configuration schema — the only thing that changes between
deployments is the value of `SAFEGUARD_HOST`.

#### Read-only replica fleets

A Safeguard cluster can include **read-only replica** appliances. Replicas serve the
full access-request workflow (credential retrieval, sessions), A2A, and all
configuration **reads**; they reject only configuration **writes**. That makes them
useful targets for an MCP fleet in two ways:

1. **Read scaling / load isolation** — run a second HTTP deployment with
   `SAFEGUARD_HOST` pointed at a replica. Agents that mostly read the catalog,
   query objects, or fetch credentials route there; agents that need to mutate
   configuration go through the primary deployment.
2. **No-config-mutation safety boundary** — to make AI agents incapable of
   mutating Safeguard configuration at all, deploy *only* an MCP fleet pointed at
   a replica. Credential retrieval, sessions, and A2A still work; configuration
   writes return a clean appliance-level error.

The deployment shape is identical to the primary fleet. The only operational
difference is which address you put in `SAFEGUARD_HOST`.

#### Agent-driven connect (stdio only)

In stdio mode the agent can also call `Safeguard_Connect` at runtime to connect on
demand — handy if you launch the server with no `SAFEGUARD_HOST` and let the agent
prompt you for the appliance address. Once a single stdio process is connected to
multiple appliances this way, pass `host=...` on `Safeguard_Execute` to target a
specific one:

```
Agent: Safeguard_Connect host="prod.safeguard.corp.com"    → authenticates to prod
Agent: Safeguard_Connect host="dr.safeguard.corp.com"      → authenticates to DR
Agent: Safeguard_Execute path="/v4/Users" host="prod.safeguard.corp.com"
Agent: Safeguard_Execute path="/v4/Users" host="dr.safeguard.corp.com"
```

When only one connection is active, the `host` parameter is optional. This pattern
does not apply to HTTP mode — each HTTP server is bound to one appliance for the
process lifetime.

#### Minimal stdio config (no pre-configured host)

If you prefer to let the agent decide which appliance to connect to, omit
`SAFEGUARD_HOST` entirely:

**Claude Desktop** (`claude_desktop_config.json`):
```json
{
  "mcpServers": {
    "safeguard": {
      "command": "npx",
      "args": ["-y", "@oneidentity/safeguard-mcp"],
      "env": {
        "SAFEGUARD_IGNORE_SSL": "true"
      }
    }
  }
}
```

The agent will call `Safeguard_Connect` when it needs to interact with an appliance
and the server will display a verification URL and code for you to authenticate.

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
   - Everything else → Core service (handles ~90% of endpoints)

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

### Transport Modes

The server supports two transport modes:

**Stdio (default)** — the standard MCP transport used by VS Code, Claude Desktop,
and most MCP clients. The server reads/writes JSON-RPC messages over stdin/stdout:

```bash
SafeguardMcp          # stdio mode (default)
```

**HTTP (Streamable HTTP)** — for shared, network-accessible deployments. The server
starts a web endpoint at `/mcp` that MCP clients connect to over HTTP. In this mode
the server is a pure relay: it holds no Safeguard tokens; every MCP request must
carry its own `Authorization: Bearer <token>` header, which the server forwards to
the appliance. The appliance is what validates the bearer on every call.

```bash
SafeguardMcp --http   # HTTP mode, default http://0.0.0.0:8080
```

Configure the listening URL via standard ASP.NET Core mechanisms (`--urls`,
`ASPNETCORE_URLS` environment variable, or `appsettings.json`).

A `/healthz` endpoint is exposed alongside `/mcp` for Kubernetes liveness/readiness
probes and load-balancer health checks; it returns `200 Healthy` once the host is up.

> **TLS termination is your responsibility.** The server binds plain HTTP by default;
> production deployments must front it with an authenticated reverse proxy, ingress
> controller, or service mesh (mTLS, OAuth proxy, API gateway, etc.) that terminates
> TLS. The bundled OAuth bridge enforces RFC 6749/8252 redirect-uri rules but does
> not itself perform TLS.

For production HTTP deployments — Kubernetes manifests, Helm chart, Docker Compose,
and the operational runbook — see [`deploy/README.md`](deploy/README.md).

## Threat Model

The HTTP relay was designed around a few hard invariants worth stating explicitly:

- **The appliance is the sole authentication authority.** The relay never validates
  a Safeguard bearer locally — every MCP request is forwarded to the appliance, and
  appliance-side revocation takes effect immediately.
- **No long-lived secrets in the server.** The relay holds no client secrets, no
  service accounts, no refresh tokens, no cookie keys. The OAuth bridge is a public
  PKCE client; it issues no shared secrets to MCP clients and stores none itself.
- **Bearers never enter persistent state.** The bearer rides on each request in a
  per-request scoped `HttpRelaySafeguardSession` and is discarded when the request
  completes.
- **Logs are scrubbed.** Both stdio and HTTP modes route through a single
  redaction provider that strips JWT-shaped tokens, OAuth `code`/`code_verifier`
  values, and `Authorization: Bearer …` headers before any log sink sees them.
- **Single appliance per process.** A given server process is bound to one
  appliance via `SAFEGUARD_HOST`. Cross-appliance token confusion is not possible
  in HTTP mode because no process serves more than one appliance.
- **Forwarded-headers trust is bounded.** The bridge derives its public URL from
  each incoming request, with `UseForwardedHeaders` enabled but configured to
  trust only loopback + RFC1918 (10/8, 172.16/12, 192.168/16) by default. That
  covers the common cluster-internal-ingress case; production deployments behind
  ingress should ensure their proxy is in a trusted CIDR or extend the trust list
  via `BRIDGE_TRUSTED_PROXIES` (comma-separated CIDRs). Untrusted hops are
  ignored, so a malicious client cannot spoof `X-Forwarded-Host` to make the
  bridge publish a different authorization-server URL.

## Building from Source

If you want to run from source or contribute:

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
git clone https://github.com/OneIdentity/safeguard-mcp.git
cd safeguard-mcp/src/SafeguardMcp
dotnet run
```

For development workflow details, see [CONTRIBUTING.md](CONTRIBUTING.md).

## Status

Under active development.

## License

Apache 2.0 — see [LICENSE](LICENSE)
