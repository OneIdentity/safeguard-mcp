# Client Setup

How to wire `safeguard-mcp` into the major MCP clients in **stdio mode** —
where the client launches the server as a local subprocess. All four clients
use the same underlying server binary (`npx -y @oneidentity/safeguard-mcp`)
and the same environment variables; they differ only in where the config
lives and what the top-level JSON keys are called.

> Looking for the shared HTTP deployment shape instead? `safeguard-mcp` can
> also run as a long-lived HTTP server that multiple users connect to over
> the network. See [`deploy/README.md`](../deploy/README.md) for reference
> deployment shapes, and [README → HTTP Mode](../README.md#http-mode-shared-server-deployment)
> for how clients point at one.

Set `SAFEGUARD_HOST` to the hostname of your appliance. In stdio mode you can
omit it only if your MCP client supports elicitation forms — the server will
then prompt for the hostname on first use. HTTP-mode deployments require
`SAFEGUARD_HOST` at startup.

On first use, the server prints a verification URL and a one-time code;
complete the sign-in from any browser to authorize the connection.

## At a glance

| Client                | Config location                          | Top-level key | Add via CLI?                  |
|-----------------------|------------------------------------------|---------------|-------------------------------|
| Claude Desktop        | `claude_desktop_config.json` (global)    | `mcpServers`  | no                            |
| Claude Code           | `.mcp.json` (project) / `~/.claude.json` | `mcpServers`  | `claude mcp add`              |
| VS Code (Copilot)     | `.vscode/mcp.json` (workspace)           | `servers`     | no                            |
| GitHub Copilot CLI    | `~/.copilot/mcp-config.json` (global)    | `mcpServers`  | `/mcp add` (interactive)      |

---

## Claude Desktop

Edit `claude_desktop_config.json` (Settings → Developer → Edit Config):

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

- **Config scope:** global, per-user.
- **Top-level key:** `mcpServers`.

---

## Claude Code

Use the `claude mcp add` CLI (recommended) or hand-edit a JSON file.

**CLI:**

```bash
claude mcp add --transport stdio safeguard \
  --env SAFEGUARD_HOST=safeguard.corp.example.com \
  -- npx -y @oneidentity/safeguard-mcp
```

Everything after `--` is passed to the server untouched; the `--env` flag must
come before the server name. See `claude mcp list` and `claude mcp remove
safeguard` to manage the entry.

**JSON (project-scoped, checked into the repo):** create `.mcp.json` at the
project root.

**JSON (user-scoped):** edit `~/.claude.json`.

```json
{
  "mcpServers": {
    "safeguard": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@oneidentity/safeguard-mcp"],
      "env": {
        "SAFEGUARD_HOST": "safeguard.corp.example.com"
      }
    }
  }
}
```

- **Config scopes:** project (`.mcp.json`), user (`~/.claude.json`), or local.
- **Top-level key:** `mcpServers`.
- Project-scoped servers prompt for approval the first time you launch
  `claude` in that repo.

---

## VS Code (GitHub Copilot)

Create `.vscode/mcp.json` in your workspace:

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

- **Config scope:** per-workspace (commit it to share with your team, or add
  it to `.gitignore` for personal use).
- **Top-level key:** `servers` — note that this differs from every other
  client, which use `mcpServers`.

---

## GitHub Copilot CLI

Use the interactive `/mcp add` slash command (recommended) or hand-edit the
config file.

**Interactive:** start `copilot`, run `/mcp add`, and fill in:
- **Server Name:** `safeguard`
- **Server Type:** `STDIO` (the portable name; `Local` works identically)
- **Command:** `npx -y @oneidentity/safeguard-mcp`
- **Environment Variables:** `{"SAFEGUARD_HOST": "safeguard.corp.example.com"}`
- **Tools:** `*`

Press <kbd>Ctrl</kbd>+<kbd>S</kbd> to save. The server is available immediately.

**File:** edit `~/.copilot/mcp-config.json`.

```json
{
  "mcpServers": {
    "safeguard": {
      "type": "local",
      "command": "npx",
      "args": ["-y", "@oneidentity/safeguard-mcp"],
      "env": {
        "SAFEGUARD_HOST": "safeguard.corp.example.com"
      },
      "tools": ["*"]
    }
  }
}
```

- **Config scope:** global, per-user.
- **Top-level key:** `mcpServers`.
- The `type` field is `local` in the Copilot CLI file format (alias for
  `stdio`). Use `stdio` if you want the same JSON to be portable to other
  clients.
- Manage with `/mcp show`, `/mcp edit safeguard`, `/mcp delete safeguard`.
