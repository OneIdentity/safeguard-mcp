// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Events_GetEvents", Title = "Events - GetEvents",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets multiple events.")]
    public Task<string> Events_GetEvents(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Events" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Events_GetEventById", Title = "Events - GetEventById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a particular event.")]
    public Task<string> Events_GetEventById(McpServer server,
        [Description("Unique ID of Event.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Events/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Events_FireTestEvent", Title = "Events - FireTestEvent",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Fires a test event for the purpose of verifying event notification configurations.")]
    public Task<string> Events_FireTestEvent(McpServer server)
        => PostAsync(server, "/v4/Events/FireTestEvent");
}
