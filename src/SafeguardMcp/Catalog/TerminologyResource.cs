using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Exposes the Safeguard terminology map as an MCP resource that AI agents can read
/// for context about product-to-API naming differences.
/// </summary>
[McpServerResourceType]
internal sealed class TerminologyResource
{
    private static readonly string Content = EmbeddedResources.Load("terminology.md");

    private TerminologyResource() { }

    [McpServerResource(UriTemplate = "safeguard://terminology")]
    [Description("Safeguard product terminology to API terminology mapping. "
        + "Read this to understand how Safeguard UI/documentation terms map to REST API endpoint names. "
        + "For example, what the product calls 'Entitlements' is the /v4/Roles endpoint in the API.")]
    public static string GetTerminologyMap() => Content;
}
