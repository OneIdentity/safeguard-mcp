// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_SyslogServers_Get", Title = "SyslogServers - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of known syslog servers.")]
    public Task<string> SyslogServers_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/SyslogServers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_SyslogServers_CreateEntity", Title = "SyslogServers - CreateEntity",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new syslog server configuration.")]
    public Task<string> SyslogServers_CreateEntity(McpServer server,
        [Description("SyslogServer to create.")] string body = null)
        => PostAsync(server, "/v4/SyslogServers", body);

    [McpServerTool(Name = "Core_SyslogServers_GetById", Title = "SyslogServers - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a syslog server configuration.")]
    public Task<string> SyslogServers_GetById(McpServer server,
        [Description("Unique ID of SyslogServer.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/SyslogServers/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_SyslogServers_UpdateEntity", Title = "SyslogServers - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the syslog server configuration.")]
    public Task<string> SyslogServers_UpdateEntity(McpServer server,
        [Description("Unique identifier of the SyslogServer to update.")] string id,
        [Description("Updated syslog server configuration.")] string body)
        => PutAsync(server, $"/v4/SyslogServers/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_SyslogServers_Delete", Title = "SyslogServers - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a syslog server.")]
    public Task<string> SyslogServers_Delete(McpServer server,
        [Description("Unique identifier of the SyslogServer.")] string id,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/SyslogServers/{Uri.EscapeDataString(id)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_SyslogServers_GetSyslogClientCertificate", Title = "SyslogServers - GetSyslogClientCertificate",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the syslog client certificate.")]
    public Task<string> SyslogServers_GetSyslogClientCertificate(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/SyslogServers/ClientCertificate" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_SyslogServers_SaveSyslogClientCertificate", Title = "SyslogServers - SaveSyslogClientCertificate",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update the syslog client certificate.")]
    public Task<string> SyslogServers_SaveSyslogClientCertificate(McpServer server,
        [Description("Settings to save.")] string body)
        => PutAsync(server, "/v4/SyslogServers/ClientCertificate", body);

    [McpServerTool(Name = "Core_SyslogServers_ResetSyslogClientCertificate", Title = "SyslogServers - ResetSyslogClientCertificate",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Reset the syslog client certificate.")]
    public Task<string> SyslogServers_ResetSyslogClientCertificate(McpServer server)
        => DeleteAsync(server, "/v4/SyslogServers/ClientCertificate");

    [McpServerTool(Name = "Core_SyslogServers_GetSyslogClientCertificateHistory", Title = "SyslogServers - GetSyslogClientCertificateHistory",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the syslog client certificate history.")]
    public Task<string> SyslogServers_GetSyslogClientCertificateHistory(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/SyslogServers/ClientCertificate/History" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));
}
