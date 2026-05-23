// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_UserGroups_GetUserGroups", Title = "UserGroups - GetUserGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of user groups.")]
    public Task<string> UserGroups_GetUserGroups(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/UserGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_UserGroups_CreateUserGroup", Title = "UserGroups - CreateUserGroup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new group of users.")]
    public Task<string> UserGroups_CreateUserGroup(McpServer server,
        [Description("UserGroup to create.")] string body = null)
        => PostAsync(server, "/v4/UserGroups", body);

    [McpServerTool(Name = "Core_UserGroups_GetUserGroupById", Title = "UserGroups - GetUserGroupById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a user group.")]
    public Task<string> UserGroups_GetUserGroupById(McpServer server,
        [Description("Unique ID of UserGroup.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_UserGroups_UpdateUserGroup", Title = "UserGroups - UpdateUserGroup",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing user group.")]
    public Task<string> UserGroups_UpdateUserGroup(McpServer server,
        [Description("Unique identifier of the UserGroup.")] string id,
        [Description("Updated UserGroup.")] string body)
        => PutAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_UserGroups_DeleteUserGroup", Title = "UserGroups - DeleteUserGroup",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an user group.")]
    public Task<string> UserGroups_DeleteUserGroup(McpServer server,
        [Description("Unique identifier of the UserGroup.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_UserGroups_GetUserGroupMembers", Title = "UserGroups - GetUserGroupMembers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the members of a user group.")]
    public Task<string> UserGroups_GetUserGroupMembers(McpServer server,
        [Description("Unique identifier of the UserGroup.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}/Members" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_UserGroups_SetUserGroupMembers", Title = "UserGroups - SetUserGroupMembers",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets an existing group's membership.")]
    public Task<string> UserGroups_SetUserGroupMembers(McpServer server,
        [Description("Unique identifier of the UserGroup.")] string id,
        [Description("Users to assign to the UserGroup.")] string body)
        => PutAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}/Members", body);

    [McpServerTool(Name = "Core_UserGroups_ModifyUserGroupMembers", Title = "UserGroups - ModifyUserGroupMembers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove members to an existing group.")]
    public Task<string> UserGroups_ModifyUserGroupMembers(McpServer server,
        [Description("Unique identifier of the UserGroup.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Users to assign to the UserGroup.")] string body = null)
        => PostAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}/Members/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_UserGroups_GetUserGroupRoles", Title = "UserGroups - GetUserGroupRoles",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the security roles the user group belongs to.")]
    public Task<string> UserGroups_GetUserGroupRoles(McpServer server,
        [Description("Unique identifier of the UserGroup.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}/Roles" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_UserGroups_SetUserGroupRoles", Title = "UserGroups - SetUserGroupRoles",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Specifies which roles a user group should be assigned to.")]
    public Task<string> UserGroups_SetUserGroupRoles(McpServer server,
        [Description("Unique identifier of the UserGroup.")] string id,
        [Description("Role to assign the UserGroup to.")] string body)
        => PutAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}/Roles", body);

    [McpServerTool(Name = "Core_UserGroups_ModifyUserGroupRoles", Title = "UserGroups - ModifyUserGroupRoles",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove roles a user group should be assigned to.")]
    public Task<string> UserGroups_ModifyUserGroupRoles(McpServer server,
        [Description("Unique identifier of the UserGroup.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Role to assign the UserGroup to.")] string body = null)
        => PostAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}/Roles/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_UserGroups_SynchronizeAndUpdateProviders", Title = "UserGroups - SynchronizeAndUpdateProviders",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Directory Groups: If you ever change the primary or secondary authentication providers of a directory based User Group, those changes will not be reflected on existing users within Safeguard. A user that was added to Safeguard via a directory grou...")]
    public Task<string> UserGroups_SynchronizeAndUpdateProviders(McpServer server,
        [Description("Unique ID of UserGroup.")] string id)
        => PostAsync(server, $"/v4/UserGroups/{Uri.EscapeDataString(id)}/SynchronizeAndUpdateProviders");

    [McpServerTool(Name = "Core_UserGroups_CreateMultipleUserGroups", Title = "UserGroups - CreateMultipleUserGroups",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple new user groups.")]
    public Task<string> UserGroups_CreateMultipleUserGroups(McpServer server,
        [Description("New user groups to process.")] string body = null)
        => PostAsync(server, "/v4/UserGroups/BatchCreate", body);

    [McpServerTool(Name = "Core_UserGroups_DeleteMultipleUserGroups", Title = "UserGroups - DeleteMultipleUserGroups",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple user groups deletes.")]
    public Task<string> UserGroups_DeleteMultipleUserGroups(McpServer server,
        [Description("user groups to process.")] string body = null,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => PostAsync(server, "/v4/UserGroups/BatchDelete" + Q(("forceDelete", forceDelete)), body);

    [McpServerTool(Name = "Core_UserGroups_UpdateMultipleUserGroups", Title = "UserGroups - UpdateMultipleUserGroups",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple user groups updates.")]
    public Task<string> UserGroups_UpdateMultipleUserGroups(McpServer server,
        [Description("user groups to process.")] string body = null)
        => PostAsync(server, "/v4/UserGroups/BatchUpdate", body);
}
