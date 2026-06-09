---
id: session-access-request
name: Session Access Request (RDP/SSH)
description: Request and launch a privileged session (RDP or SSH) through Safeguard's proxy.
tags: session, access request, RDP, SSH, proxy, recording, privileged session
---

ID: session-access-request
Name: Session Access Request (RDP/SSH)
Goal: Request a privileged session (RDP or SSH) that is proxied and recorded by Safeguard.

Context: Session requests are distinct from password checkouts. The user never sees the credential — Safeguard proxies the connection, injects credentials, and records all activity. Sessions auto-close after 10 minutes of inactivity.

Typical workflow: Request → Approve → Available → Launch → Check-in

Steps:
1. Find available session accounts (what can the current user request):
   - GET /v4/Me/RequestableAccounts with query: filter=AllowSessionRequests eq true&fields=Id,Name,Asset.Name,Asset.NetworkAddress,AllowSessionRequests
2. Create a session access request:
   - POST /v4/AccessRequests with body:
     {
       "AccountId": <accountId>,
       "AccessRequestType": "RemoteDesktop",
       "ReasonCode": null,
       "ReasonComment": "Routine maintenance",
       "RequestedDurationDays": 0,
       "RequestedDurationHours": 2,
       "IsEmergency": false
     }
   - AccessRequestType options: "RemoteDesktop" (RDP), "Ssh", "Telnet", "RemoteDesktopApplication"
   - Values verified by Safeguard_Enum name="AccessRequestType". Use the enum spelling exactly;
     the API rejects case-folded variants and shorthand like "RDP"/"SSH".
3. Check request status:
   - GET /v4/AccessRequests/{requestId} -> watch State field
   - States: Pending → Approved → Available → Expired/CheckedIn
4. Once Available, get connection info:
   - GET /v4/AccessRequests/{requestId}/SessionConnectionInfo
   - Returns: Hostname, Port, UserName, ConnectionString, Protocol
   - For RDP: provides .rdp file download or connection parameters
   - For SSH: provides hostname:port with injected credentials
5. Launch the session:
   - RDP: Use the connection string with mstsc or SCALUS-registered handler
   - SSH: Connect to the proxy address with PuTTY or SCALUS-registered handler
   - Credentials are injected by Safeguard — user never sees the password
6. Check in when done:
   - POST /v4/AccessRequests/{requestId}/CheckIn
   - Or the session expires automatically based on policy duration

Session Policies (for administrators setting up):
- AccessRequestType must be "RemoteDesktop" or "Ssh" in the access policy
- SessionProperties in the policy controls recording, command restrictions
- POST /v4/AccessPolicies with SessionProperties block

Key Differences from Password Requests:
- User never sees the credential
- All activity is recorded and auditable
- Session is proxied through Safeguard (not direct to target)
- Requires session module connectivity (SPS integration or built-in proxy)

Related:
- Use password-access-request for credential checkout (non-session).
- Use access-request-audit to review completed session recordings.
- Use create-entitlement to set up session-type policies.

Source: Official Safeguard user guide and administration documentation — Privileged access requests, Session request workflow.
