// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_SshAlgorithms_Get", Title = "SshAlgorithms - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the list of permitted SSH algorithms.")]
    public Task<string> SshAlgorithms_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/SshAlgorithms" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_SshAlgorithms_Update", Title = "SshAlgorithms - Update",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Modifies SSH algorithms.")]
    public Task<string> SshAlgorithms_Update(McpServer server,
        [Description("Represents the available algorithms when establishing an SSH connection through the sessions module, or to an archive server. The list order determines the priority in which the algorithms are negotiated with the SSHD server.")] string body)
        => PutAsync(server, "/v4/SshAlgorithms", body);
}
