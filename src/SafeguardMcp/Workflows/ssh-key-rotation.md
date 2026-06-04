---
id: ssh-key-rotation
name: SSH Key Rotation Management
description: Configure and monitor SSH key check, change, discovery, and sync group operations.
tags: SSH key, rotation, check, change, discovery, sync group, profile, managed key
---

ID: ssh-key-rotation
Name: SSH Key Rotation Management
Goal: Set up and monitor automated SSH key rotation for managed accounts, parallel to password rotation.

Context: Safeguard manages SSH keys similarly to passwords — profile-based rotation with check/change schedules. Each managed account can have a single managed SSH key. SSH key operations can be toggled on/off globally via Global Services settings.

Prerequisites:
- SSH Key Management services must be enabled (Global Services)
- A partition with an SSH key profile
- Managed accounts on assets that use SSH key authentication

Steps:

## Verify SSH Key Services Are Enabled
1. Check global service status:
   - GET /v4/Settings with query: filter=Name eq 'CheckSshKeyManagementEnabled' or Name eq 'ChangeSshKeyManagementEnabled'
   - Both should be true for automated rotation

## Create an SSH Key Profile
2. Create a profile in a partition:
   - POST /v4/AssetPartitions/{partitionId}/SshKeyProfiles with body:
     {
       "Name": "Standard SSH Key Rotation",
       "Description": "90-day rotation with weekly checks"
     }
3. Add Check SSH Key settings:
   - POST /v4/AssetPartitions/{partitionId}/SshKeyCheckSchedules with body:
     {
       "Name": "Weekly SSH Key Check",
       "ScheduleType": "Weekly",
       "TimeOfDay": "03:00",
       "DaysOfWeek": ["Sunday"]
     }
   - Assign to the profile
4. Add Change SSH Key settings:
   - POST /v4/AssetPartitions/{partitionId}/SshKeyChangeSchedules with body:
     {
       "Name": "90-Day SSH Key Rotation",
       "ScheduleType": "Monthly",
       "RepeatInterval": 3,
       "TimeOfDay": "04:00"
     }
   - Assign to the profile

## Assign Profile to Accounts
5. Update account to use the SSH key profile:
   - PUT /v4/AssetAccounts/{accountId} with:
     {
       "SshKeyProfileId": <profileId>,
       "HasSshKey": true
     }

## Monitor SSH Key Health
6. Accounts with SSH key task failures:
   - GET /v4/AssetAccounts with query: filter=TaskProperties.HasSshKeyTaskFailure eq true&fields=Id,Name,Asset.Name,TaskProperties.LastFailureSshKeyTaskDate
7. Accounts never checked:
   - GET /v4/AssetAccounts with query: filter=TaskProperties.LastSuccessSshKeyCheckDate eq null AND HasSshKey eq true&fields=Id,Name,Asset.Name
8. Recent SSH key changes:
   - GET /v4/AssetAccounts with query: filter=HasSshKey eq true&orderby=-TaskProperties.LastSuccessSshKeyChangeDate&fields=Id,Name,Asset.Name,TaskProperties.LastSuccessSshKeyChangeDate&limit=20

## SSH Key Discovery
9. Discover SSH keys on managed assets:
   - POST /v4/AssetPartitions/{partitionId}/SshKeyDiscoverySchedules with schedule settings
   - Or trigger immediate discovery: POST /v4/AssetAccounts/{accountId}/DiscoverSshKeys
10. Review discovered keys:
    - GET /v4/DiscoveredSshKeys with query: fields=Id,Fingerprint,KeyType,Account.Name,Asset.Name

## SSH Key Sync Groups
11. Sync groups ensure the same key is deployed to multiple accounts:
    - POST /v4/AssetPartitions/{partitionId}/SshKeySyncGroups with body:
      {
        "Name": "Shared Service Key",
        "AccountIds": [<id1>, <id2>, <id3>]
      }
    - When the key rotates, all synced accounts get the new key

## A2A SSH Key Retrieval
- SSH keys can also be retrieved via A2A (like passwords)
- A2A event listeners fire when SSH keys are rotated

Related:
- Use password-rotation-status for parallel password management health.
- Use task-triage to investigate SSH key task failures.
- Use a2a-credential-retrieval for automated SSH key retrieval by applications.

Source: Official Safeguard administration documentation — Asset Management, Profiles, SSH Key Profiles.
