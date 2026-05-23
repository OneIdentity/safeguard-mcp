// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Branding_GetBrandingTypes", Title = "Branding - GetBrandingTypes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of branding customization types.")]
    public Task<string> Branding_GetBrandingTypes(McpServer server)
        => GetAsync(server, "/v4/Branding");

    [McpServerTool(Name = "Core_Branding_GetBrandingApplication", Title = "Branding - GetBrandingApplication",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets application page branding setting.")]
    public Task<string> Branding_GetBrandingApplication(McpServer server)
        => GetAsync(server, "/v4/Branding/Application");

    [McpServerTool(Name = "Core_Branding_UpdateBrandingApplication", Title = "Branding - UpdateBrandingApplication",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates application branding setting.")]
    public Task<string> Branding_UpdateBrandingApplication(McpServer server,
        [Description("Branding Application Setting.")] string body)
        => PutAsync(server, "/v4/Branding/Application", body);

    [McpServerTool(Name = "Core_Branding_DeleteBrandingApplication", Title = "Branding - DeleteBrandingApplication",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes application branding setting and sets it to defaults.")]
    public Task<string> Branding_DeleteBrandingApplication(McpServer server)
        => DeleteAsync(server, "/v4/Branding/Application");

    [McpServerTool(Name = "Core_Branding_GetBrandingLoginPage", Title = "Branding - GetBrandingLoginPage",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets login page branding setting.")]
    public Task<string> Branding_GetBrandingLoginPage(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Branding/LoginPage" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Branding_UpdateBrandingLoginPage", Title = "Branding - UpdateBrandingLoginPage",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates login page branding setting.")]
    public Task<string> Branding_UpdateBrandingLoginPage(McpServer server,
        [Description("Branding Login Page Setting.")] string body)
        => PutAsync(server, "/v4/Branding/LoginPage", body);

    [McpServerTool(Name = "Core_Branding_DeleteBrandingLoginPage", Title = "Branding - DeleteBrandingLoginPage",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes login page branding setting and sets it to defaults.")]
    public Task<string> Branding_DeleteBrandingLoginPage(McpServer server)
        => DeleteAsync(server, "/v4/Branding/LoginPage");
}
