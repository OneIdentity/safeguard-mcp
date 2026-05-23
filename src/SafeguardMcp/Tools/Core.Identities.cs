// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Identities_GetIdentities", Title = "Identities - GetIdentities",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of identities.")]
    public Task<string> Identities_GetIdentities(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Identities" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Identities_GetIdentityById", Title = "Identities - GetIdentityById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single identity.")]
    public Task<string> Identities_GetIdentityById(McpServer server,
        [Description("Database ID of identity.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Identities/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Identities_GetIdentityProvider", Title = "Identities - GetIdentityProvider",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the IdentityProvider associated with the identity.")]
    public Task<string> Identities_GetIdentityProvider(McpServer server,
        [Description("Unique identifier of the Identity.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Identities/{Uri.EscapeDataString(id)}/IdentityProvider" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Identities_GetIdentityUser", Title = "Identities - GetIdentityUser",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the user associated with the identity.")]
    public Task<string> Identities_GetIdentityUser(McpServer server,
        [Description("Unique identifier of the Identity.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Identities/{Uri.EscapeDataString(id)}/User" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Identities_GetIdentityUserGroup", Title = "Identities - GetIdentityUserGroup",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the UserGroup associated with the identity.")]
    public Task<string> Identities_GetIdentityUserGroup(McpServer server,
        [Description("Unique identifier of the Identity.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Identities/{Uri.EscapeDataString(id)}/UserGroup" + Q(("fields", fields)));
}
