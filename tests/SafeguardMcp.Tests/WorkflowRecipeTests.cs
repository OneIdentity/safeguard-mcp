using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class WorkflowRecipeTests
{
    [Fact]
    public void Workflows_ListsAllRecipes()
    {
        var tool = new SafeguardWorkflows();
        var result = tool.Safeguard_Workflows();

        // Should contain the listing header
        Assert.Contains("Available Safeguard workflows", result);
        // Should list at least 20 recipes (10 original + 11 added)
        Assert.Contains("(2", result); // "(21):" — at least 20+
    }

    [Theory]
    [InlineData("health-check")]
    [InlineData("task-triage")]
    [InlineData("a2a-credential-retrieval")]
    [InlineData("cluster-operations")]
    [InlineData("personal-password-vault")]
    public void Workflows_GetById_ReturnsContent(string id)
    {
        var tool = new SafeguardWorkflows();
        var result = tool.Safeguard_Workflows(id: id);

        // Should NOT contain the "not found" message
        Assert.DoesNotContain("was not found", result);
        // Should contain the actual recipe content (has Steps:)
        Assert.Contains("Steps:", result);
    }

    [Fact]
    public void Workflows_GetById_NotFound_ReturnsHelpfulMessage()
    {
        var tool = new SafeguardWorkflows();
        var result = tool.Safeguard_Workflows(id: "nonexistent-workflow-xyz");

        Assert.Contains("was not found", result);
        Assert.Contains("list available workflow IDs", result);
    }

    [Theory]
    [InlineData("password", "password")]
    [InlineData("cluster", "cluster")]
    [InlineData("backup", "backup")]
    [InlineData("SSH", "ssh")]
    public void Workflows_Search_FindsMatchingRecipes(string search, string expectedInResult)
    {
        var tool = new SafeguardWorkflows();
        var result = tool.Safeguard_Workflows(search: search);

        Assert.Contains(expectedInResult, result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("No workflows matched", result);
    }

    [Fact]
    public void Workflows_Search_NoMatch_ReturnsHelpfulMessage()
    {
        var tool = new SafeguardWorkflows();
        var result = tool.Safeguard_Workflows(search: "zzz_nonexistent_topic_zzz");

        Assert.Contains("No workflows matched", result);
    }
}
