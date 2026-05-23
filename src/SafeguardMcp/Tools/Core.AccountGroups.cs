// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_AccountGroups_GetAccountGroups", Title = "AccountGroups - GetAccountGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of account group entities.")]
    public Task<string> AccountGroups_GetAccountGroups(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AccountGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccountGroups_CreateAccountGroup", Title = "AccountGroups - CreateAccountGroup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates an AccountGroup.")]
    public Task<string> AccountGroups_CreateAccountGroup(McpServer server,
        [Description("AccountGroup to create.")] string body = null)
        => PostAsync(server, "/v4/AccountGroups", body);

    [McpServerTool(Name = "Core_AccountGroups_GetAccountGroupById", Title = "AccountGroups - GetAccountGroupById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single account group.")]
    public Task<string> AccountGroups_GetAccountGroupById(McpServer server,
        [Description("Unique ID of account group.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AccountGroups/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AccountGroups_UpdateAccountGroup", Title = "AccountGroups - UpdateAccountGroup",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an AccountGroup.")]
    public Task<string> AccountGroups_UpdateAccountGroup(McpServer server,
        [Description("Unique identifier of the AccountGroup.")] string id,
        [Description("Updated AccountGroup.")] string body)
        => PutAsync(server, $"/v4/AccountGroups/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_AccountGroups_Delete", Title = "AccountGroups - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an AccountGroup.")]
    public Task<string> AccountGroups_Delete(McpServer server,
        [Description("Unique identifier of the AccountGroup.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/AccountGroups/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_AccountGroups_GetAccounts", Title = "AccountGroups - GetAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all PolicyAccounts that belong to an AccountGroup.")]
    public Task<string> AccountGroups_GetAccounts(McpServer server,
        [Description("Unique identifier of the AccountGroup.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AccountGroups/{Uri.EscapeDataString(id)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccountGroups_SetAccounts", Title = "AccountGroups - SetAccounts",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the accounts assigned to this group.")]
    public Task<string> AccountGroups_SetAccounts(McpServer server,
        [Description("Unique identifier of the AccountGroup.")] string id,
        [Description("Accounts to assign to the AccountGroup.")] string body)
        => PutAsync(server, $"/v4/AccountGroups/{Uri.EscapeDataString(id)}/Accounts", body);

    [McpServerTool(Name = "Core_AccountGroups_ModifyAccounts", Title = "AccountGroups - ModifyAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove accounts assigned to this group.")]
    public Task<string> AccountGroups_ModifyAccounts(McpServer server,
        [Description("Unique identifier of the AccountGroup.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Accounts to assign to the AccountGroup.")] string body = null)
        => PostAsync(server, $"/v4/AccountGroups/{Uri.EscapeDataString(id)}/Accounts/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AccountGroups_GetPolicies", Title = "AccountGroups - GetPolicies",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets information about policies that this account group is assigned to.")]
    public Task<string> AccountGroups_GetPolicies(McpServer server,
        [Description("Unique identifier of the AccountGroup.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AccountGroups/{Uri.EscapeDataString(id)}/Policies" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccountGroups_SetPolicies", Title = "AccountGroups - SetPolicies",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the policies this group is assigned to.")]
    public Task<string> AccountGroups_SetPolicies(McpServer server,
        [Description("Unique identifier of the AccountGroup to update.")] string id,
        [Description("Policies to assign the AccountGroup to.")] string body)
        => PutAsync(server, $"/v4/AccountGroups/{Uri.EscapeDataString(id)}/Policies", body);

    [McpServerTool(Name = "Core_AccountGroups_ModifyPolicies", Title = "AccountGroups - ModifyPolicies",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove policies this group is assigned to.")]
    public Task<string> AccountGroups_ModifyPolicies(McpServer server,
        [Description("Unique identifier of the AccountGroup to update.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Policies to assign the AccountGroup to.")] string body = null)
        => PostAsync(server, $"/v4/AccountGroups/{Uri.EscapeDataString(id)}/Policies/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AccountGroups_CreateMultiple", Title = "AccountGroups - CreateMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple new account groups.")]
    public Task<string> AccountGroups_CreateMultiple(McpServer server,
        [Description("New account groups to process.")] string body = null)
        => PostAsync(server, "/v4/AccountGroups/BatchCreate", body);

    [McpServerTool(Name = "Core_AccountGroups_DeleteMultiple", Title = "AccountGroups - DeleteMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple account groups to delete.")]
    public Task<string> AccountGroups_DeleteMultiple(McpServer server,
        [Description("account groups to process.")] string body = null,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => PostAsync(server, "/v4/AccountGroups/BatchDelete" + Q(("forceDelete", forceDelete)), body);

    [McpServerTool(Name = "Core_AccountGroups_UpdateMultiple", Title = "AccountGroups - UpdateMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple account groups to update.")]
    public Task<string> AccountGroups_UpdateMultiple(McpServer server,
        [Description("account groups to process.")] string body = null)
        => PostAsync(server, "/v4/AccountGroups/BatchUpdate", body);

    [McpServerTool(Name = "Core_AccountGroups_CheckUniqueName", Title = "AccountGroups - CheckUniqueName",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if the current name is unique prior to create/update.")]
    public Task<string> AccountGroups_CheckUniqueName(McpServer server,
        [Description("Parameters for checking for unique name.")] string body = null)
        => PostAsync(server, "/v4/AccountGroups/CheckUniqueName", body);

    [McpServerTool(Name = "Core_AccountGroups_TestRule", Title = "AccountGroups - TestRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests a dynamic grouping rule.")]
    public Task<string> AccountGroups_TestRule(McpServer server,
        [Description("Dynamic grouping rule to test.")] string body = null,
        [Description("Unique ID of the account group.")] string id = null,
        [Description("Do not return no-op results.")] string operationalOnly = null,
        [Description("Items per page.")] string limit = null)
        => PostAsync(server, "/v4/AccountGroups/TestRule" + Q(("id", id), ("operationalOnly", operationalOnly), ("limit", limit)), body);
}
