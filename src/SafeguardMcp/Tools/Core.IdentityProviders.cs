// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_IdentityProviders_GetIdentityProviders", Title = "IdentityProviders - GetIdentityProviders",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a queryable list of identity providers.")]
    public Task<string> IdentityProviders_GetIdentityProviders(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/IdentityProviders" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_IdentityProviders_CreateIdentityProvider", Title = "IdentityProviders - CreateIdentityProvider",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new identity provider.")]
    public Task<string> IdentityProviders_CreateIdentityProvider(McpServer server,
        [Description("IdentityProvider to create.")] string body = null)
        => PostAsync(server, "/v4/IdentityProviders", body);

    [McpServerTool(Name = "Core_IdentityProviders_GetIdentityProviderById", Title = "IdentityProviders - GetIdentityProviderById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific identity provider.")]
    public Task<string> IdentityProviders_GetIdentityProviderById(McpServer server,
        [Description("Unique ID of IdentityProvider.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_IdentityProviders_UpdateIdentityProvider", Title = "IdentityProviders - UpdateIdentityProvider",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing identity provider.")]
    public Task<string> IdentityProviders_UpdateIdentityProvider(McpServer server,
        [Description("Unique identifier of the IdentityProvider.")] string id,
        [Description("Updated IdentityProvider.")] string body)
        => PutAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_IdentityProviders_DeleteIdentityProvider", Title = "IdentityProviders - DeleteIdentityProvider",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an identity provider.")]
    public Task<string> IdentityProviders_DeleteIdentityProvider(McpServer server,
        [Description("Unique identifier of the IdentityProvider.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_IdentityProviders_GetEntries", Title = "IdentityProviders - GetEntries",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Searches the specified directory.")]
    public Task<string> IdentityProviders_GetEntries(McpServer server,
        [Description("Unique ID of a Directory IdentityProvider.")] string id,
        [Description("Sets the searchBase for the Ldap query, defaults to base of the domain for ldap, or base of forest for AD. Must be in DN Syntax.")] string searchBase = null,
        [Description("Defines the scope of the query, either base, one, or sub, defaults to sub.")] string searchScope = null,
        [Description("Either User, Group, or Computer. Defaults to User.")] string searchType = null,
        [Description("Sets a search constraint on the \"name\" of the object to return.")] string searchName = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}/DirectoryServiceEntries" + Q(("searchBase", searchBase), ("searchScope", searchScope), ("searchType", searchType), ("searchName", searchName), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_IdentityProviders_SearchUserGroups", Title = "IdentityProviders - SearchUserGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Searches the specified directory for Group objects as UserGroups.")]
    public Task<string> IdentityProviders_SearchUserGroups(McpServer server,
        [Description("Unique ID of a Directory IdentityProvider.")] string id,
        [Description("Sets the searchBase for the Ldap query, defaults to base of the domain for ldap, or base of forest for AD. Must be in DN Syntax.")] string searchBase = null,
        [Description("Defines the scope of the query, either base, one, or sub, defaults to sub.")] string searchScope = null,
        [Description("Sets a search constraint on the \"name\" of the object to return.")] string searchName = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}/DirectoryUserGroups" + Q(("searchBase", searchBase), ("searchScope", searchScope), ("searchName", searchName), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_IdentityProviders_SearchUsers", Title = "IdentityProviders - SearchUsers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Searches the specified directory for User objects as Users.")]
    public Task<string> IdentityProviders_SearchUsers(McpServer server,
        [Description("Unique ID of a Directory IdentityProvider.")] string id,
        [Description("Sets the searchBase for the Ldap query, defaults to base of the domain for ldap, or base of forest for AD. Must be in DN Syntax.")] string searchBase = null,
        [Description("Defines the scope of the query, either base, one, or sub, defaults to sub.")] string searchScope = null,
        [Description("Sets a search constraint on the \"name\" of the object to return.")] string searchName = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}/DirectoryUsers" + Q(("searchBase", searchBase), ("searchScope", searchScope), ("searchName", searchName), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_IdentityProviders_RegenerateToken", Title = "IdentityProviders - RegenerateToken",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Re-generate service token for SCIM identity provider.")]
    public Task<string> IdentityProviders_RegenerateToken(McpServer server,
        [Description("Unique identifier of the IdentityProvider.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => PostAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}/RegenerateScimToken" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_IdentityProviders_Synchronize", Title = "IdentityProviders - Synchronize",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Synchronize all directory related objects with the remote server.")]
    public Task<string> IdentityProviders_Synchronize(McpServer server,
        [Description("Unique ID of the directory.")] string id,
        [Description("Whether to sync all entities imported from this directory or just those that have been modified.")] string fullSync = null)
        => PostAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}/Synchronize" + Q(("fullSync", fullSync)));

    [McpServerTool(Name = "Core_IdentityProviders_GetProviderType", Title = "IdentityProviders - GetProviderType",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the identity provider type associated with this provider.")]
    public Task<string> IdentityProviders_GetProviderType(McpServer server,
        [Description("Unique identifier of the IdentityProvider.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}/Type" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_IdentityProviders_UpdateFederationMetadataFromUrl", Title = "IdentityProviders - UpdateFederationMetadataFromUrl",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("If an external federation provider has been configured with an HTTPS URL for the metadata, Safeguard automatically monitors it and will periodically download the metadata to check for updates. In certain scenarios, you may need to immediately trig...")]
    public Task<string> IdentityProviders_UpdateFederationMetadataFromUrl(McpServer server,
        [Description("Unique identifier of the IdentityProvider.")] string id)
        => PostAsync(server, $"/v4/IdentityProviders/{Uri.EscapeDataString(id)}/UpdateFederationMetadataFromUrl");

    [McpServerTool(Name = "Core_IdentityProviders_GetDefaultSchema", Title = "IdentityProviders - GetDefaultSchema",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get default schema for directory provider.")]
    public Task<string> IdentityProviders_GetDefaultSchema(McpServer server,
        [Description("Identity provider type name.")] string typeRefName)
        => GetAsync(server, $"/v4/IdentityProviders/DefaultSchema/{Uri.EscapeDataString(typeRefName)}");

    [McpServerTool(Name = "Core_IdentityProviders_DiscoverDomains", Title = "IdentityProviders - DiscoverDomains",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Discovers the available domains using provided domain credentials.")]
    public Task<string> IdentityProviders_DiscoverDomains(McpServer server,
        [Description("Credentials for authenticating to the directory.")] string body = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => PostAsync(server, "/v4/IdentityProviders/DiscoverDomains" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)), body);

    [McpServerTool(Name = "Core_IdentityProviders_DiscoverSchema", Title = "IdentityProviders - DiscoverSchema",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Discovers the available schema attributes using provided domain credentials.")]
    public Task<string> IdentityProviders_DiscoverSchema(McpServer server,
        [Description("Credentials for authenticating to the directory.")] string body = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => PostAsync(server, "/v4/IdentityProviders/DiscoverSchema" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)), body);

    [McpServerTool(Name = "Core_IdentityProviders_DiscoverSchemaByClass", Title = "IdentityProviders - DiscoverSchemaByClass",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Discovers the available schema attributes using provided domain credentials for a specific object class.")]
    public Task<string> IdentityProviders_DiscoverSchemaByClass(McpServer server,
        [Description("Name of object class to discover schema for.")] string objectClassName,
        [Description("Credentials for authenticating to active directory.")] string body = null,
        [Description("Comma-separated property names.")] string fields = null)
        => PostAsync(server, $"/v4/IdentityProviders/DiscoverSchema/{Uri.EscapeDataString(objectClassName)}" + Q(("fields", fields)), body);
}
