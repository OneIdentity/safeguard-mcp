---
id: directory-integration
name: Directory Integration Setup (LDAP/AD)
description: Connect Active Directory or LDAP as an identity provider and import users/groups.
tags: directory, LDAP, active directory, identity provider, user import, synchronization, groups
---

ID: directory-integration
Name: Directory Integration Setup (LDAP/AD)
Goal: Integrate an Active Directory or LDAP directory as an identity and authentication provider, then import users and groups into Safeguard.

Context: Safeguard supports AD, LDAP 2.4, SAML 2.0, Radius, and Starling as identity/authentication providers. Directory integration enables user auto-import, group sync, and directory-based authentication. Requires Appliance Administrator role.

Steps:

## Add the Identity Provider
1. Create an Active Directory provider:
   - POST /v4/IdentityProviders with body:
     {
       "TypeReferenceName": "ActiveDirectory",
       "Name": "Corporate AD",
       "ConnectionProperties": {
         "DomainName": "corp.example.com",
         "ServiceAccountName": "svc-safeguard",
         "ServiceAccountPassword": "<password>",
         "UseSslEncryption": true
       }
     }
   - Or for LDAP:
     {
       "TypeReferenceName": "Ldap",
       "Name": "OpenLDAP",
       "ConnectionProperties": {
         "HostName": "ldap.example.com",
         "Port": 389,
         "ServiceAccountDistinguishedName": "cn=svc,dc=example,dc=com",
         "ServiceAccountPassword": "<password>",
         "UseSslEncryption": false,
         "BaseDn": "dc=example,dc=com"
       }
     }
2. Verify connectivity:
   - POST /v4/IdentityProviders/{providerId}/TestConnection
   - Should return success

## Configure Synchronization
3. Set sync intervals:
   - PUT /v4/IdentityProviders/{providerId} with:
     {
       "SyncAdditionsEveryMinutes": 60,
       "SyncDeletesEveryMinutes": 1440,
       "SyncChangesEveryMinutes": 60
     }
4. For AD forests with multiple domains:
   - Mark domains as "Available for Identity and Authentication"
   - GET /v4/IdentityProviders/{providerId}/Domains -> lists discovered domains
   - PUT /v4/IdentityProviders/{providerId}/Domains/{domainId} with {"IsAvailableForIdentityAndAuthentication": true}

## Import Directory User Groups
5. Add a directory user group (imports members automatically):
   - POST /v4/UserGroups with body:
     {
       "Name": "Domain Admins Import",
       "DirectoryProperties": {
         "IdentityProviderId": <providerId>,
         "GroupDistinguishedName": "CN=Domain Admins,CN=Users,DC=corp,DC=example,DC=com"
       }
     }
   - Members are imported based on group membership
   - Users with incomplete/invalid attributes are skipped (import continues)

## Import Individual Directory Users
6. Create a user from the directory:
   - POST /v4/Users with body:
     {
       "IdentityProviderId": <providerId>,
       "DirectoryProperties": {
         "DistinguishedName": "CN=John Smith,OU=IT,DC=corp,DC=example,DC=com"
       }
     }
   - Contact information is imported read-only from the directory

## Set Authentication Provider
7. Users imported from a directory default to that directory for authentication
   - Override per-user: PUT /v4/Users/{userId} with:
     {"PrimaryAuthenticationProviderId": <alternateProviderId>}
   - Can set to External Federation, Radius, or Certificate auth

## Network Requirements
- Port 3268 (LDAP Global Catalog) must be open for AD
- Port 389 (LDAP) or 636 (LDAPS) for standard LDAP
- DNS resolution from the appliance to domain controllers
- If using SSL: upload the directory server's CA to Trusted Certificates

## Force Synchronization
8. Trigger immediate sync:
   - POST /v4/IdentityProviders/{providerId}/Synchronize
   - Or for a specific group: POST /v4/UserGroups/{groupId}/SynchronizeAndUpdateProviders

Related:
- Use user-audit to verify imported users and their permissions.
- Use certificate-management if SSL is needed for directory connections.
- Use create-entitlement to grant imported users access to managed accounts.

Source: Official Safeguard administration documentation — Identity and Authentication, User Management, User Groups.
