---
id: count-with-filter
name: Count Rows with count=true (Optional Filter)
description: Use ?count=true on any collection endpoint to ask the appliance "how many?" without pulling rows.
tags: count, how many, tally, count=true, filter, scoped count, summarize
---

ID: count-with-filter
Name: Count Rows with count=true (Optional Filter)
Goal: Answer "how many <thing>?" against any collection endpoint without
paging through rows. Combine with `filter=` for scoped counts (per partition,
per requester, per time window, per failure state, etc.).

When to use this:
- "How many assets are in partition X?"
- "How many failed logins happened this week?"
- "How many accounts need a password change?"
- Any single-number summary where row data is not needed.

How it works (verbatim appliance behavior):
- Add `count=true` to the query string on any collection endpoint.
- The appliance returns a bare JSON integer as the entire response body --
  not an object, not an array, not `{ Count: N }`. Just a number like `52`.
- Combine freely with `filter=` (and any other query parameter): the count
  reflects rows that would have been returned, after the filter is applied.
- See `safeguard://query-syntax` (Pagination section) for the canonical
  description of the parameter.

Verbatim response shapes captured against a live appliance:

```
GET /v4/Assets?count=true
52

GET /v4/AssetAccounts?count=true
53

GET /v4/AuditLog/Logins?count=true
63

GET /v4/Assets?filter=AssetPartitionId eq 1&count=true
0

GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-01' and LogTime lt '2026-06-09'&count=true
4
```

Parsing target: read the response body as a JSON number (or, equivalently, as
text and parse to int). Do NOT look for a `Count` field; there isn't one. The
shape is the same across `/v4/Assets`, `/v4/AssetAccounts`, and
`/v4/AuditLog/Logins`, and we have not seen it differ on any other collection
endpoint -- if a future endpoint returns something else, treat that as a bug
to report, not as a shape to handle.

Steps:
1. Pick the collection endpoint you would have called for the rows
   (`/v4/Assets`, `/v4/AssetAccounts`, `/v4/AuditLog/Logins`,
   `/v4/AccessRequests`, etc.).
2. Compose the filter you would have used for the row query.
3. Append `count=true` to the query string.
4. Issue a single GET. Parse the response body as a JSON integer.

Worked examples:

- "How many assets in partition 1?"
  - `GET /v4/Assets?filter=AssetPartitionId eq 1&count=true`
- "How many accounts need a password change?"
  - `GET /v4/AssetAccounts?filter=TaskProperties.HasAccountTaskFailure eq true&count=true`
- "How many failed logins this week?"
  - `GET /v4/AuditLog/Logins?filter=LogTime ge '2026-06-02' and LogTime lt '2026-06-09' and LoginRequestStatus ne 'Success'&count=true`

Limits:
- `count=true` only answers "how many"; it does NOT return distinct values,
  per-group counts, or any other aggregate. For "group by" questions, see
  the `summarize-audit-log` recipe (chunked paging + local tally) or the
  `time-bucketed-counts` recipe (N count calls, one per bucket).
- No server-side `groupBy=`, `distinct=`, or `aggregate=` parameter exists.
  Do not try to invent one.

Related:
- `summarize-audit-log` -- group-by patterns via chunked paging.
- `time-bucketed-counts` -- per-bucket counts for trend questions.
- `safeguard://query-syntax` -- canonical description of `count=true` and
  filter syntax.
