using System.Reflection;

namespace SafeguardMcp.Catalog;

/// <summary>
/// One workflow recipe loaded from the embedded markdown files under Workflows/.
/// Recipes have YAML-ish front matter (id, name, description, tags) followed by
/// human-readable content describing a multi-step Safeguard operation.
/// </summary>
internal sealed record WorkflowRecipe(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    string Content,
    string Tool = null);

/// <summary>
/// A recipe that scored above zero against a set of expanded search terms.
/// <see cref="Strong"/> is true when the match came from the recipe's
/// <c>id</c> or one of its <c>tags</c>, as opposed to a description-only
/// substring hit. Strong matches drive the "Strong recipe match" callout
/// in Safeguard_Discover.
/// </summary>
internal sealed record RecipeMatch(WorkflowRecipe Recipe, int Score, bool Strong);

/// <summary>
/// Shared, lazily-loaded index of workflow recipes. Both Safeguard_Reference topic=workflows
/// and Safeguard_Discover read from this so the recipe set is loaded once
/// per process. Scoring is term-driven (operates on the same expanded term
/// list that ScoreMatch uses for endpoints) so the two views stay in sync.
/// </summary>
internal static class RecipeIndex
{
    public static IReadOnlyList<WorkflowRecipe> Recipes { get; } = Load();

    public static IReadOnlyList<RecipeMatch> Score(IReadOnlyList<string> expandedTerms)
    {
        if (expandedTerms == null || expandedTerms.Count == 0)
            return Array.Empty<RecipeMatch>();

        var matches = new List<RecipeMatch>();
        foreach (var recipe in Recipes)
        {
            int score = 0;
            bool strong = false;

            foreach (var term in expandedTerms)
            {
                if (string.IsNullOrWhiteSpace(term))
                    continue;

                // Exact id match beats everything (rare but unambiguous).
                if (recipe.Id.Equals(term, StringComparison.OrdinalIgnoreCase))
                {
                    if (score < 5) score = 5;
                    strong = true;
                    continue;
                }

                if (recipe.Id.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    if (score < 3) score = 3;
                    strong = true;
                }

                // Tag scoring: exact equality (case-insensitive) marks the
                // match as strong; a substring hit still scores but doesn't
                // promote to strong (otherwise a search for "move" matches a
                // recipe tagged "removed" via plain substring containment,
                // which is noise).
                foreach (var tag in recipe.Tags)
                {
                    if (tag.Equals(term, StringComparison.OrdinalIgnoreCase))
                    {
                        if (score < 4) score = 4;
                        strong = true;
                        break;
                    }

                    if (tag.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        if (score < 2) score = 2;
                    }
                }

                if (recipe.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    if (score < 2) score = 2;
                }

                if (recipe.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    if (score < 1) score = 1;
                }
            }

            if (score > 0)
                matches.Add(new RecipeMatch(recipe, score, strong));
        }

        matches.Sort((a, b) => b.Score != a.Score
            ? b.Score.CompareTo(a.Score)
            : string.Compare(a.Recipe.Id, b.Recipe.Id, StringComparison.Ordinal));

        return matches;
    }

    private static WorkflowRecipe[] Load()
    {
        var assembly = typeof(RecipeIndex).Assembly;
        const string prefix = "SafeguardMcp.Workflows.";
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal)
                && name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
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
            // `tool:` is permitted to appear multiple times to declare a
            // recipe backed by more than one composite tool; accumulate
            // into a single comma-joined value so downstream rendering
            // can split as needed.
            if (string.Equals(key, "tool", StringComparison.OrdinalIgnoreCase)
                && metadata.TryGetValue(key, out var existing)
                && !string.IsNullOrWhiteSpace(existing))
            {
                metadata[key] = existing + ", " + value;
            }
            else
            {
                metadata[key] = value;
            }
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
            ? Array.Empty<string>()
            : tagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        metadata.TryGetValue("tool", out var toolRaw);
        var tool = string.IsNullOrWhiteSpace(toolRaw) ? null : toolRaw.Trim();

        var content = string.Join('\n', lines.Skip(contentStartIndex)).Trim();
        return new WorkflowRecipe(id, name, description, tags, content, tool);
    }
}
