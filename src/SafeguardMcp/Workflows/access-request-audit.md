---
id: access-request-audit
name: Review Access Request Activity
description: Audit recent access request activity for compliance review.
tags: access request, audit, approval, checkout, session, compliance
---

ID: access-request-audit
Name: Review Access Request Activity
Goal: Audit recent access request activity for compliance review.

Safeguard workflow context: Access requests flow through states Pending -> Approved -> Available -> Expired/Revoked. Requesters check out passwords or sessions, approvers approve or deny, reviewers audit completed requests.

Steps:
1. Get recent access requests:
   - GET /v4/AccessRequests with query: orderby=-CreatedDate&limit=50&fields=Id,AccessRequestType,State,RequesterName,AccountName,AssetName,CreatedDate,ApprovedDate,ExpiresAfter
2. Filter by state if needed:
   - Pending: filter=State eq 'Pending'
   - Approved/Active: filter=State eq 'Available'
   - Completed: filter=State eq 'Expired' or State eq 'CheckedIn'
3. Get details of a specific request:
   - GET /v4/AccessRequests/{requestId}
4. Check who approved:
   - GET /v4/AccessRequests/{requestId}/Approvals
5. For session requests, check if recorded:
   - Look for SessionId in the request details

Access request states:
- Pending: Waiting for approval
- Approved: Approved but checkout time has not arrived
- Available: Ready for the requester (can view password or launch session)
- Expired: Checkout duration elapsed
- Denied: Denied by approver
- Revoked: Retracted by approver after approval

Related:
- Use user-audit to inspect a specific user's recent request activity.
- Use entitlement-review to understand why a user could request an account.

Source: Official Safeguard admin documentation.
