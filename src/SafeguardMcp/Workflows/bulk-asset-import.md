---
id: bulk-asset-import
name: Bulk Create Assets and Accounts
description: Onboard multiple assets and their accounts programmatically.
tags: bulk, import, asset, account, onboarding, platform, partition
---

ID: bulk-asset-import
Name: Bulk Create Assets and Accounts
Goal: Onboard multiple assets and their accounts programmatically.

Safeguard guidance: It is the responsibility of the Asset Administrator to add assets and accounts. All assets must be governed by a profile. All new assets are automatically governed by the default profile unless otherwise specified. An asset can only be in one partition at a time.

Steps:
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

Tips:
- Use Safeguard_Schema to see the full field list for POST /v4/Assets and POST /v4/AssetAccounts.
- Create assets before accounts because accounts reference the asset ID.
- The partition determines the password management profile.

Related:
- Use account-discovery-status when onboarding via discovery rather than manual creation.
- Use password-rotation-status after onboarding to verify management health.

Source: Official Safeguard admin documentation.
