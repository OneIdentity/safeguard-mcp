// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Users_GetUsers", Title = "Users - GetUsers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of users.")]
    public Task<string> Users_GetUsers(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Users" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Users_CreateUser", Title = "Users - CreateUser",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new application user.")]
    public Task<string> Users_CreateUser(McpServer server,
        [Description("User to create.")] string body = null)
        => PostAsync(server, "/v4/Users", body);

    [McpServerTool(Name = "Core_Users_GetUserById", Title = "Users - GetUserById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single user.")]
    public Task<string> Users_GetUserById(McpServer server,
        [Description("Unique ID of User.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Users_UpdateUser", Title = "Users - UpdateUser",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing application user.")]
    public Task<string> Users_UpdateUser(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Updated User.")] string body)
        => PutAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Users_Delete", Title = "Users - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a User.")]
    public Task<string> Users_Delete(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_Users_GetEnterpriseAccounts", Title = "Users - GetEnterpriseAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the accounts from the specified user's enterprise vault.")]
    public Task<string> Users_GetEnterpriseAccounts(McpServer server,
        [Description("Unique identifier of the user.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/EnterpriseAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Users_GetEnterpriseAccount", Title = "Users - GetEnterpriseAccount",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an account from the specified user's vault.")]
    public Task<string> Users_GetEnterpriseAccount(McpServer server,
        [Description("Unique identifier of the user.")] string id,
        [Description("Unique identifier of the enterprise account.")] string accountId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/EnterpriseAccounts/{Uri.EscapeDataString(accountId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Users_GetEnterpriseAccountOwner", Title = "Users - GetEnterpriseAccountOwner",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets owner of an account from the specified user's vault.")]
    public Task<string> Users_GetEnterpriseAccountOwner(McpServer server,
        [Description("Unique identifier of the user.")] string id,
        [Description("Unique identifier of the enterprise account.")] string accountId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/EnterpriseAccounts/{Uri.EscapeDataString(accountId)}/Owner" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Users_SetEnterpriseAccountOwner", Title = "Users - SetEnterpriseAccountOwner",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets owner of an account from the specified user's vault.")]
    public Task<string> Users_SetEnterpriseAccountOwner(McpServer server,
        [Description("Unique identifier of the user.")] string id,
        [Description("Unique identifier of the enterprise account.")] string accountId,
        [Description("New owner to assign.")] string body)
        => PutAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/EnterpriseAccounts/{Uri.EscapeDataString(accountId)}/Owner", body);

    [McpServerTool(Name = "Core_Users_SetEnterpriseAccountBatchOwner", Title = "Users - SetEnterpriseAccountBatchOwner",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets owner of an account for a batch of enterprise accounts.")]
    public Task<string> Users_SetEnterpriseAccountBatchOwner(McpServer server,
        [Description("Unique identifier of the user.")] string id,
        [Description("Owner and accounts to update.")] string body)
        => PutAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/EnterpriseAccounts/BatchOwner", body);

    [McpServerTool(Name = "Core_Users_GetLinkedAccounts", Title = "Users - GetLinkedAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get policy accounts that have been linked to this user.")]
    public Task<string> Users_GetLinkedAccounts(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/LinkedPolicyAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Users_SaveLinkedAccounts", Title = "Users - SaveLinkedAccounts",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates set of accounts linked to this user.")]
    public Task<string> Users_SaveLinkedAccounts(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("List of accounts to be linked.")] string body)
        => PutAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/LinkedPolicyAccounts", body);

    [McpServerTool(Name = "Core_Users_ModifyLinkedAccounts", Title = "Users - ModifyLinkedAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove user linked accounts.")]
    public Task<string> Users_ModifyLinkedAccounts(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("List of accounts to be linked.")] string body = null)
        => PostAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/LinkedPolicyAccounts/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Users_GetUserPartitions", Title = "Users - GetUserPartitions",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets information about partitions this user owns.")]
    public Task<string> Users_GetUserPartitions(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/OwnedPartitions" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Users_GetUserOwnership", Title = "Users - GetUserOwnership",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets information about assets, partitions, accounts this user owns.")]
    public Task<string> Users_GetUserOwnership(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Ownership" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Users_SetPassword", Title = "Users - SetPassword",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the password of the local user.")]
    public Task<string> Users_SetPassword(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Password to set for this user.")] string body,
        [Description("Force user to change password at next login. If not specified, then the current value will remain unchanged.")] string changePasswordAtNextLogin = null)
        => PutAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Password" + Q(("changePasswordAtNextLogin", changePasswordAtNextLogin)), body);

    [McpServerTool(Name = "Core_Users_GetPhoto", Title = "Users - GetPhoto",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the user's photo.")]
    public Task<string> Users_GetPhoto(McpServer server,
        [Description("Unique identifier of the User.")] string id)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Photo");

    [McpServerTool(Name = "Core_Users_UpdatePhoto", Title = "Users - UpdatePhoto",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing application user's photo.")]
    public Task<string> Users_UpdatePhoto(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Updated Photo (64K max size).")] string body)
        => PutAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Photo", body);

    [McpServerTool(Name = "Core_Users_DeletePhoto", Title = "Users - DeletePhoto",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an application user photo.")]
    public Task<string> Users_DeletePhoto(McpServer server,
        [Description("Unique identifier of the User.")] string id)
        => DeleteAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Photo");

    [McpServerTool(Name = "Core_Users_GetRawPhoto", Title = "Users - GetRawPhoto",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the user's photo in raw format (64K max size).")]
    public Task<string> Users_GetRawPhoto(McpServer server,
        [Description("Unique identifier of the User.")] string id)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Photo/Raw");

    [McpServerTool(Name = "Core_Users_GetUserPreferences", Title = "Users - GetUserPreferences",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all preferences for the given user.")]
    public Task<string> Users_GetUserPreferences(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Preferences" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Users_GetUserPreference", Title = "Users - GetUserPreference",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific preference for the given user.")]
    public Task<string> Users_GetUserPreference(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Unique identifier of the UserPreference.")] string name,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Preferences/{Uri.EscapeDataString(name)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Users_SetUserPreference", Title = "Users - SetUserPreference",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates or create a preference for the given user.")]
    public Task<string> Users_SetUserPreference(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Unique identifier of the UserPreference.")] string name,
        [Description("Value to set for this preference.")] string body)
        => PutAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Preferences/{Uri.EscapeDataString(name)}", body);

    [McpServerTool(Name = "Core_Users_DeleteUserPreference", Title = "Users - DeleteUserPreference",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a preference for the given user.")]
    public Task<string> Users_DeleteUserPreference(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Unique identifier of the UserPreference.")] string name)
        => DeleteAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Preferences/{Uri.EscapeDataString(name)}");

    [McpServerTool(Name = "Core_Users_GetRoles", Title = "Users - GetRoles",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets information about roles this user belongs to.")]
    public Task<string> Users_GetRoles(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Roles" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Users_SetRoles", Title = "Users - SetRoles",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Specifies which roles a user should be assigned to explicitly.")]
    public Task<string> Users_SetRoles(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Roles to assign the User to.")] string body)
        => PutAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Roles", body);

    [McpServerTool(Name = "Core_Users_ModifyRoles", Title = "Users - ModifyRoles",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove roles a user should be assigned to.")]
    public Task<string> Users_ModifyRoles(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Role to assign the User to.")] string body = null)
        => PostAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Roles/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Users_GetUserSubscribers", Title = "Users - GetUserSubscribers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of subscriptions for a user.")]
    public Task<string> Users_GetUserSubscribers(McpServer server,
        [Description("Unique ID of the user.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Subscribers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Users_GetUserSubscriberById", Title = "Users - GetUserSubscriberById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single subscriber for this user.")]
    public Task<string> Users_GetUserSubscriberById(McpServer server,
        [Description("Unique ID of an User.")] string id,
        [Description("Unique ID of the event subscriber.")] string subscriberId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Subscribers/{Uri.EscapeDataString(subscriberId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Users_DisableUserEventSubscriber", Title = "Users - DisableUserEventSubscriber",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Disable an event subscriber for this user.")]
    public Task<string> Users_DisableUserEventSubscriber(McpServer server,
        [Description("Unique identifier of the User to update.")] string id,
        [Description("Unique ID of the event subscriber.")] string subscriberId)
        => PostAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Subscribers/{Uri.EscapeDataString(subscriberId)}/Disable");

    [McpServerTool(Name = "Core_Users_EnableUserEventSubscriber", Title = "Users - EnableUserEventSubscriber",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enable an event subscriber for this user.")]
    public Task<string> Users_EnableUserEventSubscriber(McpServer server,
        [Description("Unique identifier of the User to update.")] string id,
        [Description("Unique ID of the event subscriber.")] string subscriberId)
        => PostAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Subscribers/{Uri.EscapeDataString(subscriberId)}/Enable");

    [McpServerTool(Name = "Core_Users_Unlock", Title = "Users - Unlock",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Unlocks the specified user account.")]
    public Task<string> Users_Unlock(McpServer server,
        [Description("Unique identifier of the User.")] string id)
        => PostAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/Unlock");

    [McpServerTool(Name = "Core_Users_GetUserGroups", Title = "Users - GetUserGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the user's group memberships.")]
    public Task<string> Users_GetUserGroups(McpServer server,
        [Description("Unique identifier of the User.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Users/{Uri.EscapeDataString(id)}/UserGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Users_CreateMultipleUsers", Title = "Users - CreateMultipleUsers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple new users.")]
    public Task<string> Users_CreateMultipleUsers(McpServer server,
        [Description("New users to process.")] string body = null)
        => PostAsync(server, "/v4/Users/BatchCreate", body);

    [McpServerTool(Name = "Core_Users_DeleteMultipleUsers", Title = "Users - DeleteMultipleUsers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple users to delete.")]
    public Task<string> Users_DeleteMultipleUsers(McpServer server,
        [Description("User Ids to process.")] string body = null,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => PostAsync(server, "/v4/Users/BatchDelete" + Q(("forceDelete", forceDelete)), body);

    [McpServerTool(Name = "Core_Users_UpdateMultipleUsers", Title = "Users - UpdateMultipleUsers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple users to update.")]
    public Task<string> Users_UpdateMultipleUsers(McpServer server,
        [Description("Users to process.")] string body = null)
        => PostAsync(server, "/v4/Users/BatchUpdate", body);

    [McpServerTool(Name = "Core_Users_ValidatePassword", Title = "Users - ValidatePassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Validates that a password meets requirements.")]
    public Task<string> Users_ValidatePassword(McpServer server,
        [Description("Password to validate.")] string body = null)
        => PostAsync(server, "/v4/Users/ValidatePassword", body);
}
