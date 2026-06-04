---
id: set-initial-account-password
name: Set Initial Password on a New Account
description: Use Safeguard's server-side password operations so plaintext never enters your transcript.
tags: password, initial, rotate, change, generate, sensitive, credential, bootstrap, onboarding
---

ID: set-initial-account-password
Name: Set Initial Password on a New Account
Goal: Give a newly-created managed account a strong, partition-rule-compliant password without ever handling the plaintext value.

Safeguard guidance: Safeguard manages account passwords. The product is designed so the appliance generates rule-compliant secrets and pushes them to managed assets — agents and operators should never need to mint passwords client-side. Generating a password outside Safeguard (e.g. with a local pwgen, PowerShell `Get-Random`, or an LLM) bypasses the partition's password rule, leaks the secret into model context and shell transcripts, and is unnecessary in every scenario the product was designed for.

Decision tree:
1. Did you just create a new managed account and want Safeguard to set its password? → Use **ChangePassword** (server-side, no plaintext returned).
2. Do you genuinely need a rule-compliant value out of band (rare, e.g. you must hand it to a human admin)? → Use **GeneratePassword** (returns one sample string).
3. Do you already have a known value you must align to (e.g. importing an existing account whose password you control)? → Use **PUT Password** (you supply the value).

Default flow (#1) — ChangePassword, no plaintext ever transits the model:
1. Confirm the account exists and is managed:
   - GET /v4/AssetAccounts/{id} with query: fields=Id,Name,Asset.Id,Asset.Name,SyncGroupId,DisableReason
2. Trigger Safeguard to rotate per the partition's password rule and push to the asset:
   - POST /v4/AssetAccounts/{id}/ChangePassword
   - No request body. Optional query: extendedLogging=true for richer task log on failure.
   - Response is a PasswordActivityLog (HTTP 201 Created or 202 Accepted). The new password is NOT in the response — Safeguard stores it.
3. For multiple accounts, call ChangePassword in parallel by id from the client. There is no batch variant.
4. Verify task outcome on each account:
   - GET /v4/AssetAccounts/{id} with query: fields=Id,Name,TaskProperties
   - Inspect TaskProperties.HasAccountTaskFailure, LastPasswordChangeDate, LastSuccessPasswordChangeDate.

When you genuinely need a value (#2) — GeneratePassword:
1. POST /v4/AssetAccounts/{id}/GeneratePassword (no body) — returns a rule-compliant sample string in the response body.
2. Treat the returned value as sensitive: do not echo it in your summary, do not log it, do not include it in tables shown to the user. Hand it off via the channel the human asked for and stop discussing it.
3. If you only want to verify what a partition's rule would produce without pinning to one account, use:
   - POST /v4/AssetPartitions/{id}/PasswordRules/{ruleId}/GeneratePassword

When you already have a known value (#3) — PUT Password:
1. PUT /v4/AssetAccounts/{id}/Password with the password value as the request body.
2. Same sensitivity rules as #2 — do not echo the value back to the user.

Why not generate a password locally?
- Local pwgen / Get-Random / LLM-generated strings are not guaranteed to satisfy the partition's password rule (length, character class, dictionary checks, history). ChangePassword/GeneratePassword always are.
- Locally-minted plaintext travels through your shell transcript and the model's context window. ChangePassword keeps it inside Safeguard.
- There is no batch endpoint for ChangePassword/CheckPassword — calling them per id in parallel is the documented and intended pattern.

Sensitive material — general rule:
- Treat the following as sensitive credential material: account passwords, SSH private keys, API keys, A2A registration secrets, certificate private keys, and the contents of any /v4/AssetAccounts/{id}/Password, /SshKey, or /A2A endpoint response. Do not echo these in transcripts, summaries, status tables, or follow-up tool calls. Reference them by account id, not value.

Related workflows:
- Use password-rotation-status to verify scheduled rotations across an estate.
- Use ssh-key-rotation for the SSH-key-bearing equivalent of this flow.
- Use bulk-asset-import to create the assets and accounts before running this recipe on the new account ids.

Source: Verified against pangaeaappliance src/Service/Core/Controllers/V4/Partitions/AssetAccountsController_Tasks.cs (ChangePasswordAsync) and src/Service/Core/Controllers/V2/Partitions/AssetAccountsController.cs (GeneratePasswordAsync).
