// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_LoginMessage_Get", Title = "LoginMessage - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the Login Message.")]
    public Task<string> LoginMessage_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/LoginMessage" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_LoginMessage_UpdateEntity", Title = "LoginMessage - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the Login Message.")]
    public Task<string> LoginMessage_UpdateEntity(McpServer server,
        [Description("Updated Login Message.")] string body)
        => PutAsync(server, "/v4/LoginMessage", body);
}
