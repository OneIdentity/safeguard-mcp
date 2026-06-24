using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Pins the proactive audit query-syntax reference produced by
/// <see cref="SafeguardApiTool.BuildAuditQuerySyntaxReference"/>. For the audit
/// collections (/v4/AuditLog/Logins, /Search, /ObjectChanges) the reference must
/// spell out the canonical FILTERABLE property names in their nested form
/// (UserProperties.UserName), name LogTime as the orderby time field, and call out
/// the traps the agent otherwise only learns after a failed call: there is no flat
/// UserName / ModifiedByUserId column and -Timestamp is not a valid time field.
/// Non-audit paths get no audit block.
///
/// Field set verified against the appliance DTOs: LoginActivityLog, AuditSearchLog,
/// and ObjectChangeLog all expose the actor via the nested UserProperties object and
/// order on LogTime.
/// </summary>
public class AuditQuerySyntaxReferenceTests
{
    [Fact]
    public void Logins_ContainsNestedFormAndNonFilterableCallout()
    {
        var text = SafeguardApiTool.BuildAuditQuerySyntaxReference("/v4/AuditLog/Logins");

        Assert.NotNull(text);
        Assert.Contains("UserProperties.UserName", text);
        Assert.Contains("LogTime", text);
        // Non-filterable callout: ModifiedByUserId is named and flagged as not a field.
        Assert.Contains("ModifiedByUserId", text);
        Assert.Contains("Not filterable", text);
        Assert.Contains("-Timestamp", text);
    }

    [Fact]
    public void Search_ContainsNestedFormAndTimeField()
    {
        var text = SafeguardApiTool.BuildAuditQuerySyntaxReference("/v4/AuditLog/Search");

        Assert.NotNull(text);
        Assert.Contains("UserProperties.UserName", text);
        Assert.Contains("orderby=-LogTime", text);
    }

    [Fact]
    public void ObjectChanges_UsesVerifiedFieldsAndScopingParams()
    {
        var text = SafeguardApiTool.BuildAuditQuerySyntaxReference("/v4/AuditLog/ObjectChanges");

        Assert.NotNull(text);
        Assert.Contains("UserProperties.UserName", text);
        Assert.Contains("LogTime", text);
        Assert.Contains("ModifiedByUserId", text);
        // ObjectChanges additionally exposes assetId / accountId scoping params.
        Assert.Contains("assetId", text);
        Assert.Contains("accountId", text);
    }

    [Fact]
    public void ObjectChangesSubResourcePath_StillProducesReference()
    {
        var text = SafeguardApiTool.BuildAuditQuerySyntaxReference("/v4/AuditLog/ObjectChanges/User");

        Assert.NotNull(text);
        Assert.Contains("UserProperties.UserName", text);
    }

    [Fact]
    public void NonAuditPath_ReturnsNull()
    {
        Assert.Null(SafeguardApiTool.BuildAuditQuerySyntaxReference("/v4/Users"));
    }

    [Fact]
    public void UnknownAuditEndpoint_ReturnsNull()
    {
        Assert.Null(SafeguardApiTool.BuildAuditQuerySyntaxReference("/v4/AuditLog/Appliances"));
    }
}
