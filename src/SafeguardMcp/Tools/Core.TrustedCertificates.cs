// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_TrustedCertificates_Get", Title = "TrustedCertificates - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a queryable list of trusted certificates.")]
    public Task<string> TrustedCertificates_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/TrustedCertificates" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_TrustedCertificates_CreateEntity", Title = "TrustedCertificates - CreateEntity",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Installs a new certificate authority certificate.")]
    public Task<string> TrustedCertificates_CreateEntity(McpServer server,
        [Description("ServerCertificate to create.")] string body = null)
        => PostAsync(server, "/v4/TrustedCertificates", body);

    [McpServerTool(Name = "Core_TrustedCertificates_GetById", Title = "TrustedCertificates - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a trusted certificate.")]
    public Task<string> TrustedCertificates_GetById(McpServer server,
        [Description("Unique ID of ServerCertificate.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/TrustedCertificates/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_TrustedCertificates_Delete", Title = "TrustedCertificates - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a trusted certificate.")]
    public Task<string> TrustedCertificates_Delete(McpServer server,
        [Description("Unique identifier of the ServerCertificate.")] string id)
        => DeleteAsync(server, $"/v4/TrustedCertificates/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_TrustedCertificates_AddCertChain", Title = "TrustedCertificates - AddCertChain",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Installs a new certificate authority certificate chain.")]
    public Task<string> TrustedCertificates_AddCertChain(McpServer server,
        [Description("Base-64 encoded DER data for certificate chain.")] string body = null)
        => PostAsync(server, "/v4/TrustedCertificates/Chain", body);
}
