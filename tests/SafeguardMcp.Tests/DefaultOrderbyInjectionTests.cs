using System;
using System.Collections.Generic;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Pins the default-orderby injection that
/// <see cref="SafeguardApiTool.MaybeInjectDefaultOrderby"/> applies when a GET
/// targets an audit collection path (/v4/AuditLog/Logins, /Search, /ObjectChanges)
/// and the caller supplied no orderby. The MCP injects a newest-first default on
/// the canonical time field so "the latest row" is deterministic, and a
/// default_orderby_applied notice fires so the agent can override it. An explicit
/// orderby is left untouched; non-audit and singleton paths are untouched.
///
/// Canonical field verified live against the appliance: orderby=-LogTime is valid
/// and orders newest-first on Logins, Search, and ObjectChanges; orderby=-Timestamp
/// is rejected (HTTP 400, code 70001).
/// </summary>
public class DefaultOrderbyInjectionTests
{
    private static IDictionary<string, string> Params(params (string Key, string Value)[] pairs)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in pairs)
            d[k] = v;
        return d;
    }

    [Fact]
    public void Logins_NoOrderby_InjectsLogTimeDescendingAndNotice()
    {
        var result = SafeguardApiTool.MaybeInjectDefaultOrderby(
            "GET", "/v4/AuditLog/Logins", null, out var injected);

        Assert.Equal("-LogTime", injected);
        Assert.NotNull(result);
        Assert.Equal("-LogTime", result["orderby"]);

        // A notice fires so the agent knows the ordering was MCP-applied.
        var notice = SafeguardApiTool.BuildDefaultOrderbyNotice(injected);
        Assert.NotNull(notice);
        Assert.Equal(NoticeKinds.DefaultOrderbyApplied, notice.Kind);
        Assert.Contains("-LogTime", notice.Message);
        Assert.Contains("orderby", notice.Suggestion);
    }

    [Fact]
    public void ObjectChanges_NoOrderby_InjectsVerifiedLogTimeField()
    {
        var result = SafeguardApiTool.MaybeInjectDefaultOrderby(
            "GET", "/v4/AuditLog/ObjectChanges", null, out var injected);

        Assert.Equal("-LogTime", injected);
        Assert.Equal("-LogTime", result["orderby"]);
    }

    [Fact]
    public void Search_NoOrderby_InjectsLogTimeDescending()
    {
        var result = SafeguardApiTool.MaybeInjectDefaultOrderby(
            "GET", "/v4/AuditLog/Search", Params(("limit", "50")), out var injected);

        Assert.Equal("-LogTime", injected);
        Assert.Equal("-LogTime", result["orderby"]);
        // Pre-existing params are preserved.
        Assert.Equal("50", result["limit"]);
    }

    [Fact]
    public void Logins_ExplicitOrderby_LeftUntouched()
    {
        var parameters = Params(("orderby", "LogTime"));

        var result = SafeguardApiTool.MaybeInjectDefaultOrderby(
            "GET", "/v4/AuditLog/Logins", parameters, out var injected);

        Assert.Null(injected);
        Assert.Same(parameters, result);
        Assert.Equal("LogTime", result["orderby"]);
        Assert.Null(SafeguardApiTool.BuildDefaultOrderbyNotice(injected));
    }

    [Fact]
    public void NonAuditPath_LeftUntouched()
    {
        var result = SafeguardApiTool.MaybeInjectDefaultOrderby(
            "GET", "/v4/Users", null, out var injected);

        Assert.Null(injected);
        Assert.Null(result);
    }

    [Fact]
    public void AuditSingletonPath_LeftUntouched()
    {
        // A single-record lookup must never be reordered.
        var result = SafeguardApiTool.MaybeInjectDefaultOrderby(
            "GET", "/v4/AuditLog/Logins/42", null, out var injected);

        Assert.Null(injected);
        Assert.Null(result);
    }

    [Fact]
    public void NonGetMethod_LeftUntouched()
    {
        var result = SafeguardApiTool.MaybeInjectDefaultOrderby(
            "POST", "/v4/AuditLog/Search", null, out var injected);

        Assert.Null(injected);
        Assert.Null(result);
    }

    [Fact]
    public void UnknownAuditEndpoint_LeftUntouched()
    {
        // Audit endpoints whose canonical time field is unverified get no default.
        var result = SafeguardApiTool.MaybeInjectDefaultOrderby(
            "GET", "/v4/AuditLog/Appliances", null, out var injected);

        Assert.Null(injected);
        Assert.Null(result);
    }
}
