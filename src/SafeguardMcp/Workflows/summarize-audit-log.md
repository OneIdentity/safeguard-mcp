---
id: summarize-audit-log
name: Summarize Audit Log by Group (Chunked Paging + Local Tally)
description: Answer "group by" / "distinct values" questions over the audit log by paging filtered rows and tallying in agent context.
tags: summarize, group by, distinct, audit log, who logged in, who used, tally, count
---

ID: summarize-audit-log
Name: Summarize Audit Log by Group (Chunked Paging + Local Tally)
Goal: Answer "who/what is in this audit slice and how often" -- questions
like "who logged in this week", "which platforms saw password changes
today", "which requesters opened access requests against asset X" -- when
the appliance offers no server-side grouping.

Why this exists:
- Safeguard has NO `groupBy=`, `distinct=`, or `aggregate=` query parameter.
  See `safeguard://query-syntax` and the IN3 note in our internal facts:
  audit endpoints push rows straight from the database with no aggregation
  surface.
- `count=true` (see `count-with-filter`) answers "how many" but not
  "how many of each". The only way to get per-group counts is to pull the
  rows and tally in agent context.

When this works well:
- The filter narrows the slice to a manageable page count (a few pages, not
  hundreds). "This week", "today", "for requester X", "for asset Y" are
  good narrowing dimensions.
- The grouping field is small (a requester name, a platform, a status) so
  the per-row payload stays cheap to project with `fields=`.

When to STOP and narrow further instead:
- If a quick `count=true` on the same filter returns thousands of rows,
  paging them all to tally is wasteful. Tighten the time window or add
  another filter clause before proceeding.
- There is no `Safeguard_Summarize` tool today and the appliance will not
  grow one without explicit operator demand -- the honest fallback is
  filter-narrowing, not a bigger page.

Steps:
1. Decide the time range (or other narrowing dimension). Express it as a
   filter:
   - `filter=LogTime ge '2026-06-02' and LogTime lt '2026-06-09'`
2. (Optional but recommended) Run a sanity-check count first:
   - Same filter, `&count=true`. If the count is small enough to page
     comfortably, continue. If not, narrow the filter.
3. Choose the projection with `fields=`:
   - Include the grouping field and any disambiguators you need
     (e.g. `fields=UserName,UserId,ClientIpAddress,LogTime`).
   - On `/v4/AuditLog/ObjectChanges` you do NOT need to pass `fields=`
     explicitly just to drop the heavy `OldValue` / `NewValue` snapshots --
     the list-style ObjectChanges routes already default-project them out
     (see the ObjectChanges default-projection note in
     `Safeguard_QueryHelp`). Only override `fields=` if you want a
     narrower or wider projection than that default.
4. Page through the rows:
   - `page=0&limit=200`, then `page=1&limit=200`, etc., until a page comes
     back short (or empty). Keep `filter=` and `fields=` identical across
     pages.
5. Tally the grouping field in agent context:
   - Build a dictionary keyed by the grouping field value; increment per
     row. Report top-N or the full distribution as the operator asked.

Worked example -- "who used this appliance this week":
1. Time range: this week so far.
   - `filter=LogTime ge '2026-06-02' and LogTime lt '2026-06-09'`
2. Sanity-check the volume:
   - `GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-02' and LogTime lt '2026-06-09'&count=true`
   - Response: a bare integer (e.g. `4`). If that is in the tens or low
     hundreds, paging is fine.
3. Project just what we need to identify the user:
   - `fields=UserName,UserDisplayName,UserId,ClientIpAddress,LogTime,LoginRequestStatus`
4. Page until short:
   - `GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-02' and LogTime lt '2026-06-09'&fields=UserName,UserDisplayName,UserId,ClientIpAddress,LogTime,LoginRequestStatus&page=0&limit=200`
   - Increment page until a page returns fewer than `limit` rows.
5. Tally `UserName` (or `UserId`, for collision-safe grouping). Report the
   distinct user list and per-user login count.

Variations the same pattern handles:
- "Which platforms saw password changes today" -- swap endpoint to
  `/v4/AuditLog/Passwords/Activities`, group by `Platform.DisplayName`
  (or `PlatformDisplayName` field, depending on payload), filter
  `EventName eq 'PasswordChangeSucceeded'`.
- "Which requesters opened access requests against asset X" -- endpoint
  `/v4/AuditLog/AccessRequests/Activities`, filter by `AssetId` and a
  time range, group by `RequesterName`.

Related:
- `count-with-filter` -- one-call "how many" without paging.
- `time-bucketed-counts` -- trend questions ("per day", "per hour") via
  N count calls instead of paging rows.
- `safeguard://query-syntax` -- `filter=`, `fields=`, `page=` / `limit=`
  reference, including the ObjectChanges default-projection note.

Source: Live appliance behavior; no server-side aggregation surface exists.
