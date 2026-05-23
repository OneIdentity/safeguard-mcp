// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_AccessRequests_Get", Title = "AccessRequests - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of AccessRequest entities for the currently authenticated user.")]
    public Task<string> AccessRequests_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AccessRequests" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccessRequests_Create", Title = "AccessRequests - Create",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new NewAccessRequest to the appliance.")]
    public Task<string> AccessRequests_Create(McpServer server,
        [Description("NewAccessRequest to create.")] string body = null)
        => PostAsync(server, "/v4/AccessRequests", body);

    [McpServerTool(Name = "Core_AccessRequests_GetById", Title = "AccessRequests - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single AccessRequest.")]
    public Task<string> AccessRequests_GetById(McpServer server,
        [Description("Unique ID of AccessRequest.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AccessRequests_Acknowledge", Title = "AccessRequests - Acknowledge",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Acknowledges requests that have been denied/revoked/expired.")]
    public Task<string> AccessRequests_Acknowledge(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id,
        [Description("Brief description of why action is justified.")] string body = null)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Acknowledge", body);

    [McpServerTool(Name = "Core_AccessRequests_Approve", Title = "AccessRequests - Approve",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Approves the AccessRequest.")]
    public Task<string> AccessRequests_Approve(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id,
        [Description("Brief description of why action is justified.")] string body = null)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Approve", body);

    [McpServerTool(Name = "Core_AccessRequests_GetApproverSet", Title = "AccessRequests - GetApproverSet",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets Approver Set of a single AccessRequest.")]
    public Task<string> AccessRequests_GetApproverSet(McpServer server,
        [Description("Unique ID of AccessRequest.")] string id)
        => GetAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/ApproverSet");

    [McpServerTool(Name = "Core_AccessRequests_Cancel", Title = "AccessRequests - Cancel",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Cancels the AccessRequest.")]
    public Task<string> AccessRequests_Cancel(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id,
        [Description("Brief description of why action is justified.")] string body = null)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Cancel", body);

    [McpServerTool(Name = "Core_AccessRequests_CheckIn", Title = "AccessRequests - CheckIn",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Returns control of the session/password and finish AccessRequest.")]
    public Task<string> AccessRequests_CheckIn(McpServer server,
        [Description("Unique identifier of the access request.")] string id)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/CheckIn");

    [McpServerTool(Name = "Core_AccessRequests_CheckOutApiKeys", Title = "AccessRequests - CheckOutApiKeys",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Releases the API key information for this asset request if the request is approved and active.")]
    public Task<string> AccessRequests_CheckOutApiKeys(McpServer server,
        [Description("Unique identifier of the access request.")] string id)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/CheckOutApiKeys");

    [McpServerTool(Name = "Core_AccessRequests_CheckOutFile", Title = "AccessRequests - CheckOutFile",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Downloads the file for this asset request if the request is approved and active.")]
    public Task<string> AccessRequests_CheckOutFile(McpServer server,
        [Description("Unique identifier of the access request.")] string id)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/CheckOutFile");

    [McpServerTool(Name = "Core_AccessRequests_CheckOutPassword", Title = "AccessRequests - CheckOutPassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Releases the password for this asset request if the request is approved and active.")]
    public Task<string> AccessRequests_CheckOutPassword(McpServer server,
        [Description("Unique identifier of the access request.")] string id)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/CheckOutPassword");

    [McpServerTool(Name = "Core_AccessRequests_CheckOutSshKey", Title = "AccessRequests - CheckOutSshKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Releases the SSH key information for this asset request if the request is approved and active.")]
    public Task<string> AccessRequests_CheckOutSshKey(McpServer server,
        [Description("Unique identifier of the access request.")] string id,
        [Description("The format of the SSH private key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/CheckOutSshKey" + Q(("keyFormat", keyFormat)));

    [McpServerTool(Name = "Core_AccessRequests_CheckOutTotp", Title = "AccessRequests - CheckOutTotp",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks out Time-based One Time Passcode (TOTP) codes.")]
    public Task<string> AccessRequests_CheckOutTotp(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/CheckOutTotp");

    [McpServerTool(Name = "Core_AccessRequests_Close", Title = "AccessRequests - Close",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Closes an AccessRequest pending review. Used by an admin when a review cannot be completed.")]
    public Task<string> AccessRequests_Close(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id,
        [Description("Brief description of why action is justified.")] string body = null)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Close", body);

    [McpServerTool(Name = "Core_AccessRequests_Deny", Title = "AccessRequests - Deny",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Denies the AccessRequest.")]
    public Task<string> AccessRequests_Deny(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id,
        [Description("Brief description of why action is justified.")] string body = null)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Deny", body);

    [McpServerTool(Name = "Core_AccessRequests_InitializeSession", Title = "AccessRequests - InitializeSession",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Initiate the session.")]
    public Task<string> AccessRequests_InitializeSession(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id,
        [Description("Configuration for initiating session.")] string body = null)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/InitializeSession", body);

    [McpServerTool(Name = "Core_AccessRequests_ReleaseFileInfo", Title = "AccessRequests - ReleaseFileInfo",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Releases the file information for this asset request if the request is approved and active.")]
    public Task<string> AccessRequests_ReleaseFileInfo(McpServer server,
        [Description("Unique identifier of the access request.")] string id)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/ReleaseFileInfo");

    [McpServerTool(Name = "Core_AccessRequests_Review", Title = "AccessRequests - Review",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Reviews the AccessRequest release.")]
    public Task<string> AccessRequests_Review(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id,
        [Description("Brief description of why action is justified.")] string body = null)
        => PostAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Review", body);

    [McpServerTool(Name = "Core_AccessRequests_GetSessions", Title = "AccessRequests - GetSessions",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all sessions for a specific AccessRequest.")]
    public Task<string> AccessRequests_GetSessions(McpServer server,
        [Description("Unique ID of AccessRequest.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Sessions" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccessRequests_GetSessionById", Title = "AccessRequests - GetSessionById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific session of an AccessRequest.")]
    public Task<string> AccessRequests_GetSessionById(McpServer server,
        [Description("Unique ID of AccessRequest.")] string id,
        [Description("Unique ID of the session.")] string sessionId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Sessions/{Uri.EscapeDataString(sessionId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AccessRequests_TerminateSession", Title = "AccessRequests - TerminateSession",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Terminate the session.")]
    public Task<string> AccessRequests_TerminateSession(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id,
        [Description("Unique ID of the session to replay.")] string sessionId)
        => DeleteAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Sessions/{Uri.EscapeDataString(sessionId)}");

    [McpServerTool(Name = "Core_AccessRequests_GetSessionPlaybackData", Title = "AccessRequests - GetSessionPlaybackData",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Retrieve the session playback data required to replay the session recording matching this session id.")]
    public Task<string> AccessRequests_GetSessionPlaybackData(McpServer server,
        [Description("Unique identifier of the AccessRequest.")] string id,
        [Description("Unique ID of the session to replay.")] string sessionId)
        => GetAsync(server, $"/v4/AccessRequests/{Uri.EscapeDataString(id)}/Sessions/{Uri.EscapeDataString(sessionId)}/Playback");

    [McpServerTool(Name = "Core_AccessRequests_GetActiveSessions", Title = "AccessRequests - GetActiveSessions",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all active sessions.")]
    public Task<string> AccessRequests_GetActiveSessions(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AccessRequests/ActiveSessions" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AccessRequests_TerminateSessions", Title = "AccessRequests - TerminateSessions",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Terminates the specified sessions.")]
    public Task<string> AccessRequests_TerminateSessions(McpServer server,
        [Description("List of sessions to terminate.")] string body = null)
        => PostAsync(server, "/v4/AccessRequests/ActiveSessions/Terminate", body);

    [McpServerTool(Name = "Core_AccessRequests_ApproveMultiple", Title = "AccessRequests - ApproveMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple access request approvals.")]
    public Task<string> AccessRequests_ApproveMultiple(McpServer server,
        [Description("Approval requests to process.")] string body = null)
        => PostAsync(server, "/v4/AccessRequests/BatchApprove", body);

    [McpServerTool(Name = "Core_AccessRequests_CreateMultiple", Title = "AccessRequests - CreateMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple new access requests.")]
    public Task<string> AccessRequests_CreateMultiple(McpServer server,
        [Description("New access requests to process.")] string body = null)
        => PostAsync(server, "/v4/AccessRequests/BatchCreate", body);

    [McpServerTool(Name = "Core_AccessRequests_DenyMultiple", Title = "AccessRequests - DenyMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple access request denials.")]
    public Task<string> AccessRequests_DenyMultiple(McpServer server,
        [Description("Denial requests to process.")] string body = null)
        => PostAsync(server, "/v4/AccessRequests/BatchDeny", body);

    [McpServerTool(Name = "Core_AccessRequests_ReviewMultiple", Title = "AccessRequests - ReviewMultiple",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple access request reviews.")]
    public Task<string> AccessRequests_ReviewMultiple(McpServer server,
        [Description("Review requests to process.")] string body = null)
        => PostAsync(server, "/v4/AccessRequests/BatchReview", body);
}
