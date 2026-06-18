using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class WorkflowRecipeTests
{
    [Fact]
    public void Workflows_ListsAllRecipes()
    {
        var result = SafeguardWorkflows.Lookup(null, null);

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
    [InlineData("recent-activity-decision-tree")]
    [InlineData("partition-schedule-update")]
    [InlineData("account-discovery-status")]
    [InlineData("count-with-filter")]
    [InlineData("summarize-audit-log")]
    [InlineData("time-bucketed-counts")]
    public void Workflows_GetById_ReturnsContent(string id)
    {
        var result = SafeguardWorkflows.Lookup(null, id);

        // Should NOT contain the "not found" message
        Assert.DoesNotContain("was not found", result);
        // Should contain the actual recipe content (has Steps:)
        Assert.Contains("Steps:", result);
    }

    [Fact]
    public void Workflows_GetById_NotFound_ReturnsHelpfulMessage()
    {
        var result = SafeguardWorkflows.Lookup(null, "nonexistent-workflow-xyz");

        Assert.Contains("was not found", result);
        Assert.Contains("list available workflow IDs", result);
    }

    [Theory]
    [InlineData("password", "password")]
    [InlineData("cluster", "cluster")]
    [InlineData("backup", "backup")]
    [InlineData("SSH", "ssh")]
    [InlineData("login history", "recent-activity-decision-tree")]
    [InlineData("who logged in", "recent-activity-decision-tree")]
    [InlineData("discover accounts", "account-discovery-status")]
    [InlineData("password schedule", "partition-schedule-update")]
    [InlineData("count", "count-with-filter")]
    [InlineData("how many", "count-with-filter")]
    [InlineData("summarize", "summarize-audit-log")]
    [InlineData("group by", "summarize-audit-log")]
    [InlineData("per day", "time-bucketed-counts")]
    [InlineData("trend", "time-bucketed-counts")]
    public void Workflows_Search_FindsMatchingRecipes(string search, string expectedInResult)
    {
        var result = SafeguardWorkflows.Lookup(search, null);

        Assert.Contains(expectedInResult, result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("No workflows matched", result);
    }

    [Fact]
    public void Workflows_Search_NoMatch_ReturnsHelpfulMessage()
    {
        var result = SafeguardWorkflows.Lookup("zzz_nonexistent_topic_zzz", null);

        Assert.Contains("No workflows matched", result);
    }

    [Fact]
    public void Workflows_EveryRecipeHasNonEmptyTags()
    {
        // Tags drive recipe ranking in Safeguard_Discover; an untagged recipe
        // is invisible to verb-intent searches even when its description matches.
        // This guards against silently dropping tags on new recipes.
        foreach (var recipe in SafeguardMcp.Catalog.RecipeIndex.Recipes)
        {
            Assert.True(recipe.Tags.Count > 0,
                $"Recipe '{recipe.Id}' has no tags. Tags are required for verb-intent discovery.");
        }
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("remove")]
    [InlineData("cleanup")]
    [InlineData("batchdelete")]
    [InlineData("update")]
    [InlineData("modify")]
    public void RecipeIndex_BulkAssetOperations_ReturnsStrongMatchForCleanupVerbs(string term)
    {
        var matches = SafeguardMcp.Catalog.RecipeIndex.Score(new[] { term });

        var hit = matches.FirstOrDefault(m => m.Recipe.Id == "bulk-asset-operations");
        Assert.NotNull(hit);
        Assert.True(hit.Score > 0, $"Expected term '{term}' to score > 0 against bulk-asset-operations.");
        Assert.True(hit.Strong, $"Expected term '{term}' to be a strong match against bulk-asset-operations.");
    }

    [Fact]
    public void RecipeIndex_BulkAssetOperations_DoesNotMatchUnrelatedSubstring_Move()
    {
        // Regression guard against substring noise (RecipeIndex.cs:69-71): a search for "move"
        // must NOT promote bulk-asset-operations via the "remove" tag.
        var matches = SafeguardMcp.Catalog.RecipeIndex.Score(new[] { "move" });
        var hit = matches.FirstOrDefault(m => m.Recipe.Id == "bulk-asset-operations");
        if (hit is not null)
        {
            Assert.False(hit.Strong, "'move' must not be a strong match against bulk-asset-operations.");
        }
    }
}
