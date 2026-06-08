# Example Prompts

This guide shows example prompts for common Safeguard MCP workflows. These prompts work
with any MCP-compatible AI client (Claude Desktop, VS Code with Copilot, etc.).

## Getting Connected

### Connect to an appliance

> Connect to my Safeguard appliance at safeguard.corp.example.com

The agent will use `Safeguard_Connect` with the configured credentials (from environment
variables or by prompting you for them).

### Connect to multiple appliances

> Connect to both prod-safeguard.corp.com and dr-safeguard.corp.com

The server maintains multiple connections simultaneously, enabling cross-server workflows.

## Discovery & Exploration

### Find endpoints by keyword

> What API endpoints does Safeguard have for managing SSH keys?

> Find all endpoints related to access requests

> Show me what I can do with asset partitions

### Learn about request/response format

> What fields are required to create a new asset account?

> Show me the schema for creating an access request policy

### Understand query syntax

> How do I filter assets by platform type and sort by name?

> What pagination options does the Safeguard API support?

## Account & Asset Management

### Look up accounts

> List all asset accounts on the Linux partition

> Find the service account named "svc-backup" and show me its details

### Create assets and accounts

> Create a new Linux asset called "web-prod-01" at 10.0.1.50 in the Servers partition,
> then add a root account to it with password rotation enabled

### Bulk operations

> Import these 20 servers as assets and create root accounts for each:
> web01.corp.com, web02.corp.com, ...

## Access Requests & Policies

### Check access request activity

> Show me all pending access requests for the last 24 hours

> Who has checked out the DBA account on the production database server?

### Request access

> Request password checkout for the admin account on server db-prod-01

> Request an SSH session to linux-prod-03 using the root account

### Emergency access

> I need emergency breakglass access to the domain admin account

## Password & Key Management

### Check rotation status

> Show me all accounts where password rotation has failed in the last week

> What's the current rotation status for accounts in the Finance partition?

### Diagnose failures

> The service account svc-exchange failed its last password change. Help me figure out why.

The agent will follow the password-task-triage workflow: check task status, pull error
details, verify connectivity, and inspect profile configuration.

### SSH key rotation

> Set up SSH key rotation for the deploy account on all production Linux servers

## Health & Diagnostics

### Appliance health check

> Run a full health check on the Safeguard appliance

The agent follows the health-check workflow: appliance status, hardware metrics, cluster
state, certificate expiry, and license status.

### Cluster operations

> What's the current cluster status? Are all members healthy?

> Show me any recent cluster events or failovers

### Backup status

> When was the last backup taken? Is the backup schedule configured?

## Audit & Compliance

### Permission audit

> Show me all users with the GlobalAdmin permission

> What entitlements (roles) does user "jsmith" belong to?

### Access review

> List all access policies that grant password checkout to the DBA group

> Show me who has accessed the root account on prod-db-01 in the last 30 days

## Configuration & Setup

### Directory integration

> Set up LDAP directory integration with corp.example.com to import users from
> the IT-Admins organizational unit

### A2A credential retrieval

> Set up application-to-application credential retrieval for the backup service
> to pull the SQL service account password

### Certificate management

> Show me all SSL certificates on the appliance and their expiration dates

> Upload a new SSL certificate for the web interface

## Cross-Server Workflows

### Compare configurations

> Compare the access policies between prod-safeguard and dr-safeguard

### Migration

> Export all assets and accounts from the old appliance and recreate them on the new one

## Tips

- **Start broad, then narrow**: Ask the agent to discover endpoints first, then drill into
  specifics. This matches the Navigate → Understand → Execute pattern.

- **Use natural language**: Say "entitlements" even though the API calls them "Roles" — the
  terminology mapper handles the translation transparently.

- **Let the agent iterate**: Complex operations (bulk imports, multi-step setup) may take
  several tool calls. The agent will chain Discover → Schema → Execute as needed.

- **Check workflows first**: For multi-step procedures, the agent can consult built-in
  workflow recipes that encode best-practice sequences from the admin guide.
