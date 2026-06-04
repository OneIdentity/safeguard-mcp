---
id: account-discovery-status
name: Check Discovery Job Results
description: Review account and asset discovery jobs, results, and enablement steps.
tags: discovery, account discovery, asset discovery, job, managed
---

ID: account-discovery-status
Name: Check Discovery Job Results
Goal: Review account and asset discovery job status and results.

Safeguard guidance: Account Discovery jobs discover accounts of the assets that are in the scope of a profile. After the job runs, you can select whether to manage the account.

Steps:
1. List account discovery jobs:
   - GET /v4/AccountDiscoveryJobs with query: fields=Id,Name,LastRunDate,LastSuccessDate,AssetPartitionId
2. Check recent results:
   - GET /v4/DiscoveredAccounts with query: orderby=-DiscoveredDate&limit=50&fields=Id,AccountName,AssetName,DiscoveredDate,IsManaged
3. List asset discovery jobs:
   - GET /v4/AssetDiscoveryJobs with query: fields=Id,Name,LastRunDate,LastSuccessDate
4. Check discovered assets:
   - GET /v4/DiscoveredAssets with query: orderby=-DiscoveredDate&limit=50&fields=Id,Name,NetworkAddress,DiscoveredDate
5. To manage a discovered account:
   - POST /v4/DiscoveredAccounts/{id}/Enable
6. To run a discovery job manually:
   - POST /v4/AccountDiscoveryJobs/{id}/Run

Related:
- Use bulk-asset-import when you need manual onboarding instead of discovery.
- Use password-rotation-status after enabling discovered accounts for management.

Source: Official Safeguard admin documentation.
