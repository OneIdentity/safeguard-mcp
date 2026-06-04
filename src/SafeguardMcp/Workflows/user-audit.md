---
id: user-audit
name: Audit User Permissions and Activity
description: Review a user's admin roles, entitlements, and recent activity.
tags: user, audit, permissions, roles, activity, entitlement
---

ID: user-audit
Name: Audit User Permissions and Activity
Goal: Review what permissions a user has and their recent activity.

Steps:
1. Find the user:
   - GET /v4/Users with query: filter=UserName ieq 'targetuser'&fields=Id,Name,UserName,AdminRoles,IdentityProviderName
2. Check admin roles (built-in permissions):
   - Review AdminRoles in the response: Authorizer, UserAdmin, HelpdeskAdmin, AssetAdmin, ApplianceAdmin, PolicyAdmin, Auditor, OperationsAdmin
3. Check entitlement membership:
   - GET /v4/Users/{userId}/Roles with query: fields=Id,Name
4. Get the user's recent activity:
   - GET /v4/AuditLog/AccessRequests/Activities with query: filter=UserId eq {userId}&orderby=-LogTime&limit=25
5. Check what they can currently access:
   - GET /v4/Me/RequestEntitlements (only works for the authenticated user)
   - For other users, trace Roles -> Policies -> ScopeItems

Related:
- Use entitlement-review to trace the policies behind access.
- Use access-request-audit to review broader request history.

Source: Official Safeguard admin documentation.
