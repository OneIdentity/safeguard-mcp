// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Token_LoginResponse", Title = "Token - LoginResponse",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("After obtaining an access token from an STS, a client application must exchange that token for a Safeguard user token that can then be used to access all API methods. This method will attempt to authorize the user from the STS and if successful, w...")]
    public Task<string> Token_LoginResponse(McpServer server,
        [Description("Currently, just the `access_token.` from the OAuth2 protocol is needed. In the future, other properties may be added.")] string body = null)
        => PostAsync(server, "/v4/Token/LoginResponse", body);

    [McpServerTool(Name = "Core_Token_Logout", Title = "Token - Logout",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("An explicit logout by an end user to have their Safeguard User Token deleted from the system such that it cannot be used again.")]
    public Task<string> Token_Logout(McpServer server,
        [Description("A value indicating whether the logout was due to inactivity or not. Defaults to false.")] string timedOut = null)
        => PostAsync(server, "/v4/Token/Logout" + Q(("timedOut", timedOut)));
}
