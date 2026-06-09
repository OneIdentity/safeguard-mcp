using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tests;

public class DefaultProjectionsTests
{
    [Theory]
    [InlineData("/v4/AuditLog/ObjectChanges")]
    [InlineData("/v4/AuditLog/ObjectChanges/User")]
    [InlineData("/v4/AuditLog/ObjectChanges/User/2152")]
    public void Default_Is_Injected_On_List_Routes_When_Fields_Omitted(string path)
    {
        var applied = DefaultProjections.TryGetDefaultFields(
            method: "GET",
            normalizedPath: path,
            callerSuppliedFields: false,
            out var defaultFields);

        Assert.True(applied);
        Assert.Equal("-OldValue,-NewValue", defaultFields);
    }

    [Theory]
    [InlineData("/v4/AuditLog/ObjectChanges")]
    [InlineData("/v4/AuditLog/ObjectChanges/User")]
    [InlineData("/v4/AuditLog/ObjectChanges/User/2152")]
    public void Default_Is_NOT_Injected_When_Caller_Supplied_Fields(string path)
    {
        // Even when the caller's explicit fields= names OldValue/NewValue (or anything else),
        // the explicit value passes through verbatim. Default never overrides explicit.
        var applied = DefaultProjections.TryGetDefaultFields(
            method: "GET",
            normalizedPath: path,
            callerSuppliedFields: true,
            out var defaultFields);

        Assert.False(applied);
        Assert.Null(defaultFields);
    }

    [Fact]
    public void Default_Is_NOT_Injected_On_Singleton_Route()
    {
        // /v4/AuditLog/ObjectChanges/{objectType}/{objectId}/{logId} is the singleton.
        // The caller's intent signal is strong; preserve OldValue/NewValue.
        var applied = DefaultProjections.TryGetDefaultFields(
            method: "GET",
            normalizedPath: "/v4/AuditLog/ObjectChanges/User/2152/abc-123",
            callerSuppliedFields: false,
            out var defaultFields);

        Assert.False(applied);
        Assert.Null(defaultFields);
    }

    [Theory]
    [InlineData("/v4/AuditLog/Logins")]
    [InlineData("/v4/AuditLog/Passwords")]
    [InlineData("/v4/AuditLog/AccessRequests")]
    [InlineData("/v4/AuditLog/Appliances")]
    public void Default_Is_NOT_Injected_On_Other_Audit_Endpoints(string path)
    {
        var applied = DefaultProjections.TryGetDefaultFields(
            method: "GET",
            normalizedPath: path,
            callerSuppliedFields: false,
            out var defaultFields);

        Assert.False(applied);
        Assert.Null(defaultFields);
    }

    [Theory]
    [InlineData("/v4/Users")]
    [InlineData("/v4/AssetAccounts")]
    [InlineData("/v4/Assets/123")]
    [InlineData("/v4/AuditLog")]
    public void Default_Is_NOT_Injected_On_NonAudit_Or_NonObjectChanges_Endpoints(string path)
    {
        var applied = DefaultProjections.TryGetDefaultFields(
            method: "GET",
            normalizedPath: path,
            callerSuppliedFields: false,
            out var defaultFields);

        Assert.False(applied);
        Assert.Null(defaultFields);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public void Default_Is_NOT_Injected_For_NonGET_Methods(string method)
    {
        var applied = DefaultProjections.TryGetDefaultFields(
            method: method,
            normalizedPath: "/v4/AuditLog/ObjectChanges",
            callerSuppliedFields: false,
            out var defaultFields);

        Assert.False(applied);
        Assert.Null(defaultFields);
    }

    [Fact]
    public void ObjectChanges_Prefix_Match_Does_Not_Spill_Into_Sibling_Paths()
    {
        // Defensive: a path that begins with the same prefix but is a different endpoint
        // should not be picked up by the matcher.
        var applied = DefaultProjections.TryGetDefaultFields(
            method: "GET",
            normalizedPath: "/v4/AuditLog/ObjectChangesSomethingElse",
            callerSuppliedFields: false,
            out _);

        Assert.False(applied);
    }
}
