---
id: partition-reassignment
name: Move an Account or Asset to a Different Partition
description: Reassign an asset or asset account to a different AssetPartition.
tags: move, transfer, migrate, reassign, partition, partition reassignment, partition transfer, AssetPartitionId
---

ID: partition-reassignment
Name: Move an Account or Asset to a Different Partition
Goal: Reassign an existing managed asset (or one of its accounts) to a different AssetPartition without recreating it.

Context: Safeguard does not expose a dedicated "transfer" endpoint. Reassignment is performed by updating the entity's `AssetPartitionId` on the standard PUT endpoint. The change moves the asset (and all of its accounts) into the target partition's owner/approver/policy scope; existing access requests remain auditable. Account-only reassignment is rare — when needed, the same pattern applies on the AssetAccount.

Required permissions: Asset Administrator (or partition delegated owner of BOTH the source and target partitions).

Steps:

1. Locate the source asset and confirm its current partition:
   - `GET /v4/Assets/{id}?fields=Id,Name,AssetPartitionId,AssetPartitionName`
   - Note the existing `AssetPartitionId`.

2. Locate the target partition:
   - `GET /v4/AssetPartitions?filter=Name ieq 'TargetPartition'&fields=Id,Name`
   - Capture its `Id`.

3. Read the full asset payload (PUT replaces the entity):
   - `GET /v4/Assets/{id}`
   - Keep the response body verbatim — you will mutate only `AssetPartitionId`.

4. Update the asset's partition assignment:
   - `PUT /v4/Assets/{id}` with the payload from step 3, replacing the `AssetPartitionId` field with the target partition's `Id`. Keep all other fields.

5. (Optional) Move a single account instead of the whole asset:
   - `GET /v4/AssetAccounts/{accountId}`
   - `PUT /v4/AssetAccounts/{accountId}` with the same body but a new `AssetPartitionId`.
   - Note: moving an account to a partition whose owners cannot see the parent asset will surface validation errors — prefer moving the asset.

6. Verify the reassignment:
   - `GET /v4/AssetPartitions/{newPartitionId}/Assets?filter=Id eq {assetId}`
   - Confirm the asset appears under the new partition.

Common failure: "Cannot change AssetPartition because dependent policies exist." Remove the asset from any account-group or asset-group bound to a policy in the source partition, then retry.
