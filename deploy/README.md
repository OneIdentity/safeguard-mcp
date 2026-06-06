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
* `SAFEGUARD_HOST`, `RSTS_CLIENT_ID`, `MCP_PUBLIC_URL`,
  `BRIDGE_AUTH_CODE_TTL_SECONDS` are non-secret config.

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
  --set mcpPublicUrl=https://mcp.example.com \
  --set rstsClientId=https://mcp.example.com \
  --set ingress.host=mcp.example.com

# Optional smoke tests (curl-based pods that verify /healthz and
# the well-known metadata documents).
helm test safeguard-mcp -n safeguard-mcp
```

Enable the relay HPA with `--set relay.autoscaling.enabled=true`.

## Quick start — Docker Compose

```sh
cd deploy/compose
cp .env.example .env
$EDITOR .env             # set SAFEGUARD_HOST, MCP_PUBLIC_URL, RSTS_CLIENT_ID
docker compose up -d
```

Front the container with a reverse proxy that terminates TLS at
`MCP_PUBLIC_URL` and forwards to `127.0.0.1:8080`.
