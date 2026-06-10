---
id: account-discovery-status
name: Set Up, Run, and Review Account Discovery
description: End-to-end account-discovery workflow on a partition -- create an AccountDiscoverySchedule, scope it (partition-wide or to specific assets), run it now, bulk-manage the discovered accounts, and inspect results. Includes the original "check discovery job results" walkthrough.
tags: discovery, account discovery, asset discovery, job, managed, discover accounts, bulk discovery, AccountDiscoverySchedule, BatchManage, schedule, onboard
---

ID: account-discovery-status
Name: Set Up, Run, and Review Account Discovery
Goal: Stand up bulk account discovery on a partition and bring the resulting accounts under management, or inspect results from an existing discovery run.

Context: Account discovery in Safeguard is owned by `AccountDiscoverySchedule` records, which always live UNDER a partition (`/v4/AssetPartitions/{partitionId}/AccountDiscoverySchedules/...`). There is no top-level `/v4/AccountDiscoveryJobs` collection on this appliance. A schedule's scope is the partition by default, but you can narrow it to a specific list of assets via the schedule's `/Assets` sub-endpoint -- the appliance supports both partition-scope (the default) and per-asset scope on the SAME schedule object.

Discovered accounts also live under the partition (`/v4/AssetPartitions/{partitionId}/DiscoveredAccounts/...`). There is no top-level `/v4/DiscoveredAccounts` and no `/Enable` action -- the action that brings a discovered account under management is `Manage`, available both per-account and as a batch.

============================================================
Section 1: Setup -- Stand up bulk discovery for a partition
============================================================

Steps:

1. Find the partition you want to scope discovery to:
   - `GET /v4/AssetPartitions?fields=Id,Name`

2. Check whether the partition already has an AccountDiscoverySchedule you can reuse:
   - `GET /v4/AssetPartitions/{partitionId}/AccountDiscoverySchedules?fields=Id,Name,ScheduleType,DiscoveryRuleIds`
   - If a usable schedule already exists, skip to step 4.

3. Create a new AccountDiscoverySchedule on the partition. The schedule carries the rules (regex / directory-query / etc.) plus the run cadence:
   - `POST /v4/AssetPartitions/{partitionId}/AccountDiscoverySchedules`
   - Body: at minimum a `Name`, a `ScheduleType` (see the `partition-schedule-update` recipe for the ScheduleType conditional-required-field map -- the same rules apply here), and one or more rule definitions. Use `Safeguard_Schema POST /v4/AssetPartitions/{id}/AccountDiscoverySchedules` to see the full body shape on your appliance.

4. Scope the schedule.
   - DEFAULT: the schedule covers EVERY asset in the partition that the schedule's rules match. Nothing to do.
   - To narrow to a specific asset list (per-asset scope): `PUT /v4/AssetPartitions/{partitionId}/AccountDiscoverySchedules/{scheduleId}/Assets` with the body `[ { "Id": <assetId> }, ... ]`. Both scoping modes are supported -- this is an opt-in narrowing, not a different schedule type.
   - To inspect the current scope: `GET /v4/AssetPartitions/{partitionId}/AccountDiscoverySchedules/{scheduleId}/Assets?fields=Id,Name`.

5. Run the schedule now (don't wait for the next scheduled run). On THIS appliance there is no global "Run discovery N" endpoint; you trigger discovery one of two ways:
   - Run the schedule itself end-to-end: `POST /v4/AssetPartitions/{partitionId}/AccountDiscoverySchedules/{scheduleId}/RunNow` if your appliance build exposes it, OR
   - Run discovery against a single asset using the schedule's rules: `POST /v4/Assets/{assetId}/DiscoverAccounts` (per-asset trigger).
   - For a dry run that does NOT persist results: `POST /v4/AssetPartitions/{partitionId}/AccountDiscoverySchedules/TestDiscovery` with the schedule body in the post -- useful when authoring rules.

6. Wait for the job to finish (it is asynchronous). Watch the AuditLog for completion:
   - `GET /v4/AuditLog/Discovery/Accounts?count=true&orderby=-StartTime&limit=20`

7. Bulk-bring the discovered accounts under management. The appliance exposes BOTH a per-row and a batch path -- prefer the batch:
   - Batch: `POST /v4/AssetPartitions/{partitionId}/DiscoveredAccounts/BatchManage` with body `[ { "AssetId": ..., "AccountName": "...", "AssetPartitionId": ... }, ... ]`. Use `Safeguard_Schema POST /v4/AssetPartitions/{id}/DiscoveredAccounts/BatchManage` to confirm the exact item shape on your appliance.
   - Per-row: `POST /v4/AssetPartitions/{partitionId}/DiscoveredAccounts/{assetId}/{accountName}/Manage` (the per-account variant -- use when you are managing a small selection or scripting one-by-one).
   - To list candidates first: `GET /v4/AssetPartitions/{partitionId}/DiscoveredAccounts?filter=IsManaged eq false&fields=AssetId,AssetName,AccountName,DiscoveredDate&limit=200`.

============================================================
Section 2: Inspection -- Check discovery job results
============================================================

Steps:

1. List the account discovery schedules across all partitions:
   - `GET /v4/AssetPartitions/AccountDiscoverySchedules?fields=Id,Name,AssetPartitionId,AssetPartitionName,ScheduleType`

2. List discovered accounts across all partitions:
   - `GET /v4/AssetPartitions/DiscoveredAccounts?orderby=-DiscoveredDate&limit=50&fields=AssetId,AssetName,AccountName,DiscoveredDate,IsManaged`
   - For a single partition: `GET /v4/AssetPartitions/{partitionId}/DiscoveredAccounts?...`.

3. List asset discovery jobs:
   - `GET /v4/AssetPartitions/DiscoveryJobs?fields=Id,Name,AssetPartitionId,LastRunDate,LastSuccessDate`
   - Single partition: `GET /v4/AssetPartitions/{partitionId}/DiscoveryJobs?...`.

4. Inspect discovered assets from a specific asset-discovery run via the audit-log linkage:
   - `GET /v4/AuditLog/Discovery/Assets?orderby=-StartTime&limit=20&fields=Id,JobName,AssetPartitionName,StartTime,EndTime`
   - For a specific run: `GET /v4/AuditLog/Discovery/Assets/{id}/DiscoveredAssets?fields=Name,NetworkAddress,DiscoveredDate`.

5. Bring a single discovered account under management:
   - `POST /v4/AssetPartitions/{partitionId}/DiscoveredAccounts/{assetId}/{accountName}/Manage`

6. Run a one-shot asset discovery job now:
   - `POST /v4/AssetPartitions/{partitionId}/DiscoveryJobs/{jobId}/RunDiscovery`

7. Run account discovery against a single asset on demand (uses the rules from any AccountDiscoverySchedules that target this asset):
   - `POST /v4/Assets/{assetId}/DiscoverAccounts`

Related:
- Use `bulk-asset-operations` when you need manual onboarding (or modify/delete) instead of discovery.
- Use `password-rotation-status` after managing discovered accounts to confirm they enter the rotation.
- Use `partition-schedule-update` to change the run cadence of an AccountDiscoverySchedule -- it follows the same ScheduleType conditional-required-field rules as the password/SSH schedules.

Source: Live appliance verification of the discovery endpoint surface (`/v4/AccountDiscoveryJobs` and `/v4/DiscoveredAccounts/{id}/Enable` were tested and return 404 on this appliance; the partition-scoped paths above are the real surface). `BatchManage` and the per-asset / per-partition scope toggle confirmed against the live endpoint catalog.
