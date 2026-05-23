// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Assets_GetAllAssets", Title = "Assets - GetAllAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of assets across all accessible partitions.")]
    public Task<string> Assets_GetAllAssets(McpServer server,
        [Description("List of comma-separated tag IDs by which to filter results. Preferred over using filter.")] string tagNames = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Assets" + Q(("tagNames", tagNames), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_CreateAsset", Title = "Assets - CreateAsset",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new Asset.")]
    public Task<string> Assets_CreateAsset(McpServer server,
        [Description("Asset to create.")] string body = null)
        => PostAsync(server, "/v4/Assets", body);

    [McpServerTool(Name = "Core_Assets_GetAssetById", Title = "Assets - GetAssetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single Asset entity.")]
    public Task<string> Assets_GetAssetById(McpServer server,
        [Description("Unique ID of an Asset.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Assets_UpdateAsset", Title = "Assets - UpdateAsset",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an Asset.")]
    public Task<string> Assets_UpdateAsset(McpServer server,
        [Description("Unique identifier of the Asset to update.")] string id,
        [Description("Updated Asset.")] string body)
        => PutAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Assets_Delete", Title = "Assets - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an Asset.")]
    public Task<string> Assets_Delete(McpServer server,
        [Description("Unique identifier of the Asset to remove.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_Assets_GetAssetAccounts", Title = "Assets - GetAssetAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets list of accounts that belong to this Asset.")]
    public Task<string> Assets_GetAssetAccounts(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_CheckAccountName", Title = "Assets - CheckAccountName",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if the current account name is unique for this asset prior to create/update.")]
    public Task<string> Assets_CheckAccountName(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Parameters for checking for unique name.")] string body = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/CheckUniqueAccountName", body);

    [McpServerTool(Name = "Core_Assets_GetAssetDependentAccounts", Title = "Assets - GetAssetDependentAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets list of directory accounts that this asset is dependent on.")]
    public Task<string> Assets_GetAssetDependentAccounts(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DependentAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_SetAssetDependentAccounts", Title = "Assets - SetAssetDependentAccounts",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the set of dependent accounts running services on this asset.")]
    public Task<string> Assets_SetAssetDependentAccounts(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("List of dependent accounts to assign to this asset.")] string body)
        => PutAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DependentAccounts", body);

    [McpServerTool(Name = "Core_Assets_ModifyAssetDependentAccounts", Title = "Assets - ModifyAssetDependentAccounts",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Add/Remove dependent accounts running services on this asset.")]
    public Task<string> Assets_ModifyAssetDependentAccounts(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("List of dependent accounts to assign to this asset.")] string body)
        => PutAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DependentAccounts/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Assets_SearchDirectoryAccounts", Title = "Assets - SearchDirectoryAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Searches the specified directory for User objects as DirectoryAccounts.")]
    public Task<string> Assets_SearchDirectoryAccounts(McpServer server,
        [Description("Unique ID of an Asset.")] string id,
        [Description("Sets the searchBase for the Ldap query, defaults to base of the domain for ldap, or base of forest for AD. Must be in DN Syntax.")] string searchBase = null,
        [Description("Defines the scope of the query, either base, one, or sub, defaults to sub.")] string searchScope = null,
        [Description("Sets a search constraint on the \"name\" of the object to return.")] string searchName = null,
        [Description("Whether to look up directory group information.")] string includeGroups = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DirectoryAccounts" + Q(("searchBase", searchBase), ("searchScope", searchScope), ("searchName", searchName), ("includeGroups", includeGroups), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_SearchDirectoryAssets", Title = "Assets - SearchDirectoryAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Searches the specified directory for Computer objects as Assets.")]
    public Task<string> Assets_SearchDirectoryAssets(McpServer server,
        [Description("Unique ID of an Asset.")] string id,
        [Description("Sets the searchBase for the Ldap query, defaults to base of the domain for ldap, or base of forest for AD. Must be in DN Syntax.")] string searchBase = null,
        [Description("Defines the scope of the query, either base, one, or sub, defaults to sub.")] string searchScope = null,
        [Description("Sets a search constraint on the \"name\" of the object to return.")] string searchName = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DirectoryAssets" + Q(("searchBase", searchBase), ("searchScope", searchScope), ("searchName", searchName), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_GetEntries", Title = "Assets - GetEntries",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Searches the specified directory.")]
    public Task<string> Assets_GetEntries(McpServer server,
        [Description("Unique ID of an Asset.")] string id,
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
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DirectoryServiceEntries" + Q(("searchBase", searchBase), ("searchScope", searchScope), ("searchType", searchType), ("searchName", searchName), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_DisableAsset", Title = "Assets - DisableAsset",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Disable asset and its accounts from automated platform tasks.")]
    public Task<string> Assets_DisableAsset(McpServer server,
        [Description("Unique identifier of the Asset.")] string id)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/Disable");

    [McpServerTool(Name = "Core_Assets_RunDiscovery", Title = "Assets - RunDiscovery",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Runs Discovery on the given asset.")]
    public Task<string> Assets_RunDiscovery(McpServer server,
        [Description("id of the asset to run discovery on.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DiscoverAccounts" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_Assets_GetDiscoveredAccounts", Title = "Assets - GetDiscoveredAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an asset's discovered accounts.")]
    public Task<string> Assets_GetDiscoveredAccounts(McpServer server,
        [Description("Unique ID of an Asset.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DiscoveredAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_GetDiscoveredAccount", Title = "Assets - GetDiscoveredAccount",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a discovered account.")]
    public Task<string> Assets_GetDiscoveredAccount(McpServer server,
        [Description("Unique ID of a asset.")] string id,
        [Description("Name of a discovered account. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string accountKey,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DiscoveredAccounts/{Uri.EscapeDataString(accountKey)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Assets_GetDiscoveredServices", Title = "Assets - GetDiscoveredServices",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an asset's discovered services.")]
    public Task<string> Assets_GetDiscoveredServices(McpServer server,
        [Description("Unique ID of an Asset.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DiscoveredServices" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_RunServiceDiscovery", Title = "Assets - RunServiceDiscovery",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Runs Service Discovery on the given asset.")]
    public Task<string> Assets_RunServiceDiscovery(McpServer server,
        [Description("id of the asset to run service discovery on.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DiscoverServices" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_Assets_DiscoverSshHostKeyById", Title = "Assets - DiscoverSshHostKeyById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests the configured connection to this asset.")]
    public Task<string> Assets_DiscoverSshHostKeyById(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Optionally override asset connection settings.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/DiscoverSshHostKey" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_Assets_GetAssetEffectiveManagedBy", Title = "Assets - GetAssetEffectiveManagedBy",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all effective owners of the specified asset.")]
    public Task<string> Assets_GetAssetEffectiveManagedBy(McpServer server,
        [Description("Unique identifier of the asset.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/EffectiveManagedBy" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_TestNetworkAddress", Title = "Assets - TestNetworkAddress",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Check which managed network the specified asset would be assigned to.")]
    public Task<string> Assets_TestNetworkAddress(McpServer server,
        [Description("Unique identifier of the Asset.")] string id)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/EffectiveManagedNetwork");

    [McpServerTool(Name = "Core_Assets_EnableAsset", Title = "Assets - EnableAsset",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enable asset and its accounts from automated platform tasks.")]
    public Task<string> Assets_EnableAsset(McpServer server,
        [Description("Unique identifier of the Asset.")] string id)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/Enable");

    [McpServerTool(Name = "Core_Assets_InstallSshKeyById", Title = "Assets - InstallSshKeyById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Installs an SSH key for the service account.")]
    public Task<string> Assets_InstallSshKeyById(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Database ID of SSH Key to install (optional - will be generated if not specified). Also option to override existing asset connection settings.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/InstallSshKey" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_Assets_GetAssetManagedBy", Title = "Assets - GetAssetManagedBy",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all owners of the specified asset.")]
    public Task<string> Assets_GetAssetManagedBy(McpServer server,
        [Description("Unique identifier of the asset.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/ManagedBy" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_SetAssetManagedBy", Title = "Assets - SetAssetManagedBy",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the assigned owners of this asset.")]
    public Task<string> Assets_SetAssetManagedBy(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("List of owners to assign to this asset.")] string body)
        => PutAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/ManagedBy", body);

    [McpServerTool(Name = "Core_Assets_ModifyManagedBy", Title = "Assets - ModifyManagedBy",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove assigned owners of this asset.")]
    public Task<string> Assets_ModifyManagedBy(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("List of owners to assign to this asset.")] string body = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/ManagedBy/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Assets_GetPlatform", Title = "Assets - GetPlatform",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets platform information for specified asset.")]
    public Task<string> Assets_GetPlatform(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/Platform" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Assets_RetrieveSshHostKeyById", Title = "Assets - RetrieveSshHostKeyById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Retrieve Ssh Host Key of the asset.")]
    public Task<string> Assets_RetrieveSshHostKeyById(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Asset connection settings.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/RetrieveSshHostKey" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_Assets_GetAssetSshHostKey", Title = "Assets - GetAssetSshHostKey",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the SshHostKey identifying this asset.")]
    public Task<string> Assets_GetAssetSshHostKey(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/SshHostKey" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Assets_SetAssetSshHostKey", Title = "Assets - SetAssetSshHostKey",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the ssh host id of this asset.")]
    public Task<string> Assets_SetAssetSshHostKey(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("SSH host id to assign to this asset.")] string body)
        => PutAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/SshHostKey", body);

    [McpServerTool(Name = "Core_Assets_RemoveSshHostKey", Title = "Assets - RemoveSshHostKey",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes the ssh host id of this asset.")]
    public Task<string> Assets_RemoveSshHostKey(McpServer server,
        [Description("Unique identifier of the Asset.")] string id)
        => DeleteAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/SshHostKey");

    [McpServerTool(Name = "Core_Assets_Synchronize", Title = "Assets - Synchronize",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Synchronize all directory related objects with the remote server.")]
    public Task<string> Assets_Synchronize(McpServer server,
        [Description("Unique ID of asset.")] string id,
        [Description("Whether to sync all entities imported from this directory or just those that have been modified.")] string fullSync = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/Synchronize" + Q(("fullSync", fullSync)));

    [McpServerTool(Name = "Core_Assets_GetAssetTags", Title = "Assets - GetAssetTags",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an asset's tags.")]
    public Task<string> Assets_GetAssetTags(McpServer server,
        [Description("Unique identifier of the asset.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/Tags" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_UpdateAssetTags", Title = "Assets - UpdateAssetTags",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an asset's tags.")]
    public Task<string> Assets_UpdateAssetTags(McpServer server,
        [Description("Unique identifier of the asset.")] string id,
        [Description("List of tags to associate with the Asset.")] string body)
        => PutAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/Tags", body);

    [McpServerTool(Name = "Core_Assets_ModifyAssetTags", Title = "Assets - ModifyAssetTags",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove tags on this asset.")]
    public Task<string> Assets_ModifyAssetTags(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("List of tags to assign to this asset.")] string body = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/Tags/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Assets_TestConnectionById", Title = "Assets - TestConnectionById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests the configured connection to this asset.")]
    public Task<string> Assets_TestConnectionById(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Optionally override asset connection settings.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/TestConnection" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_Assets_UpdateDependentServices", Title = "Assets - UpdateDependentServices",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Update account password for services and tasks on this assets based on current profile settings.")]
    public Task<string> Assets_UpdateDependentServices(McpServer server,
        [Description("Unique identifier of the Asset.")] string id,
        [Description("Parameters needed to run platform task.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/Assets/{Uri.EscapeDataString(id)}/UpdateDependentAsset" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_Assets_CreateMultipleAssets", Title = "Assets - CreateMultipleAssets",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple new assets.")]
    public Task<string> Assets_CreateMultipleAssets(McpServer server,
        [Description("New assets to process.")] string body = null)
        => PostAsync(server, "/v4/Assets/BatchCreate", body);

    [McpServerTool(Name = "Core_Assets_DeleteMultiple", Title = "Assets - DeleteMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple asset deletes.")]
    public Task<string> Assets_DeleteMultiple(McpServer server,
        [Description("Asset IDs to process.")] string body = null,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => PostAsync(server, "/v4/Assets/BatchDelete" + Q(("forceDelete", forceDelete)), body);

    [McpServerTool(Name = "Core_Assets_UpdateMultipleAssets", Title = "Assets - UpdateMultipleAssets",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple asset updates.")]
    public Task<string> Assets_UpdateMultipleAssets(McpServer server,
        [Description("assets to process.")] string body = null)
        => PostAsync(server, "/v4/Assets/BatchUpdate", body);

    [McpServerTool(Name = "Core_Assets_CheckAssetName", Title = "Assets - CheckAssetName",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if the current name is unique prior to create/update.")]
    public Task<string> Assets_CheckAssetName(McpServer server,
        [Description("Parameters for checking for unique name.")] string body = null)
        => PostAsync(server, "/v4/Assets/CheckUniqueName", body);

    [McpServerTool(Name = "Core_Assets_GetDefaultSchema", Title = "Assets - GetDefaultSchema",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get default schema for directory provider.")]
    public Task<string> Assets_GetDefaultSchema(McpServer server,
        [Description("Identity provider type name.")] string platformType)
        => GetAsync(server, $"/v4/Assets/DefaultSchema/{Uri.EscapeDataString(platformType)}");

    [McpServerTool(Name = "Core_Assets_GetAllDependentAccounts", Title = "Assets - GetAllDependentAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets list of dependent accounts for all assets.")]
    public Task<string> Assets_GetAllDependentAccounts(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Assets/DependentAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Assets_DiscoverSchema", Title = "Assets - DiscoverSchema",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Discovers the available schema attributes using provided domain credentials.")]
    public Task<string> Assets_DiscoverSchema(McpServer server,
        [Description("Credentials for authenticating to the directory.")] string body = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => PostAsync(server, "/v4/Assets/DiscoverSchema" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)), body);

    [McpServerTool(Name = "Core_Assets_DiscoverSchemaByClass", Title = "Assets - DiscoverSchemaByClass",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Discovers the available schema attributes using provided domain credentials for a specific object class.")]
    public Task<string> Assets_DiscoverSchemaByClass(McpServer server,
        [Description("Name of object class to discover schema for.")] string objectClassName,
        [Description("Credentials for authenticating to active directory.")] string body = null,
        [Description("Comma-separated property names.")] string fields = null)
        => PostAsync(server, $"/v4/Assets/DiscoverSchema/{Uri.EscapeDataString(objectClassName)}" + Q(("fields", fields)), body);

    [McpServerTool(Name = "Core_Assets_DiscoverSshHostKey", Title = "Assets - DiscoverSshHostKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Gets the ssh host key for an asset.")]
    public Task<string> Assets_DiscoverSshHostKey(McpServer server,
        [Description("Optionally override asset connection settings.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, "/v4/Assets/DiscoverSshHostKey" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_Assets_InstallSshKey", Title = "Assets - InstallSshKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Installs an SSH key for the service account.")]
    public Task<string> Assets_InstallSshKey(McpServer server,
        [Description("Information about which asset to install an SSH key for the service account.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, "/v4/Assets/InstallSshKey" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_Assets_TestConnection", Title = "Assets - TestConnection",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests the configured connection to this asset.")]
    public Task<string> Assets_TestConnection(McpServer server,
        [Description("Information about which asset to test the connection with.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, "/v4/Assets/TestConnection" + Q(("extendedLogging", extendedLogging)), body);
}
