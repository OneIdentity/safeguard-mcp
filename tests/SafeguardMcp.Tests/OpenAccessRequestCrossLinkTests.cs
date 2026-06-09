using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Cross-link checks that the launch / RDP / SSH / access-request searches in
/// Safeguard_Discover surface the new Safeguard_OpenAccessRequest composite
/// tool (via the session-access-request recipe's <c>tool:</c> front matter),
/// and that the 90408 wrong-asset error hint also points at it.
/// </summary>
public class OpenAccessRequestCrossLinkTests
{
    private static ApiEndpoint[] Fixture() =>
    [
        new ApiEndpoint("Core", "POST", "/v4/AccessRequests",                  "Create an access request", "", true),
        new ApiEndpoint("Core", "GET",  "/v4/AccessRequests",                  "Gets a list of access requests", "", false),
        new ApiEndpoint("Core", "POST", "/v4/AccessRequests/{id}/InitializeSession", "Initialize the session for an approved request", "", true),
    ];

    [Fact]
    public void Discover_LaunchSearch_SurfacesCompositeTool()
    {
        var result = SafeguardApiTool.FormatDiscovery(Fixture(), null, "launch", null);
        Assert.Contains("session-access-request", result);
        Assert.Contains("Safeguard_OpenAccessRequest", result);
    }

    [Fact]
    public void Discover_RdpSearch_SurfacesCompositeTool()
    {
        var result = SafeguardApiTool.FormatDiscovery(Fixture(), null, "RDP", null);
        Assert.Contains("session-access-request", result);
        Assert.Contains("Safeguard_OpenAccessRequest", result);
    }

    [Fact]
    public void Discover_AccessRequestSearch_SurfacesCompositeTool()
    {
        var result = SafeguardApiTool.FormatDiscovery(Fixture(), null, "access request", null);
        Assert.Contains("session-access-request", result);
        Assert.Contains("Safeguard_OpenAccessRequest", result);
    }

    [Fact]
    public void ErrorHint_90408_PointsAtCompositeToolAndEntitlementsCheck()
    {
        var hint = ApiToolHelpers.GetErrorHint(
            statusCode: 403,
            apiMessage: "Access request user is not authorized to use this request type (90408)",
            hasModelState: false,
            ctx: new ErrorContext("Core", "POST", "/v4/AccessRequests"),
            paths: Array.Empty<ApiSchemaPropertyPath>());

        Assert.NotNull(hint);
        Assert.Contains("Safeguard_OpenAccessRequest", hint);
        Assert.Contains("RequestEntitlements", hint);
        Assert.Contains("AssetId", hint);
    }
}
