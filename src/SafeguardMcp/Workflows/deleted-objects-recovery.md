---
id: deleted-objects-recovery
name: Find and Restore Deleted Accounts, Assets, or Users
description: List soft-deleted Safeguard objects, review the audit-log history of their removal, and restore or purge them.
tags: deleted, removed, archived, trashed, restore, recover, purge, audit history, deletedaccounts, deletedassets, auditlog/objectchanges
---

ID: deleted-objects-recovery
Name: Find and Restore Deleted Accounts, Assets, or Users
Goal: Locate soft-deleted Safeguard objects, see who deleted them and when, and either restore or purge them.

Context: When an Asset Administrator deletes an asset, account, or user via the standard DELETE endpoints, Safeguard moves the entity to the recycle-bin endpoints under `/v4/Deleted/*` rather than purging it immediately. The retention window is governed by `/v4/Deleted/PurgeSettings`. Restoration recreates the entity with its original Id; purge is irreversible.

Required permissions: Asset Administrator (assets/accounts) or User Administrator (users).

Steps:

1. List soft-deleted entities by type:
   - `GET /v4/Deleted` — enumerate the delete-type buckets available.
   - `GET /v4/Deleted/AssetAccounts` — deleted managed accounts.
   - `GET /v4/Deleted/Assets` — deleted assets.
   - `GET /v4/Deleted/Users` — deleted users.
   - Each result carries the original `Id`, `Name`, `DeletedDate`, and `DeletedByUserDisplayName`.

2. (Optional) Review the audit-log change history for context:
   - `GET /v4/AuditLog/ObjectChanges/AssetAccount/{id}?startDate=...&endDate=...`
   - `GET /v4/AuditLog/ObjectChanges/Asset/{id}?startDate=...&endDate=...`
   - `GET /v4/AuditLog/ObjectChanges/User/{id}?startDate=...&endDate=...`
   - The `Delete` action will appear with the actor and timestamp.

3. Restore the entity (recreates it in-place):
   - `POST /v4/Deleted/AssetAccounts/{id}/Restore`
   - `POST /v4/Deleted/Assets/{id}/Restore`
   - `POST /v4/Deleted/Users/{id}/Restore`

4. Or purge it permanently (irreversible — bypass the retention window):
   - `DELETE /v4/Deleted/AssetAccounts/{id}`
   - `DELETE /v4/Deleted/Assets/{id}`
   - `DELETE /v4/Deleted/Users/{id}`

5. Review or adjust the retention policy:
   - `GET /v4/Deleted/PurgeSettings`
   - `PUT /v4/Deleted/PurgeSettings`

Common pitfall: an account whose parent asset has been deleted must be restored only after the asset is restored, or the operation fails referential validation.
