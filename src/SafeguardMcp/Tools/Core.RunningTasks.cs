// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_RunningTasks_Get", Title = "RunningTasks - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of running tasks.")]
    public Task<string> RunningTasks_Get(McpServer server,
        [Description("Log time range start. Default 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Log time range end (Preferred over 'filter').")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null,
        [Description("Whether to include tasks currently submitted to Starling.")] string includeSubmitted = null)
        => GetAsync(server, "/v4/RunningTasks" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q), ("includeSubmitted", includeSubmitted)));

    [McpServerTool(Name = "Core_RunningTasks_GetByName", Title = "RunningTasks - GetByName",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of running tasks by task name.")]
    public Task<string> RunningTasks_GetByName(McpServer server,
        [Description("Name of tasks to filter by. (Preferred over 'filter').")] string taskName,
        [Description("Log time range start. Default 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Log time range end (Preferred over 'filter').")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null,
        [Description("Whether to include tasks currently submitted to Starling.")] string includeSubmitted = null)
        => GetAsync(server, $"/v4/RunningTasks/{Uri.EscapeDataString(taskName)}" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q), ("includeSubmitted", includeSubmitted)));

    [McpServerTool(Name = "Core_RunningTasks_GetById", Title = "RunningTasks - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single running task.")]
    public Task<string> RunningTasks_GetById(McpServer server,
        [Description("Name of tasks to filter by. (Preferred over 'filter').")] string taskName,
        [Description("Unique identifier of the Task.")] string id,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Whether to include tasks currently submitted to Starling.")] string includeSubmitted = null)
        => GetAsync(server, $"/v4/RunningTasks/{Uri.EscapeDataString(taskName)}/{Uri.EscapeDataString(id)}" + Q(("fields", fields), ("includeSubmitted", includeSubmitted)));

    [McpServerTool(Name = "Core_RunningTasks_CancelPlatformTask", Title = "RunningTasks - CancelPlatformTask",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Cancels the queued or running task, whenever possible.")]
    public Task<string> RunningTasks_CancelPlatformTask(McpServer server,
        [Description("Name of tasks to filter by. (Preferred over 'filter').")] string taskName,
        [Description("Unique identifier of the Task.")] string id,
        [Description("Include tasks submitted to Starling.")] string includeSubmitted = null)
        => DeleteAsync(server, $"/v4/RunningTasks/{Uri.EscapeDataString(taskName)}/{Uri.EscapeDataString(id)}" + Q(("includeSubmitted", includeSubmitted)));
}
