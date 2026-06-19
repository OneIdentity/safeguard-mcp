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
  docker.io/oneidentity/safeguard-mcp
```

The container's `ENTRYPOINT` is pinned to `--http`; to run it in
stdio mode (e.g. wired into an MCP client that launches its own
subprocess), override the entrypoint and pass `-i` so stdin stays
attached:

```bash
docker run -i --rm \
  --entrypoint /app/SafeguardMcp \
  -e SAFEGUARD_HOST=safeguard.corp.example.com \
  docker.io/oneidentity/safeguard-mcp
```

The container runs as a built-in nonroot user (`app`, UID 1654).
For production HTTP deployments, see the
[reference deployment shapes](deploy/README.md).

### Binary Downloads

Self-contained binaries (no runtime needed) are available from
[GitHub Releases](https://github.com/OneIdentity/safeguard-mcp/releases)
for Linux x64, Windows x64, and macOS ARM64.

### Verifying Downloads

Each release publishes a `SHA256SUMS` file alongside the archives, plus a
[cosign](https://github.com/sigstore/cosign) public key (`cosign.pub`) for
verifying container image signatures. Verify before extracting.

**Checksums:**

```bash
sha256sum -c SHA256SUMS --ignore-missing
```

The `SHA256SUMS` file covers the release archives, the per-platform SPDX
SBOM (`sbom-linux-amd64.spdx.json`), and the `cosign.pub` key itself.

**Container image signature** — the image is signed with a key
held in an Azure Key Vault HSM. Download `cosign.pub` from the Release
assets and verify the image digest:

```bash
cosign verify --key cosign.pub docker.io/oneidentity/safeguard-mcp:<tag>
```

## Quick Start

Once your client is wired up (see below), see [`docs/EXAMPLES.md`](docs/EXAMPLES.md) for
example prompts covering discovery, account/asset management, access requests, password
and SSH key rotation, health checks, audits, and cross-server workflows.

### Wiring up your MCP client

See [`docs/CLIENT-SETUP.md`](docs/CLIENT-SETUP.md) for copy-pasteable stdio
configurations for **Claude Desktop**, **Claude Code**, **VS Code (GitHub
Copilot)**, and **GitHub Copilot CLI**.

All four clients launch the same server (`npx -y @oneidentity/safeguard-mcp`)
and read the same `SAFEGUARD_HOST` environment variable; they differ only in
config file location and JSON key names.

On first use, the server prints a verification URL and one-time code; complete
the sign-in from any browser to authorize the connection.

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

```jsonc
{
  "mcpServers": {
    "safeguard": {
      "url": "https://mcp.corp.example.com/mcp"
    }
  }
}
```

No `Authorization` header — the client discovers the bridge from
`/.well-known/oauth-protected-resource` and runs the PKCE flow itself.

**Paste-bearer setup (manual fallback)** — only for older MCP clients
that don't yet implement OAuth discovery, or for scripts/CI. Each user
acquires a Safeguard user token locally and drops it into their MCP
client config:

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

When you're done with the token (e.g. switching machines, ending a
shift), revoke it server-side with the paired command:

```bash
safeguard-mcp logout --host safeguard.corp.example.com --input token.txt
```

Both `login` and `logout` exist solely to bridge clients that can't run
the OAuth flow themselves; once your client supports MCP OAuth
discovery, neither is needed.

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

In stdio mode you can omit `SAFEGUARD_HOST` if your MCP client supports
elicitation forms; on the first `Safeguard_Connect` call the server will pop
a form asking for the appliance address. Without elicitation support, the
server returns an error telling you to set `SAFEGUARD_HOST` and restart.
HTTP mode always requires `SAFEGUARD_HOST` at startup.

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

`SAFEGUARD_IGNORE_SSL` is read once at process start. If the server hits a
TLS verification failure during connect and `SAFEGUARD_IGNORE_SSL` is not
set, stdio mode will offer an elicitation prompt asking whether to disable
TLS verification for the lifetime of the current session; accepting flips
the policy in-memory only. HTTP mode does not prompt — set the env var.

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
  completes. See [Bearer token handling](#bearer-token-handling) below for the full
  lifecycle in both modes.
- **Logs are scrubbed.** Both stdio and HTTP modes route through a single
  redaction provider that strips JWT-shaped tokens, OAuth `code`/`code_verifier`
  values, and `Authorization: Bearer …` headers before any log sink sees them.
- **Single appliance per process.** A given server process is bound to one
  appliance via `SAFEGUARD_HOST`. Cross-appliance token confusion is not possible
  in HTTP mode because no process serves more than one appliance.
- **Plaintext credentials are tagged for an audience-aware future.** Account
  passwords, SSH private keys, API client secrets, TOTP codes, and secure-file
  contents are returned only through `Safeguard_RetrieveCredential`, in a
  two-block response that puts the plaintext under `audience=["user"]` so a
  host that filters on the MCP `audience` annotation could route it to a
  secure pane without it entering the LLM's context. The annotation is an
  optional hint the spec does not require hosts to honor, so treat block 2 as
  potentially visible to the model unless you have verified your host filters
  on it; the separation is in place so any host that adds the filter gets the
  routing automatically. `Safeguard_Execute` refuses every sensitive path and
  redirects the caller to the matching `Safeguard_RetrieveCredential` kind.
  See [Sensitive credential delivery](#sensitive-credential-delivery) below.
- **Forwarded-headers trust is bounded.** The bridge derives its public URL from
  each incoming request, with `UseForwardedHeaders` enabled but configured to
  trust only loopback + RFC1918 (10/8, 172.16/12, 192.168/16) by default. That
  covers the common cluster-internal-ingress case; production deployments behind
  ingress should ensure their proxy is in a trusted CIDR or extend the trust list
  via `BRIDGE_TRUSTED_PROXIES` (comma-separated CIDRs). Untrusted hops are
  ignored, so a malicious client cannot spoof `X-Forwarded-Host` to make the
  bridge publish a different authorization-server URL.

### Bearer token handling

Both transports must hold a Safeguard user token in memory while they are
actively calling the appliance on the caller's behalf. The window is bounded,
deterministic, and as defensively allocated as the framework allows. Neither
mode persists the token to disk; neither mode logs it; neither mode keeps a
managed-string copy on a long-lived field. The two transports differ only in
how long the in-memory hold lasts, because their trust boundaries differ.

#### HTTP mode — per-request hold, then wiped

The HTTP relay does not authenticate users itself. Every MCP request must
arrive carrying its own `Authorization: Bearer <token>`; the relay forwards
that bearer to the appliance for the duration of that one request and is
done with it when the request returns.

1. **Per-request scope, never wider.** `HttpRelaySafeguardSession` is
   registered with ASP.NET Core's scoped lifetime, so a fresh instance is
   created for every MCP HTTP request and disposed when the request ends.
   There is no singleton, no static field, and no cross-request dictionary
   anywhere in the process that holds a bearer. The next request from the
   same caller must bring its own `Authorization` header — the server has
   nothing to fall back on.

2. **`SecureString` for the in-flight copy.** When the session first needs
   to call the appliance during a request, it extracts the bearer from the
   request's `Authorization` header, copies it into a sealed `SecureString`
   via `SessionHelpers.ToSecureString`, and hands that `SecureString` to the
   SafeguardDotNet SDK's `Safeguard.Connect(host, secureBearer, …)`. The SDK
   keeps its own `SecureString` copy inside the connection it returns; our
   local copy is wrapped in `using` and zeroed the moment the SDK has the
   connection. The SDK uses its copy transiently when building each outbound
   `Authorization` header to the appliance.

3. **Deterministic zeroing on disposal.** When the DI scope ends —
   `HttpRelaySafeguardSession.Dispose()` → `_connection.Dispose()` —
   the SDK's `SecureString` is zeroed before its buffer is released. The
   bytes do not sit in the managed heap waiting for GC to maybe-someday
   reclaim them. Disposal is driven by the framework's scope teardown;
   no code path can forget to release the connection.

4. **No appliance-side state to clean up.** A 401 from the appliance during
   a request is surfaced to the caller as "token expired or revoked;
   re-acquire and retry" — the relay does not attempt to refresh, because
   the bearer is not the relay's to refresh. Token rotation is the caller's
   responsibility.

The end-to-end shape: the bearer enters with the request, is held just
long enough to talk to the appliance, and is wiped from process memory
when the request returns. The agent is the sole long-term holder of the
token; the relay borrows it briefly for each request.

#### Stdio mode — process-lifetime hold, then wiped

A stdio process is a single user's local agent talking to a single local
MCP client over the process's stdin/stdout. The process itself performs
the Safeguard login that produces the token — device-code by default, or
PKCE when `SAFEGUARD_PROVIDER` / `SAFEGUARD_USER` / `SAFEGUARD_PASSWORD`
are all set — so the trust boundary is the process boundary itself.

1. **One cached connection per process.** `StdioSafeguardSession` is a
   singleton; on first tool call it drives the login flow and caches the
   resulting `ISafeguardConnection`. The SDK holds the token inside a
   `SecureString` on that connection; all subsequent tool calls in the
   same process reuse it. There is no second copy on the session object,
   no on-disk file, no environment variable that retains the token after
   login completes.

2. **Refresh in place, never re-materialized as a string.** Before every
   tool call the session checks `GetAccessTokenLifetimeRemaining()` and
   silently calls `RefreshAccessToken()` if fewer than five minutes
   remain. Both calls happen entirely inside the SDK against its own
   `SecureString`; the bearer is never lifted back into a managed
   `string` on our side.

3. **Deterministic zeroing on logout or graceful shutdown.**
   `Safeguard_Disconnect` (the MCP tool) and graceful host shutdown both
   route through `DisposeConnectionLocked()`, which calls
   `connection.LogOut()` — the SDK posts to the appliance's
   `/Token/Logout` to revoke server-side and zeroes its `SecureString` —
   then `connection.Dispose()`. The session's `_connection` field is
   nulled; the next tool call re-auths from scratch. The companion CLI
   `safeguard-mcp logout` runs in its own short-lived process and reaches
   the same appliance endpoint directly; the stdio server it pairs with
   discovers the resulting invalidation on the next API call (a 401
   surfaces as "re-authenticate" to the agent).

4. **No disk persistence, ever.** Tokens are never written to disk by the
   stdio server. `safeguard-mcp login --output <path>` is the one and
   only path that places a token on disk, and only because the user
   explicitly asked for it — and it does so with a restrictive ACL
   (Unix 0600; Windows: explicit user-only ACE, inheritance disabled).
   `safeguard-mcp logout` revokes that token server-side; the file
   itself is the user's to manage.

In both modes, `RedactingLoggerProvider` is wired in front of every log
sink (stderr and the rolling file) and strips JWT-shaped tokens, OAuth
`code`/`code_verifier` values, and `Authorization: Bearer …` headers
before any sink sees them — so even an exception that surfaces a
request/response payload cannot leak the bearer to disk through a log.

### Sensitive credential delivery

Bearer tokens authenticate the caller; the plaintext secrets Safeguard
manages (account passwords, SSH private keys, API client secrets, TOTP
codes, generated passwords, secure-file contents) are a separate class
of sensitive material with their own delivery contract. The server's goal
is that the LLM can drive the workflow that produces those secrets
without the secret itself ever needing to enter the assistant's context
window.

#### Two-block, audience-split response

`Safeguard_RetrieveCredential` is the single tool that returns plaintext
credential material. Every call returns a two-block MCP response:

- **Block 1 — `audience=["assistant"]`.** A JSON envelope: `kind`, the
  subject ids (`accessRequestId`, `accountId`, `apiKeyId` as applicable),
  delivery flags, and any notices. This block carries **no secret value**;
  it is what the LLM reads to confirm "yes, the credential was delivered
  for the right subject" and to plan the next step.
- **Block 2 — `audience=["user"]`.** The human-formatted plaintext
  (`PlatformAccount password = …`, `BEGIN OPENSSH PRIVATE KEY …`, the
  TOTP code with its validity window, etc.).

The MCP spec (2025-06-18) defines `audience` as an optional annotation a
host MAY use to route content — for example, rendering `audience=["user"]`
content in a secure pane separate from the assistant's transcript. The
spec does not require hosts to filter on it, and host support varies; do
not assume block 2 is hidden from the model unless you have verified your
host filters tool-result content blocks by audience. The two-block split
is in place so that any host that does honor the annotation will keep the
plaintext out of the model's context automatically, and so that operators
who want stronger separation have a clear contract to write a custom
filter against. Because hosts may not honor the hint, Safeguard's own
controls — auditability and rotation on the appliance — are the
authoritative defense against accidental disclosure: every retrieval is
logged, and credentials can be rotated if a host's handling is wrong for
your environment.

The `audience` annotation is defined in the MCP specification:
[*Resources → Annotations*](https://modelcontextprotocol.io/specification/2025-06-18/server/resources#annotations)
("An array indicating the intended audience(s) for this resource. Valid
values are `\"user\"` and `\"assistant\"`."). Annotations apply to
resources, resource templates, and content blocks, which is the path
this tool uses.

