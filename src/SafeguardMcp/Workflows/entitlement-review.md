---
id: entitlement-review
name: Review Who Can Access What
description: Audit entitlements and policies to see who can request which accounts.
tags: entitlement, role, policy, access, review, authorization, audit
---

ID: entitlement-review
Name: Review Who Can Access What
Goal: Audit entitlements (roles) to see who has access to what accounts.

Note: In the Safeguard API, "Entitlements" are exposed as /v4/Roles.
Safeguard guidance: An entitlement is a set of access request policies that restrict system access to authorized users. An administrator creates an entitlement, then creates one or more access request policies, and finally adds users or user groups.

Steps:
1. List all entitlements (roles):
   - GET /v4/Roles with query: fields=Id,Name,Description,MemberCount,PolicyCount
2. For a specific entitlement, get members (who can request):
   - GET /v4/Roles/{roleId}/Members with query: fields=Id,Name,UserName,IdentityProviderName
3. Get the access policies (what they can request):
   - GET /v4/Roles/{roleId}/Policies with query: fields=Id,Name,AccessRequestProperties,ScopeItems
4. For each policy, get the scope (which accounts or assets):
   - GET /v4/AccessPolicies/{policyId}/ScopeItems
5. Entitlement reports (summary view):
   - GET /v4/Reports/Entitlements/UserEntitlements with query: userIds=<id>&fields=Id,UserName,AccountName,AssetName,EntitlementName

Object relationships:
- Entitlement (Role) -> has Members (users/groups) and Policies
- AccessPolicy -> has ScopeItems (accounts, assets, groups), Approvers, and Reviewers
- A policy can only belong to one entitlement

Related:
- Use create-entitlement to build a new role and policy.
- Use user-audit to inspect one user's roles and recent activity.

Source: Official Safeguard admin documentation.
