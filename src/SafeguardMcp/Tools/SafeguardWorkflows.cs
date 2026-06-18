using System.Text;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tools;

/// <summary>
/// Workflow-recipe lookup behind <c>Safeguard_Reference topic=workflows</c>. Formerly the standalone
/// Safeguard_Workflows tool; kept as a helper so the recipe search/listing logic stays in one place.
/// </summary>
internal static class SafeguardWorkflows
{
    private static IReadOnlyList<WorkflowRecipe> Recipes => RecipeIndex.Recipes;

    public static string Lookup(string search, string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var recipe = Recipes.FirstOrDefault(r => r.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
            return recipe is null
                ? $"Workflow '{id}' was not found. Use Safeguard_Reference topic=workflows with no search to list available workflow IDs."
                : recipe.Content;
        }

        var matches = FilterRecipes(search);
        if (matches.Length == 0)
            return $"No workflows matched '{search}'. Use Safeguard_Reference topic=workflows with no search to list available workflow IDs.";

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
