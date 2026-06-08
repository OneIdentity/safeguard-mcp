# Deploying safeguard-mcp

This directory ships **reference deployment shapes** for
`safeguard-mcp` in HTTP mode (the streamable-HTTP MCP transport at
`/mcp`, plus the optional OAuth metadata bridge at `/authorize`,
`/authorize/callback`, `/token`, `/register`, and `/.well-known/*`).

These are documentation-grade samples, not a turnkey product. They
encode the architecture choices and operational invariants that the
project relies on; copy them, parameterize them for your environment,
and adapt to your platform conventions.

## When to use what

| Shape | Path | Best for |
|---|---|---|
| Plain `kubectl` manifests | [`k8s/`](./k8s) | Multi-host / HA. Dropping into an existing cluster you already manage with `kubectl apply -f`, GitOps tools (Argo CD, Flux), or kustomize. |
| Helm chart | [`helm/safeguard-mcp/`](./helm/safeguard-mcp) | Multi-host / HA where you already speak Helm — easier upgrades, values-schema validation, opt-in HPA, reusable across environments. |
| Docker Compose | [`compose/`](./compose) | Single-host. Laptops, dev VMs, small POCs, anywhere you don't need horizontal scaling. **Single-replica by definition.** |

The Kubernetes shapes deploy a **two-Deployment topology**: a
horizontally-scalable stateless relay and a single-replica OAuth
metadata bridge. The Compose shape collapses both endpoint sets into
a single container — there is only one process, so the relay/bridge
distinction becomes moot.

### Primary vs. read-only replica fleets

A given deployment binds to **one** Safeguard appliance via
`SAFEGUARD_HOST`. To address more than one appliance, stand up more
than one deployment — same image, same chart, same configuration
schema, just different `SAFEGUARD_HOST`.

A particularly useful split is **primary vs. read-only replica**.
Safeguard read-only replicas serve the full access-request workflow
(credential retrieval, sessions), A2A, and all configuration reads;
they reject only configuration writes. Pointing a second deployment
at a replica gives you either:

* **Read scaling / load isolation** — route read-heavy agents at the
  replica deployment and config-mutating workflows at the primary.
* **A no-config-mutation safety boundary** — deploy *only* the
  replica-targeted fleet to make AI agents in that environment
  incapable of mutating Safeguard configuration. Cred retrieval,
  sessions, and A2A still work; config writes return a clean
  appliance-level error.

The deployment artifacts in this directory work unchanged for both
roles. The only operational difference is the value of
`SAFEGUARD_HOST`.

## Two-Deployment rationale

Sticky sessions don't work for OAuth. A typical login crosses
multiple user-agents and origins:

* The MCP client opens the user's system browser to `/authorize`.
* The browser is redirected to rSTS, the user authenticates, rSTS
  redirects back to `/authorize/callback`.
* The MCP client process (not the browser) POSTs `/token` from its
  own network identity.

Cookie- and source-IP-based session affinity routes those calls to
different pods than the one holding the in-flight state, so any
form of stickiness is actively harmful. The Ingress in
`k8s/40-ingress.yaml` and the chart's `templates/ingress.yaml`
deliberately set **no affinity annotations**.

The relay is fine to scale freely because it holds no per-request
state — the bearer token rides on every MCP call and
`HttpRelaySafeguardSession` is registered scoped per-request, so any
relay pod can serve any request.

The bridge holds in-flight OAuth state in process memory:

* `AuthorizeFlowStore` — bridge↔rSTS PKCE pair, keyed by opaque
  `bridge_session_id`. Lifetime ≈ user-login round-trip.
* `AuthCodeStore` — bridge auth-code → rSTS auth-code redemption
  record. Lifetime = `BRIDGE_AUTH_CODE_TTL_SECONDS` (default 60s,
  max 300s).
* `ClientRegistry` — RFC 7591 dynamic-registration entries.
  Lifetime = 30 days.

A second bridge replica without a shared cache splits this state
across pods, so a user can `/authorize` against pod A and arrive
at `/authorize/callback` on pod B (no entry → 400) or redeem
`/token` on pod C (no entry → `invalid_grant`). This is why
`bridge.replicaCount > 1` is **unsupported** until shared-cache
work lands. The Helm chart enforces this as a soft guard via
`values.schema.json`: setting `bridge.replicaCount >= 2` requires
the explicit opt-in flag `bridge.acceptInProcessStateLossOnRestart:
true`. This is a correctness/availability constraint, **not** a
security control — the security guarantees (no token storage,
PKCE binding, exact redirect-URI match) hold regardless of replica
count.

A bridge restart loses every in-flight `/authorize` and unredeemed
`/token` entry. Affected users simply restart their login. Keep the
bridge's liveness probe lenient (the manifests and chart already do
— `failureThreshold: 5`, `periodSeconds: 30`) so a slow rSTS
round-trip doesn't trigger a restart that wipes other users' state.

## Operator runbook: register the bridge in rSTS

Before the bridge can complete a login, rSTS must know about it as
a `RelyingPartyApplication`. Configure one with:

