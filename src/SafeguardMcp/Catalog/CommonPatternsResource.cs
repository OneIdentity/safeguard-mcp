using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Catalog;

/// <summary>
/// MCP Resource providing common Safeguard API usage patterns.
/// Helps agents construct correct API calls for frequent operations.
/// </summary>
[McpServerResourceType]
internal sealed class CommonPatternsResource
{
    private static readonly string Content = EmbeddedResources.Load("common-patterns.md");

    private CommonPatternsResource() { }

    [McpServerResource(UriTemplate = "safeguard://common-patterns")]
    [Description("Common Safeguard API patterns — lookup by name, create with dependencies, "
        + "bulk operations, audit queries, and error handling. Preload to write correct API calls faster.")]
    public static string GetCommonPatterns() => Content;
}
