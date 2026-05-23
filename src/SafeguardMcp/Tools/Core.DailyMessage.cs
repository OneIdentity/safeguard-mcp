// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_DailyMessage_Get", Title = "DailyMessage - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the Message of the Day.")]
    public Task<string> DailyMessage_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/DailyMessage" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_DailyMessage_UpdateEntity", Title = "DailyMessage - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the Message of the Day.")]
    public Task<string> DailyMessage_UpdateEntity(McpServer server,
        [Description("Updated Message of the day.")] string body)
        => PutAsync(server, "/v4/DailyMessage", body);
}
