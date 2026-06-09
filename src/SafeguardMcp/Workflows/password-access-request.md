---
id: password-access-request
name: Password Access Request (Checkout)
description: Request and check out a privileged password through the access request workflow.
tags: password, access request, checkout, check-in, credential, workflow, entitlement, approve, release, secret
---

ID: password-access-request
Name: Password Access Request (Checkout)
Goal: Request and retrieve a privileged account password through the standard access request workflow.

Context: Password checkout gives the requester temporary access to view or copy a credential. The workflow enforces approval, time limits, and audit. After check-in, the password is optionally rotated.

Typical workflow: Discover Entitlements → Find Account → Request → Approve → Available → View Password → Check-in → (Auto-rotate)

Steps:
1. Discover what the current user is entitled to request:
   - GET /v4/Me/RequestEntitlements with query: fields=Account.Id,Account.Name,Asset.Name,Asset.NetworkAddress,Policy.Name,Policy.AccessRequestProperties
   - Each result is a (Account, Asset, Policy) tuple showing one requestable combination
   - Key fields: Account.Name, Asset.NetworkAddress (to identify the target), Policy.AccessRequestProperties.AccessRequestType (Password, Ssh, etc.)
   - Use Account.AllowPasswordRequest eq true filter to narrow to password-eligible accounts
2. Find requestable accounts for the current user:
   - GET /v4/Me/RequestableAccounts with query: fields=Id,Name,Asset.Name,Asset.NetworkAddress,AllowPasswordRequests&filter=AllowPasswordRequests eq true
   - Alternative: GET /v4/Me/RequestableAccounts?q=<search> for quick search
3. Create a password access request:
   - POST /v4/AccessRequests with body:
     {
       "AccountId": <accountId>,
       "AccessRequestType": "Password",
       "ReasonComment": "Server maintenance ticket #12345",
       "RequestedDurationDays": 0,
       "RequestedDurationHours": 4,
       "IsEmergency": false
     }
4. Check request status:
   - GET /v4/AccessRequests/{requestId} -> State field
   - If Pending: waiting for approver action
   - If Available: ready for password retrieval
5. Retrieve the password (once Available):
   - POST /v4/AccessRequests/{requestId}/CheckOutPassword
   - Returns the password in plain text
   - Or for API keys: POST /v4/AccessRequests/{requestId}/CheckOutApiKey
6. Check in when done:
   - POST /v4/AccessRequests/{requestId}/CheckIn
   - If policy has "Change password after check-in" enabled, rotation occurs automatically
7. Verify the request completed:
   - GET /v4/AccessRequests/{requestId} -> State should be "CheckedIn" or "Expired"

For Approvers:
- GET /v4/Me/ActionableRequests -> lists requests awaiting your approval
- POST /v4/AccessRequests/{requestId}/Approve with body: {"Comment": "Approved for maintenance window"}
- POST /v4/AccessRequests/{requestId}/Deny with body: {"Comment": "Reason for denial"}

Emergency Access:
- Set "IsEmergency": true to bypass approval requirements
- Only works if the policy has "Allow Emergency Access" enabled
- Triggers emergency notification to configured contacts
- See workflow: emergency-access for full details

Key Policy Settings (for administrators):
- MaximumDurationHours: caps how long checkout lasts
- AllowSimultaneousAccess: whether multiple users can check out same password
- ChangeShelvedPasswordAfterCheckin: auto-rotate after check-in

Related:
- Use session-access-request for proxied sessions (RDP/SSH) where you don't see the password.
- Use emergency-access for breakglass scenarios.
- Use access-request-audit to review completed requests.
- Use create-entitlement to set up password checkout policies.

Source: Official Safeguard user guide — Privileged access requests, Password release request workflow.
