// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_HardwareSecurityModule_GetHardwareSecurityModule", Title = "HardwareSecurityModule - GetHardwareSecurityModule",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the hardware security module integration status.")]
    public Task<string> HardwareSecurityModule_GetHardwareSecurityModule(McpServer server)
        => GetAsync(server, "/v4/HardwareSecurityModule");

    [McpServerTool(Name = "Core_HardwareSecurityModule_PutHardwareSecurityModule", Title = "HardwareSecurityModule - PutHardwareSecurityModule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the hardware security module configuration.")]
    public Task<string> HardwareSecurityModule_PutHardwareSecurityModule(McpServer server,
        [Description("The hardware security module configuration details.")] string body)
        => PutAsync(server, "/v4/HardwareSecurityModule", body);

    [McpServerTool(Name = "Core_HardwareSecurityModule_PostHardwareSecurityModule", Title = "HardwareSecurityModule - PostHardwareSecurityModule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enables the hardware security module integration for the cluster.")]
    public Task<string> HardwareSecurityModule_PostHardwareSecurityModule(McpServer server,
        [Description("The hardware security module configuration details.")] string body = null)
        => PostAsync(server, "/v4/HardwareSecurityModule", body);

    [McpServerTool(Name = "Core_HardwareSecurityModule_DeleteHardwareSecurityModule", Title = "HardwareSecurityModule - DeleteHardwareSecurityModule",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Disables the hardware security module integration.")]
    public Task<string> HardwareSecurityModule_DeleteHardwareSecurityModule(McpServer server)
        => DeleteAsync(server, "/v4/HardwareSecurityModule");

    [McpServerTool(Name = "Core_HardwareSecurityModule_GetClientCertificates", Title = "HardwareSecurityModule - GetClientCertificates",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the hardware security module client certificates.")]
    public Task<string> HardwareSecurityModule_GetClientCertificates(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/HardwareSecurityModule/ClientCertificates" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_HardwareSecurityModule_PostClientCertificates", Title = "HardwareSecurityModule - PostClientCertificates",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a hardware security module client certificate.")]
    public Task<string> HardwareSecurityModule_PostClientCertificates(McpServer server,
        [Description("The client certificate to add.")] string body = null)
        => PostAsync(server, "/v4/HardwareSecurityModule/ClientCertificates", body);

    [McpServerTool(Name = "Core_HardwareSecurityModule_GetClientCertificateByThumbprint", Title = "HardwareSecurityModule - GetClientCertificateByThumbprint",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a hardware security module client certificate.")]
    public Task<string> HardwareSecurityModule_GetClientCertificateByThumbprint(McpServer server,
        [Description("The hexadecimal string of the certificate's thumbprint for which to get.")] string thumbprint,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/HardwareSecurityModule/ClientCertificates/{Uri.EscapeDataString(thumbprint)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_HardwareSecurityModule_DeleteClientCertificates", Title = "HardwareSecurityModule - DeleteClientCertificates",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes a client certificate.")]
    public Task<string> HardwareSecurityModule_DeleteClientCertificates(McpServer server,
        [Description("The thumbprint of the certificate to delete.")] string thumbprint)
        => DeleteAsync(server, $"/v4/HardwareSecurityModule/ClientCertificates/{Uri.EscapeDataString(thumbprint)}");

    [McpServerTool(Name = "Core_HardwareSecurityModule_GetAppliancesByThumbprint", Title = "HardwareSecurityModule - GetAppliancesByThumbprint",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the appliances the specified hardware security module certificate is used by.")]
    public Task<string> HardwareSecurityModule_GetAppliancesByThumbprint(McpServer server,
        [Description("The hexadecimal string of the certificate's thumbprint for which to get.")] string thumbprint,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/HardwareSecurityModule/ClientCertificates/{Uri.EscapeDataString(thumbprint)}/Appliances" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_HardwareSecurityModule_AssignClientCertificateByThumbprint", Title = "HardwareSecurityModule - AssignClientCertificateByThumbprint",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the appliances this client certificate is used by.")]
    public Task<string> HardwareSecurityModule_AssignClientCertificateByThumbprint(McpServer server,
        [Description("The hexadecimal string of the certificate's thumbprint for which to get.")] string thumbprint,
        [Description("List of Safeguard cluster appliances for which to assign the client certificate.")] string body)
        => PutAsync(server, $"/v4/HardwareSecurityModule/ClientCertificates/{Uri.EscapeDataString(thumbprint)}/Appliances", body);

    [McpServerTool(Name = "Core_HardwareSecurityModule_GetServerCertificates", Title = "HardwareSecurityModule - GetServerCertificates",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the server certificates.")]
    public Task<string> HardwareSecurityModule_GetServerCertificates(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/HardwareSecurityModule/ServerCertificates" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_HardwareSecurityModule_PostServerCertificates", Title = "HardwareSecurityModule - PostServerCertificates",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a server certificate.")]
    public Task<string> HardwareSecurityModule_PostServerCertificates(McpServer server,
        [Description("The server certificate to add.")] string body = null)
        => PostAsync(server, "/v4/HardwareSecurityModule/ServerCertificates", body);

    [McpServerTool(Name = "Core_HardwareSecurityModule_GetServerCertificate", Title = "HardwareSecurityModule - GetServerCertificate",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a server certificate.")]
    public Task<string> HardwareSecurityModule_GetServerCertificate(McpServer server,
        [Description("The thumbprint of the certificate.")] string thumbprint)
        => GetAsync(server, $"/v4/HardwareSecurityModule/ServerCertificates/{Uri.EscapeDataString(thumbprint)}");

    [McpServerTool(Name = "Core_HardwareSecurityModule_DeleteServerCertificates", Title = "HardwareSecurityModule - DeleteServerCertificates",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes a server certificate.")]
    public Task<string> HardwareSecurityModule_DeleteServerCertificates(McpServer server,
        [Description("The thumbprint of the certificate to delete.")] string thumbprint)
        => DeleteAsync(server, $"/v4/HardwareSecurityModule/ServerCertificates/{Uri.EscapeDataString(thumbprint)}");

    [McpServerTool(Name = "Core_HardwareSecurityModule_GetHardwareSecurityModuleStatus", Title = "HardwareSecurityModule - GetHardwareSecurityModuleStatus",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Forces a health check, and gets the hardware security module integration status.")]
    public Task<string> HardwareSecurityModule_GetHardwareSecurityModuleStatus(McpServer server)
        => GetAsync(server, "/v4/HardwareSecurityModule/Status");
}
