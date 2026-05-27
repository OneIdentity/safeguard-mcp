---
id: bulk-asset-import
name: Bulk Create Assets and Accounts
description: Onboard multiple assets and their accounts programmatically using single or batch endpoints.
tags: bulk, import, asset, account, onboarding, platform, partition, batch, batchcreate
---

ID: bulk-asset-import
Name: Bulk Create Assets and Accounts
Goal: Onboard multiple assets and their accounts programmatically using single or batch endpoints.

Safeguard guidance: It is the responsibility of the Asset Administrator to add assets and accounts. All assets must be governed by a profile. All new assets are automatically governed by the default profile unless otherwise specified. An asset can only be in one partition at a time.

Steps (Single-Item):
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

Steps (Batch — for high-volume imports):
For importing many objects at once, use the batch endpoints. These accept a JSON array
body where each element has the same format as the single-item POST body above.

7. Batch create assets:
   - POST /v4/Assets/BatchCreate with body:
     [
       { "Name": "server-01", "NetworkAddress": "10.0.0.1", "PlatformId": <id>, "AssetPartitionId": <id> },
       { "Name": "server-02", "NetworkAddress": "10.0.0.2", "PlatformId": <id>, "AssetPartitionId": <id> }
     ]
   - Returns an array of created assets with their assigned IDs
8. Batch create accounts:
   - POST /v4/AssetAccounts/BatchCreate with body:
     [
       { "Name": "admin", "Asset": { "Id": <assetId1> } },
       { "Name": "admin", "Asset": { "Id": <assetId2> } }
     ]
9. Batch update (to modify multiple objects):
   - POST /v4/Assets/BatchUpdate with body: array of full asset objects (include Id)
   - POST /v4/AssetAccounts/BatchUpdate with body: array of full account objects (include Id)
10. Batch delete (to remove multiple objects):
    - POST /v4/Assets/BatchDelete with body: array of objects with Id: [{"Id": 1}, {"Id": 2}]
    - POST /v4/AssetAccounts/BatchDelete with body: same pattern

Available Batch Endpoints:
| Resource          | BatchCreate | BatchUpdate | BatchDelete |
|-------------------|-------------|-------------|-------------|
| /v4/Assets        | ✅          | ✅          | ✅          |
| /v4/AssetAccounts | ✅          | ✅          | ✅          |
| /v4/Users         | ✅          | ✅          | ✅          |
| /v4/UserGroups    | ✅          | ✅          | ✅          |
| /v4/AccountGroups | ✅          | ✅          | ✅          |
| /v4/AssetGroups   | ✅          | ✅          | ✅          |

Tips:
- Use Safeguard_Schema to see the full field list for POST /v4/Assets and POST /v4/AssetAccounts.
- Create assets before accounts because accounts reference the asset ID.
- The partition determines the password management profile.
- For imports of more than ~10 objects, prefer the Batch endpoints to reduce API round-trips.
- Batch endpoints use POST (not PUT or DELETE) — the operation is in the URL path segment.
- Each item in a batch array is independent; partial failures return per-item error details.

Related:
- Use account-discovery-status when onboarding via discovery rather than manual creation.
- Use password-rotation-status after onboarding to verify management health.

Source: Official Safeguard admin documentation.
