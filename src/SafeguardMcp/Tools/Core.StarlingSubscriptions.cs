// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_StarlingSubscriptions_GetV3", Title = "StarlingSubscriptions - GetV3",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get all Starling subscriptions. Deprecated: Use /v4/Starling/Subscription instead.")]
    public Task<string> StarlingSubscriptions_GetV3(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/StarlingSubscriptions" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_StarlingSubscriptions_CreateEntityV3", Title = "StarlingSubscriptions - CreateEntityV3",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds the new Starling subscription information. Deprecated: Use /v4/Starling/Subscription instead.")]
    public Task<string> StarlingSubscriptions_CreateEntityV3(McpServer server,
        [Description("The Starling subscription information to add.")] string body = null)
        => PostAsync(server, "/v4/StarlingSubscriptions", body);

    [McpServerTool(Name = "Core_StarlingSubscriptions_GetByIdV3", Title = "StarlingSubscriptions - GetByIdV3",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the specified Starling subscription information. Deprecated: Use /v4/Starling/Subscription instead.")]
    public Task<string> StarlingSubscriptions_GetByIdV3(McpServer server,
        [Description("Unique id of the Starling subscription to return.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/StarlingSubscriptions/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_StarlingSubscriptions_DeleteV3", Title = "StarlingSubscriptions - DeleteV3",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove and unjoin the specified Starling subscription. Deprecated: Use /v4/Starling/Subscription instead.")]
    public Task<string> StarlingSubscriptions_DeleteV3(McpServer server,
        [Description("The unique id of the Starling subscription to be deleted and removed from the system.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force the removal of the subscription information from Safeguard, regardless of whether or not the request to Starling to unjoin was successful.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/StarlingSubscriptions/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_StarlingSubscriptions_GetJoinUrlV3", Title = "StarlingSubscriptions - GetJoinUrlV3",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the necessary URL used by a client to initiate the Starling join process. Deprecated: Use /v4/Starling/Subscription instead.")]
    public Task<string> StarlingSubscriptions_GetJoinUrlV3(McpServer server)
        => GetAsync(server, "/v4/StarlingSubscriptions/JoinUrl");

    [McpServerTool(Name = "Core_StarlingSubscriptions_GetRealmV3", Title = "StarlingSubscriptions - GetRealmV3",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("The DNS suffix name(s) for which the Starling authentication provider will be used. This value needs to match the email address suffix of a user that intends to be authenticated. If the federated authentication supports more than one realm, enter ...")]
    public Task<string> StarlingSubscriptions_GetRealmV3(McpServer server)
        => GetAsync(server, "/v4/StarlingSubscriptions/Realm");

    [McpServerTool(Name = "Core_StarlingSubscriptions_PutRealmV3", Title = "StarlingSubscriptions - PutRealmV3",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("The DNS suffix name(s) for which the Starling authentication provider will be used. This value needs to match the email address suffix of a user that intends to be authenticated. If the federated authentication supports more than one realm, enter ...")]
    public Task<string> StarlingSubscriptions_PutRealmV3(McpServer server,
        [Description("If the federated authentication supports more than one realm, enter each realm separated by a space or comma.")] string body)
        => PutAsync(server, "/v4/StarlingSubscriptions/Realm", body);

    [McpServerTool(Name = "Core_StarlingSubscriptions_GetRequireAuthentication", Title = "StarlingSubscriptions - GetRequireAuthentication",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Controls the 'ForceAuthn' attribute of a SAML2 AuthnRequest. When set to true, the user will be required to reenter their credentials on the external federation site, even if they are already logged in, thus negating any single sign-on benefits, b...")]
    public Task<string> StarlingSubscriptions_GetRequireAuthentication(McpServer server)
        => GetAsync(server, "/v4/StarlingSubscriptions/RequireAuthentication");

    [McpServerTool(Name = "Core_StarlingSubscriptions_PutRequireAuthentication", Title = "StarlingSubscriptions - PutRequireAuthentication",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Controls the 'ForceAuthn' attribute of a SAML2 AuthnRequest. When set to true, the user will be required to reenter their credentials on the external federation site, even if they are already logged in, thus negating any single sign-on benefits, b...")]
    public Task<string> StarlingSubscriptions_PutRequireAuthentication(McpServer server,
        [Description("Set to true to require the user to reenter their credentials on the external federation site, even if they are already logged in. Set to false to allow the user to automatically be logged into Safeguard if the external federation site supports SSO.")] string body)
        => PutAsync(server, "/v4/StarlingSubscriptions/RequireAuthentication", body);
}
