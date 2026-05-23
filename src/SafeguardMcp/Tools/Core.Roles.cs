// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Roles_Get", Title = "Roles - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of roles.")]
    public Task<string> Roles_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Roles" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Roles_CreateEntity", Title = "Roles - CreateEntity",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new role.")]
    public Task<string> Roles_CreateEntity(McpServer server,
        [Description("Role to create.")] string body = null)
        => PostAsync(server, "/v4/Roles", body);

    [McpServerTool(Name = "Core_Roles_GetById", Title = "Roles - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a role.")]
    public Task<string> Roles_GetById(McpServer server,
        [Description("Unique ID of Role.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Roles_UpdateEntity", Title = "Roles - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing application role.")]
    public Task<string> Roles_UpdateEntity(McpServer server,
        [Description("Unique identifier of the Role.")] string id,
        [Description("Updated Role.")] string body)
        => PutAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Roles_Delete", Title = "Roles - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an application role.")]
    public Task<string> Roles_Delete(McpServer server,
        [Description("Unique identifier of the Role.")] string id)
        => DeleteAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Roles_CheckUniquePolicyName", Title = "Roles - CheckUniquePolicyName",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if the current name is unique prior to create/update.")]
    public Task<string> Roles_CheckUniquePolicyName(McpServer server,
        [Description("Unique identifier of the role.")] string id,
        [Description("Parameters for checking for unique name.")] string body = null)
        => PostAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}/CheckUniquePolicyName", body);

    [McpServerTool(Name = "Core_Roles_GetMembers", Title = "Roles - GetMembers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the membership included in the role.")]
    public Task<string> Roles_GetMembers(McpServer server,
        [Description("Unique identifier of the Role.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}/Members" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Roles_SetMembers", Title = "Roles - SetMembers",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the accounts assigned to this group.")]
    public Task<string> Roles_SetMembers(McpServer server,
        [Description("Unique identifier of the AccountGroup.")] string id,
        [Description("Accounts to assign to the AccountGroup.")] string body)
        => PutAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}/Members", body);

    [McpServerTool(Name = "Core_Roles_ModifyMembers", Title = "Roles - ModifyMembers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove accounts assigned to this group.")]
    public Task<string> Roles_ModifyMembers(McpServer server,
        [Description("Unique identifier of the AccountGroup.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Accounts to assign to the AccountGroup.")] string body = null)
        => PostAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}/Members/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Roles_GetPasswordPolicies", Title = "Roles - GetPasswordPolicies",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the policies of the role.")]
    public Task<string> Roles_GetPasswordPolicies(McpServer server,
        [Description("Unique identifier of the Role.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}/Policies" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Roles_GetPolicyPriorities", Title = "Roles - GetPolicyPriorities",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the policy priorities assigned to this role.")]
    public Task<string> Roles_GetPolicyPriorities(McpServer server,
        [Description("Unique identifier of the Role.")] string id)
        => GetAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}/PolicyPriorities");

    [McpServerTool(Name = "Core_Roles_SetPolicyPriorities", Title = "Roles - SetPolicyPriorities",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the priorities of a list of policies for a given role. All policies belonging to the specified role must be included.")]
    public Task<string> Roles_SetPolicyPriorities(McpServer server,
        [Description("Unique identifier of the role.")] string id,
        [Description("Set the priorities of the policies in the specified role.")] string body)
        => PutAsync(server, $"/v4/Roles/{Uri.EscapeDataString(id)}/PolicyPriorities", body);

    [McpServerTool(Name = "Core_Roles_GetRolePriorities", Title = "Roles - GetRolePriorities",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the role priorities.")]
    public Task<string> Roles_GetRolePriorities(McpServer server)
        => GetAsync(server, "/v4/Roles/Priorities");

    [McpServerTool(Name = "Core_Roles_SetRolePriorities", Title = "Roles - SetRolePriorities",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates all role priorities. All roles must be included.")]
    public Task<string> Roles_SetRolePriorities(McpServer server,
        [Description("Priorities of all roles.")] string body)
        => PutAsync(server, "/v4/Roles/Priorities", body);
}
