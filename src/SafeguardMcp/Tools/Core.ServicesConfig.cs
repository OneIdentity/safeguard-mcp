// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_ServicesConfig_Get", Title = "ServicesConfig - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the status of the services configuration.")]
    public Task<string> ServicesConfig_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/ServicesConfig" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_ServicesConfig_UpdateEntity", Title = "ServicesConfig - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the services configuration.")]
    public Task<string> ServicesConfig_UpdateEntity(McpServer server,
        [Description("Updated ServicesConfig.")] string body)
        => PutAsync(server, "/v4/ServicesConfig", body);

    [McpServerTool(Name = "Core_ServicesConfig_GetService", Title = "ServicesConfig - GetService",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the status of the specified services configuration.")]
    public Task<string> ServicesConfig_GetService(McpServer server,
        [Description("Name of service to check if enabled.")] string serviceName)
        => GetAsync(server, $"/v4/ServicesConfig/{Uri.EscapeDataString(serviceName)}");

    [McpServerTool(Name = "Core_ServicesConfig_UpdateService", Title = "ServicesConfig - UpdateService",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the specified services configuration.")]
    public Task<string> ServicesConfig_UpdateService(McpServer server,
        [Description("Name of service to set enabled.")] string serviceName,
        [Description("Whether service is enabled.")] string body)
        => PutAsync(server, $"/v4/ServicesConfig/{Uri.EscapeDataString(serviceName)}", body);
}
