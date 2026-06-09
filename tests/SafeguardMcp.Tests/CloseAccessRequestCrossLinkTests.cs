using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Cross-link checks that direct POSTs to /v4/AccessRequests/{id}/Cancel,
/// /CheckIn, /Close, /Acknowledge surface a workflow_recipe_suggested
/// notice naming Safeguard_CloseAccessRequest, and that the
/// session-access-request recipe footer lists the new composite tool
/// alongside Safeguard_OpenAccessRequest.
/// </summary>
public class CloseAccessRequestCrossLinkTests
{
    private static ApiEndpoint[] Fixture() =>
    [
        new ApiEndpoint("Core", "POST", "/v4/AccessRequests/{id}/Cancel",      "Cancel the access request", "", true),
        new ApiEndpoint("Core", "POST", "/v4/AccessRequests/{id}/CheckIn",     "Check in", "", false),
        new ApiEndpoint("Core", "POST", "/v4/AccessRequests/{id}/Close",       "Close pending review", "", true),
        new ApiEndpoint("Core", "POST", "/v4/AccessRequests/{id}/Acknowledge", "Acknowledge", "", true),
    ];

    [Theory]
    [InlineData("/v4/AccessRequests/42/Cancel")]
    [InlineData("/v4/AccessRequests/42/CheckIn")]
    [InlineData("/v4/AccessRequests/42/Close")]
    [InlineData("/v4/AccessRequests/42/Acknowledge")]
    public void Notice_DirectSubEndpoint_PointsAtCloseCompositeTool(string path)
    {
        var notice = SafeguardApiTool.BuildRecipeCrossLinkNotice("POST", path);
        Assert.NotNull(notice);
        Assert.Equal(NoticeKinds.WorkflowRecipeSuggested, notice.Kind);
        Assert.Contains("Safeguard_CloseAccessRequest", notice.Message);
    }

    [Fact]
    public void Notice_PostCreate_StillPointsAtOpenCompositeTool()
    {
        var notice = SafeguardApiTool.BuildRecipeCrossLinkNotice("POST", "/v4/AccessRequests");
        Assert.NotNull(notice);
        Assert.Contains("Safeguard_OpenAccessRequest", notice.Suggestion);
    }

    [Fact]
    public void Recipe_FooterListsBothCompositeTools()
    {
        var path = System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "SafeguardMcp", "Workflows", "session-access-request.md");
        path = System.IO.Path.GetFullPath(path);
        Assert.True(System.IO.File.Exists(path), $"Recipe not found at {path}");
        var content = System.IO.File.ReadAllText(path);
        Assert.Contains("tool: Safeguard_OpenAccessRequest", content);
        Assert.Contains("tool: Safeguard_CloseAccessRequest", content);
    }
}
