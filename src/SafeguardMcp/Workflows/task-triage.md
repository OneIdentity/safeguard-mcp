---
id: task-triage
name: Diagnose Password/SSH Key Task Failures
description: Find accounts with failing platform tasks and identify root causes.
tags: task, triage, password, ssh, failure, connectivity, service account
---

ID: task-triage
Name: Diagnose Password/SSH Key Task Failures
Goal: Find accounts with failing platform tasks and identify root causes.

Safeguard guidance: The most common causes of failure are either connectivity issues between the appliance and the managed system, or problems with service accounts.

Steps:
1. Find accounts with task failures:
   - GET /v4/AssetAccounts with query: filter=TaskProperties.HasAccountTaskFailure eq true&fields=Id,Name,Asset.Name,Asset.Id,TaskProperties&orderby=-TaskProperties.LastFailurePasswordChangeDate&limit=50
2. For each failing account, get recent task logs:
   - GET /v4/AssetAccounts/{accountId}/ChangeRequests with query: orderby=-StartDate&limit=5
   - Look at the error message in the response
3. Test connectivity to the asset:
   - POST /v4/Assets/{assetId}/TestConnection
   - This verifies network access and service account credentials
4. Check the service account and profile:
   - GET /v4/Assets/{assetId} with query: fields=Id,Name,AssetPartitionId,ConnectionProperties
   - GET /v4/AssetPartitions/{partitionId}/Profiles to see password change settings

Common root causes:
- Service account password out of sync with target system
- Network connectivity blocked (TestConnection reveals this)
- Account locked out on the target platform
- Insufficient service account permissions (non-built-in admin on Windows needs UAC disabled)
- SSH host key changed or missing
- Cipher mismatch ("no cipher supported" error)

Resolution:
- Fix the underlying issue, then retry with:
  - POST /v4/AssetAccounts/{accountId}/CheckPassword
  - POST /v4/AssetAccounts/{accountId}/ChangePassword

Related:
- Start from health-check for a deployment-wide view.
- Use password-rotation-status to measure overall password management health.

Source: Official Safeguard troubleshooting documentation.