#### Refuse-and-redirect on `Safeguard_Execute`

The dynamic catalog flags every Safeguard API path that returns plaintext
credential material as sensitive. When `Safeguard_Execute` is called
against any of those paths, the appliance is **not** contacted — the
server returns a structured `sensitive_endpoint_redirected` envelope
naming the matching `Safeguard_RetrieveCredential` kind and the
arguments to pass. The agent lifts `data.next_call` into a follow-up
tool invocation, which routes through the two-block response above.
This makes the audience split the **only** path the plaintext can take
out of the server: there is no Execute escape hatch that returns a raw
secret in a single block addressed to the assistant.

A heuristic backstop covers any future sensitive path that hasn't been
added to the catalog yet (response shape and field-name pattern
matching), tuned to not fire on audit-log payloads. If a future build
adds a sensitive endpoint and the catalog hasn't been updated, the
heuristic still steers the agent toward
`Safeguard_RetrieveCredential` instead of returning the secret through
`Safeguard_Execute`.

#### Supported kinds

- Access-request material: `access-request-password`,
  `access-request-ssh-key`, `access-request-api-key`,
  `access-request-totp`, `access-request-file` (each requires
  `accessRequestId`).
- Personal vault material: `personal-account-password`,
  `personal-account-password-history`, `personal-account-totp` (each
  requires `accountId`).
