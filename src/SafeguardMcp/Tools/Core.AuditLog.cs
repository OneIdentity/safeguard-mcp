// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_AuditLog_GetAuditTypes", Title = "AuditLog - GetAuditTypes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of audit log types.")]
    public Task<string> AuditLog_GetAuditTypes(McpServer server)
        => GetAsync(server, "/v4/AuditLog");

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestTypes", Title = "AuditLog - GetAccessRequestTypes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of audit log access request types.")]
    public Task<string> AuditLog_GetAccessRequestTypes(McpServer server)
        => GetAsync(server, "/v4/AuditLog/AccessRequests");

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestActivities", Title = "AuditLog - GetAccessRequestActivities",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of access request activity log entries. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestActivities(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Get activity that occurred for a specific account (Preferred over filter).")] string accountId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/AccessRequests/Activities" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("accountId", accountId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestActivitiesByRequestId", Title = "AuditLog - GetAccessRequestActivitiesByRequestId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets access request activity log entries for a request. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestActivitiesByRequestId(McpServer server,
        [Description("The unique ID of the access request.")] string requestId,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/AccessRequests/Activities/{Uri.EscapeDataString(requestId)}" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestActivitiesById", Title = "AuditLog - GetAccessRequestActivitiesById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets access request activity log entry with given ID. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestActivitiesById(McpServer server,
        [Description("The unique ID of the access request.")] string requestId,
        [Description("The database ID of the activity log entry.")] string logId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/AccessRequests/Activities/{Uri.EscapeDataString(requestId)}/{Uri.EscapeDataString(logId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestActivitySessionLog", Title = "AuditLog - GetAccessRequestActivitySessionLog",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of access request session activity log entries. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestActivitySessionLog(McpServer server,
        [Description("The unique ID of the access request.")] string requestId,
        [Description("The database ID of the activity log entry.")] string logId,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/AccessRequests/Activities/{Uri.EscapeDataString(requestId)}/{Uri.EscapeDataString(logId)}/SessionLog" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequests", Title = "AuditLog - GetAccessRequests",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of AccessRequest log entries. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequests(McpServer server,
        [Description("Get requests that were submitted after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get requests that were submitted before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/AccessRequests/Requests" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestsByLogId", Title = "AuditLog - GetAccessRequestsByLogId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of AccessRequest entries. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestsByLogId(McpServer server,
        [Description("id of specify request. (Preferred over 'filter').")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/AccessRequests/Requests/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestSessionActivity", Title = "AuditLog - GetAccessRequestSessionActivity",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of access request Session log entries. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestSessionActivity(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Get activity that occurred for a specific account (Preferred over filter).")] string accountId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/AccessRequests/Sessions" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("accountId", accountId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestSessionActivitiesByRequestId", Title = "AuditLog - GetAccessRequestSessionActivitiesByRequestId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets access request session activity log entries for a request. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestSessionActivitiesByRequestId(McpServer server,
        [Description("The unique ID of the access request.")] string requestId,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/AccessRequests/Sessions/{Uri.EscapeDataString(requestId)}" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestSessionActivitiesBySessionId", Title = "AuditLog - GetAccessRequestSessionActivitiesBySessionId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets access request session activity log entries for a request. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestSessionActivitiesBySessionId(McpServer server,
        [Description("The unique ID of the access request.")] string requestId,
        [Description("The unique session ID of a session in the access request.")] string sessionId,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/AccessRequests/Sessions/{Uri.EscapeDataString(requestId)}/{Uri.EscapeDataString(sessionId)}" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestSessionActivitiesById", Title = "AuditLog - GetAccessRequestSessionActivitiesById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets access request session activity log entry with given ID. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestSessionActivitiesById(McpServer server,
        [Description("The unique ID of the access request.")] string requestId,
        [Description("The unique session ID of a session in the access request.")] string sessionId,
        [Description("The database ID of the activity log entry.")] string logId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/AccessRequests/Sessions/{Uri.EscapeDataString(requestId)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(logId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestSessionAuditPortalLink", Title = "AuditLog - GetAccessRequestSessionAuditPortalLink",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the perma-link to the SPS portal for the session data. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestSessionAuditPortalLink(McpServer server,
        [Description("The unique ID of the access request.")] string requestId,
        [Description("The unique session ID of a session in the access request.")] string sessionId)
        => GetAsync(server, $"/v4/AuditLog/AccessRequests/Sessions/{Uri.EscapeDataString(requestId)}/{Uri.EscapeDataString(sessionId)}/AuditPortalLink");

    [McpServerTool(Name = "Core_AuditLog_GetAccessRequestSessionPlayback", Title = "AuditLog - GetAccessRequestSessionPlayback",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Retrieve the session playback data required to replay the session recording matching this session id. Must have PolicyAdmin, ApplicationAuditor or Auditor permission or be an approver or reviewer.")]
    public Task<string> AuditLog_GetAccessRequestSessionPlayback(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string requestId,
        [Description("Unique ID of the session to replay.")] string sessionId)
        => GetAsync(server, $"/v4/AuditLog/AccessRequests/Sessions/{Uri.EscapeDataString(requestId)}/{Uri.EscapeDataString(sessionId)}/Playback");

    [McpServerTool(Name = "Core_AuditLog_GetApplianceLog", Title = "AuditLog - GetApplianceLog",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of ApplianceLog entries.")]
    public Task<string> AuditLog_GetApplianceLog(McpServer server,
        [Description("Get activity that occurred for either audit category Pangaea.Data.Cassandra.ApplianceLogCategory.Patch or Pangaea.Data.Cassandra.ApplianceLogCategory.Appliance (Preferred over filter).")] string category = null,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Appliances" + Q(("category", category), ("startDate", startDate), ("endDate", endDate), ("userId", userId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetApplianceLogById", Title = "AuditLog - GetApplianceLogById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets ApplianceLog entry by id.")]
    public Task<string> AuditLog_GetApplianceLogById(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Get activity that occurred for either audit category Pangaea.Data.Cassandra.ApplianceLogCategory.Patch or Pangaea.Data.Cassandra.ApplianceLogCategory.Appliance (Preferred over filter).")] string category = null,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Appliances/{Uri.EscapeDataString(id)}" + Q(("category", category), ("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetArchiveActivity", Title = "AuditLog - GetArchiveActivity",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of ArchiveActivityLog entries.")]
    public Task<string> AuditLog_GetArchiveActivity(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Archives" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetArchiveActivityById", Title = "AuditLog - GetArchiveActivityById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific ArchiveActivityLog entry.")]
    public Task<string> AuditLog_GetArchiveActivityById(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Archives/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetDirectorySyncActivity", Title = "AuditLog - GetDirectorySyncActivity",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of directory sync activity logs.")]
    public Task<string> AuditLog_GetDirectorySyncActivity(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/DirectorySync" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetDirectorySyncActivityByName", Title = "AuditLog - GetDirectorySyncActivityByName",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets directory sync activity log entries for a specific task.")]
    public Task<string> AuditLog_GetDirectorySyncActivityByName(McpServer server,
        [Description("The type of task.")] string taskName,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/DirectorySync/{Uri.EscapeDataString(taskName)}" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetDirectorySyncActivityById", Title = "AuditLog - GetDirectorySyncActivityById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets directory sync activity log entry for a specific task and ID.")]
    public Task<string> AuditLog_GetDirectorySyncActivityById(McpServer server,
        [Description("The type of task.")] string taskName,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/DirectorySync/{Uri.EscapeDataString(taskName)}/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetDiscoveryTypes", Title = "AuditLog - GetDiscoveryTypes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of audit log discovery types.")]
    public Task<string> AuditLog_GetDiscoveryTypes(McpServer server)
        => GetAsync(server, "/v4/AuditLog/Discovery");

    [McpServerTool(Name = "Core_AuditLog_GetAccountDiscoveryLogs", Title = "AuditLog - GetAccountDiscoveryLogs",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of AccountDiscoveryLog entries.")]
    public Task<string> AuditLog_GetAccountDiscoveryLogs(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Discovery/Accounts" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAccountDiscoveryLogById", Title = "AuditLog - GetAccountDiscoveryLogById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of AccountDiscoveryLog entries.")]
    public Task<string> AuditLog_GetAccountDiscoveryLogById(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Discovery/Accounts/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetDiscoveredAccounts", Title = "AuditLog - GetDiscoveredAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets accounts discovered from a particular discovery task.")]
    public Task<string> AuditLog_GetDiscoveredAccounts(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/Discovery/Accounts/{Uri.EscapeDataString(id)}/DiscoveredAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAssetDiscoveryLogs", Title = "AuditLog - GetAssetDiscoveryLogs",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of AssetDiscoveryLog entries.")]
    public Task<string> AuditLog_GetAssetDiscoveryLogs(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Discovery/Assets" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAssetDiscoveryLogById", Title = "AuditLog - GetAssetDiscoveryLogById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of AssetDiscoveryLog entries by id.")]
    public Task<string> AuditLog_GetAssetDiscoveryLogById(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Discovery/Assets/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetDiscoveredAssetsById", Title = "AuditLog - GetDiscoveredAssetsById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the discovered assets from a specific job log.")]
    public Task<string> AuditLog_GetDiscoveredAssetsById(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/Discovery/Assets/{Uri.EscapeDataString(id)}/DiscoveredAssets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetServiceDiscoveryLogs", Title = "AuditLog - GetServiceDiscoveryLogs",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of ServiceDiscoveryLog entries.")]
    public Task<string> AuditLog_GetServiceDiscoveryLogs(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Discovery/Services" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetServiceDiscoveryLogById", Title = "AuditLog - GetServiceDiscoveryLogById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a ServiceDiscoveryLog entry by id.")]
    public Task<string> AuditLog_GetServiceDiscoveryLogById(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Discovery/Services/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetDiscoveredServices", Title = "AuditLog - GetDiscoveredServices",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets Services discovered from a particular discovery task.")]
    public Task<string> AuditLog_GetDiscoveredServices(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/Discovery/Services/{Uri.EscapeDataString(id)}/DiscoveredServices" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetSshKeyDiscoveryLogs", Title = "AuditLog - GetSshKeyDiscoveryLogs",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of SshKeyDiscoveryLog entries.")]
    public Task<string> AuditLog_GetSshKeyDiscoveryLogs(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Get activity that occurred for a specific account (Preferred over filter).")] string accountId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Discovery/SshKeys" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("accountId", accountId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetSshKeyDiscoveryLogById", Title = "AuditLog - GetSshKeyDiscoveryLogById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of SshKeyDiscoveryLog entries.")]
    public Task<string> AuditLog_GetSshKeyDiscoveryLogById(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Discovery/SshKeys/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetLicenseHistory", Title = "AuditLog - GetLicenseHistory",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of LicenseHistoryLog entries.")]
    public Task<string> AuditLog_GetLicenseHistory(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Licenses" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetLicenseHistoryByType", Title = "AuditLog - GetLicenseHistoryByType",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of LicenseHistoryLog entries.")]
    public Task<string> AuditLog_GetLicenseHistoryByType(McpServer server,
        [Description("Product license to get history for.")] string licenseType,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/Licenses/{Uri.EscapeDataString(licenseType)}" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetByLicenseHistoryByLogId", Title = "AuditLog - GetByLicenseHistoryByLogId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific LicenseHistoryLog entry.")]
    public Task<string> AuditLog_GetByLicenseHistoryByLogId(McpServer server,
        [Description("The database ID of the object that was changed.")] string licenseType,
        [Description("The database ID of the log entry.")] string logId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Licenses/{Uri.EscapeDataString(licenseType)}/{Uri.EscapeDataString(logId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetLoginHistory", Title = "AuditLog - GetLoginHistory",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of audit log login entries.")]
    public Task<string> AuditLog_GetLoginHistory(McpServer server,
        [Description("Log time range start. Default 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Log time range end (Preferred over 'filter').")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Logins" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetLoginHistoryById", Title = "AuditLog - GetLoginHistoryById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of LoginActivityLog entries.")]
    public Task<string> AuditLog_GetLoginHistoryById(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Logins/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetAuditLogMaintenance", Title = "AuditLog - GetAuditLogMaintenance",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the data and audit log maintenance settings.")]
    public Task<string> AuditLog_GetAuditLogMaintenance(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/AuditLog/Maintenance" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_SaveAuditLogMaintenance", Title = "AuditLog - SaveAuditLogMaintenance",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update the data and audit log maintenance settings.")]
    public Task<string> AuditLog_SaveAuditLogMaintenance(McpServer server,
        [Description("Settings to save.")] string body)
        => PutAsync(server, "/v4/AuditLog/Maintenance", body);

    [McpServerTool(Name = "Core_AuditLog_RunNow", Title = "AuditLog - RunNow",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Schedule a data maintenance job to run now. Supports cancellation during archiving by force complete of the cluster lock.")]
    public Task<string> AuditLog_RunNow(McpServer server,
        [Description("If true, this is a test run and we will not purge audit logs after archive. Defaults to false.")] string archiveOnly = null,
        [Description("If true, sync the audit data after archive/purge completes. Defaults to false.")] string syncAuditData = null)
        => PostAsync(server, "/v4/AuditLog/Maintenance/RunNow" + Q(("archiveOnly", archiveOnly), ("syncAuditData", syncAuditData)));

    [McpServerTool(Name = "Core_AuditLog_GetObjectChanges", Title = "AuditLog - GetObjectChanges",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of ObjectChangeLog entries.")]
    public Task<string> AuditLog_GetObjectChanges(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Get activity that occurred for a specific account (Preferred over filter).")] string accountId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/ObjectChanges" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("accountId", accountId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetByObjectType", Title = "AuditLog - GetByObjectType",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of ObjectChangeLog entries for the passed objectType.")]
    public Task<string> AuditLog_GetByObjectType(McpServer server,
        [Description("The type of object that was changed.")] string objectType,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/ObjectChanges/{Uri.EscapeDataString(objectType)}" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetByObjectId", Title = "AuditLog - GetByObjectId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of ObjectChangeLog entries for the passed objectType and objectId.")]
    public Task<string> AuditLog_GetByObjectId(McpServer server,
        [Description("The type of object that was changed.")] string objectType,
        [Description("The database ID of the object that was changed.")] string objectId,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/ObjectChanges/{Uri.EscapeDataString(objectType)}/{Uri.EscapeDataString(objectId)}" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetByObjectLogId", Title = "AuditLog - GetByObjectLogId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific of ObjectChangeLog entry for the passed objectType and objectId and logId.")]
    public Task<string> AuditLog_GetByObjectLogId(McpServer server,
        [Description("The type of object that was changed.")] string objectType,
        [Description("The database ID of the object that was changed.")] string objectId,
        [Description("The database ID of the log entry.")] string logId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/ObjectChanges/{Uri.EscapeDataString(objectType)}/{Uri.EscapeDataString(objectId)}/{Uri.EscapeDataString(logId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetPasswordActivity", Title = "AuditLog - GetPasswordActivity",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of PasswordActivityLog entries.")]
    public Task<string> AuditLog_GetPasswordActivity(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Get activity that occurred for a specific account (Preferred over filter).")] string accountId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Passwords" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("accountId", accountId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetPasswordActivityByName", Title = "AuditLog - GetPasswordActivityByName",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets password activity log entries for a specific task.")]
    public Task<string> AuditLog_GetPasswordActivityByName(McpServer server,
        [Description("The type of task.")] string taskName,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/Passwords/{Uri.EscapeDataString(taskName)}" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetPasswordActivityById", Title = "AuditLog - GetPasswordActivityById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of PasswordActivityLog entries.")]
    public Task<string> AuditLog_GetPasswordActivityById(McpServer server,
        [Description("The type of task.")] string taskName,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Passwords/{Uri.EscapeDataString(taskName)}/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetPatchHistory", Title = "AuditLog - GetPatchHistory",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of PatchHistory entries.")]
    public Task<string> AuditLog_GetPatchHistory(McpServer server,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Patches" + Q(("startDate", startDate), ("endDate", endDate), ("userId", userId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetPatchHistoryById", Title = "AuditLog - GetPatchHistoryById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific patch history entry.")]
    public Task<string> AuditLog_GetPatchHistoryById(McpServer server,
        [Description("Database Id of the log to retrieve.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AuditLog/Patches/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_GetPlatformScriptIndex", Title = "AuditLog - GetPlatformScriptIndex",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of platform script log entries.")]
    public Task<string> AuditLog_GetPlatformScriptIndex(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/PlatformScripts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetPlatformScriptLogById", Title = "AuditLog - GetPlatformScriptLogById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets platform script log entries for a specific platform.")]
    public Task<string> AuditLog_GetPlatformScriptLogById(McpServer server,
        [Description("Unique ID of the platform.")] string platformId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AuditLog/PlatformScripts/{Uri.EscapeDataString(platformId)}" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetPlatformScriptById", Title = "AuditLog - GetPlatformScriptById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific base64 encoded script.")]
    public Task<string> AuditLog_GetPlatformScriptById(McpServer server,
        [Description("Unique ID of the platform.")] string platformId,
        [Description("Database Id of the log to retrieve.")] string id)
        => GetAsync(server, $"/v4/AuditLog/PlatformScripts/{Uri.EscapeDataString(platformId)}/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_AuditLog_GetRawPlatformScriptById", Title = "AuditLog - GetRawPlatformScriptById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific script in raw format.")]
    public Task<string> AuditLog_GetRawPlatformScriptById(McpServer server,
        [Description("Unique ID of the platform.")] string platformId,
        [Description("Database Id of the log to retrieve.")] string id)
        => GetAsync(server, $"/v4/AuditLog/PlatformScripts/{Uri.EscapeDataString(platformId)}/{Uri.EscapeDataString(id)}/Raw");

    [McpServerTool(Name = "Core_AuditLog_GetSigningCertificate", Title = "AuditLog - GetSigningCertificate",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the audit log signing certificate.")]
    public Task<string> AuditLog_GetSigningCertificate(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/AuditLog/Retention/SigningCertificate" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AuditLog_SaveSigningCertificate", Title = "AuditLog - SaveSigningCertificate",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update the audit log signing certificate.")]
    public Task<string> AuditLog_SaveSigningCertificate(McpServer server,
        [Description("Settings to save.")] string body)
        => PutAsync(server, "/v4/AuditLog/Retention/SigningCertificate", body);

    [McpServerTool(Name = "Core_AuditLog_ResetSigningCertificate", Title = "AuditLog - ResetSigningCertificate",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Reset the audit log signing certificate.")]
    public Task<string> AuditLog_ResetSigningCertificate(McpServer server)
        => DeleteAsync(server, "/v4/AuditLog/Retention/SigningCertificate");

    [McpServerTool(Name = "Core_AuditLog_GetSigningCertificateHistory", Title = "AuditLog - GetSigningCertificateHistory",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the audit log signing certificate history.")]
    public Task<string> AuditLog_GetSigningCertificateHistory(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Retention/SigningCertificate/History" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_SearchAuditLog", Title = "AuditLog - SearchAuditLog",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a set of AuditSearchLog entries.")]
    public Task<string> AuditLog_SearchAuditLog(McpServer server,
        [Description("Get activity that occurred for a specific audit category (Preferred over filter).")] string category = null,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over filter).")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Get activity that occurred for a specific user (Preferred over filter).")] string userId = null,
        [Description("Get activity that occurred for a specific asset (Preferred over filter).")] string assetId = null,
        [Description("Get activity that occurred for a specific account (Preferred over filter).")] string accountId = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AuditLog/Search" + Q(("category", category), ("startDate", startDate), ("endDate", endDate), ("userId", userId), ("assetId", assetId), ("accountId", accountId), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AuditLog_GetAuditLogStreamService", Title = "AuditLog - GetAuditLogStreamService",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets current state of the audit log stream service. The audit log stream service is the ability for the audit log data to be requested by and streamed to a linked SPS appliance.")]
    public Task<string> AuditLog_GetAuditLogStreamService(McpServer server)
        => GetAsync(server, "/v4/AuditLog/StreamService");

    [McpServerTool(Name = "Core_AuditLog_UpdateAccessRequestBroker", Title = "AuditLog - UpdateAccessRequestBroker",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update the audit log streaming service.")]
    public Task<string> AuditLog_UpdateAccessRequestBroker(McpServer server,
        [Description("Audit log streaming service.")] string body)
        => PutAsync(server, "/v4/AuditLog/StreamService", body);
}
