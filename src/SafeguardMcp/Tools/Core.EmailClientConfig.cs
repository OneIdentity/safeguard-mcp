// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_EmailClientConfig_GetEmailConfig", Title = "EmailClientConfig - GetEmailConfig",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the email client configuration.")]
    public Task<string> EmailClientConfig_GetEmailConfig(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/EmailClientConfig" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_EmailClientConfig_UpdateEntity", Title = "EmailClientConfig - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the email client configuration.")]
    public Task<string> EmailClientConfig_UpdateEntity(McpServer server,
        [Description("Updated EmailClientConfig.")] string body)
        => PutAsync(server, "/v4/EmailClientConfig", body);

    [McpServerTool(Name = "Core_EmailClientConfig_GetAuthenticationCertificate", Title = "EmailClientConfig - GetAuthenticationCertificate",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the email client authentication certificate.")]
    public Task<string> EmailClientConfig_GetAuthenticationCertificate(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/EmailClientConfig/ClientCertificate" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_EmailClientConfig_SaveAuthenticationCertificate", Title = "EmailClientConfig - SaveAuthenticationCertificate",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update the email client authentication certificate.")]
    public Task<string> EmailClientConfig_SaveAuthenticationCertificate(McpServer server,
        [Description("Settings to save.")] string body)
        => PutAsync(server, "/v4/EmailClientConfig/ClientCertificate", body);

    [McpServerTool(Name = "Core_EmailClientConfig_ResetAuthenticationCertificate", Title = "EmailClientConfig - ResetAuthenticationCertificate",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Reset the email client authentication certificate.")]
    public Task<string> EmailClientConfig_ResetAuthenticationCertificate(McpServer server)
        => DeleteAsync(server, "/v4/EmailClientConfig/ClientCertificate");

    [McpServerTool(Name = "Core_EmailClientConfig_GetAuthenticationCertificateHistory", Title = "EmailClientConfig - GetAuthenticationCertificateHistory",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the email client authentication certificate history.")]
    public Task<string> EmailClientConfig_GetAuthenticationCertificateHistory(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/EmailClientConfig/ClientCertificate/History" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_EmailClientConfig_SendTestEmail", Title = "EmailClientConfig - SendTestEmail",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Sends an email via an SMTP server.")]
    public Task<string> EmailClientConfig_SendTestEmail(McpServer server,
        [Description("Email configuration overrides for test email.")] string body = null)
        => PostAsync(server, "/v4/EmailClientConfig/SendTestEmail", body);
}
