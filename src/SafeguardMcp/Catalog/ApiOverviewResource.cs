using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Catalog;

/// <summary>
/// MCP Resource providing a high-level map of Safeguard API services and their endpoints.
/// Helps agents orient quickly without needing to call Safeguard_Discover first.
/// </summary>
[McpServerResourceType]
internal sealed class ApiOverviewResource
{
    private static readonly string Content = EmbeddedResources.Load("api-overview.md");

    private ApiOverviewResource() { }

    [McpServerResource(UriTemplate = "safeguard://api-overview")]
    [Description("High-level map of Safeguard services, endpoint categories, and key object relationships. "
        + "Preload this to understand the API landscape before navigating specific endpoints.")]
    public static string GetApiOverview() => Content;
}