* **`Realm` = `RSTS_CLIENT_ID`.** Must be an absolute URI; rSTS
  asserts this on the entity. A natural choice is `MCP_PUBLIC_URL`
  itself, e.g. `https://mcp.example.com`.
* **`RedirectUrl` = the bridge's exact callback URL**,
  `<MCP_PUBLIC_URL>/authorize/callback`. rSTS validates by exact-URI
  match against this single value — there are no wildcards, and
  there is only one `RedirectUrl` per `RelyingPartyApplication`.
* **`AllowedOAuth2GrantTypes` must include `AuthorizationCode` and
  `Pkce`.**
* **No client secret.** The bridge is a public PKCE client; rSTS
  accepts `authorization_code` redemption with `code_verifier`
  alone. Do not configure a client secret on this entity.

Configure the rSTS provider IDs (`primaryProviderID`,
`secondaryProviderID`) for the IdPs you want the bridge to surface
to MCP clients. Clients pass these through as query parameters on
`/authorize`; if absent, rSTS prompts the user via its own UI.

## No Kubernetes Secrets ship by design

Nothing in `safeguard-mcp` is secret enough to need a
`kind: Secret`:

* The relay never persists Safeguard tokens — they ride on each MCP
  request as a `Bearer` header, are bound to per-request scope via
  `HttpRelaySafeguardSession`, and are dropped when the request ends.
* The bridge has no rSTS client secret (public PKCE client).
* `SAFEGUARD_HOST`, `MCP_PUBLIC_URL` (optional), `RSTS_CLIENT_ID`
  (optional), `BRIDGE_AUTH_CODE_TTL_SECONDS`, `BRIDGE_DISABLED`
  (optional kill switch) are non-secret config.

If you pull the container image from a private registry, create a
docker-registry pull-secret out-of-band and add an `imagePullSecrets`
entry to each Deployment:

```sh
kubectl -n safeguard-mcp create secret docker-registry safeguard-mcp-pull \
  --docker-server=<registry> \
  --docker-username=<user> \
  --docker-password=<token>
```

## Quick start — Kubernetes (plain manifests)

```sh
# Edit the placeholder values in 10-configmap.yaml first.
kubectl apply -f deploy/k8s/
```

The files are numbered so a single `kubectl apply -f` succeeds on
the first pass: namespace → ConfigMap → workloads → Services →
Ingress.

## Quick start — Kubernetes (Helm)

```sh
helm install safeguard-mcp deploy/helm/safeguard-mcp \
  --create-namespace -n safeguard-mcp \
  --set safeguardHost=safeguard.example.com \
  --set ingress.host=mcp.example.com

# Optional smoke tests (curl-based pods that verify /healthz and
# the well-known metadata documents).
helm test safeguard-mcp -n safeguard-mcp
```

The bridge is on by default; URLs are inferred from each incoming
request via `X-Forwarded-Proto` / `X-Forwarded-Host`. Pass
`--set oauthBridge.enabled=false` to run relay only.

### Advanced: pinning the published URL

Set `mcpPublicUrl` (and optionally `rstsClientId`) only when:

* The rSTS `RelyingPartyApplication.Realm` was registered under a
  name that differs from the user-facing hostname, or
* You want well-known metadata to advertise a fixed canonical URL
  even when requests arrive on alternate hostnames.

```sh
helm install safeguard-mcp deploy/helm/safeguard-mcp \
  --create-namespace -n safeguard-mcp \
  --set safeguardHost=safeguard.example.com \
  --set mcpPublicUrl=https://mcp.example.com \
  --set rstsClientId=https://mcp.example.com \
  --set ingress.host=mcp.example.com
```

Enable the relay HPA with `--set relay.autoscaling.enabled=true`.

## Quick start — Docker Compose

```sh
cd deploy/compose
cp .env.example .env
$EDITOR .env             # set SAFEGUARD_HOST (only)
docker compose up -d
```

Front the container with a reverse proxy that terminates TLS, sets
`X-Forwarded-Proto` and `X-Forwarded-Host`, and forwards to
`127.0.0.1:8080`. The bridge will publish well-known metadata under
the user-facing hostname automatically. Set `MCP_PUBLIC_URL` /
`RSTS_CLIENT_ID` in `.env` only if you need to pin the published URL.

## Inferred URLs and forwarded-headers trust

The OAuth bridge derives its public URL from `Request.Scheme +
Request.Host + Request.PathBase` on each request. To keep that
inference correct behind an ingress, the server enables
`UseForwardedHeaders` with a trust list that includes loopback +
RFC1918 (10/8, 172.16/12, 192.168/16) by default — covering the
common cluster-internal-ingress case. Production deployments behind
ingress should ensure their proxy is in a trusted CIDR, or extend the
list via `BRIDGE_TRUSTED_PROXIES` (comma-separated CIDRs). Untrusted
hops are ignored, so a malicious client cannot spoof
`X-Forwarded-Host` to make the bridge publish a different
authorization-server URL.
