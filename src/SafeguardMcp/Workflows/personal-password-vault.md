---
id: personal-password-vault
name: Personal Password Vault Setup
description: Enable and manage the personal password vault for end users.
tags: personal password, vault, user passwords, share, permission, storage
---

ID: personal-password-vault
Name: Personal Password Vault Setup
Goal: Enable the personal password vault feature for users and manage stored credentials.

Context: The personal password vault lets users securely store up to 100 personal passwords (separate from managed account passwords). Users can set expiration dates, view change history, and share individual passwords with one other user. Requires the "Personal Passwords" permission.

Benefits:
- Organization-sanctioned secure storage (replaces sticky notes and spreadsheets)
- Encrypted and stored separately from managed account passwords
- Fully audited — admins know when passwords are retrieved or changed
- Admins can recover passwords when employees leave (by changing auth to local, setting a password, then logging in)
- There is NO way to recover the vault of a deleted user

Steps:

## Enable Personal Password Vault for Users
1. Grant permission to an individual user:
   - PUT /v4/Users/{userId} with body:
     {"Permissions": [...existing permissions..., "PersonalPassword"]}
2. Grant permission via a user group (recommended for scale):
   - PUT /v4/UserGroups/{groupId} with body:
     {"Permissions": [...existing permissions..., "PersonalPassword"]}
   - All members of the group inherit the permission
3. Verify a user has the permission:
   - GET /v4/Users/{userId} -> check Permissions array includes "PersonalPassword"

## User Operations (Personal Vault)
4. List personal passwords (as the user):
   - GET /v4/Me/PersonalPasswords with query: fields=Id,Name,Description,ExpirationDate,LastModifiedDate
5. Create a personal password:
   - POST /v4/Me/PersonalPasswords with body:
     {
       "Name": "AWS Console - Production",
       "Description": "Root account for prod AWS",
       "Password": "<the-password>",
       "ExpirationDate": "2025-03-01T00:00:00Z"
     }
6. Retrieve a stored password:
   - POST /v4/Me/PersonalPasswords/{passwordId}/Retrieve
   - Returns the password in plain text (audited)
7. Update a personal password:
   - PUT /v4/Me/PersonalPasswords/{passwordId} with updated fields
   - Previous versions are kept in history
8. View password change history:
   - GET /v4/Me/PersonalPasswords/{passwordId}/History
9. Delete a personal password:
   - DELETE /v4/Me/PersonalPasswords/{passwordId}

## Sharing Passwords
10. Share a password with one other user:
    - POST /v4/Me/PersonalPasswords/{passwordId}/Share with body:
      {"UserId": <otherUserId>}
    - Only ONE share per password is allowed
11. Revoke a share:
    - DELETE /v4/Me/PersonalPasswords/{passwordId}/Share
    - The other user can also opt out of the share

## Administrator Operations
12. Audit personal password vault access:
    - GET /v4/AuditLog/PersonalPasswords/Activities with query: orderby=-LogTime&limit=50
    - Shows who retrieved or changed personal passwords
13. Recover a departed user's passwords:
    - Change the user's authentication provider to Local
    - Set a password for the user account
    - Log in as that user and access the personal password vault
    - NOTE: If the user is deleted, their vault data is permanently lost

## Permission Revocation
- If the PersonalPassword permission is revoked (directly or by group removal):
  - User loses access to vault features immediately
  - Vault data is RETAINED (not deleted)
  - If permission is re-granted later, all data is accessible again

Related:
- Use user-audit to review specific user activity including vault access.
- Use directory-integration to manage user groups and permissions at scale.

Source: Official Safeguard administration documentation — Vaults, Personal password vault.
