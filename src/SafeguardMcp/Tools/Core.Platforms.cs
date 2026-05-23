// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Platforms_GetPlatforms", Title = "Platforms - GetPlatforms",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of platforms.")]
    public Task<string> Platforms_GetPlatforms(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Platforms" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Platforms_CreatePlatform", Title = "Platforms - CreatePlatform",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Create a starling connect or custom platform.")]
    public Task<string> Platforms_CreatePlatform(McpServer server,
        [Description("Platform to create.")] string body = null)
        => PostAsync(server, "/v4/Platforms", body);

    [McpServerTool(Name = "Core_Platforms_GetPlatformById", Title = "Platforms - GetPlatformById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific platform.")]
    public Task<string> Platforms_GetPlatformById(McpServer server,
        [Description("Unique ID of Platform.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Platforms/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Platforms_UpdatePlatform", Title = "Platforms - UpdatePlatform",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a custom platform.")]
    public Task<string> Platforms_UpdatePlatform(McpServer server,
        [Description("Unique identifier of the Platform.")] string id,
        [Description("Updated Platform.")] string body)
        => PutAsync(server, $"/v4/Platforms/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Platforms_DeletePlatform", Title = "Platforms - DeletePlatform",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a Platform.")]
    public Task<string> Platforms_DeletePlatform(McpServer server,
        [Description("Unique identifier of the Platform.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/Platforms/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_Platforms_GetPlatformScript", Title = "Platforms - GetPlatformScript",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the script associated with a custom platform.")]
    public Task<string> Platforms_GetPlatformScript(McpServer server,
        [Description("Unique ID of Platform.")] string id)
        => GetAsync(server, $"/v4/Platforms/{Uri.EscapeDataString(id)}/Script");

    [McpServerTool(Name = "Core_Platforms_PutScript", Title = "Platforms - PutScript",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates script for the custom platform in base64 format.")]
    public Task<string> Platforms_PutScript(McpServer server,
        [Description("Unique ID of Platform.")] string id,
        [Description("Updated base64 platform script.")] string body)
        => PutAsync(server, $"/v4/Platforms/{Uri.EscapeDataString(id)}/Script", body);

    [McpServerTool(Name = "Core_Platforms_DeleteScript", Title = "Platforms - DeleteScript",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes script for the custom platform.")]
    public Task<string> Platforms_DeleteScript(McpServer server,
        [Description("Unique ID of Platform.")] string id)
        => DeleteAsync(server, $"/v4/Platforms/{Uri.EscapeDataString(id)}/Script");

    [McpServerTool(Name = "Core_Platforms_GetPlatformScriptRaw", Title = "Platforms - GetPlatformScriptRaw",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the script associated with a custom platform in raw format.")]
    public Task<string> Platforms_GetPlatformScriptRaw(McpServer server,
        [Description("Unique ID of Platform.")] string id)
        => GetAsync(server, $"/v4/Platforms/{Uri.EscapeDataString(id)}/Script/Raw");

    [McpServerTool(Name = "Core_Platforms_ValidateScript", Title = "Platforms - ValidateScript",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Validates script for the custom platform in base64 format.")]
    public Task<string> Platforms_ValidateScript(McpServer server,
        [Description("Updated base64 platform script.")] string body = null)
        => PostAsync(server, "/v4/Platforms/ValidateScript", body);
}