- Asset-account API material: `asset-account-api-secret-history`
  (requires `accountId` AND `apiKeyId`).
- Out-of-band generation: `generated-password` (no extra args; produces
  a rule-compliant value without persisting it).

#### What the server still never does

- It never persists plaintext credential material to disk or any log
  sink, regardless of audience. The redaction pipeline in front of the
  log sinks treats credential blocks the same as bearer tokens.
- It never reuses block-2 content for any in-process purpose other than
  returning it to the caller — there is no cache, no rolling buffer, no
  envelope around it that an internal handler reads.
- It never mints credentials client-side (no `Get-Random`, no LLM-
  generated values) for managed accounts; password generation goes
  through the appliance's password rule via
  `POST /v4/AssetAccounts/{id}/GeneratePassword` or
  `.../ChangePassword`, captured by the `generated-password` kind.

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

The complete tool surface is **11 tools**:

| Tool | Purpose |
|------|---------|
| `Safeguard_Connect` | Authenticate to one or more appliances via device code |
| `Safeguard_Disconnect` | Revoke the active Safeguard token and drop the cached connection |
| `Safeguard_Discover` | Search the API catalog by keyword, service, or HTTP method (at least one narrower required) |
| `Safeguard_Schema` | Get the request/response shape for a specific endpoint |
| `Safeguard_Reference` | On-demand reference: query syntax, workflow recipes, enum values, terminology, overview, and common patterns (`topic=` selects the source; `search=` returns just one section) |
| `Safeguard_Execute` | Call any endpoint on any service (auto-routes from the bare /v4/... path) |
| `Safeguard_OpenAccessRequest` | One-call composite that pre-checks entitlements and submits an access request |
| `Safeguard_CloseAccessRequest` | State-aware close: dispatches to Cancel / CheckIn / Close / Acknowledge based on the request's current state |
| `Safeguard_RetrieveCredential` | Returns plaintext credential material (passwords, SSH keys, API secrets, TOTP codes, files) in a two-block response that splits metadata from plaintext by MCP audience — see [Sensitive credential delivery](#sensitive-credential-delivery) |

An agent working through a task follows this pattern:

1. **Discover** — "find me endpoints related to password change failures"
2. **Schema** — "what fields does a POST to /v4/AssetAccounts require?"
3. **QueryHelp** — "how do I filter by name and sort by date?"
4. **Execute** — call the endpoint with the correct parameters and body

For a small number of end-to-end flows where the multi-step path has well-known
pitfalls — opening an access request is the first example — a single composite tool
collapses the sequence into one call and catches the common failure modes up front.

### Dynamic Catalog: Version-Resilient Discovery

The API catalog is loaded from the appliance's own Swagger/OpenAPI spec at connect time,
so the tool surface always matches the actual API of the connected appliance — whether it's
running Safeguard 7.5, 8.2, or a future release.

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

### Schema Tool: Enabling Write Operations

For read operations (GET), an agent just needs Discover and Execute. But **write operations**
(POST, PUT, PATCH) are where most AI integrations fall apart — the agent doesn't know what
fields are required, what the valid values are, or how objects relate to each other.

The Schema tool solves this by extracting structured field information from the OpenAPI spec:
required fields, types, descriptions, nested object shapes, and a minimal working example.
An agent can call `Safeguard_Schema` for `POST /v4/AssetAccounts`, learn that `Name` and
`Asset.Id` are required, and construct a valid request body without trial and error.

For fields whose type is an enumeration, `Safeguard_Reference topic=enum` returns the valid values by name —
so the agent never has to guess whether the API expects `"RemoteDesktop"` or `"Rdp"`, or
whether casing matters.

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
Partition→AssetPartitions, Platform→Platforms, and others. The map has grown beyond
simple noun-to-noun aliases to cover **verbs** an agent will reach for
(`move`/`transfer`/`migrate` → partition reassignment), **umbrella concepts** that
have no literal API endpoint (`privileged access`, `pam` → access requests), and
**vague status questions** (`uptime`/`boot`/`system time` → ApplianceStatus / SystemTime
/ Version / Health). It's designed to grow as real-world usage reveals additional gaps.

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

Recipes are reachable directly via `Safeguard_Reference topic=workflows`, and also surface
inline whenever `Safeguard_Discover` or `Safeguard_Execute` touches an endpoint family
a recipe covers — so an agent that started in the raw API can find the higher-level
path mid-task without having to think to ask for it.

### Helping the agent call the API correctly

Discovering an endpoint and reading its schema is only half the battle. Agents still
mistype property names, search for the wrong concept, send the wrong combination of
fields, or learn from a misleading error message. The server includes a layer of
correction and guidance that fires on every Discover and Execute call:

- **Search-time term expansion.** Discover quietly expands a search phrase to include
  the API's own vocabulary. "Entitlement" also searches "roles." "Privileged access"
  also searches "access requests." "Move" / "transfer" / "migrate" all point to
  partition reassignment. Agents who phrase a request the way an administrator would
  still land on the right endpoint.

- **Workflow recipes ranked ahead of raw endpoints.** When a search matches both a
  multi-step recipe and individual endpoints, the recipe is surfaced first — so the
  agent reaches for the documented sequence (request → approve → check out → close)
  before trying to assemble one from scratch.

- **Composite tools where the multi-step flow is error-prone.** For access requests,
  where the most common failure is a misleading "not authorized" error caused by
  picking the wrong asset for the right account, the server exposes
  `Safeguard_OpenAccessRequest`. It checks the agent's actual entitlements *before*
  submitting, so the agent gets a clear "this account-and-asset combination isn't in
  your scope" message instead of an opaque appliance error after the fact.
  `Safeguard_CloseAccessRequest` is the matching closer — it reads the request's
  current state and dispatches to the right verb (Cancel before approval, CheckIn
  while checked out, Close after check-in, Acknowledge after expiry) so the agent
  doesn't have to encode the close-state matrix itself. And
  `Safeguard_RetrieveCredential` owns every endpoint that returns plaintext secret
  material; see [Sensitive credential delivery](#sensitive-credential-delivery) for
  the audience-split response shape and the refuse-and-redirect envelope that
  steers `Safeguard_Execute` callers back to it.

- **"Did you mean" on rejected property names.** When the appliance rejects a filter,
  sort, or field-selection because of a wrong property name, the response includes the
  nearest valid name from the schema ("`UserName` → did you mean `Name`?"). Most agent
  loops repair themselves on the next call.

- **Errors come back with next-step hints.** Common HTTP errors are annotated with the
  tool to reach for or the field to check, instead of being passed through raw. See
  [Response intelligence](#response-intelligence) below for the full list.

- **Cross-linking between Discover, Execute, and Workflows.** Any Execute call against
  a recipe-covered endpoint family includes a one-line pointer back to the matching
  recipe and any composite tool that covers it.

- **Responses are sized so they fit an agent's context window.** Collection GETs get a
  default `limit=50` injected, oversized payloads are truncated with a note explaining
  how to narrow the query (`fields=`, filters, `format=csv`), and the agent is told
  *why* the result was clipped instead of just receiving a silently-shortened payload.
  See [Response intelligence](#response-intelligence) for the knobs.

Taken together, these techniques mean the agent rarely fails twice in the same way: a
wrong term, a wrong field, or a wrong combination produces a response that contains
both the failure and the fix.

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
via tool calls (`Safeguard_Reference`, `Safeguard_Discover`).

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

#### Multi-appliance access from one agent

To talk to more than one appliance from a single MCP client, declare one
`safeguard-mcp` server entry per appliance in your client config — each with
its own `SAFEGUARD_HOST`. Each entry is its own process bound to one
appliance. There is no per-call `host` parameter on `Safeguard_Connect` or
`Safeguard_Execute`; a process serves exactly one appliance for its
lifetime, in both stdio and HTTP mode.

The agent routes by **server name**, not by appliance address: every entry
exposes the same tool names (`Safeguard_Execute`, `Safeguard_Discover`, …)
with identical descriptions, and `SAFEGUARD_HOST` is invisible at the tool
layer. Give each entry a descriptive key (`safeguard-prod`,
`safeguard-dr`, …) so the model has a clear signal, and tell the agent
which one to use in your prompt. Discovery and catalog results are also
per-server, so `Safeguard_Discover` on one entry will not surface endpoints
from another.

#### Stdio config without a pre-configured host

If your MCP client supports elicitation forms, you can omit `SAFEGUARD_HOST`
and let the server prompt for the appliance on first connect:

```json
{
  "mcpServers": {
    "safeguard": {
      "command": "npx",
      "args": ["-y", "@oneidentity/safeguard-mcp"]
    }
  }
}
```

See [`docs/CLIENT-SETUP.md`](docs/CLIENT-SETUP.md) for the equivalent in each
client. Without elicitation support, set `SAFEGUARD_HOST` in `env`. HTTP mode
always requires `SAFEGUARD_HOST` at startup.

### Unified Dispatcher with Service Auto-Routing

Safeguard's three services (Core, Appliance, Notification) each handle different endpoint
families. Agents and users often don't know — or shouldn't need to know — which service owns
a given endpoint. The unified `Safeguard_Execute` tool resolves the correct service
automatically by looking up the path in the API catalog. One tool handles everything.

#### Path format contract

Callers pass bare `/v4/...` paths. The appliance's real URL for any endpoint is
`https://{host}/service/{Name}/v4/...` (Core/Appliance/Notification), and the
dispatcher prepends `/service/{Name}/` for you. Paths that already include the
`/service/{name}/` prefix are rejected at pre-flight with a directive that
names the corrected `/v4/...` form — they would otherwise hit the wire as
`/service/{name}/service/{name}/v4/...` and 404. This contract applies to
`Safeguard_Execute` and `Safeguard_Schema`; `Safeguard_Reference topic=query-syntax` surfaces
the same directive as a notice but still returns the general syntax help.

#### Service resolution

When you call `Safeguard_Execute` with a path like `/v4/AssetAccounts`, the dispatcher:

1. **Catalog lookup** — scans the dynamic catalog for an endpoint whose path template
   matches the request path, accounting for `{id}` placeholders. If found, uses that
   endpoint's service.

2. **Heuristic fallback** — for the handful of undocumented endpoints that aren't in
   swagger, applies keyword-based routing:
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

- **Error enrichment** — HTTP and appliance errors get contextual next-step hints
  rather than being passed through raw:
  - 400 → "Use Safeguard_Schema to see required fields"
  - 401 → "Call Safeguard_Connect to re-authenticate"
  - 404 → "Verify the ID exists using a GET call"
  - rejected property name on a filter/sort/field selection → "did you mean *X*?"
    using the nearest valid name from the schema
  - access-request "not authorized to use this request type" →
    "Use Safeguard_OpenAccessRequest, which pre-checks entitlements before submitting"
  - any call against a recipe-covered endpoint family → one-line pointer back to the
    matching workflow recipe and any composite tool that covers it

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
