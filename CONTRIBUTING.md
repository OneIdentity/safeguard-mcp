# Contributing to Safeguard MCP Server

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)
- Access to a Safeguard for Privileged Passwords (SPP) appliance for integration testing

## Building

```bash
cd src/SafeguardMcp
dotnet build
```

## Running Tests

**Unit tests** (no appliance required):

```bash
dotnet test tests/SafeguardMcp.Tests
```

**Integration tests** (requires a live SPP appliance):

```bash
export SPP_HOST=safeguard.example.com
export SPP_PASSWORD=YourPassword
export SPP_VERIFY=false          # for self-signed certs
dotnet test tests/SafeguardMcp.IntegrationTests
```

Integration tests are automatically skipped when `SPP_HOST` is not set. Environment
variables follow the same `SPP_*` convention used by PySafeguard, safeguard.js, and
safeguard-ansible.

| Variable | Default | Purpose |
|----------|---------|---------|
| `SPP_HOST` | *(required)* | Appliance hostname or IP |
| `SPP_USERNAME` | `Admin` | Username |
| `SPP_PASSWORD` | *(required)* | Password |
| `SPP_PROVIDER` | `local` | Identity provider |
| `SPP_VERIFY` | `true` | Set `false` for self-signed certs |

## Running Locally

**Stdio mode** (for use with VS Code, Claude Desktop):

```bash
cd src/SafeguardMcp
dotnet run
```

**HTTP mode** (for network access, containers):

```bash
cd src/SafeguardMcp
dotnet run -- --http
```

Set environment variables before running:

| Variable | Purpose |
|----------|---------|
| `SAFEGUARD_HOST` | Appliance hostname or IP |
| `SAFEGUARD_USER` | Login username |
| `SAFEGUARD_PASSWORD` | Login password |
| `SAFEGUARD_PROVIDER` | Identity provider (default: `local`) |
| `SAFEGUARD_IGNORE_SSL` | Set `true` for self-signed certs |

## Project Structure

```
src/SafeguardMcp/
├── Program.cs          # Entry point, dual-transport setup
├── Tools/              # MCP tool classes (auto-discovered via [McpServerToolType])
├── Catalog/            # Dynamic API catalog, MCP resources, terminology mapping
└── Workflows/          # Embedded markdown workflow recipes (YAML front matter)

tests/SafeguardMcp.Tests/   # xUnit tests for pure logic (no live appliance needed)
```

## Adding a Workflow Recipe

Workflow recipes are Markdown files with YAML front matter, loaded as embedded resources.

1. Create a new file in `src/SafeguardMcp/Workflows/`:

```markdown
---
id: my-new-workflow
name: My New Workflow
description: Brief description of what this workflow accomplishes
tags: [tag1, tag2, tag3]
---

## Overview

What this workflow does and when to use it.

## Prerequisites

What must be in place before starting.

## Steps

### Step 1: Do the first thing

**Endpoint:** `GET /v4/SomeEndpoint`
**Parameters:** `filter=Name eq 'value'`

Explanation of what this does and what to look for in the response.

### Step 2: Do the next thing

...

## Troubleshooting

Common issues and how to resolve them.
```

2. Build to verify it compiles as an embedded resource:

```bash
dotnet build
```

3. Test that the workflow loads:

```bash
dotnet test --filter "WorkflowRecipeTests"
```

The `WorkflowRecipeTests.AllRecipes_LoadSuccessfully` test automatically validates all
embedded workflow files have valid front matter.

**No code changes required** — the `<EmbeddedResource Include="Workflows\*.md" />` glob
in the csproj picks up new files automatically.

## Adding a Terminology Mapping

Edit `src/SafeguardMcp/Catalog/TerminologyMap.cs` and add a new entry to the `Aliases` array:

```csharp
// Description of the mapping
["ui term", "alternate term", "apiname"],
```

Each entry is a group of synonyms — searching for any one matches all others.

## Code Conventions

- Target: .NET 10, C# 13
- Nullable reference types: disabled (the SDK doesn't support them cleanly)
- All SDK calls are synchronous — wrap in `Task.Run()` for async contexts
- Use `internal` visibility for helper functions to enable testing via `InternalsVisibleTo`
- Tests use xUnit with `[Theory]`/`[InlineData]` for parameterized cases

## Pull Request Guidelines

- Ensure `dotnet build` passes with no errors
- Ensure `dotnet test` passes with no failures
- Add tests for any new pure logic (helpers, parsing, routing)
- Integration tests (requiring a live appliance) are run manually and not required for PR merge
