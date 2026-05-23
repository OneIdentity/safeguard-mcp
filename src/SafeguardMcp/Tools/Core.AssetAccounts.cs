// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_AssetAccounts_GetAllAccounts", Title = "AssetAccounts - GetAllAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of accounts across all partitions.")]
    public Task<string> AssetAccounts_GetAllAccounts(McpServer server,
        [Description("List of comma-separated tag IDs by which to filter results. Preferred over using filter.")] string tagNames = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetAccounts" + Q(("tagNames", tagNames), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_CreateAssetAccount", Title = "AssetAccounts - CreateAssetAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new asset account to the appliance.")]
    public Task<string> AssetAccounts_CreateAssetAccount(McpServer server,
        [Description("AssetAccount to create.")] string body = null)
        => PostAsync(server, "/v4/AssetAccounts", body);

    [McpServerTool(Name = "Core_AssetAccounts_GetAssetAccountById", Title = "AssetAccounts - GetAssetAccountById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an asset account.")]
    public Task<string> AssetAccounts_GetAssetAccountById(McpServer server,
        [Description("Unique ID of an AssetAccount.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetAccounts_UpdateAssetAccount", Title = "AssetAccounts - UpdateAssetAccount",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing asset account.")]
    public Task<string> AssetAccounts_UpdateAssetAccount(McpServer server,
        [Description("Unique identifier of the AssetAccount to update.")] string id,
        [Description("Updated AssetAccount.")] string body)
        => PutAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_AssetAccounts_DeleteAssetAccount", Title = "AssetAccounts - DeleteAssetAccount",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an account.")]
    public Task<string> AssetAccounts_DeleteAssetAccount(McpServer server,
        [Description("Unique identifier of the AssetAccount to remove.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_AssetAccounts_ResetTaskInfo", Title = "AssetAccounts - ResetTaskInfo",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Clears the last task information for the specified task on this account.")]
    public Task<string> AssetAccounts_ResetTaskInfo(McpServer server,
        [Description("Unique identifier of the AssetAccount to get tasks for.")] string id,
        [Description("Task information to clear.")] string taskName)
        => DeleteAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/{Uri.EscapeDataString(taskName)}");

    [McpServerTool(Name = "Core_AssetAccounts_GetAllApiKeys", Title = "AssetAccounts - GetAllApiKeys",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get all API keys associated with this account.")]
    public Task<string> AssetAccounts_GetAllApiKeys(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ApiKeys" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_CreateAccountApiKey", Title = "AssetAccounts - CreateAccountApiKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new account API key to the appliance.")]
    public Task<string> AssetAccounts_CreateAccountApiKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("AccountApiKey to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ApiKeys", body);

    [McpServerTool(Name = "Core_AssetAccounts_GetApiKey", Title = "AssetAccounts - GetApiKey",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get an API key associated with this account by ID.")]
    public Task<string> AssetAccounts_GetApiKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Unique identifier of the API key.")] string apiKeyId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ApiKeys/{Uri.EscapeDataString(apiKeyId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetAccounts_UpdateAccountApiKey", Title = "AssetAccounts - UpdateAccountApiKey",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing account API key.")]
    public Task<string> AssetAccounts_UpdateAccountApiKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Unique identifier of the API key.")] string apiKeyId,
        [Description("Updated AccountApiKey.")] string body)
        => PutAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ApiKeys/{Uri.EscapeDataString(apiKeyId)}", body);

    [McpServerTool(Name = "Core_AssetAccounts_DeleteAccountApiKey", Title = "AssetAccounts - DeleteAccountApiKey",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an account API key.")]
    public Task<string> AssetAccounts_DeleteAccountApiKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Unique identifier of the API key.")] string apiKeyId)
        => DeleteAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ApiKeys/{Uri.EscapeDataString(apiKeyId)}");

    [McpServerTool(Name = "Core_AssetAccounts_ChangeApiKey", Title = "AssetAccounts - ChangeApiKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Changes account API key on the remote system.")]
    public Task<string> AssetAccounts_ChangeApiKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Unique identifier of the API key.")] string apiKeyId,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ApiKeys/{Uri.EscapeDataString(apiKeyId)}/ChangeApiKey" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_CheckApiKey", Title = "AssetAccounts - CheckApiKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if account API key matches stored secret.")]
    public Task<string> AssetAccounts_CheckApiKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Unique identifier of the API key.")] string apiKeyId,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ApiKeys/{Uri.EscapeDataString(apiKeyId)}/CheckApiKey" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_SetApiKeySecret", Title = "AssetAccounts - SetApiKeySecret",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the API key secret.")]
    public Task<string> AssetAccounts_SetApiKeySecret(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Unique identifier of the API key.")] string apiKeyId,
        [Description("Secret for the API key.")] string body)
        => PutAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ApiKeys/{Uri.EscapeDataString(apiKeyId)}/ClientSecret", body);

    [McpServerTool(Name = "Core_AssetAccounts_RetrievePastApiKeys", Title = "AssetAccounts - RetrievePastApiKeys",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets API key secrets previously assigned to the API key.")]
    public Task<string> AssetAccounts_RetrievePastApiKeys(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Unique identifier of the API key.")] string apiKeyId,
        [Description("Get past passwords that were active after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get past passwords that were active before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ApiKeys/{Uri.EscapeDataString(apiKeyId)}/ClientSecrets" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_ChangeFile", Title = "AssetAccounts - ChangeFile",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Changes account file on the remote system.")]
    public Task<string> AssetAccounts_ChangeFile(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ChangeFile" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_ChangePassword", Title = "AssetAccounts - ChangePassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Changes account password on the remote system.")]
    public Task<string> AssetAccounts_ChangePassword(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ChangePassword" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_ChangeSshKey", Title = "AssetAccounts - ChangeSshKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Changes account SSH key on the remote system.")]
    public Task<string> AssetAccounts_ChangeSshKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ChangeSshKey" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_CheckFile", Title = "AssetAccounts - CheckFile",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if account file matches stored file.")]
    public Task<string> AssetAccounts_CheckFile(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/CheckFile" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_CheckPassword", Title = "AssetAccounts - CheckPassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if account password matches stored password.")]
    public Task<string> AssetAccounts_CheckPassword(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/CheckPassword" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_CheckSshKey", Title = "AssetAccounts - CheckSshKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if account SSH key matches stored password.")]
    public Task<string> AssetAccounts_CheckSshKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/CheckSshKey" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_DemoteAccount", Title = "AssetAccounts - DemoteAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Demote the account on the remote system.")]
    public Task<string> AssetAccounts_DemoteAccount(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/DemoteAccount" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_GetDependentSystems", Title = "AssetAccounts - GetDependentSystems",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets systems that are dependent on this directory account.")]
    public Task<string> AssetAccounts_GetDependentSystems(McpServer server,
        [Description("Unique identifier of the directory account.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/DependentSystems" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_DisableAssetAccount", Title = "AssetAccounts - DisableAssetAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Disable account from automated platform tasks and requests.")]
    public Task<string> AssetAccounts_DisableAssetAccount(McpServer server,
        [Description("Unique identifier of the account.")] string id)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/Disable");

    [McpServerTool(Name = "Core_AssetAccounts_GetDiscoveredSshKeys", Title = "AssetAccounts - GetDiscoveredSshKeys",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the discovered SSH keys for this account.")]
    public Task<string> AssetAccounts_GetDiscoveredSshKeys(McpServer server,
        [Description("Unique ID of the account.")] string id,
        [Description("The format of the SSH private key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/DiscoveredSshKeys" + Q(("keyFormat", keyFormat), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_RevokeSshKey", Title = "AssetAccounts - RevokeSshKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Revokes a discovered SSH authorized key by removing it from the authorized key store on the asset for this account.")]
    public Task<string> AssetAccounts_RevokeSshKey(McpServer server,
        [Description("Unique ID of the account.")] string id,
        [Description("Fingerprint of the SSH key to be removed.")] string fingerprint,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/DiscoveredSshKeys/{Uri.EscapeDataString(fingerprint)}/Revoke" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_DiscoverSshKeys", Title = "AssetAccounts - DiscoverSshKeys",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Discovers authorized keys for the account on the remote system.")]
    public Task<string> AssetAccounts_DiscoverSshKeys(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/DiscoverSshKeys" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_GetAssetAccountEffectiveManagedBy", Title = "AssetAccounts - GetAssetAccountEffectiveManagedBy",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all effective owners of the specified account.")]
    public Task<string> AssetAccounts_GetAssetAccountEffectiveManagedBy(McpServer server,
        [Description("Unique identifier of the account.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/EffectiveManagedBy" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_ElevateAccount", Title = "AssetAccounts - ElevateAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Elevate the account on the remote system.")]
    public Task<string> AssetAccounts_ElevateAccount(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ElevateAccount" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_EnableAssetAccount", Title = "AssetAccounts - EnableAssetAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enable account from automated platform tasks and requests.")]
    public Task<string> AssetAccounts_EnableAssetAccount(McpServer server,
        [Description("Unique identifier of the account.")] string id)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/Enable");

    [McpServerTool(Name = "Core_AssetAccounts_GeneratePassword", Title = "AssetAccounts - GeneratePassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Generate sample password using password rule assigned to this account.")]
    public Task<string> AssetAccounts_GeneratePassword(McpServer server,
        [Description("Unique identifier of the account.")] string id)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/GeneratePassword");

    [McpServerTool(Name = "Core_AssetAccounts_InstallSshKey", Title = "AssetAccounts - InstallSshKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Installs the SSH key assigned to the account on the remote system.")]
    public Task<string> AssetAccounts_InstallSshKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Extra parameters for this task.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/InstallSshKey" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_AssetAccounts_GetAssetAccountManagedBy", Title = "AssetAccounts - GetAssetAccountManagedBy",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all owners of the specified account.")]
    public Task<string> AssetAccounts_GetAssetAccountManagedBy(McpServer server,
        [Description("Unique identifier of the account.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ManagedBy" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_SetAssetAccountManagedBy", Title = "AssetAccounts - SetAssetAccountManagedBy",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the assigned owners of this account.")]
    public Task<string> AssetAccounts_SetAssetAccountManagedBy(McpServer server,
        [Description("Unique identifier of the account.")] string id,
        [Description("List of owners to assign to this account.")] string body)
        => PutAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ManagedBy", body);

    [McpServerTool(Name = "Core_AssetAccounts_ModifyManagedBy", Title = "AssetAccounts - ModifyManagedBy",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove assigned owners of this account.")]
    public Task<string> AssetAccounts_ModifyManagedBy(McpServer server,
        [Description("Unique identifier of the account.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("List of owners to assign to this account.")] string body = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/ManagedBy/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetAccounts_SetPassword", Title = "AssetAccounts - SetPassword",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the account password.")]
    public Task<string> AssetAccounts_SetPassword(McpServer server,
        [Description("Unique identifier of the AssetAccount to set password for.")] string id,
        [Description("Password to set for this account. Maximum length is 1 MB.")] string body)
        => PutAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/Password", body);

    [McpServerTool(Name = "Core_AssetAccounts_RetrievePastPasswords", Title = "AssetAccounts - RetrievePastPasswords",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets passwords previously assigned to the account.")]
    public Task<string> AssetAccounts_RetrievePastPasswords(McpServer server,
        [Description("Unique identifier of the AssetAccount to set password for.")] string id,
        [Description("Get past passwords that were active after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get past passwords that were active before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/Passwords" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_RestoreAccount", Title = "AssetAccounts - RestoreAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Restore the account on the remote system.")]
    public Task<string> AssetAccounts_RestoreAccount(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/RestoreAccount" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_GetSecureFile", Title = "AssetAccounts - GetSecureFile",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the SecureFile associated with this account.")]
    public Task<string> AssetAccounts_GetSecureFile(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SecureFile");

    [McpServerTool(Name = "Core_AssetAccounts_DeleteAccountFile", Title = "AssetAccounts - DeleteAccountFile",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an account file.")]
    public Task<string> AssetAccounts_DeleteAccountFile(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id)
        => DeleteAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SecureFile");

    [McpServerTool(Name = "Core_AssetAccounts_CancelUpload", Title = "AssetAccounts - CancelUpload",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Cancels the streaming upload of a file.")]
    public Task<string> AssetAccounts_CancelUpload(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id)
        => DeleteAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SecureFile/CancelUpload");

    [McpServerTool(Name = "Core_AssetAccounts_RetrievePastFiles", Title = "AssetAccounts - RetrievePastFiles",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets files previously assigned to the account.")]
    public Task<string> AssetAccounts_RetrievePastFiles(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Get past files that were active after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get past files that were active before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SecureFiles" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_DownloadPastSecureFile", Title = "AssetAccounts - DownloadPastSecureFile",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the past SecureFile associated with this account.")]
    public Task<string> AssetAccounts_DownloadPastSecureFile(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("The version of the file to fetch.")] string fileVersion)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SecureFiles/{Uri.EscapeDataString(fileVersion)}");

    [McpServerTool(Name = "Core_AssetAccounts_GetSshKey", Title = "AssetAccounts - GetSshKey",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the SSH key assigned to this account.")]
    public Task<string> AssetAccounts_GetSshKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("The format of the SSH public key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SshKey" + Q(("fields", fields), ("keyFormat", keyFormat)));

    [McpServerTool(Name = "Core_AssetAccounts_SetSshKey", Title = "AssetAccounts - SetSshKey",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Assign a specific SSH key to this account.")]
    public Task<string> AssetAccounts_SetSshKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("SSH key to assign to account. If no PrivateKey is provided a new key will be generated.")] string body,
        [Description("The format of the SSH public key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null)
        => PutAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SshKey" + Q(("keyFormat", keyFormat)), body);

    [McpServerTool(Name = "Core_AssetAccounts_RemoveSshKey", Title = "AssetAccounts - RemoveSshKey",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Unassign the SSH key assigned to this account.")]
    public Task<string> AssetAccounts_RemoveSshKey(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SshKey" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_AssetAccounts_GetSshKeyHistory", Title = "AssetAccounts - GetSshKeyHistory",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets SSH keys previously assigned to the account.")]
    public Task<string> AssetAccounts_GetSshKeyHistory(McpServer server,
        [Description("Unique identifier of the AssetAccount to set password for.")] string id,
        [Description("The format of the SSH private key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null,
        [Description("Get past passwords that were active after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get past passwords that were active before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SshKeys" + Q(("keyFormat", keyFormat), ("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_SuspendAccount", Title = "AssetAccounts - SuspendAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Suspend the account on the remote system.")]
    public Task<string> AssetAccounts_SuspendAccount(McpServer server,
        [Description("Unique identifier of the AssetAccount.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/SuspendAccount" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetAccounts_GetAccountTags", Title = "AssetAccounts - GetAccountTags",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an account's tags.")]
    public Task<string> AssetAccounts_GetAccountTags(McpServer server,
        [Description("Unique identifier of the account.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/Tags" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_UpdateAccountTags", Title = "AssetAccounts - UpdateAccountTags",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an account's tags.")]
    public Task<string> AssetAccounts_UpdateAccountTags(McpServer server,
        [Description("Unique identifier of the account.")] string id,
        [Description("List of tags to associate with the account.")] string body)
        => PutAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/Tags", body);

    [McpServerTool(Name = "Core_AssetAccounts_ModifyAccountTags", Title = "AssetAccounts - ModifyAccountTags",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove tags on this account.")]
    public Task<string> AssetAccounts_ModifyAccountTags(McpServer server,
        [Description("Unique identifier of the account.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("List of tags to assign to this account.")] string body = null)
        => PostAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/Tags/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetAccounts_GetTasks", Title = "AssetAccounts - GetTasks",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all Tasks that have been executed against this account.")]
    public Task<string> AssetAccounts_GetTasks(McpServer server,
        [Description("Unique identifier of the AssetAccount to get tasks for.")] string id,
        [Description("Log time range start. Default 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Log time range end (Preferred over 'filter').")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/Tasks" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetAccounts_GetTotpAuthenticatorByAccountId", Title = "AssetAccounts - GetTotpAuthenticatorByAccountId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the TOTP Authenticator for this account.")]
    public Task<string> AssetAccounts_GetTotpAuthenticatorByAccountId(McpServer server,
        [Description("Unique ID of an AssetAccount.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/TotpAuthenticator" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetAccounts_SetAccountTotpAuthenticator", Title = "AssetAccounts - SetAccountTotpAuthenticator",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the TOTP Authenticator of this account.")]
    public Task<string> AssetAccounts_SetAccountTotpAuthenticator(McpServer server,
        [Description("Unique ID of an AssetAccount.")] string id,
        [Description("TOTP Authenticator to assign to Asset Account. Accepts Key URI or secret string.")] string body)
        => PutAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/TotpAuthenticator", body);

    [McpServerTool(Name = "Core_AssetAccounts_DeleteTotpAuthenticator", Title = "AssetAccounts - DeleteTotpAuthenticator",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Delete the TOTP Authenticator assigned to this account.")]
    public Task<string> AssetAccounts_DeleteTotpAuthenticator(McpServer server,
        [Description("Unique ID of an AssetAccount.")] string id)
        => DeleteAsync(server, $"/v4/AssetAccounts/{Uri.EscapeDataString(id)}/TotpAuthenticator");

    [McpServerTool(Name = "Core_AssetAccounts_CreateMultipleAccounts", Title = "AssetAccounts - CreateMultipleAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple new asset accounts.")]
    public Task<string> AssetAccounts_CreateMultipleAccounts(McpServer server,
        [Description("New asset accounts to process.")] string body = null)
        => PostAsync(server, "/v4/AssetAccounts/BatchCreate", body);

    [McpServerTool(Name = "Core_AssetAccounts_DeleteMultiple", Title = "AssetAccounts - DeleteMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple asset account deletes.")]
    public Task<string> AssetAccounts_DeleteMultiple(McpServer server,
        [Description("asset accounts to process.")] string body = null,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => PostAsync(server, "/v4/AssetAccounts/BatchDelete" + Q(("forceDelete", forceDelete)), body);

    [McpServerTool(Name = "Core_AssetAccounts_UpdateMultipleAccounts", Title = "AssetAccounts - UpdateMultipleAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple asset account updates.")]
    public Task<string> AssetAccounts_UpdateMultipleAccounts(McpServer server,
        [Description("asset accounts to process.")] string body = null)
        => PostAsync(server, "/v4/AssetAccounts/BatchUpdate", body);
}
