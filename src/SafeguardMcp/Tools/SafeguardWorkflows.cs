using System.ComponentModel;
using System.Reflection;
using System.Text;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

[McpServerToolType]
public class SafeguardWorkflows
{
    private static readonly WorkflowRecipe[] Recipes = LoadRecipes();

    [McpServerTool(Name = "Safeguard_Workflows", Title = "Safeguard Workflow Recipes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Get step-by-step workflow recipes for common Safeguard operations. "
        + "Each recipe describes the sequence of API calls needed to accomplish a goal. "
        + "Use this when you need to perform a multi-step operation and want guidance on the correct approach.")]
    public string Safeguard_Workflows(
        [Description("Search for workflows by keyword (e.g. 'health', 'password failure', 'bulk import', 'access request').")] string search = null,
        [Description("Get a specific workflow by ID (e.g. 'task-triage', 'health-check', 'bulk-assets').")] string id = null)
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

    private static WorkflowRecipe[] LoadRecipes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string prefix = "SafeguardMcp.Workflows.";
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var recipes = new List<WorkflowRecipe>();
        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                continue;

            using var reader = new StreamReader(stream);
            var raw = reader.ReadToEnd();
            var recipe = ParseRecipe(raw);
            if (recipe is not null)
                recipes.Add(recipe);
        }

        return recipes.ToArray();
    }

    private static WorkflowRecipe ParseRecipe(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var normalized = raw.Replace("\r\n", "\n").Replace("\r", "\n").TrimStart('\uFEFF');
        var lines = normalized.Split('\n');
        if (lines.Length < 3 || !string.Equals(lines[0].Trim(), "---", StringComparison.Ordinal))
            return null;

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var contentStartIndex = -1;

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.Equals(line, "---", StringComparison.Ordinal))
            {
                contentStartIndex = i + 1;
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            metadata[key] = value;
        }

        if (contentStartIndex < 0
            || !metadata.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id)
            || !metadata.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name)
            || !metadata.TryGetValue("description", out var description) || string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        metadata.TryGetValue("tags", out var tagsRaw);
        var tags = string.IsNullOrWhiteSpace(tagsRaw)
            ? []
            : tagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var content = string.Join('\n', lines.Skip(contentStartIndex)).Trim();
        return new WorkflowRecipe(id, name, description, tags, content);
    }

    private static WorkflowRecipe[] FilterRecipes(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return Recipes;

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

    private record WorkflowRecipe(string Id, string Name, string Description, string[] Tags, string Content);
}
