using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Catalog;

/// <summary>
/// MCP Resource providing comprehensive Safeguard API query syntax reference.
/// Clients can preload this into context to avoid repeated Safeguard_Reference topic=query-syntax calls.
/// </summary>
[McpServerResourceType]
internal sealed class QuerySyntaxResource
{
    private static readonly string Content = EmbeddedResources.Load("query-syntax.md");

    private QuerySyntaxResource() { }

    [McpServerResource(UriTemplate = "safeguard://query-syntax")]
    [Description("Complete Safeguard API query syntax reference — filter operators, field selection, "
        + "ordering, pagination, and search. Preload this to write correct query parameters without tool calls.")]
    public static string GetQuerySyntax() => Content;
}
