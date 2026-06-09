---
id: recent-activity-decision-tree
name: Pick the Right Endpoint for Recent-Activity Questions
description: Decision tree for choosing between /v4/Users.LastLoginDate, /v4/AuditLog/Logins, and /v4/AuditLog/AccessRequests/Activities when answering "who logged in?" / "what did this user do?" / "what happened on this appliance?" style questions.
tags: activity, login, history, who logged in, last login, lastlogindate, lastrequestdate, recent activity, login history, logins
---

ID: recent-activity-decision-tree
Name: Pick the Right Endpoint for Recent-Activity Questions
Goal: Choose the cheapest endpoint that actually answers the recent-activity question being asked, instead of dumping the audit log and post-filtering.

Context: Safeguard exposes three overlapping surfaces for recent-activity questions. They are NOT interchangeable -- each one trades cost against fidelity differently:

- `/v4/Users` carries denormalized timestamp fields (`LastLoginDate`, `LastRequestDate`) that are cheap one-row lookups but give you a single timestamp, no detail.
- `/v4/AuditLog/Logins` is the authoritative login event stream (success and failure, every login type) -- pageable, filterable, and the right answer for any "who logged in" question that needs more than the last successful timestamp.
- `/v4/AuditLog/AccessRequests/Activities` is the access-request activity stream -- credential checkouts, session launches, approvals, etc. It is NOT a login log; do not use it to answer login questions.

Use this decision tree to land on the right call before you paginate.

Steps:

Decision tree --

1. "What is this user's last successful login timestamp?"
   - `GET /v4/Users/{id}?fields=Id,UserName,LastLoginDate`
   - One row, one timestamp. No pagination, no filter. This is the cheapest answer and the only correct answer when the question is just "are they still active?" with no need for the underlying events.

2. "When did this user last submit an access request?"
   - `GET /v4/Users/{id}?fields=Id,UserName,LastRequestDate`
   - Same shape as above. Use this when the asker actually means "are they still using Safeguard for credential access?" rather than "are they logging in at all?".

3. "I want every login (success AND failure) for this user over a time window."
   - `GET /v4/AuditLog/Logins?filter=UserId eq {id} and LogTime ge '2026-01-01T00:00:00Z'&orderby=-LogTime&fields=Id,UserId,UserName,LogTime,LoginType,Success`
   - The `userId` query parameter is also supported as a shortcut: `?userId={id}&startDate=...`.
   - This is the right call for forensic questions: failed-password counts, login source IPs, MFA fallbacks, etc.

4. "I want every login on this appliance over a time window, regardless of user."
   - `GET /v4/AuditLog/Logins?startDate=2026-06-01T00:00:00Z&orderby=-LogTime&fields=Id,UserId,UserName,LogTime,LoginType,Success&limit=200`
   - Page with `page=2`, `page=3`, ... until empty. Pair with the `summarize-audit-log` recipe for tallying by user, by hour, by success/failure.

5. "I want this user's access-request activity (credential checkouts, session launches, approvals, etc.)."
   - `GET /v4/AuditLog/AccessRequests/Activities?userId={id}&orderby=-LogTime&fields=Id,UserId,UserName,LogTime,ActivityType,Action`
   - This is the right call when the question is "what did they DO once they were logged in?" -- not "did they log in?". Pair with `user-audit` when you also need their permissions in the same pass.

6. "I want everything that happened around this user -- logins, requests, sessions -- in one call."
   - Use `GET /v4/AuditLog/Search?userId={id}&startDate=...&orderby=-LogTime` -- the cross-category search endpoint. Heavier than the targeted endpoints above; use it only when you genuinely need the union.

What about LastActivityDate?

- `/v4/Users` does NOT expose a `LastActivityDate` field on this appliance major version. The two timestamps you get for free on Users are `LastLoginDate` and `LastRequestDate`. Use `LastRequestDate` as the proxy for "have they done anything via Safeguard recently?", or fall back to `/v4/AuditLog/Logins` when you need an actual activity boundary instead of the request-submission timestamp.

Common mistakes to avoid:

- Filtering `/v4/AuditLog/AccessRequests/Activities` to answer "who logged in" -- access-request activities are NOT login events; you will undercount by everyone who logged in but never opened a request.
- Paging `/v4/AuditLog/Logins` without `orderby=-LogTime` -- the default ordering is not chronological and you will read pages out of order.
- Using `LastLoginDate` to detect failed logins -- it only tracks the last SUCCESSFUL login.

Related:
- Use `user-audit` to pull a user's permissions and activity together in one workflow.
- Use `summarize-audit-log` (from the audit-summary recipe) to tally large `/v4/AuditLog/Logins` result sets without dragging every row into context.
- Use `access-request-audit` for deeper questions about specific access requests.

Source: Live appliance schema (`GET /v4/Users` response model) and `/v4/AuditLog/Logins` / `/v4/AuditLog/AccessRequests/Activities` discovery output.
