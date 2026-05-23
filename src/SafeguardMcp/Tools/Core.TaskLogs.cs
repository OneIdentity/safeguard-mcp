// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_TaskLogs_Get", Title = "TaskLogs - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Fetch a list of Task Ids for which there are task logs available.")]
    public Task<string> TaskLogs_Get(McpServer server)
        => GetAsync(server, "/v4/TaskLogs");

    [McpServerTool(Name = "Core_TaskLogs_RemoveAllLogs", Title = "TaskLogs - RemoveAllLogs",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove all extended debug logging for platform tasks.")]
    public Task<string> TaskLogs_RemoveAllLogs(McpServer server)
        => DeleteAsync(server, "/v4/TaskLogs");

    [McpServerTool(Name = "Core_TaskLogs_GetLogsForTaskId", Title = "TaskLogs - GetLogsForTaskId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Fetch a list of logs available for the Task identified by the given Id.")]
    public Task<string> TaskLogs_GetLogsForTaskId(McpServer server,
        [Description("Task Guid.")] string taskId)
        => GetAsync(server, $"/v4/TaskLogs/{Uri.EscapeDataString(taskId)}");

    [McpServerTool(Name = "Core_TaskLogs_GetTaskLog", Title = "TaskLogs - GetTaskLog",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return all events from the named log for the given Task.")]
    public Task<string> TaskLogs_GetTaskLog(McpServer server,
        [Description("Task Guid.")] string taskId,
        [Description("Log name.")] string logName)
        => GetAsync(server, $"/v4/TaskLogs/{Uri.EscapeDataString(taskId)}/{Uri.EscapeDataString(logName)}");
}
