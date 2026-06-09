using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class UncatalogedSensitiveShapeHeuristicTests
{
    [Fact]
    public void Heuristic_Fires_OnTopLevelPasswordWithStringValue()
    {
        var notice = SafeguardApiTool.BuildUncatalogedSensitiveShapeNotice(
            body: "{\"Password\":\"hunter2\",\"Id\":1}",
            method: "GET",
            path: "/v4/SomeNewEndpoint/1");
        Assert.NotNull(notice);
        Assert.Equal(NoticeKinds.UncatalogedSensitiveShape, notice.Kind);
        Assert.Contains("Password", notice.Message);
    }

    [Fact]
    public void Heuristic_Fires_OnFirstArrayElementPrivateKey()
    {
        var notice = SafeguardApiTool.BuildUncatalogedSensitiveShapeNotice(
            body: "[{\"PrivateKey\":\"-----BEGIN RSA-----...\",\"Id\":1},{\"PrivateKey\":\"\"}]",
            method: "GET",
            path: "/v4/SomeOther/1/Keys");
        Assert.NotNull(notice);
        Assert.Contains("PrivateKey", notice.Message);
    }

    [Fact]
    public void Heuristic_DoesNotFire_OnSchemaShapedResponse_WithNullValue()
    {
        // Audit-log "Password event" rows describe the event metadata; the
        // Password-named field is null/empty/non-string. The string-typed
        // non-empty check skips this naturally.
        var notice = SafeguardApiTool.BuildUncatalogedSensitiveShapeNotice(
            body: "{\"Password\":null,\"EventName\":\"PasswordChanged\",\"LogTime\":\"2025-01-01T00:00:00Z\"}",
            method: "GET",
            path: "/v4/AuditLog/Passwords");
        Assert.Null(notice);
    }

    [Fact]
    public void Heuristic_DoesNotFire_OnSchemaDescriptionStyleResponse()
    {
        // A Safeguard_Schema response describes a Password property as type metadata,
        // not as a value; the field appears under "Properties" not as a top-level
        // string. Heuristic must skip these naturally.
        var notice = SafeguardApiTool.BuildUncatalogedSensitiveShapeNotice(
            body: "{\"Properties\":[{\"Name\":\"Password\",\"Type\":\"string\"}]}",
            method: "GET",
            path: "/v4/Schema/SomeEntity");
        Assert.Null(notice);
    }

    [Fact]
    public void Heuristic_DoesNotFire_OnEmptyPasswordValue()
    {
        var notice = SafeguardApiTool.BuildUncatalogedSensitiveShapeNotice(
            body: "{\"Password\":\"\",\"Id\":1}",
            method: "GET",
            path: "/v4/Anything");
        Assert.Null(notice);
    }

    [Fact]
    public void Heuristic_DoesNotFire_OnCataloged_SensitivePath()
    {
        // Cataloged sensitive paths are never reached via Execute (the guard
        // refuse-and-redirects before any I/O), so even if a synthetic response
        // body showed up here we must not double-flag — the catalog wins.
        var notice = SafeguardApiTool.BuildUncatalogedSensitiveShapeNotice(
            body: "\"hunter2\"",
            method: "POST",
            path: "/v4/AccessRequests/123/CheckOutPassword");
        Assert.Null(notice);
    }
}
