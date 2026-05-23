// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_CloudAssistant_GetCloudAssistantStatus", Title = "CloudAssistant - GetCloudAssistantStatus",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the Cloud Assistant status.")]
    public Task<string> CloudAssistant_GetCloudAssistantStatus(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/CloudAssistant" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_CloudAssistant_DisableCloudAssistant", Title = "CloudAssistant - DisableCloudAssistant",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Disable the Cloud Assistant integration for this cluster.")]
    public Task<string> CloudAssistant_DisableCloudAssistant(McpServer server)
        => PostAsync(server, "/v4/CloudAssistant/Disable");

    [McpServerTool(Name = "Core_CloudAssistant_EnableCloudAssistant", Title = "CloudAssistant - EnableCloudAssistant",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enable the Cloud Assistant integration for this cluster.")]
    public Task<string> CloudAssistant_EnableCloudAssistant(McpServer server)
        => PostAsync(server, "/v4/CloudAssistant/Enable");

    [McpServerTool(Name = "Core_CloudAssistant_GetCloudAssistantEnrolledUsers", Title = "CloudAssistant - GetCloudAssistantEnrolledUsers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all users enrolled in Cloud Assistant.")]
    public Task<string> CloudAssistant_GetCloudAssistantEnrolledUsers(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/CloudAssistant/EnrolledUsers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_CloudAssistant_SetCloudAssistantUsers", Title = "CloudAssistant - SetCloudAssistantUsers",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets which users are enrolled in CloudAssistant.")]
    public Task<string> CloudAssistant_SetCloudAssistantUsers(McpServer server,
        [Description("Users to enroll.")] string body)
        => PutAsync(server, "/v4/CloudAssistant/EnrolledUsers", body);

    [McpServerTool(Name = "Core_CloudAssistant_ModifyCloudAssistantUsers", Title = "CloudAssistant - ModifyCloudAssistantUsers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove users enrolled in Cloud Assistant.")]
    public Task<string> CloudAssistant_ModifyCloudAssistantUsers(McpServer server,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Users to enroll/unenroll.")] string body = null)
        => PostAsync(server, $"/v4/CloudAssistant/EnrolledUsers/{Uri.EscapeDataString(operation)}", body);
}
