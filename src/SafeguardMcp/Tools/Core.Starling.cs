// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Starling_GetBase", Title = "Starling - GetBase",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get Starling endpoints.")]
    public Task<string> Starling_GetBase(McpServer server)
        => GetAsync(server, "/v4/Starling");

    [McpServerTool(Name = "Core_Starling_GetJoinedAssets", Title = "Starling - GetJoinedAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get assets joined to Starling Connect.")]
    public Task<string> Starling_GetJoinedAssets(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Starling/JoinedAssets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Starling_GetRealm", Title = "Starling - GetRealm",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("The DNS suffix name(s) for which the Starling authentication provider will be used. This value needs to match the email address suffix of a user that intends to be authenticated. If the federated authentication supports more than one realm, enter ...")]
    public Task<string> Starling_GetRealm(McpServer server)
        => GetAsync(server, "/v4/Starling/Realm");

    [McpServerTool(Name = "Core_Starling_PutRealm", Title = "Starling - PutRealm",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("The DNS suffix name(s) for which the Starling authentication provider will be used. This value needs to match the email address suffix of a user that intends to be authenticated. If the federated authentication supports more than one realm, enter ...")]
    public Task<string> Starling_PutRealm(McpServer server,
        [Description("The DNS suffix name(s) for which the Starling authentication provider will be used.")] string body)
        => PutAsync(server, "/v4/Starling/Realm", body);

    [McpServerTool(Name = "Core_Starling_GetStarlingRegisteredConnectors", Title = "Starling - GetStarlingRegisteredConnectors",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get a list of Starling registered connectors from safeguard.")]
    public Task<string> Starling_GetStarlingRegisteredConnectors(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Starling/RegisteredConnectors" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Starling_CreateStarlingRegisteredConnectors", Title = "Starling - CreateStarlingRegisteredConnectors",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Create a Starling registered connector.")]
    public Task<string> Starling_CreateStarlingRegisteredConnectors(McpServer server,
        [Description("Information needed by Safeguard in order to use a Starling connector as a platform type.")] string body = null)
        => PostAsync(server, "/v4/Starling/RegisteredConnectors", body);

    [McpServerTool(Name = "Core_Starling_GetStarlingRegisteredConnector", Title = "Starling - GetStarlingRegisteredConnector",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get a Starling registered connector from safeguard.")]
    public Task<string> Starling_GetStarlingRegisteredConnector(McpServer server,
        [Description("The unique id of the Starling connector.")] string id)
        => GetAsync(server, $"/v4/Starling/RegisteredConnectors/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Starling_UpdateStarlingRegisteredConnectors", Title = "Starling - UpdateStarlingRegisteredConnectors",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update a Starling registered connector.")]
    public Task<string> Starling_UpdateStarlingRegisteredConnectors(McpServer server,
        [Description("The unique id of the Starling connector.")] string id,
        [Description("The updated information needed by Safeguard in order to use a Starling connector as a platform type.")] string body)
        => PutAsync(server, $"/v4/Starling/RegisteredConnectors/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Starling_DeleteStarlingRegisteredConnectors", Title = "Starling - DeleteStarlingRegisteredConnectors",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Delete a starling registered connector.")]
    public Task<string> Starling_DeleteStarlingRegisteredConnectors(McpServer server,
        [Description("Starling registered connector Id.")] string id)
        => DeleteAsync(server, $"/v4/Starling/RegisteredConnectors/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Starling_GetRegisteredConnectorsFromStarling", Title = "Starling - GetRegisteredConnectorsFromStarling",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get a list of registered connectors from starling.")]
    public Task<string> Starling_GetRegisteredConnectorsFromStarling(McpServer server)
        => GetAsync(server, "/v4/Starling/RegisteredConnectors/FromStarling");

    [McpServerTool(Name = "Core_Starling_GetRequireAuthentication", Title = "Starling - GetRequireAuthentication",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Controls the 'ForceAuthn' attribute of a SAML2 AuthnRequest. When set to true, the user will be required to reenter their credentials on the external federation site, even if they are already logged in, thus negating any single sign-on benefits, b...")]
    public Task<string> Starling_GetRequireAuthentication(McpServer server)
        => GetAsync(server, "/v4/Starling/RequireAuthentication");

    [McpServerTool(Name = "Core_Starling_PutRequireAuthentication", Title = "Starling - PutRequireAuthentication",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Controls the 'ForceAuthn' attribute of a SAML2 AuthnRequest. When set to true, the user will be required to reenter their credentials on the external federation site, even if they are already logged in, thus negating any single sign-on benefits, b...")]
    public Task<string> Starling_PutRequireAuthentication(McpServer server,
        [Description("Set to true to require the user to reenter their credentials on the external federation site, even if they are already logged in. Set to false to allow the user to automatically be logged into Safeguard if the external federation site supports SSO.")] string body)
        => PutAsync(server, "/v4/Starling/RequireAuthentication", body);

    [McpServerTool(Name = "Core_Starling_GetSubscription", Title = "Starling - GetSubscription",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get all Starling subscriptions.")]
    public Task<string> Starling_GetSubscription(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Starling/Subscription" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Starling_SetEntity", Title = "Starling - SetEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Adds the new Starling subscription information.")]
    public Task<string> Starling_SetEntity(McpServer server,
        [Description("The Starling subscription information to add.")] string body)
        => PutAsync(server, "/v4/Starling/Subscription", body);

    [McpServerTool(Name = "Core_Starling_Delete", Title = "Starling - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove and unjoin the specified Starling subscription.")]
    public Task<string> Starling_Delete(McpServer server,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force the removal of the subscription information from Safeguard, regardless of whether or not the request to Starling to unjoin was successful.")] string forceDelete = null)
        => DeleteAsync(server, "/v4/Starling/Subscription" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_Starling_GetJoinUrl", Title = "Starling - GetJoinUrl",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the necessary URL used by a client to initiate the Starling join process.")]
    public Task<string> Starling_GetJoinUrl(McpServer server)
        => GetAsync(server, "/v4/Starling/Subscription/JoinUrl");
}
