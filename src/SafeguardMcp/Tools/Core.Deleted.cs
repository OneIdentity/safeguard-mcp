// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Deleted_GetSubUrls", Title = "Deleted - GetSubUrls",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of delete types.")]
    public Task<string> Deleted_GetSubUrls(McpServer server)
        => GetAsync(server, "/v4/Deleted");

    [McpServerTool(Name = "Core_Deleted_GetDeletedAssetAccounts", Title = "Deleted - GetDeletedAssetAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of deleted asset accounts.")]
    public Task<string> Deleted_GetDeletedAssetAccounts(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Deleted/AssetAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Deleted_GetDeletedAssetAccountById", Title = "Deleted - GetDeletedAssetAccountById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single deleted asset account entity.")]
    public Task<string> Deleted_GetDeletedAssetAccountById(McpServer server,
        [Description("Unique ID of an asset account.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Deleted/AssetAccounts/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Deleted_PurgeDeletedAssetAccount", Title = "Deleted - PurgeDeletedAssetAccount",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Purge a single deleted asset account entity. It will no longer be recoverable.")]
    public Task<string> Deleted_PurgeDeletedAssetAccount(McpServer server,
        [Description("Unique ID of a account.")] string id)
        => DeleteAsync(server, $"/v4/Deleted/AssetAccounts/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Deleted_RestoreDeletedAssetAccount", Title = "Deleted - RestoreDeletedAssetAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Restore a single deleted asset account entity.")]
    public Task<string> Deleted_RestoreDeletedAssetAccount(McpServer server,
        [Description("Unique ID of an asset account.")] string id,
        [Description("Asset account to restore.")] string body = null)
        => PostAsync(server, $"/v4/Deleted/AssetAccounts/{Uri.EscapeDataString(id)}/Restore", body);

    [McpServerTool(Name = "Core_Deleted_GetDeletedAssets", Title = "Deleted - GetDeletedAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of deleted assets.")]
    public Task<string> Deleted_GetDeletedAssets(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Deleted/Assets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Deleted_GetDeletedAssetById", Title = "Deleted - GetDeletedAssetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single deleted asset entity.")]
    public Task<string> Deleted_GetDeletedAssetById(McpServer server,
        [Description("Unique ID of an Asset.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Deleted/Assets/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Deleted_PurgeDeletedAsset", Title = "Deleted - PurgeDeletedAsset",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Purge a single deleted asset entity. It will no longer be recoverable.")]
    public Task<string> Deleted_PurgeDeletedAsset(McpServer server,
        [Description("Unique ID of a asset.")] string id)
        => DeleteAsync(server, $"/v4/Deleted/Assets/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Deleted_RestoreDeletedAsset", Title = "Deleted - RestoreDeletedAsset",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Restore a single deleted asset entity.")]
    public Task<string> Deleted_RestoreDeletedAsset(McpServer server,
        [Description("Unique ID of an Asset.")] string id,
        [Description("Asset to restore.")] string body = null)
        => PostAsync(server, $"/v4/Deleted/Assets/{Uri.EscapeDataString(id)}/Restore", body);

    [McpServerTool(Name = "Core_Deleted_GetPurgeSettings", Title = "Deleted - GetPurgeSettings",
        ReadOnly = true, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Gets the current purge settings.")]
    public Task<string> Deleted_GetPurgeSettings(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Deleted/PurgeSettings" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Deleted_UpdatePurgeSettings", Title = "Deleted - UpdatePurgeSettings",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Updates the purge settings.")]
    public Task<string> Deleted_UpdatePurgeSettings(McpServer server,
        [Description("Represents setting governing how long to retain deleted entities.")] string body)
        => PutAsync(server, "/v4/Deleted/PurgeSettings", body);

    [McpServerTool(Name = "Core_Deleted_GetDeletedUsers", Title = "Deleted - GetDeletedUsers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of deleted users.")]
    public Task<string> Deleted_GetDeletedUsers(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Deleted/Users" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Deleted_GetDeletedUserById", Title = "Deleted - GetDeletedUserById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single deleted user entity.")]
    public Task<string> Deleted_GetDeletedUserById(McpServer server,
        [Description("Unique ID of a user.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Deleted/Users/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Deleted_PurgeDeletedUser", Title = "Deleted - PurgeDeletedUser",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Purge a single deleted user entity. It will no longer be recoverable.")]
    public Task<string> Deleted_PurgeDeletedUser(McpServer server,
        [Description("Unique ID of a user.")] string id)
        => DeleteAsync(server, $"/v4/Deleted/Users/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Deleted_RestoreDeletedUser", Title = "Deleted - RestoreDeletedUser",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Restore a single deleted user entity.")]
    public Task<string> Deleted_RestoreDeletedUser(McpServer server,
        [Description("Unique ID of a user.")] string id,
        [Description("User to restore.")] string body = null)
        => PostAsync(server, $"/v4/Deleted/Users/{Uri.EscapeDataString(id)}/Restore", body);
}
