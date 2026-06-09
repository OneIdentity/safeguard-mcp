---
id: time-bucketed-counts
name: Time-Bucketed Counts (One count=true Call per Bucket)
description: Answer trend questions ("per day", "per hour") by issuing N count=true calls, one per time bucket, and stitching the results into a series.
tags: trend, per day, per hour, time series, count by bucket, count, summarize, how many
---

ID: time-bucketed-counts
Name: Time-Bucketed Counts (One count=true Call per Bucket)
Goal: Build a "rows per time bucket" series for trend questions like
"logins per day this week" or "password changes per hour today", without
pulling any row data over the wire.

Why this exists:
- The appliance has no server-side aggregation, so there is no
  `groupBy=Day` parameter. See `count-with-filter` for the underlying
  `count=true` primitive and `summarize-audit-log` for the row-level
  group-by alternative.
- For purely temporal "how many in each bucket?" questions, paging rows
  to tally is wasteful when each bucket can be answered with a single
  `count=true` call. This recipe is the cheap path: N calls, one per
  bucket, each returning a bare integer.

When to use this:
- The grouping dimension is time (day, hour, minute, week) and only time.
- The number of buckets is small (tens, not thousands). Seven daily
  buckets for "this week" is fine; one-second buckets across a month is
  not.
- You want a series of counts, not the underlying rows.

Steps:
1. Pick the bucket size (day, hour, minute, week) and the bucket
   boundaries up front. Use a half-open `[start, end)` convention so
   adjacent buckets do not double-count rows at the boundary.
2. For each bucket, issue one GET with
   `filter=<TimeField> ge '<start>' and <TimeField> lt '<end>'&count=true`.
   Carry any non-time filter clauses (e.g.
   `EventName eq 'PasswordChangeSucceeded'`) into every bucket unchanged.
3. Parse each response body as a JSON integer (bare scalar -- see
   `count-with-filter` for the verbatim shape).
4. Stitch the per-bucket integers into a series labelled by bucket start.

Worked example -- "logins per day this week" against /v4/AuditLog/Logins:

```
GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-02' and LogTime lt '2026-06-03'&count=true
GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-03' and LogTime lt '2026-06-04'&count=true
GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-04' and LogTime lt '2026-06-05'&count=true
GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-05' and LogTime lt '2026-06-06'&count=true
GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-06' and LogTime lt '2026-06-07'&count=true
GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-07' and LogTime lt '2026-06-08'&count=true
GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-08' and LogTime lt '2026-06-09'&count=true
```

Each response body is a single integer. Stitch:

```
2026-06-02: 0
2026-06-03: 1
2026-06-04: 0
2026-06-05: 2
2026-06-06: 0
2026-06-07: 0
2026-06-08: 1
```

Worked example -- "password changes per hour today" against
`/v4/AuditLog/Passwords/Activities`: same pattern, swap bucket size to one
hour and the endpoint and time field appropriately
(`filter=LogTime ge '...' and LogTime lt '...' and EventName eq 'PasswordChangeSucceeded'&count=true`).

Notes:
- The two timestamps use the half-open `[start, end)` convention so
  adjacent buckets do not double-count rows at the boundary.
- All buckets share the same outer filter; only the time-window clause
  changes per call. The other filter clauses (e.g.
  `EventName eq 'PasswordChangeSucceeded'`) carry forward unchanged.
- If you also need a "by day AND by user" grouping, time bucketing alone
  is not enough -- combine with `summarize-audit-log` (page the day's
  rows once, then tally locally by user).

Related:
- `count-with-filter` -- the single-call primitive this recipe iterates.
- `summarize-audit-log` -- non-time grouping (page rows, tally locally).
- `safeguard://query-syntax` -- `filter=` time-range syntax and
  `count=true`.

Source: Composes the `count=true` primitive; no server-side aggregation
surface exists.
