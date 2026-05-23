// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_A2ARegistrations_Get", Title = "A2ARegistrations - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of registrations.")]
    public Task<string> A2ARegistrations_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/A2ARegistrations" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_A2ARegistrations_CreateEntity", Title = "A2ARegistrations - CreateEntity",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new registration.")]
    public Task<string> A2ARegistrations_CreateEntity(McpServer server,
        [Description("Registration to create.")] string body = null)
        => PostAsync(server, "/v4/A2ARegistrations", body);

    [McpServerTool(Name = "Core_A2ARegistrations_GetById", Title = "A2ARegistrations - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a registration.")]
    public Task<string> A2ARegistrations_GetById(McpServer server,
        [Description("Unique ID of Registration.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_A2ARegistrations_UpdateEntity", Title = "A2ARegistrations - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing application registration.")]
    public Task<string> A2ARegistrations_UpdateEntity(McpServer server,
        [Description("Unique identifier of the Registration.")] string id,
        [Description("Updated Registration.")] string body)
        => PutAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_A2ARegistrations_Delete", Title = "A2ARegistrations - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove an application registration.")]
    public Task<string> A2ARegistrations_Delete(McpServer server,
        [Description("Unique identifier of the registration.")] string id)
        => DeleteAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_A2ARegistrations_GetRegistrationAccessRequestBroker", Title = "A2ARegistrations - GetRegistrationAccessRequestBroker",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the access request broker information for the registration.")]
    public Task<string> A2ARegistrations_GetRegistrationAccessRequestBroker(McpServer server,
        [Description("Unique identifier of the registration.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/AccessRequestBroker" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_A2ARegistrations_UpdateRegistrationAccessRequestBroker", Title = "A2ARegistrations - UpdateRegistrationAccessRequestBroker",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update the access request broker information for the registration.")]
    public Task<string> A2ARegistrations_UpdateRegistrationAccessRequestBroker(McpServer server,
        [Description("Unique identifier of the registration.")] string id,
        [Description("Registration access request broker information.")] string body)
        => PutAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/AccessRequestBroker", body);

    [McpServerTool(Name = "Core_A2ARegistrations_RemoveRegistrationAccessRequestBroker", Title = "A2ARegistrations - RemoveRegistrationAccessRequestBroker",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove the access request broker information for the registration.")]
    public Task<string> A2ARegistrations_RemoveRegistrationAccessRequestBroker(McpServer server,
        [Description("Unique identifier of the registration.")] string id)
        => DeleteAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/AccessRequestBroker");

    [McpServerTool(Name = "Core_A2ARegistrations_GetRegistrationAccessRequestBrokerApiKey", Title = "A2ARegistrations - GetRegistrationAccessRequestBrokerApiKey",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the API key for the access request broker for the registration.")]
    public Task<string> A2ARegistrations_GetRegistrationAccessRequestBrokerApiKey(McpServer server,
        [Description("Unique identifier of the registration.")] string id)
        => GetAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/AccessRequestBroker/ApiKey");

    [McpServerTool(Name = "Core_A2ARegistrations_RegenerateRegistrationAccessRequestBrokerApiKey", Title = "A2ARegistrations - RegenerateRegistrationAccessRequestBro...",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Regenerate the API key for the access request broker for the registration.")]
    public Task<string> A2ARegistrations_RegenerateRegistrationAccessRequestBrokerApiKey(McpServer server,
        [Description("Unique identifier of the registration.")] string id)
        => PostAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/AccessRequestBroker/ApiKey");

    [McpServerTool(Name = "Core_A2ARegistrations_GetRegistrationRetrievableAccounts", Title = "A2ARegistrations - GetRegistrationRetrievableAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the retrievable accounts for the registration.")]
    public Task<string> A2ARegistrations_GetRegistrationRetrievableAccounts(McpServer server,
        [Description("Unique identifier of the registration.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/RetrievableAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_A2ARegistrations_PostRegistrationRetrievableAccount", Title = "A2ARegistrations - PostRegistrationRetrievableAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add a retrievable account for the registration.")]
    public Task<string> A2ARegistrations_PostRegistrationRetrievableAccount(McpServer server,
        [Description("Unique identifier of the registration.")] string id,
        [Description("Updated retrievable account.")] string body = null)
        => PostAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/RetrievableAccounts", body);

    [McpServerTool(Name = "Core_A2ARegistrations_GetRegistrationRetrievableAccount", Title = "A2ARegistrations - GetRegistrationRetrievableAccount",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get a retrievable account for the registration.")]
    public Task<string> A2ARegistrations_GetRegistrationRetrievableAccount(McpServer server,
        [Description("Unique identifier of the registration.")] string id,
        [Description("Unique identifier of the retrievable account that is associated with the registration.")] string accountId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/RetrievableAccounts/{Uri.EscapeDataString(accountId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_A2ARegistrations_PutRegistrationRetrievableAccount", Title = "A2ARegistrations - PutRegistrationRetrievableAccount",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update a retrievable account for the registration.")]
    public Task<string> A2ARegistrations_PutRegistrationRetrievableAccount(McpServer server,
        [Description("Unique identifier of the registration.")] string id,
        [Description("Unique identifier of the retrievable account that is associated with the registration.")] string accountId,
        [Description("Updated retrievable account.")] string body)
        => PutAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/RetrievableAccounts/{Uri.EscapeDataString(accountId)}", body);

    [McpServerTool(Name = "Core_A2ARegistrations_DeleteRegistrationRetrievableAccount", Title = "A2ARegistrations - DeleteRegistrationRetrievableAccount",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove the access request broker information for the registration.")]
    public Task<string> A2ARegistrations_DeleteRegistrationRetrievableAccount(McpServer server,
        [Description("Unique identifier of the registration.")] string id,
        [Description("Unique identifier of the retrievable account that is associated with the registration.")] string accountId)
        => DeleteAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/RetrievableAccounts/{Uri.EscapeDataString(accountId)}");

    [McpServerTool(Name = "Core_A2ARegistrations_GetRegistrationRetrievableAccountApiKey", Title = "A2ARegistrations - GetRegistrationRetrievableAccountApiKey",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the API key for a retrievable account for the registration.")]
    public Task<string> A2ARegistrations_GetRegistrationRetrievableAccountApiKey(McpServer server,
        [Description("Unique identifier of the registration.")] string id,
        [Description("Unique identifier of the retrievable account that is associated with the registration.")] string accountId)
        => GetAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/RetrievableAccounts/{Uri.EscapeDataString(accountId)}/ApiKey");

    [McpServerTool(Name = "Core_A2ARegistrations_RegenerateRegistrationRetrievableAccountApiKey", Title = "A2ARegistrations - RegenerateRegistrationRetrievableAccou...",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Regenerate the API key for a retrievable account for the registration.")]
    public Task<string> A2ARegistrations_RegenerateRegistrationRetrievableAccountApiKey(McpServer server,
        [Description("Unique identifier of the registration.")] string id,
        [Description("Unique identifier of the retrievable account that is associated with the registration.")] string accountId)
        => PostAsync(server, $"/v4/A2ARegistrations/{Uri.EscapeDataString(id)}/RetrievableAccounts/{Uri.EscapeDataString(accountId)}/ApiKey");
}
