using System;
using System.Collections.Generic;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Pins the <c>empty_audit_result</c> notice that
/// <see cref="SafeguardApiTool.BuildEmptyAuditResultNotice"/> attaches when a GET
/// against an audit-log path (/v4/AuditLog/*) returns an empty JSON array. The
/// notice disambiguates "0 rows match &lt;filter&gt; in window &lt;start&gt;..&lt;end&gt;"
/// from "no filter applied" and "default 1-day window", echoing the effective
/// filter and time window the server applied. Non-empty results and non-audit
/// paths get no notice.
///
/// Default-window semantics verified against PangaeaAppliance's
/// CassandraDataExtensions.ProcessCassandraDateFilter: endDate defaults to now,
/// startDate defaults to one day before the effective endDate, identically for
/// Logins / ObjectChanges / Search.
/// </summary>
public class EmptyAuditResultNoticeTests
{
    private static IDictionary<string, string> Params(params (string Key, string Value)[] pairs)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in pairs)
            d[k] = v;
        return d;
    }

    [Fact]
    public void EmptyResult_WithUserSuppliedFilter_EchoesFilterAndWindow()
    {
        var parameters = Params(
            ("filter", "EventName eq 'Login'"),
            ("startDate", "2026-01-01T00:00:00Z"),
            ("endDate", "2026-01-02T00:00:00Z"));

        var notice = SafeguardApiTool.BuildEmptyAuditResultNotice(
            "GET", "/v4/AuditLog/Logins", "[]", parameters);

        Assert.NotNull(notice);
        Assert.Equal(NoticeKinds.EmptyAuditResult, notice.Kind);
        // Filter echoed verbatim.
        Assert.Contains("EventName eq 'Login'", notice.Message);
        Assert.Contains("0 rows match", notice.Message);
        // Effective window echoed from the supplied bounds.
        Assert.Contains("2026-01-01T00:00:00Z", notice.Message);
        Assert.Contains("2026-01-02T00:00:00Z", notice.Message);
        // Not flagged as the default look-back since the agent supplied bounds.
        Assert.DoesNotContain("default 1-day", notice.Message);
    }

    [Fact]
    public void EmptyResult_WithScopingParam_TreatsItAsFilter()
    {
        var parameters = Params(("userId", "42"));

        var notice = SafeguardApiTool.BuildEmptyAuditResultNotice(
            "GET", "/v4/AuditLog/ObjectChanges", "[]", parameters);

        Assert.NotNull(notice);
        Assert.Contains("0 rows match", notice.Message);
        Assert.Contains("userId=42", notice.Message);
    }

    [Fact]
    public void EmptyResult_NoFilter_EchoesDefaultWindow()
    {
        var notice = SafeguardApiTool.BuildEmptyAuditResultNotice(
            "GET", "/v4/AuditLog/Logins", "[]", null);

        Assert.NotNull(notice);
        Assert.Equal(NoticeKinds.EmptyAuditResult, notice.Kind);
        // No filter: the message says so explicitly.
        Assert.Contains("no filter applied", notice.Message);
        // The default 1-day look-back is echoed and called out.
        Assert.Contains("default 1-day window", notice.Message);
        Assert.Contains("startDate/endDate", notice.Suggestion);
    }

    [Fact]
    public void EmptyResult_NoFilter_OnlyLimitInjected_StillCountsAsNoFilter()
    {
        // MaybeInjectLimit/MaybeInjectDefaultFields add limit=/fields= — those are
        // response-shaping params, not filters, so the notice must still report
        // "no filter applied".
        var parameters = Params(("limit", "100"), ("fields", "Id,LogTime"));

        var notice = SafeguardApiTool.BuildEmptyAuditResultNotice(
            "GET", "/v4/AuditLog/Logins", "[]", parameters);

        Assert.NotNull(notice);
        Assert.Contains("no filter applied", notice.Message);
        Assert.Contains("default 1-day window", notice.Message);
    }

    [Fact]
    public void EmptyResult_PartialWindow_EndOnly_DefaultsStartToOneDayBefore()
    {
        var parameters = Params(("endDate", "2026-03-10T00:00:00Z"));

        var notice = SafeguardApiTool.BuildEmptyAuditResultNotice(
            "GET", "/v4/AuditLog/Search", "[]", parameters);

        Assert.NotNull(notice);
        // Supplying endDate alone is not the all-default window.
        Assert.DoesNotContain("default 1-day window", notice.Message);
        Assert.Contains("2026-03-10T00:00:00Z minus 1 day", notice.Message);
        Assert.Contains("2026-03-10T00:00:00Z", notice.Message);
    }

    [Fact]
    public void NonEmptyResult_GetsNoNotice()
    {
        var notice = SafeguardApiTool.BuildEmptyAuditResultNotice(
            "GET", "/v4/AuditLog/Logins", "[{\"Id\":1}]", null);

        Assert.Null(notice);
    }

    [Fact]
    public void NonAuditPath_GetsNoNotice()
    {
        var notice = SafeguardApiTool.BuildEmptyAuditResultNotice(
            "GET", "/v4/Users", "[]", null);

        Assert.Null(notice);
    }

    [Fact]
    public void NonGetMethod_GetsNoNotice()
    {
        var notice = SafeguardApiTool.BuildEmptyAuditResultNotice(
            "POST", "/v4/AuditLog/Logins", "[]", null);

        Assert.Null(notice);
    }

    [Fact]
    public void NonArrayBody_GetsNoNotice()
    {
        // A count=true response is a bare scalar, not an empty array.
        var notice = SafeguardApiTool.BuildEmptyAuditResultNotice(
            "GET", "/v4/AuditLog/Logins", "0", Params(("count", "true")));

        Assert.Null(notice);
    }
}
