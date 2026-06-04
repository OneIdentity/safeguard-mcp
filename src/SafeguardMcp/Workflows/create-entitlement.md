---
id: create-entitlement
name: Create an Entitlement with Policy
description: Set up a new entitlement (role) with an access policy for password checkout.
tags: create, entitlement, role, policy, password checkout, access request
---

ID: create-entitlement
Name: Create an Entitlement with Policy
Goal: Set up a new entitlement (role) with an access policy for password checkout.

Note: In the Safeguard API, "Entitlements" are exposed as /v4/Roles.
Safeguard guidance: Typically, you create entitlements for various job functions. Password and SSH key release entitlements consist of users, user groups, and access request policies.

Steps:
1. Create the entitlement (role):
   - POST /v4/Roles with body: {"Name": "Help Desk Admins", "Description": "Access for help desk team"}
2. Add members (users who can request access):
   - POST /v4/Roles/{roleId}/Members/Add with body: [{"Id": <userId>}, {"Id": <userId2>}]
   - Or add user groups for easier management
3. Create an access request policy:
   - POST /v4/AccessPolicies with body:
     {
       "Name": "HD Password Access",
       "RoleId": <roleId>,
       "AccessRequestProperties": {
         "AccessRequestType": "Password",
         "AllowSimultaneousAccess": false,
         "MaximumDurationDays": 0,
         "MaximumDurationHours": 8
       }
     }
4. Set scope (which accounts the policy covers):
   - POST /v4/AccessPolicies/{policyId}/ScopeItems/Add with body listing account or asset group IDs
5. Optionally add approvers:
   - POST /v4/AccessPolicies/{policyId}/ApproverSets with body listing approver user or group IDs
6. Verify the setup:
   - GET /v4/Roles/{roleId} -> check MemberCount and PolicyCount

Related:
- Use entitlement-review to confirm who can access what after setup.
- Use access-request-audit to review resulting request activity.

Source: Official Safeguard admin documentation.
