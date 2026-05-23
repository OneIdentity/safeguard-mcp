// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_ServerCertificateSignatureRequests_Get", Title = "ServerCertificateSignatureRequests - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a queryable list of certificate signing requests.")]
    public Task<string> ServerCertificateSignatureRequests_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/ServerCertificateSignatureRequests" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_ServerCertificateSignatureRequests_CreateEntity", Title = "ServerCertificateSignatureRequests - CreateEntity",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new certificate signing request.")]
    public Task<string> ServerCertificateSignatureRequests_CreateEntity(McpServer server,
        [Description("ServerCertificateSignatureRequest to create.")] string body = null)
        => PostAsync(server, "/v4/ServerCertificateSignatureRequests", body);

    [McpServerTool(Name = "Core_ServerCertificateSignatureRequests_GetById", Title = "ServerCertificateSignatureRequests - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a certificate signing request.")]
    public Task<string> ServerCertificateSignatureRequests_GetById(McpServer server,
        [Description("Thumbprint of ServerCertificateSignatureRequest.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/ServerCertificateSignatureRequests/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_ServerCertificateSignatureRequests_Delete", Title = "ServerCertificateSignatureRequests - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a certificate signing request.")]
    public Task<string> ServerCertificateSignatureRequests_Delete(McpServer server,
        [Description("Unique identifier of the ServerCertificateSignatureRequest.")] string id)
        => DeleteAsync(server, $"/v4/ServerCertificateSignatureRequests/{Uri.EscapeDataString(id)}");
}
