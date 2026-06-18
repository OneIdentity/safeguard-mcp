using System.Text;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Parses embedded markdown into level-2 (<c>## </c>) sections so reference lookups can return a
/// single heading-addressed section instead of the whole document. Reads the same embedded markdown
/// the MCP resources expose, so tool output and <c>safeguard://*</c> resources never drift.
/// </summary>
internal static class MarkdownSections
{
    private static readonly Dictionary<string, string> Cache = new(StringComparer.Ordinal);
    private static readonly Lock Gate = new();

    private static string Load(string fileName)
    {
        lock (Gate)
        {
            if (!Cache.TryGetValue(fileName, out var content))
            {
                content = EmbeddedResources.Load(fileName);
                Cache[fileName] = content;
            }
            return content;
        }
    }

    /// <summary>
    /// Returns the table of contents when <paramref name="search"/> is empty, otherwise the
    /// <c>## </c> section(s) whose heading or body match the search terms.
    /// </summary>
    public static string Select(string fileName, string topicLabel, string search)
    {
        var sections = Parse(Load(fileName));

        if (string.IsNullOrWhiteSpace(search))
            return BuildToc(sections, topicLabel);

        var trimmed = search.Trim();
        var terms = trimmed.Split([' ', '\t', '\r', '\n', ',', ';', '/', '-', '_'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var matches = sections.Where(s => Matches(s, trimmed, terms)).ToArray();
        if (matches.Length == 0)
            return $"No {topicLabel} section matched '{search}'.\n\n" + BuildToc(sections, topicLabel);

        var sb = new StringBuilder();
        foreach (var section in matches)
            sb.AppendLine(section.Text.TrimEnd()).AppendLine();
        return sb.ToString().TrimEnd();
    }

    private static bool Matches(Section section, string search, string[] terms)
    {
        var text = section.Heading + "\n" + section.Text;
        if (text.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;
        return terms.Length > 0 && terms.All(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildToc(IReadOnlyList<Section> sections, string topicLabel)
    {
        var sb = new StringBuilder();
        sb.Append(topicLabel)
            .AppendLine(" sections — call Safeguard_Reference with search=\"<keyword>\" to get one:");
        foreach (var section in sections)
        {
            if (!section.IsIntro)
                sb.Append("  - ").AppendLine(section.Heading);
        }
        return sb.ToString().TrimEnd();
    }

    private static List<Section> Parse(string markdown)
    {
        var sections = new List<Section>();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var current = new StringBuilder();
        var heading = "(intro)";
        var isIntro = true;

        foreach (var line in lines)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (current.Length > 0)
                    sections.Add(new Section(heading, current.ToString().TrimEnd(), isIntro));
                current.Clear();
                heading = line[3..].Trim();
                isIntro = false;
            }
            current.AppendLine(line);
        }
        if (current.Length > 0)
            sections.Add(new Section(heading, current.ToString().TrimEnd(), isIntro));
        return sections;
    }

    private readonly struct Section(string heading, string text, bool isIntro)
    {
        public string Heading { get; } = heading;
        public string Text { get; } = text;
        public bool IsIntro { get; } = isIntro;
    }
}
