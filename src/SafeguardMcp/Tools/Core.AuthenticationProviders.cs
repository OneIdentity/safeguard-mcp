// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_AuthenticationProviders_Get", Title = "AuthenticationProviders - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a queryable list of authentication providers.")]
    public Task<string> AuthenticationProviders_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuthenticationProviders" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuthenticationProviders_GetById", Title = "AuthenticationProviders - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single authentication provider.")]
    public Task<string> AuthenticationProviders_GetById(McpServer server,
        [Description("Unique ID of AuthenticationProvider.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuthenticationProviders/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuthenticationProviders_ForceAsDefault", Title = "AuthenticationProviders - ForceAsDefault",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("When set to `true.` on a provider, the login page will not display a drop down list of all available providers. Instead, the end user will be defaulted in to using the specified provider. Only one provider can be marked as the default at a time. W...")]
    public Task<string> AuthenticationProviders_ForceAsDefault(McpServer server,
        [Description("Unique ID of the AuthenticationProvider.")] string id)
        => PostAsync(server, $"/v4/AuthenticationProviders/{Uri.EscapeDataString(id)}/ForceAsDefault");

    [McpServerTool(Name = "Core_AuthenticationProviders_ClearDefault", Title = "AuthenticationProviders - ClearDefault",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Any authentication provider that was marked as the default will be cleared such that the system will not have a default provider and the login page will display a drop down list of all available authentication providers for the user to choose from.")]
    public Task<string> AuthenticationProviders_ClearDefault(McpServer server)
        => PostAsync(server, "/v4/AuthenticationProviders/ClearDefault");
}
