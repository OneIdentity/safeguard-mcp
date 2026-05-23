// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Settings_Get", Title = "Settings - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all of the application's settings.")]
    public Task<string> Settings_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Settings" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Settings_GetById", Title = "Settings - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an application setting.")]
    public Task<string> Settings_GetById(McpServer server,
        [Description("Unique ID of Setting.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Settings/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Settings_UpdateEntity", Title = "Settings - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a setting's value.")]
    public Task<string> Settings_UpdateEntity(McpServer server,
        [Description("Unique identifier of the Setting to update.")] string id,
        [Description("Updated Setting.")] string body)
        => PutAsync(server, $"/v4/Settings/{Uri.EscapeDataString(id)}", body);
}
