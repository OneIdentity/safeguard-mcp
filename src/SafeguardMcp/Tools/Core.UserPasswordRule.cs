// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_UserPasswordRule_GetUserPasswordRule", Title = "UserPasswordRule - GetUserPasswordRule",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the user password rule.")]
    public Task<string> UserPasswordRule_GetUserPasswordRule(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/UserPasswordRule" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_UserPasswordRule_UpdateUserPasswordRule", Title = "UserPasswordRule - UpdateUserPasswordRule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the user password rule.")]
    public Task<string> UserPasswordRule_UpdateUserPasswordRule(McpServer server,
        [Description("Updated PasswordRule.")] string body)
        => PutAsync(server, "/v4/UserPasswordRule", body);

    [McpServerTool(Name = "Core_UserPasswordRule_GeneratePassword", Title = "UserPasswordRule - GeneratePassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Generates a random password using this rule.")]
    public Task<string> UserPasswordRule_GeneratePassword(McpServer server,
        [Description("The password rule enforced when user passwords are set.")] string body = null)
        => PostAsync(server, "/v4/UserPasswordRule/GeneratePassword", body);

    [McpServerTool(Name = "Core_UserPasswordRule_ValidateUserPassword", Title = "UserPasswordRule - ValidateUserPassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Validates a proposed password against this rule.")]
    public Task<string> UserPasswordRule_ValidateUserPassword(McpServer server,
        [Description("Password to validate against this rule.")] string body = null)
        => PostAsync(server, "/v4/UserPasswordRule/ValidatePassword", body);
}
