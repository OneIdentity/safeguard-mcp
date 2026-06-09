---
id: session-access-request
name: Session Access Request (RDP/SSH)
description: Request and launch a privileged session (RDP or SSH) through Safeguard's proxy.
tags: session, access request, RDP, SSH, proxy, recording, privileged session, launch, mstsc, rdp launch, request
tool: Safeguard_OpenAccessRequest
---

ID: session-access-request
Name: Session Access Request (RDP/SSH)
Goal: Request a privileged session (RDP or SSH) that is proxied and recorded by Safeguard.

Context: Session requests are distinct from password checkouts. The user never sees the credential — Safeguard proxies the connection, injects credentials, and records all activity. Sessions auto-close after 10 minutes of inactivity.

Preferred path: Use the composite tool Safeguard_OpenAccessRequest. It pre-validates the (account, asset, type) combination against /v4/Me/RequestEntitlements before submitting, which catches the most common failure mode — appliance error 90408 "not authorized to use this request type" — that actually means "your entitlement for this type is scoped to a different asset". The raw multi-step flow below is the same operation broken into individual calls for diagnostics.

Typical workflow: Discover entitlements → Request → Approve → Available → Launch → Check-in

Steps:
1. Find what the current user can request (account+asset+type combinations):
   - GET /v4/Me/RequestEntitlements
   - Useful query: accessRequestType=RemoteDesktop&fields=Account.Id,Account.Name,Asset.Id,Asset.Name,Policy.Name
   - Each row carries the Account, Asset, and Policy you must combine; AccountId+AssetId from the same row is the only combination the appliance will accept for that policy.
   - Note: /v4/Me/AccessRequestAssets does NOT expose an AccessRequestTypes field; resolve types via the entitlement's Policy.AccessRequestProperties.AccessRequestType.
2. Create a session access request:
   - POST /v4/AccessRequests with body:
     {
       "AccountId": <accountId>,
       "AssetId": <assetId>,
       "AccessRequestType": "RemoteDesktop",
       "ReasonCodeId": null,
       "ReasonComment": "Routine maintenance",
       "RequestedDurationDays": 0,
       "RequestedDurationHours": 2,
       "RequestedDurationMinutes": 0,
       "IsEmergency": false
     }
   - AccessRequestType values: "Password", "RemoteDesktop", "Ssh", "Telnet", "SshKey",
     "RemoteDesktopApplication", "ApiKey", "File". Verified by Safeguard_Enum name="AccessRequestType".
     The API rejects shorthand like "RDP"/"SSH" and is case-sensitive — use the exact spelling.
3. Check request status:
   - GET /v4/AccessRequests/{requestId} — watch the State field
   - Common states: Pending → Approved → RequestAvailable / Available → Expired / Complete
   - Auto-approval policies skip Pending and land in Available immediately.
4. Once Available, initialize the session:
   - POST /v4/AccessRequests/{requestId}/InitializeSession with body {}
   - Returns SessionsLaunchData. For each type:
     - RemoteDesktop / RemoteDesktopApplication: RdpConnectionFile is the literal .rdp file contents
       built by the appliance (gateway hostname, certificate, token, RDP display settings). Save it
       to disk and run mstsc against it. Do NOT hand-build a .rdp file — any server-side RDP setting
       change (gateway, display, certificate) would silently drift.
     - Ssh: SshConnectionString in the form
       vaultaddress=...@token=...@user@host:port@scbHost; ConnectionUri is the ssh:// SCALUS handler form.
     - Telnet: TelnetConnectionString and ConnectionUri.
5. Launch the session:
   - RDP: mstsc <saved.rdp>, or open the ConnectionUri with the SCALUS handler.
   - SSH/Telnet: use the connection string or ConnectionUri.
   - Credentials are injected by Safeguard — the user never sees the password.
6. Check in when done:
   - POST /v4/AccessRequests/{requestId}/CheckIn
   - Or the session expires automatically based on policy duration.

Common failures:
- Appliance error 90408 ("not authorized to use this request type") almost always means the
  (account, asset) pair you selected does not have a policy of the requested AccessRequestType.
  Re-query /v4/Me/RequestEntitlements with accountIds=<id>&accessRequestType=<type> to see which
  asset the entitlement is actually scoped to. Safeguard_OpenAccessRequest catches this before
  the POST.
- Policy may require ReasonCodeId (RequireReasonCode) or TicketNumber (RequireServiceTicket).
  Read RequesterProperties on the entitlement's Policy to see which are required.

Session Policies (for administrators setting up):
- AccessRequestType on the access policy must be RemoteDesktop, Ssh, Telnet, or RemoteDesktopApplication
- SessionProperties on the policy controls recording, command restrictions
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

Source: PangaeaAppliance source — AccessRequestsController, MeController_Requests, SessionsLaunchData.
