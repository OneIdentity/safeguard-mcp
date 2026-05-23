// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_PolicyAccounts_GetPolicyAccounts", Title = "PolicyAccounts - GetPolicyAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of accounts that can be assigned to a policy.")]
    public Task<string> PolicyAccounts_GetPolicyAccounts(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/PolicyAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_PolicyAccounts_GetPolicyAccountById", Title = "PolicyAccounts - GetPolicyAccountById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a policy account.")]
    public Task<string> PolicyAccounts_GetPolicyAccountById(McpServer server,
        [Description("Unique ID of PolicyAccount.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/PolicyAccounts/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_PolicyAccounts_GetAccountGroups", Title = "PolicyAccounts - GetAccountGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the account groups that manage an account belongs to.")]
    public Task<string> PolicyAccounts_GetAccountGroups(McpServer server,
        [Description("Unique identifier of the PolicyAccount.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/PolicyAccounts/{Uri.EscapeDataString(id)}/AccountGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_PolicyAccounts_GetLinkedUsers", Title = "PolicyAccounts - GetLinkedUsers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get users that have been linked to this policy account.")]
    public Task<string> PolicyAccounts_GetLinkedUsers(McpServer server,
        [Description("Unique identifier of the policy account.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/PolicyAccounts/{Uri.EscapeDataString(id)}/LinkedUsers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_PolicyAccounts_SaveLinkedUsers", Title = "PolicyAccounts - SaveLinkedUsers",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the set of users linked to this policy account.")]
    public Task<string> PolicyAccounts_SaveLinkedUsers(McpServer server,
        [Description("Unique identifier of the policy account.")] string id,
        [Description("List of users to be linked.")] string body)
        => PutAsync(server, $"/v4/PolicyAccounts/{Uri.EscapeDataString(id)}/LinkedUsers", body);

    [McpServerTool(Name = "Core_PolicyAccounts_ModifyLinkedUsers", Title = "PolicyAccounts - ModifyLinkedUsers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove account linked users.")]
    public Task<string> PolicyAccounts_ModifyLinkedUsers(McpServer server,
        [Description("Unique identifier of the policy account.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("List of users to be linked.")] string body = null)
        => PostAsync(server, $"/v4/PolicyAccounts/{Uri.EscapeDataString(id)}/LinkedUsers/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_PolicyAccounts_GetPolicies", Title = "PolicyAccounts - GetPolicies",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the policies that manage an account belongs to.")]
    public Task<string> PolicyAccounts_GetPolicies(McpServer server,
        [Description("Unique identifier of the PolicyAccount.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/PolicyAccounts/{Uri.EscapeDataString(id)}/Policies" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));
}
