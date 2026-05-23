// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Licenses_Get", Title = "Licenses - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all licenses currently staged or installed on the appliance.")]
    public Task<string> Licenses_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Licenses" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Licenses_PostLicenseAsJson", Title = "Licenses - PostLicenseAsJson",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Stages a license file contained in the JSON object, Base64 encoded.")]
    public Task<string> Licenses_PostLicenseAsJson(McpServer server,
        [Description("The LicenseFile object containing the Base64 encoded license file.")] string body = null)
        => PostAsync(server, "/v4/Licenses", body);

    [McpServerTool(Name = "Core_Licenses_GetByKey", Title = "Licenses - GetByKey",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets installed license matching the given key.")]
    public Task<string> Licenses_GetByKey(McpServer server,
        [Description("License key of the license to return.")] string key,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Licenses/{Uri.EscapeDataString(key)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Licenses_Delete", Title = "Licenses - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an installed license.")]
    public Task<string> Licenses_Delete(McpServer server,
        [Description("License key of the license to remove.")] string key)
        => DeleteAsync(server, $"/v4/Licenses/{Uri.EscapeDataString(key)}");

    [McpServerTool(Name = "Core_Licenses_PostInstall", Title = "Licenses - PostInstall",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Installs the staged license with the given signature.")]
    public Task<string> Licenses_PostInstall(McpServer server,
        [Description("The license key of the staged license to install.")] string key)
        => PostAsync(server, $"/v4/Licenses/{Uri.EscapeDataString(key)}/Install");

    [McpServerTool(Name = "Core_Licenses_GetSta", Title = "Licenses - GetSta",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Returns whether the Software Transaction Agreement has been accepted by a user already.")]
    public Task<string> Licenses_GetSta(McpServer server)
        => GetAsync(server, "/v4/Licenses/Sta");

    [McpServerTool(Name = "Core_Licenses_PostSta", Title = "Licenses - PostSta",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Records user's acceptance of the Software Transaction Agreement.")]
    public Task<string> Licenses_PostSta(McpServer server)
        => PostAsync(server, "/v4/Licenses/Sta");

    [McpServerTool(Name = "Core_Licenses_GetSummary", Title = "Licenses - GetSummary",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a summary of licenses.")]
    public Task<string> Licenses_GetSummary(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Licenses/Summary" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Licenses_GetAssetUsage", Title = "Licenses - GetAssetUsage",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all assets grouped by Desktops and Systems and by whether they are counted toward the given license or not.")]
    public Task<string> Licenses_GetAssetUsage(McpServer server,
        [Description("License key of the license to return.")] string key)
        => GetAsync(server, $"/v4/Licenses/Usage/{Uri.EscapeDataString(key)}/Assets");

    [McpServerTool(Name = "Core_Licenses_GetUserUsage", Title = "Licenses - GetUserUsage",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all users grouped by whether they are counted toward the given license or not.")]
    public Task<string> Licenses_GetUserUsage(McpServer server,
        [Description("License key of the license to return.")] string key)
        => GetAsync(server, $"/v4/Licenses/Usage/{Uri.EscapeDataString(key)}/Users");

    [McpServerTool(Name = "Core_Licenses_RunLicenseCheck", Title = "Licenses - RunLicenseCheck",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Runs the license usage check job.")]
    public Task<string> Licenses_RunLicenseCheck(McpServer server)
        => PostAsync(server, "/v4/Licenses/Usage/RunLicenseCheck");
}
