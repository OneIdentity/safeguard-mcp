---
id: password-rotation-status
name: Check Password Rotation Health
description: Review password management health across managed accounts.
tags: password, rotation, status, health, managed accounts, profile
---

ID: password-rotation-status
Name: Check Password Rotation Health
Goal: Overview of password management health across all managed accounts.

Steps:
1. Count accounts with failures:
   - GET /v4/AssetAccounts with query: filter=TaskProperties.HasAccountTaskFailure eq true&count=true
2. Get accounts never checked:
   - GET /v4/AssetAccounts with query: filter=TaskProperties.LastSuccessPasswordCheckDate eq null&fields=Id,Name,Asset.Name&limit=50
3. Get accounts with old passwords (not changed in 90+ days):
   - GET /v4/AssetAccounts with query: filter=TaskProperties.LastSuccessPasswordChangeDate lt '2024-01-01'&fields=Id,Name,Asset.Name,TaskProperties.LastSuccessPasswordChangeDate&limit=50
   - Adjust the comparison date as needed
4. Check scheduled task status:
   - GET /v4/AssetPartitions/{partitionId}/Profiles/{profileId} -> inspect CheckSchedule and ChangeSchedule
5. Summary by partition:
   - GET /v4/AssetPartitions with query: fields=Id,Name,ManagedAccountCount

Related:
- Use task-triage for detailed failure investigation.
- Use health-check for a broader deployment health view.

Source: Official Safeguard admin and troubleshooting documentation.
