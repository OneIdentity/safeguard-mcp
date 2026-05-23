// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_StarlingRegisteredConnector_GetStarlingRegisteredConnectors", Title = "StarlingRegisteredConnector - GetStarlingRegisteredConnec...",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get a list of Starling registered connectors from safeguard. Deprecated: Use /v4/Starling/RegisteredConnectors instead.")]
    public Task<string> StarlingRegisteredConnector_GetStarlingRegisteredConnectors(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/StarlingRegisteredConnector" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_StarlingRegisteredConnector_CreateStarlingRegisteredConnectors", Title = "StarlingRegisteredConnector - CreateStarlingRegisteredCon...",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Create a Starling registered connector. Deprecated: Use /v4/Starling/RegisteredConnectors instead.")]
    public Task<string> StarlingRegisteredConnector_CreateStarlingRegisteredConnectors(McpServer server,
        [Description("Information needed by Safeguard in order to use a Starling connector as a platform type.")] string body = null)
        => PostAsync(server, "/v4/StarlingRegisteredConnector", body);

    [McpServerTool(Name = "Core_StarlingRegisteredConnector_GetStarlingRegisteredConnector", Title = "StarlingRegisteredConnector - GetStarlingRegisteredConnector",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get a Starling registered connector from safeguard. Deprecated: Use /v4/Starling/RegisteredConnectors instead.")]
    public Task<string> StarlingRegisteredConnector_GetStarlingRegisteredConnector(McpServer server,
        [Description("The unique id of the Starling connector.")] string id)
        => GetAsync(server, $"/v4/StarlingRegisteredConnector/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_StarlingRegisteredConnector_UpdateStarlingRegisteredConnectors", Title = "StarlingRegisteredConnector - UpdateStarlingRegisteredCon...",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update a Starling registered connector. Deprecated: Use /v4/Starling/RegisteredConnectors instead.")]
    public Task<string> StarlingRegisteredConnector_UpdateStarlingRegisteredConnectors(McpServer server,
        [Description("The unique id of the Starling connector.")] string id,
        [Description("The updated information needed by Safeguard in order to use a Starling connector as a platform type.")] string body)
        => PutAsync(server, $"/v4/StarlingRegisteredConnector/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_StarlingRegisteredConnector_DeleteStarlingRegisteredConnectors", Title = "StarlingRegisteredConnector - DeleteStarlingRegisteredCon...",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Delete a starling registered connector. Deprecated: Use /v4/Starling/RegisteredConnectors instead.")]
    public Task<string> StarlingRegisteredConnector_DeleteStarlingRegisteredConnectors(McpServer server,
        [Description("The unique id of the Starling connector to delete.")] string id)
        => DeleteAsync(server, $"/v4/StarlingRegisteredConnector/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_StarlingRegisteredConnector_GetRegisteredConnectorsFromStarling", Title = "StarlingRegisteredConnector - GetRegisteredConnectorsFrom...",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get a list of registered connectors from starling. Deprecated: Use /v4/Starling/RegisteredConnectors instead.")]
    public Task<string> StarlingRegisteredConnector_GetRegisteredConnectorsFromStarling(McpServer server)
        => GetAsync(server, "/v4/StarlingRegisteredConnector/FromStarling");
}
