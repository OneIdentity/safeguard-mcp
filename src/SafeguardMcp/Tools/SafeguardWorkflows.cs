using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tools;

[McpServerToolType]
internal sealed class SafeguardWorkflows
{
    private static IReadOnlyList<WorkflowRecipe> Recipes => RecipeIndex.Recipes;

    [McpServerTool(Name = "Safeguard_Workflows", Title = "Safeguard Workflow Recipes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get step-by-step workflow recipes for common Safeguard operations. "
        + "Each recipe describes the sequence of API calls needed to accomplish a goal. "
        + "Use this when you need to perform a multi-step operation and want guidance on the correct approach.")]
    public string Safeguard_Workflows(
        [Description("Search for workflows by keyword (e.g. 'health', 'password failure', 'bulk import', 'access request').")] string search = null,
        [Description("Get a specific workflow by ID (e.g. 'task-triage', 'health-check', 'bulk-asset-operations').")] string id = null)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var recipe = Recipes.FirstOrDefault(r => r.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
            return recipe is null
                ? $"Workflow '{id}' was not found. Use Safeguard_Workflows with no arguments to list available workflow IDs."
                : recipe.Content;
        }

        var matches = FilterRecipes(search);
        if (matches.Length == 0)
            return $"No workflows matched '{search}'. Use Safeguard_Workflows with no arguments to list available workflow IDs.";

        return BuildListing(matches, search);
    }

    private static WorkflowRecipe[] FilterRecipes(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return Recipes.ToArray();

        var trimmedSearch = search.Trim();
        var terms = trimmedSearch.Split([' ', '\t', '\r', '\n', ',', ';', '/', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Recipes
            .Where(recipe => Matches(recipe, trimmedSearch, terms))
            .ToArray();
    }

    private static bool Matches(WorkflowRecipe recipe, string search, string[] terms)
    {
        var searchableText = $"{recipe.Id}\n{recipe.Name}\n{recipe.Description}\n{string.Join(' ', recipe.Tags)}\n{recipe.Content}";
        if (searchableText.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;

        return terms.All(term => searchableText.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildListing(WorkflowRecipe[] recipes, string search)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.IsNullOrWhiteSpace(search)
            ? $"Available Safeguard workflows ({recipes.Length}):"
            : $"Safeguard workflows matching '{search}' ({recipes.Length}):");
        sb.AppendLine("ID                     Name                                   Description");
        sb.AppendLine("---------------------  -------------------------------------  -----------------------------------------------");

        foreach (var recipe in recipes)
        {
            sb.Append(recipe.Id.PadRight(21))
                .Append("  ")
                .Append(recipe.Name.PadRight(37))
                .Append("  ")
                .AppendLine(recipe.Description);
        }

        sb.AppendLine();
        sb.Append("Use id='<workflow-id>' to get the full recipe.");
        return sb.ToString().TrimEnd();
    }
}
