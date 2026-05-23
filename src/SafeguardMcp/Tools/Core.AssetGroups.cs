// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_AssetGroups_GetAssetGroups", Title = "AssetGroups - GetAssetGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of asset group entities.")]
    public Task<string> AssetGroups_GetAssetGroups(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetGroups_CreateAssetGroup", Title = "AssetGroups - CreateAssetGroup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates an AssetGroup.")]
    public Task<string> AssetGroups_CreateAssetGroup(McpServer server,
        [Description("AssetGroup to create.")] string body = null)
        => PostAsync(server, "/v4/AssetGroups", body);

    [McpServerTool(Name = "Core_AssetGroups_GetAssetGroupById", Title = "AssetGroups - GetAssetGroupById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single asset group.")]
    public Task<string> AssetGroups_GetAssetGroupById(McpServer server,
        [Description("Unique ID of asset group.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetGroups/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetGroups_UpdateAssetGroup", Title = "AssetGroups - UpdateAssetGroup",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an AssetGroup.")]
    public Task<string> AssetGroups_UpdateAssetGroup(McpServer server,
        [Description("Unique identifier of the AssetGroup.")] string id,
        [Description("Updated AssetGroup.")] string body)
        => PutAsync(server, $"/v4/AssetGroups/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_AssetGroups_Delete", Title = "AssetGroups - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an AssetGroup.")]
    public Task<string> AssetGroups_Delete(McpServer server,
        [Description("Unique identifier of the AssetGroup.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/AssetGroups/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_AssetGroups_GetAssets", Title = "AssetGroups - GetAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all PolicyAssets that belong to an AssetGroup.")]
    public Task<string> AssetGroups_GetAssets(McpServer server,
        [Description("Unique identifier of the AssetGroup.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetGroups/{Uri.EscapeDataString(id)}/Assets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetGroups_SetAssets", Title = "AssetGroups - SetAssets",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the assets assigned to this group.")]
    public Task<string> AssetGroups_SetAssets(McpServer server,
        [Description("Unique identifier of the AssetGroup.")] string id,
        [Description("Assets to assign to the AssetGroup.")] string body)
        => PutAsync(server, $"/v4/AssetGroups/{Uri.EscapeDataString(id)}/Assets", body);

    [McpServerTool(Name = "Core_AssetGroups_ModifyAssets", Title = "AssetGroups - ModifyAssets",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove assets assigned to this group.")]
    public Task<string> AssetGroups_ModifyAssets(McpServer server,
        [Description("Unique identifier of the AssetGroup.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Assets to assign to the AssetGroup.")] string body = null)
        => PostAsync(server, $"/v4/AssetGroups/{Uri.EscapeDataString(id)}/Assets/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetGroups_GetPolicies", Title = "AssetGroups - GetPolicies",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets information about policies that this asset group is assigned to.")]
    public Task<string> AssetGroups_GetPolicies(McpServer server,
        [Description("Unique identifier of the AssetGroup.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetGroups/{Uri.EscapeDataString(id)}/Policies" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetGroups_SetPolicies", Title = "AssetGroups - SetPolicies",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the policies this group is assigned to.")]
    public Task<string> AssetGroups_SetPolicies(McpServer server,
        [Description("Unique identifier of the AssetGroup to update.")] string id,
        [Description("Policies to assign the AssetGroup to.")] string body)
        => PutAsync(server, $"/v4/AssetGroups/{Uri.EscapeDataString(id)}/Policies", body);

    [McpServerTool(Name = "Core_AssetGroups_ModifyPolicies", Title = "AssetGroups - ModifyPolicies",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Sets the policies this group is assigned to.")]
    public Task<string> AssetGroups_ModifyPolicies(McpServer server,
        [Description("Unique identifier of the AssetGroup to update.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Policies to assign the AssetGroup to.")] string body = null)
        => PostAsync(server, $"/v4/AssetGroups/{Uri.EscapeDataString(id)}/Policies/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetGroups_CreateMultiple", Title = "AssetGroups - CreateMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple new asset groups.")]
    public Task<string> AssetGroups_CreateMultiple(McpServer server,
        [Description("New asset groups to process.")] string body = null)
        => PostAsync(server, "/v4/AssetGroups/BatchCreate", body);

    [McpServerTool(Name = "Core_AssetGroups_DeleteMultiple", Title = "AssetGroups - DeleteMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple asset groups to delete.")]
    public Task<string> AssetGroups_DeleteMultiple(McpServer server,
        [Description("asset groups to process.")] string body = null,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => PostAsync(server, "/v4/AssetGroups/BatchDelete" + Q(("forceDelete", forceDelete)), body);

    [McpServerTool(Name = "Core_AssetGroups_UpdateMultiple", Title = "AssetGroups - UpdateMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple asset groups to update.")]
    public Task<string> AssetGroups_UpdateMultiple(McpServer server,
        [Description("asset groups to process.")] string body = null)
        => PostAsync(server, "/v4/AssetGroups/BatchUpdate", body);

    [McpServerTool(Name = "Core_AssetGroups_TestRule", Title = "AssetGroups - TestRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests a dynamic grouping rule.")]
    public Task<string> AssetGroups_TestRule(McpServer server,
        [Description("Dynamic grouping rule to test.")] string body = null,
        [Description("Unique identifier of the asset group.")] string id = null,
        [Description("Do not return no-op results.")] string operationalOnly = null,
        [Description("Items per page.")] string limit = null)
        => PostAsync(server, "/v4/AssetGroups/TestAssetRule" + Q(("id", id), ("operationalOnly", operationalOnly), ("limit", limit)), body);
}
