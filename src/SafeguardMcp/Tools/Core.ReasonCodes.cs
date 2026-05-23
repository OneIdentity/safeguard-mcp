// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_ReasonCodes_Get", Title = "ReasonCodes - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of reason codes.")]
    public Task<string> ReasonCodes_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/ReasonCodes" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_ReasonCodes_CreateEntity", Title = "ReasonCodes - CreateEntity",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new reason code.")]
    public Task<string> ReasonCodes_CreateEntity(McpServer server,
        [Description("ReasonCode to create.")] string body = null)
        => PostAsync(server, "/v4/ReasonCodes", body);

    [McpServerTool(Name = "Core_ReasonCodes_GetById", Title = "ReasonCodes - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a reason code.")]
    public Task<string> ReasonCodes_GetById(McpServer server,
        [Description("Unique ID of ReasonCode.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/ReasonCodes/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_ReasonCodes_UpdateEntity", Title = "ReasonCodes - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing application reason code.")]
    public Task<string> ReasonCodes_UpdateEntity(McpServer server,
        [Description("Unique identifier of the ReasonCode.")] string id,
        [Description("Updated ReasonCode.")] string body)
        => PutAsync(server, $"/v4/ReasonCodes/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_ReasonCodes_Delete", Title = "ReasonCodes - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a reason code.")]
    public Task<string> ReasonCodes_Delete(McpServer server,
        [Description("Unique identifier of the ReasonCode.")] string id)
        => DeleteAsync(server, $"/v4/ReasonCodes/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_ReasonCodes_CheckUniqueName", Title = "ReasonCodes - CheckUniqueName",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if the current name is unique prior to create/update.")]
    public Task<string> ReasonCodes_CheckUniqueName(McpServer server,
        [Description("Parameters for checking for unique name.")] string body = null)
        => PostAsync(server, "/v4/ReasonCodes/CheckUniqueName", body);
}
