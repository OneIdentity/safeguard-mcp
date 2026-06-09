using System.Collections.Generic;

namespace SafeguardMcp.IntegrationTests.EvalSuite;

/// <summary>
/// The full set of replayable error traces. New cases are appended as new
/// regressions are collected. As each owning capability area ships, flip
/// the corresponding case from <see cref="ReproMode.Placeholder"/> to
/// <see cref="ReproMode.ExpectGuidance"/> (or <see cref="ReproMode.ExpectSuccess"/>)
/// and populate <c>ExpectedGuidance</c> with the substrings the new
/// diagnostic should contain.
/// </summary>
internal static class EvalCases
{
    public static IReadOnlyList<EvalCase> All { get; } = new[]
    {
        // -- Nested-property family (flattened guess rejected) ----------------
        new EvalCase {
            Id = "E001",
            Description = "AssetAccounts: filter/orderby/fields used flattened AssetId instead of Asset.Id.",
            OwningArea = "did-you-mean-suggester, schema-property-paths",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AssetAccounts",
            Query = "filter=AssetId ge 0 and AssetId le 999&orderby=AssetName&fields=Id,Name,AssetId",
            PlaceholderNote = "Tighten to ExpectGuidance with [\"Asset.Id\"] when the did-you-mean suggester surfaces it.",
        },
        new EvalCase {
            Id = "E002",
            Description = "AuditLog/Logins: rejected UserDisplayName/UserName (nested under UserProperties).",
            OwningArea = "did-you-mean-suggester, schema-property-paths",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AuditLog/Logins",
            Query = "fields=UserDisplayName,UserName,LoggedInProviderName,ClientIpAddress",
            PlaceholderNote = "Tighten to ExpectGuidance with [\"UserProperties\"] when the schema property-path graph and the did-you-mean suggester are in place.",
        },
        new EvalCase {
            Id = "E013",
            Description = "/v4/Me: UserName rejected (correct: Name).",
            OwningArea = "did-you-mean-suggester, schema-property-paths",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/Me",
            Query = "fields=Id,UserName,DisplayName",
            PlaceholderNote = "Tighten to ExpectGuidance with [\"Name\"] when the did-you-mean suggester surfaces it.",
        },
        new EvalCase {
            Id = "E015",
            Description = "Recurrence of E001 across sessions: AssetId guessed again on AssetAccounts.",
            OwningArea = "did-you-mean-suggester, schema-property-paths",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AssetAccounts",
            Query = "filter=AssetId eq 1",
            PlaceholderNote = "Same fix as E001; passes when the did-you-mean suggester returns 'did you mean Asset.Id'.",
        },
        new EvalCase {
            Id = "E027",
            Description = "/v4/Users: DomainName, IdentityProviderName, LastSuccessLoginDate rejected.",
            OwningArea = "did-you-mean-suggester, schema-property-paths",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/Users",
            Query = "fields=Id,Name,DomainName,IdentityProviderName,LastSuccessLoginDate",
            PlaceholderNote = "Tighten with correct path hints (IdentityProvider.Name etc.) when the schema property-path graph and the did-you-mean suggester are in place.",
        },
        new EvalCase {
            Id = "E030",
            Description = "/v4/AuditLog/Search: EventDescription rejected.",
            OwningArea = "did-you-mean-suggester, schema-property-paths",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AuditLog/Search",
            Query = "fields=Id,LogTime,EventDescription",
            PlaceholderNote = "Tighten with correct property name when the schema property-path graph and the did-you-mean suggester are in place.",
        },
        new EvalCase {
            Id = "E033",
            Description = "/v4/Me/RequestEntitlements: Account.Asset.Name rejected (correct: Account.AssetName).",
            OwningArea = "schema-property-paths",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/Me/RequestEntitlements",
            Query = "fields=Account.Asset.Name",
            PlaceholderNote = "Tighten to ExpectGuidance with [\"Account.AssetName\"] when the schema property-path graph surfaces the correct path.",
        },
        new EvalCase {
            Id = "E034",
            Description = "/v4/AccessPolicies: SessionProperties.SessionType rejected.",
            OwningArea = "schema-property-paths, did-you-mean-suggester",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AccessPolicies",
            Query = "fields=Id,Name,SessionProperties.SessionType",
            PlaceholderNote = "Tighten with correct nested path when the schema property-path graph enumerates valid paths.",
        },
        new EvalCase {
            Id = "E037",
            Description = "/v4/Me/AccessRequestAssets/{id}: AccessRequestTypes rejected.",
            OwningArea = "schema-property-paths, did-you-mean-suggester",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/Me/AccessRequestAssets/0",
            Query = "fields=AccessRequestTypes",
            PlaceholderNote = "Tighten with correct field name when the schema property-path graph surfaces it.",
        },

        // -- Enum vocabularies -------------------------------------------------
        new EvalCase {
            Id = "E004",
            Description = "AuditLog/ObjectChanges: EventName eq 'Delete' rejected (correct: 'AssetDeleted').",
            OwningArea = "schema-enum-vocab",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AuditLog/ObjectChanges/Asset",
            Query = "filter=EventName eq 'Delete'",
            PlaceholderNote = "Tighten to ExpectGuidance with [\"AssetDeleted\"] when the schema surfaces enum vocab.",
        },
        new EvalCase {
            Id = "E016",
            Description = "ScheduleType: 'Hour'/'Day' rejected; correct values 'Hourly'/'Daily'.",
            OwningArea = "schema-enum-vocab",
            Mode = ReproMode.Placeholder,
            Method = "POST", Path = "/v4/AssetPartitions/0/CheckSchedules",
            Body = "{\"ScheduleType\":\"Hour\"}",
            PlaceholderNote = "Tighten to ExpectGuidance with [\"Hourly\",\"Daily\"] when the schema surfaces enum vocab.",
        },
        new EvalCase {
            Id = "E017",
            Description = "No discoverable enum-vocabulary endpoint; /v4/EnumTypes/ScheduleType is 404.",
            OwningArea = "schema-enum-vocab",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "Enum values are supplied inline in schema; no direct API contract to assert here.",
        },

        // -- Schema fidelity (shallow Get API Schema) -------------------------
        new EvalCase {
            Id = "E019",
            Description = "Get API Schema for PUT /v4/AssetPartitions/{id}/CheckSchedules/{id} returned only header.",
            OwningArea = "schema-body-shape",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When the schema body-shape work lands, call SafeguardApiTool.Safeguard_Schema and assert body shape returned.",
        },
        new EvalCase {
            Id = "E020",
            Description = "Conditional required field (StartMinute when ScheduleType=Hourly) not surfaced.",
            OwningArea = "schema-body-shape",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When the schema surfaces conditional required fields, assert schema text mentions StartMinute.",
        },
        new EvalCase {
            Id = "E038",
            Description = "Get API Schema for POST /v4/AccessRequests returned only path header.",
            OwningArea = "schema-body-shape",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When the schema body-shape work lands, assert schema returns NewAccessRequest body shape.",
        },

        // -- Type metadata -----------------------------------------------------
        new EvalCase {
            Id = "E005",
            Description = "AuditLog/ObjectChanges: ObjectId is string; numeric in [..] rejected (70012).",
            OwningArea = "schema-property-paths",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AuditLog/ObjectChanges/Asset",
            Query = "filter=ObjectId in [1,2,3]",
            PlaceholderNote = "Tighten to ExpectGuidance with [\"string\"] when the schema advertises property types.",
        },

        // -- Audit endpoint defaults & first-class scoping --------------------
        new EvalCase {
            Id = "E006",
            Description = "AuditLog/ObjectChanges silently applies a default 1-day window; missing startDate → [].",
            OwningArea = "discover-preferred-params, execute-envelope",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AuditLog/ObjectChanges/Asset",
            Query = "filter=ObjectId eq '1'",
            PlaceholderNote = "Tighten to ExpectGuidance with [\"default\",\"startDate\"] when the response envelope surfaces the notice.",
        },
        new EvalCase {
            Id = "E009",
            Description = "Agent used limit=3 and got 1 result; preferred param is count/page, not limit.",
            OwningArea = "discover-preferred-params",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AuditLog/Logins",
            Query = "limit=3",
            PlaceholderNote = "Once Discover output names preferred params (count/page), assert Discover text includes them.",
        },
        new EvalCase {
            Id = "E025",
            Description = "count-only query semantics (count=true returns bare integer) had to be discovered.",
            OwningArea = "discover-preferred-params",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AuditLog/Logins",
            Query = "count=true&startDate=2020-01-01&endDate=2020-01-02",
            PlaceholderNote = "Once Discover documents count=true on this path, assert Safeguard_Discover output mentions 'count'.",
        },
        new EvalCase {
            Id = "E028",
            Description = "Agent never used first-class scoping params (userId/assetId/accountId/startDate/endDate/count) on audit endpoints (cluster finding).",
            OwningArea = "discover-preferred-params",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "Once Discover output promotes these to 'Preferred params', assert their presence.",
        },

        // -- Operator vocabulary ----------------------------------------------
        new EvalCase {
            Id = "E011",
            Description = "RECLASSIFIED: icontains is valid; original empty result was E006 (default window).",
            OwningArea = "query-syntax-operators, execute-envelope",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When the operator-vocabulary surface ships, assert Safeguard_QuerySyntax output contains 'icontains'.",
        },
        new EvalCase {
            Id = "E029",
            Description = "Filter-operator vocabulary not surfaced (cluster finding).",
            OwningArea = "query-syntax-operators",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "Assert Safeguard_QuerySyntax lists eq,ne,gt,ge,lt,le,and,or,not,contains,ieq,icontains,sw,isw,ew,iew,in once the operator-vocabulary surface ships.",
        },

        // -- Envelope / notice channel ----------------------------------------
        new EvalCase {
            Id = "E014",
            Description = "'Auto-applied limit=50' notice appeared in the result channel and confused the agent.",
            OwningArea = "execute-envelope",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/Users",
            PlaceholderNote = "Once the response envelope lands, assert the auto-limit notice is on the envelope, not embedded in the body text.",
        },

        // -- Paging / truncation ----------------------------------------------
        new EvalCase {
            Id = "E007",
            Description = "Large OldValue/NewValue JSON in object-change records trips the 29.4 KB output cap.",
            OwningArea = "execute-paging-hints",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AuditLog/ObjectChanges/Asset",
            Query = "startDate=2020-01-01&endDate=2030-01-01",
            PlaceholderNote = "When the paging-hint work ships, assert response carries a paging hint (page=2) instead of truncating mid-record.",
        },
        new EvalCase {
            Id = "E022",
            Description = "GET /v4/Assets returned 10 records that exceed the 29.4 KB cap.",
            OwningArea = "execute-paging-hints",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/Assets",
            Query = "limit=10",
            PlaceholderNote = "When the paging-hint work ships, assert paging hint is returned instead of truncation.",
        },
        new EvalCase {
            Id = "E031",
            Description = "Even a tightly scoped audit query (userId + 1-week, 213 records) trips the cap.",
            OwningArea = "execute-paging-hints",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "Same shape as E007/E022; flip when the paging-hint work ships.",
        },

        // -- CSV hygiene ------------------------------------------------------
        new EvalCase {
            Id = "E003",
            Description = "CSV payload prepended with non-CSV header/comment line, breaking Import-Csv.",
            OwningArea = "csv-output-hygiene",
            Mode = ReproMode.Placeholder,
            Method = "GET", Path = "/v4/AuditLog/Logins",
            Query = "startDate=2020-01-01&endDate=2030-01-01",
            Format = "csv",
            PlaceholderNote = "When CSV output hygiene ships, assert response/file begins with the CSV header row (no preamble/BOM).",
        },
        new EvalCase {
            Id = "E010",
            Description = "CSV row truncation when individual rows contain very large embedded JSON (OldValue).",
            OwningArea = "execute-paging-hints, csv-output-hygiene",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When paging hints and CSV hygiene land, assert CSV rows are written whole (record count == row count).",
        },
        new EvalCase {
            Id = "E021",
            Description = "format=csv on a POST silently accepted at discovery, only rejected at execute.",
            OwningArea = "discover-preferred-params",
            Mode = ReproMode.Placeholder,
            Method = "POST", Path = "/v4/Users", Body = "{}", Format = "csv",
            PlaceholderNote = "When Discover documents 'format=csv is GET-only', assert Safeguard_Execute tool description mentions it.",
        },

        // -- Discoverability / workflows --------------------------------------
        new EvalCase {
            Id = "E008",
            Description = "OldValue/NewValue are JSON strings (escaped JSON in JSON); no first-class accessor.",
            OwningArea = "field-projection-helpers",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "Covered by field-projection helpers; no per-call reproducer.",
        },
        new EvalCase {
            Id = "E012",
            Description = "Deleted Objects API not surfaced by Safeguard_Discover.",
            OwningArea = "discovery-terminology",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When the discovery-terminology surface ships, assert Discover('deleted') includes /v4/DeletedAssets etc.",
        },
        new EvalCase {
            Id = "E018",
            Description = "Discover semantic ranking weak: 'transfer asset partition', 'Assets move', 'enum'.",
            OwningArea = "discovery-terminology",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When the discovery-terminology surface ships, assert each of these queries returns a non-empty, on-topic result.",
        },
        new EvalCase {
            Id = "E023",
            Description = "No discoverable 'move asset between partitions' verb; agents use BatchUpdate.",
            OwningArea = "discovery-terminology",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When the discovery-terminology surface ships, assert Discover('move asset') points to AssetPartitions/{id}/Assets/Add.",
        },
        new EvalCase {
            Id = "E026",
            Description = "Workflow gap: 'recent activity / who used the appliance' has no recipe.",
            OwningArea = "discovery-terminology, open-access-request",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When the recipe lands, assert Safeguard_Workflows('recent activity') returns it.",
        },
        new EvalCase {
            Id = "E035",
            Description = "No 'launch access request' workflow tool; agent stitches ~6 steps.",
            OwningArea = "open-access-request",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When Safeguard_OpenAccessRequest ships, replace with happy-path invocation.",
        },
        new EvalCase {
            Id = "E036",
            Description = "POST /v4/AccessRequests 403 'not authorized to use this request type' was a wrong-AssetId problem.",
            OwningArea = "open-access-request",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "When the composite pre-validates against /v4/Me/RequestEntitlements, assert error mentions 'AssetId' guidance.",
        },

        // -- Diagnostic / positive-trace observations -------------------------
        new EvalCase {
            Id = "E024",
            Description = "Comparative analysis: 'who has used this appliance this week' was harder than the prior 'who has been using this appliance' trace.",
            OwningArea = "meta",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "Diagnostic only; no live reproducer. Variance shrinks as discover-preferred-params, execute-envelope, and execute-paging-hints land.",
        },
        new EvalCase {
            Id = "E032",
            Description = "Positive observation: agent used userId + count=true correctly.",
            OwningArea = "positive",
            Mode = ReproMode.Placeholder,
            PlaceholderNote = "Diagnostic only; nothing to assert.",
        },
    };
}
