---
id: bulk-asset-operations
name: Bulk Asset / Account Operations (Create, Update, Delete)
description: Onboard, modify, or remove many assets and accounts in one round trip using the BatchCreate / BatchUpdate / BatchDelete endpoints.
tags: bulk, import, asset, account, onboarding, platform, partition, batch, batchcreate, batchupdate, batchdelete, many, delete, remove, cleanup, update, modify
---

ID: bulk-asset-operations
Name: Bulk Asset / Account Operations (Create, Update, Delete)
Goal: Onboard, modify, or remove many assets and accounts in one round trip using the BatchCreate / BatchUpdate / BatchDelete endpoints.

Safeguard guidance: It is the responsibility of the Asset Administrator to add, modify, and remove assets and accounts. All assets must be governed by a profile. All new assets are automatically governed by the default profile unless otherwise specified. An asset can only be in one partition at a time. Before issuing the same DELETE / POST / PUT against the same top-level collection more than ~5 times in a row, reach for the Batch* sibling first; the bulk path is one round trip and returns per-row failure detail in a single envelope, which is dramatically faster to triage than N parallel error responses.

Batch operations at a glance:

| Resource          | BatchCreate | BatchUpdate | BatchDelete |
|-------------------|-------------|-------------|-------------|
| /v4/Assets        | ✅          | ✅          | ✅          |
| /v4/AssetAccounts | ✅          | ✅          | ✅          |
| /v4/Users         | ✅          | ✅          | ✅          |
| /v4/UserGroups    | ✅          | ✅          | ✅          |
| /v4/AccountGroups | ✅          | ✅          | ✅          |
| /v4/AssetGroups   | ✅          | ✅          | ✅          |

Batch endpoint shape:
- POST /v4/{Resource}/BatchCreate — body is a JSON array; each element matches the single-item POST body.
- POST /v4/{Resource}/BatchUpdate — body is a JSON array of full objects (each MUST include Id).
- POST /v4/{Resource}/BatchDelete — body is a JSON array of {"Id": <int>} entries.
- All three use POST regardless of intent — the verb lives in the URL path segment.
- Each item in a batch array is independent: partial failures return per-item error details in the same response.
- For dependency-blocked deletes (50104 — asset/account referenced by an active AccessRequest), close the named request via Safeguard_CloseAccessRequest then re-issue the BatchDelete on the unblocked ids; do NOT fan back out to N parallel deletes.
- Use Safeguard_Discover with search="Batch" to enumerate Batch* endpoints on the live appliance.

Examples:
- POST /v4/Assets/BatchCreate
  [
    { "Name": "server-01", "NetworkAddress": "10.0.0.1", "PlatformId": <id>, "AssetPartitionId": <id> },
    { "Name": "server-02", "NetworkAddress": "10.0.0.2", "PlatformId": <id>, "AssetPartitionId": <id> }
  ]
- POST /v4/AssetAccounts/BatchCreate
  [
    { "Name": "admin", "Asset": { "Id": <assetId1> } },
    { "Name": "admin", "Asset": { "Id": <assetId2> } }
  ]
- POST /v4/Assets/BatchUpdate
  Body: array of full asset objects (each MUST include Id).
- POST /v4/Assets/BatchDelete
  [{"Id": 1}, {"Id": 2}]

Steps (Single-Item — for small N or when batch is not available):
1. Identify the target partition (or use default):
   - GET /v4/AssetPartitions with query: fields=Id,Name,IsDefault
2. Get available platforms:
   - GET /v4/Platforms with query: fields=Id,DisplayName,PlatformType
   - Common examples include Windows and Linux, but platform IDs vary by environment
3. For each asset, create it:
   - POST /v4/Assets with body:
     {
       "Name": "server-name",
       "NetworkAddress": "10.0.0.1",
       "PlatformId": <platform_id>,
       "AssetPartitionId": <partition_id>,
       "Description": "optional description"
     }
4. Set a service account for the asset (for password management):
   - PUT /v4/Assets/{assetId} with ConnectionProperties including the service account
   - Or set the service account at the partition profile level
5. For each account on the asset:
   - POST /v4/AssetAccounts with body:
     {
       "Name": "account-name",
       "Asset": { "Id": <asset_id> },
       "Description": "optional"
     }
6. Verify with TestConnection:
   - POST /v4/Assets/{assetId}/TestConnection
7. To modify a single object: PUT /v4/Assets/{id} or PUT /v4/AssetAccounts/{id}.
8. To delete a single object: DELETE /v4/Assets/{id} or DELETE /v4/AssetAccounts/{id}.

Tips:
- Use Safeguard_Schema to see the full field list for POST /v4/Assets and POST /v4/AssetAccounts.
- Create assets before accounts because accounts reference the asset ID.
- The partition determines the password management profile.
- For operations across more than ~10 objects, prefer the Batch endpoints to reduce API round-trips.
- Batch endpoints use POST (not PUT or DELETE) — the operation is in the URL path segment.
- Each item in a batch array is independent; partial failures return per-item error details.

Related:
- Use account-discovery-status when onboarding via discovery rather than manual creation.
- Use password-rotation-status after onboarding to verify management health.

Source: Official Safeguard admin documentation.
