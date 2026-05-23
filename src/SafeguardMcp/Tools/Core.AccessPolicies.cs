// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_AccessPolicies_GetAccessPolicies", Title = "AccessPolicies - GetAccessPolicies",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of access policies.")]
    public Task<string> AccessPolicies_GetAccessPolicies(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AccessPolicies" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccessPolicies_CreateAccessPolicy", Title = "AccessPolicies - CreateAccessPolicy",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new access policy.")]
    public Task<string> AccessPolicies_CreateAccessPolicy(McpServer server,
        [Description("AccessPolicy to create.")] string body = null)
        => PostAsync(server, "/v4/AccessPolicies", body);

    [McpServerTool(Name = "Core_AccessPolicies_GetAccessPolicyById", Title = "AccessPolicies - GetAccessPolicyById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an access policy.")]
    public Task<string> AccessPolicies_GetAccessPolicyById(McpServer server,
        [Description("Unique ID of AccessPolicy.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AccessPolicies_UpdateAccessPolicy", Title = "AccessPolicies - UpdateAccessPolicy",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing application access policy.")]
    public Task<string> AccessPolicies_UpdateAccessPolicy(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Updated AccessPolicy.")] string body)
        => PutAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_AccessPolicies_Delete", Title = "AccessPolicies - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an access policy.")]
    public Task<string> AccessPolicies_Delete(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id)
        => DeleteAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_AccessPolicies_GetApproverSets", Title = "AccessPolicies - GetApproverSets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the sets of identities that may approve access requests using this policy.")]
    public Task<string> AccessPolicies_GetApproverSets(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/ApproverSets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccessPolicies_SetApproverSets", Title = "AccessPolicies - SetApproverSets",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets who can approve access requests for this policy.")]
    public Task<string> AccessPolicies_SetApproverSets(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("sets of identities to assign as approvers.")] string body)
        => PutAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/ApproverSets", body);

    [McpServerTool(Name = "Core_AccessPolicies_ModifyApproverSets", Title = "AccessPolicies - ModifyApproverSets",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove who can approve access requests for this policy.")]
    public Task<string> AccessPolicies_ModifyApproverSets(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("sets of identities to assign as approvers.")] string body = null)
        => PostAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/ApproverSets/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AccessPolicies_GetNotificationContacts", Title = "AccessPolicies - GetNotificationContacts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all NotificationContacts configured for this policy.")]
    public Task<string> AccessPolicies_GetNotificationContacts(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/NotificationContacts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccessPolicies_SetNotificationContacts", Title = "AccessPolicies - SetNotificationContacts",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the list of notification contacts associated with request events for this policy.")]
    public Task<string> AccessPolicies_SetNotificationContacts(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Contacts to assign to this policy.")] string body)
        => PutAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/NotificationContacts", body);

    [McpServerTool(Name = "Core_AccessPolicies_ModifyNotificationContacts", Title = "AccessPolicies - ModifyNotificationContacts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove notification contacts associated with request events for this policy.")]
    public Task<string> AccessPolicies_ModifyNotificationContacts(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Contacts to assign to this policy.")] string body = null)
        => PostAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/NotificationContacts/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AccessPolicies_GetReasonCodes", Title = "AccessPolicies - GetReasonCodes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the reason codes assigned to this policy.")]
    public Task<string> AccessPolicies_GetReasonCodes(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/ReasonCodes" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccessPolicies_SetReasonCode", Title = "AccessPolicies - SetReasonCode",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the list of reason codes that may be used to make access requests managed by this policy.")]
    public Task<string> AccessPolicies_SetReasonCode(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("ReasonCodes to assign to this policy.")] string body)
        => PutAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/ReasonCodes", body);

    [McpServerTool(Name = "Core_AccessPolicies_ModifyReasonCodes", Title = "AccessPolicies - ModifyReasonCodes",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove reason codes that may be used to make access requests managed by this policy.")]
    public Task<string> AccessPolicies_ModifyReasonCodes(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("ReasonCodes to assign to this policy.")] string body = null)
        => PostAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/ReasonCodes/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AccessPolicies_GetReviewers", Title = "AccessPolicies - GetReviewers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the reviewers assigned to this policy.")]
    public Task<string> AccessPolicies_GetReviewers(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/Reviewers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccessPolicies_SetReviewers", Title = "AccessPolicies - SetReviewers",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets who can review access requests for this policy.")]
    public Task<string> AccessPolicies_SetReviewers(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Identities to assign as reviewers.")] string body)
        => PutAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/Reviewers", body);

    [McpServerTool(Name = "Core_AccessPolicies_ModifyReviewers", Title = "AccessPolicies - ModifyReviewers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove who can review access requests for this policy.")]
    public Task<string> AccessPolicies_ModifyReviewers(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Identities to assign as reviewers.")] string body = null)
        => PostAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/Reviewers/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AccessPolicies_GetScopeItems", Title = "AccessPolicies - GetScopeItems",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the set of account groups that are explicitly assigned to this policy.")]
    public Task<string> AccessPolicies_GetScopeItems(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/ScopeItems" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccessPolicies_SetScopeItems", Title = "AccessPolicies - SetScopeItems",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the list of accounts, assets, account groups, and asset groups that are explicitly assigned to this policy.")]
    public Task<string> AccessPolicies_SetScopeItems(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("PolicyScopeItems to manage with this policy.")] string body)
        => PutAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/ScopeItems", body);

    [McpServerTool(Name = "Core_AccessPolicies_ModifyScopeItems", Title = "AccessPolicies - ModifyScopeItems",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/remove accounts, assets, account groups, and asset groups that are explicitly assigned to this policy.")]
    public Task<string> AccessPolicies_ModifyScopeItems(McpServer server,
        [Description("Unique identifier of the AccessPolicy.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("PolicyScopeItems to manage with this policy.")] string body = null)
        => PostAsync(server, $"/v4/AccessPolicies/{Uri.EscapeDataString(id)}/ScopeItems/{Uri.EscapeDataString(operation)}", body);
}
