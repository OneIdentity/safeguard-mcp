// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_PolicyAssets_Get", Title = "PolicyAssets - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of assets that can be assigned to a policy.")]
    public Task<string> PolicyAssets_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/PolicyAssets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_PolicyAssets_GetById", Title = "PolicyAssets - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a policy asset.")]
    public Task<string> PolicyAssets_GetById(McpServer server,
        [Description("Unique ID of PolicyAsset.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/PolicyAssets/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_PolicyAssets_GetAssetGroups", Title = "PolicyAssets - GetAssetGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all asset groups that a specific asset belongs to.")]
    public Task<string> PolicyAssets_GetAssetGroups(McpServer server,
        [Description("Unique identifier of the PolicyAsset.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/PolicyAssets/{Uri.EscapeDataString(id)}/AssetGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_PolicyAssets_GetEntries", Title = "PolicyAssets - GetEntries",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Searches the specified directory.")]
    public Task<string> PolicyAssets_GetEntries(McpServer server,
        [Description("Unique ID of an directory asset.")] string id,
        [Description("Sets the searchBase for the Ldap query, defaults to base of the domain for ldap, or base of forest for AD. Must be in DN Syntax.")] string searchBase = null,
        [Description("Defines the scope of the query, either base, one, or sub, defaults to sub.")] string searchScope = null,
        [Description("Either User, Group, or Computer. Defaults to User.")] string searchType = null,
        [Description("Sets a search constraint on the \"name\" of the object to return.")] string searchName = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/PolicyAssets/{Uri.EscapeDataString(id)}/DirectoryServiceEntries" + Q(("searchBase", searchBase), ("searchScope", searchScope), ("searchType", searchType), ("searchName", searchName), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_PolicyAssets_GetPolicies", Title = "PolicyAssets - GetPolicies",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the policies that manage an asset belongs to.")]
    public Task<string> PolicyAssets_GetPolicies(McpServer server,
        [Description("Unique identifier of the PolicyAsset.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/PolicyAssets/{Uri.EscapeDataString(id)}/Policies" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));
}
