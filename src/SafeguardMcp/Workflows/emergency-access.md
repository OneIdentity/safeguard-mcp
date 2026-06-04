---
id: emergency-access
name: Emergency/Breakglass Access
description: Request emergency access to bypass approval workflows in critical situations.
tags: emergency, breakglass, override, approval bypass, critical access, incident
---

ID: emergency-access
Name: Emergency/Breakglass Access
Goal: Immediately obtain access to privileged credentials or sessions by bypassing normal approval workflows.

Context: Emergency Access overrides approver requirements — the request is immediately approved when constraints (time, requester eligibility) are met. It is audited, triggers notifications, and should only be used in genuine emergencies. Multiple users can request emergency access simultaneously for the same account.

Prerequisites:
- The access policy governing the account must have "Allow Emergency Access" enabled
- The requester must be a member of an entitlement that covers the target account
- Time restrictions may still apply unless "Ignore Time Restrictions" is also enabled on the policy

Steps:
1. Identify the target account:
   - GET /v4/Me/RequestableAccounts with query: filter=AllowPasswordRequests eq true OR AllowSessionRequests eq true&fields=Id,Name,Asset.Name
2. Create an emergency access request:
   - POST /v4/AccessRequests with body:
     {
       "AccountId": <accountId>,
       "AccessRequestType": "Password",
       "IsEmergency": true,
       "ReasonComment": "Production system down — incident INC-9876"
     }
   - The request is immediately approved (no approver action needed)
   - Escalation notifications are sent to configured contacts
3. Retrieve the credential:
   - POST /v4/AccessRequests/{requestId}/CheckOutPassword
   - Or for sessions: GET /v4/AccessRequests/{requestId}/SessionConnectionInfo
4. Perform the emergency work
5. Check in as soon as possible:
   - POST /v4/AccessRequests/{requestId}/CheckIn
   - Document what was done in the reason/comment for audit

For Administrators — Enabling Emergency Access on a Policy:
- PUT /v4/AccessPolicies/{policyId} with body including:
  {
    "AccessRequestProperties": {
      "AllowEmergencyAccess": true,
      "IgnoreTimeRestrictionsForEmergency": true
    },
    "EmergencyNotificationContacts": [
      {"Email": "security-team@company.com"}
    ]
  }

Audit Trail:
- Emergency requests are flagged in audit logs: GET /v4/AuditLog/AccessRequests/Activities with filter=IsEmergency eq true
- All emergency access is visible in compliance reports

Offline Workflow Mode (Cluster Lost Consensus):
- If the appliance loses cluster consensus, it can enter Offline Workflow Mode
- Emergency access and time-based constraints still apply in Offline Workflow Mode
- A2A operations are briefly suspended during mode transitions

Related:
- Use password-access-request for normal (non-emergency) password checkout.
- Use session-access-request for proxied session access.
- Use health-check and cluster-operations if the emergency relates to appliance state.

Source: Official Safeguard administration documentation — Access Request Policies, Emergency Access, Offline Workflow Mode.
