// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_SslCertificates_Get", Title = "SslCertificates - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the currently installed SSL certificates.")]
    public Task<string> SslCertificates_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/SslCertificates" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_SslCertificates_InstallCertificate", Title = "SslCertificates - InstallCertificate",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Installs the SSL cert and assigns it to cluster appliances.")]
    public Task<string> SslCertificates_InstallCertificate(McpServer server,
        [Description("Updated ServerCertificate.")] string body = null)
        => PostAsync(server, "/v4/SslCertificates", body);

    [McpServerTool(Name = "Core_SslCertificates_GetByThumbprint", Title = "SslCertificates - GetByThumbprint",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific SSL certificate.")]
    public Task<string> SslCertificates_GetByThumbprint(McpServer server,
        [Description("Thumbprint of certificate.")] string thumbprint,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/SslCertificates/{Uri.EscapeDataString(thumbprint)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_SslCertificates_RemoveByThumbprint", Title = "SslCertificates - RemoveByThumbprint",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove a specific SSL certificate.")]
    public Task<string> SslCertificates_RemoveByThumbprint(McpServer server,
        [Description("Thumbprint of certificate.")] string thumbprint)
        => DeleteAsync(server, $"/v4/SslCertificates/{Uri.EscapeDataString(thumbprint)}");

    [McpServerTool(Name = "Core_SslCertificates_GetAppliances", Title = "SslCertificates - GetAppliances",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the appliances the specified certificate is used by.")]
    public Task<string> SslCertificates_GetAppliances(McpServer server,
        [Description("thumbprint of SSL certificate.")] string thumbprint,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/SslCertificates/{Uri.EscapeDataString(thumbprint)}/Appliances" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_SslCertificates_AssignSslCertificate", Title = "SslCertificates - AssignSslCertificate",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update the appliances this SSL certificate is used by.")]
    public Task<string> SslCertificates_AssignSslCertificate(McpServer server,
        [Description("thumbprint of SSL certificate.")] string thumbprint,
        [Description("Appliances to update.")] string body)
        => PutAsync(server, $"/v4/SslCertificates/{Uri.EscapeDataString(thumbprint)}/Appliances", body);
}
