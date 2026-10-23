# @oneidentity/safeguard-mcp

The official [One Identity Safeguard](https://www.oneidentity.com/products/safeguard/)
[MCP](https://modelcontextprotocol.io/) server. It exposes Safeguard for Privileged
Passwords (SPP) as MCP tools so AI assistants — Claude, GitHub Copilot, and others — can
manage privileged access: look up accounts, request and retrieve credentials, create
assets, run reports, and diagnose appliance health across the full Safeguard REST API.

The package ships prebuilt native binaries — **no .NET SDK required**. The correct binary
for your platform is pulled automatically as an optional dependency.

## Install

Run on demand with `npx` (what most MCP client configs use):

```bash
npx -y @oneidentity/safeguard-mcp
```

`npx` caches the package on first use and **does not auto-upgrade**. Pin a version spec so
each launch runs what you expect:

- `@oneidentity/safeguard-mcp@latest` — re-check the registry each launch.
- `@oneidentity/safeguard-mcp@0.4.0` — pin an exact, reproducible version.

Or install globally for a persistent `safeguard-mcp` command:

```bash
npm install -g @oneidentity/safeguard-mcp
safeguard-mcp
```

## Configure your MCP client

Point the client at the server and set `SAFEGUARD_HOST` to your appliance address. A
typical stdio entry:

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

On first use the server prints a verification URL and one-time code for device-code login —
no passwords are stored in config files.

For a self-signed lab certificate, add `"SAFEGUARD_IGNORE_SSL": "true"` to `env`.

## Documentation

- Full docs, client-specific setup, and security model:
  <https://github.com/OneIdentity/safeguard-mcp#readme>
- Per-client config files: [`docs/CLIENT-SETUP.md`](https://github.com/OneIdentity/safeguard-mcp/blob/main/docs/CLIENT-SETUP.md)
- Usage examples: [`docs/EXAMPLES.md`](https://github.com/OneIdentity/safeguard-mcp/blob/main/docs/EXAMPLES.md)

## Supported platforms

- Linux x64
- Windows x64
- macOS arm64

Requires Node.js 18 or newer. For shared/HTTP deployments, a Docker image is also
available — see the GitHub documentation.

## License

Apache-2.0
