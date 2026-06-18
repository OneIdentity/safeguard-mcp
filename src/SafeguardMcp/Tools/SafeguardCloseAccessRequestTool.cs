using System.ComponentModel;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.Tools;

/// <summary>
/// Composite tool that closes a Safeguard access request safely by
/// dispatching to the correct sub-endpoint (Cancel / CheckIn / Close /
/// Acknowledge) based on the request's current State and the caller's
/// role. Replaces agents' open-coded "guess one of four POSTs" flow
/// with one call that picks the right verb deterministically and
/// returns a diagnostic naming the state when no verb is appropriate.
/// </summary>
[McpServerToolType]
internal sealed class SafeguardCloseAccessRequestTool(ISafeguardSession session)
{
    [McpServerTool(Name = "Safeguard_CloseAccessRequest", Title = "Close Safeguard Access Request",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Close a Safeguard access request, automatically dispatching to the correct action "
        + "(Cancel / CheckIn / Close / Acknowledge) based on the request's current State and the caller's role. "
        + "A 404 on the initial lookup means you neither own the request nor are a policy admin. "
        + "Terminal states (Closed/Complete/Reclaimed) no-op. comment is attached to Cancel/Close/Acknowledge "
        + "(ignored for CheckIn, truncated to 255 chars with a notice). "
        + "Returns { ok, action, request, notices }; allFields=true returns the appliance body verbatim, "
        + "else a compact field set.")]
    public async Task<string> Safeguard_CloseAccessRequest(
        McpServer server,
        [Description("Database id of the AccessRequest to close. Required. Use Safeguard_Execute "
            + "method=GET path=/v4/AccessRequests to find pending requests.")]
        string requestId,
        [Description("Free-text comment attached to Cancel / Close / Acknowledge. Ignored for CheckIn. "
            + "Truncated to 255 characters with a notice when longer.")]
        string comment = null,
        [Description("When true, returns the appliance's AccessRequest payload verbatim. When false "
            + "(default), projects to the safeguard-ps SgAccessRequestFields set "
            + "(Id, AccessRequestType, State, TicketNumber, IsEmergency, AssetId, AssetName, "
            + "AssetNetworkAddress, AccountId, AccountDomainName, AccountName).")]
        bool allFields = false,
        CancellationToken ct = default)
    {
        var inputs = new CloseAccessRequestInputs
        {
            RequestId = requestId,
            Comment = comment,
            AllFields = allFields,
        };

        var argErrors = CloseAccessRequestPlanner.ValidateArgs(inputs);
        if (argErrors.Count > 0)
            throw new McpException(BuildErrorMessage("Validation failed before any appliance call.", argErrors));

        await session.EnsureReadyAsync(server, ct);

        // Step 1: GET /v4/AccessRequests/{id} -- doubles as the permission gate.
        FullResponse getResponse;
        try
        {
            getResponse = await SafeguardInvoker.InvokeAsync(
                session, Service.Core, "GET", "AccessRequests/" + requestId,
                body: null,
                parameters: new Dictionary<string, string> { ["fields"] = "Id,State,RequesterId" },
                ct);
        }
        catch (McpException ex)
        {
            if (ex.Message.Contains("HTTP 404", StringComparison.Ordinal))
            {
                return CloseAccessRequestPlanner.BuildRefusalEnvelope(
                    $"You didn't request '{requestId}' and you are not a policy admin.",
                    notices: null);
            }
            throw;
        }

        var (state, requesterId, _) = CloseAccessRequestPlanner.ExtractGate(getResponse.Body);

        // Step 2: GET /v4/Me to learn the caller's id and admin roles.
        FullResponse meResponse;
        try
        {
            meResponse = await SafeguardInvoker.InvokeAsync(
                session, Service.Core, "GET", "Me",
                body: null,
                parameters: new Dictionary<string, string> { ["fields"] = "Id,AdminRoles" },
                ct);
        }
        catch (McpException ex)
        {
            throw new McpException("Failed to read /v4/Me for caller identity: " + ex.Message
                + "\nNo close action was attempted.");
        }

        var (myId, isPolicyAdmin) = CloseAccessRequestPlanner.ExtractMe(meResponse.Body);
        var isRequester = myId != 0 && requesterId != 0 && myId == requesterId;

        // Step 3: plan the dispatch.
        var notices = new List<Notice>();
        var truncated = CloseAccessRequestPlanner.TruncateComment(comment, notices);
        var plan = CloseAccessRequestPlanner.Plan(state, isRequester, isPolicyAdmin, truncated, requestId);
        if (plan.Notices != null)
            notices.AddRange(plan.Notices);

        if (!string.IsNullOrEmpty(plan.Refusal))
            return CloseAccessRequestPlanner.BuildRefusalEnvelope(plan.Refusal, notices);

        // Step 4: dispatch (or no-op for terminal states).
        if (string.Equals(plan.Action, "None", StringComparison.Ordinal))
        {
            var projection = CloseAccessRequestPlanner.ProjectAccessRequest(getResponse.Body, allFields);
            return CloseAccessRequestPlanner.BuildSuccessEnvelope("None", projection, notices);
        }

        FullResponse postResponse;
        try
        {
            postResponse = await SafeguardInvoker.InvokeAsync(
                session, Service.Core, "POST",
                $"AccessRequests/{requestId}/{plan.Action}",
                body: plan.Body,
                parameters: null,
                ct);
        }
        catch (McpException ex)
        {
            // Surface the underlying appliance error -- the planner picks
            // the right verb, but AllowIfState's allow-list lives on the
            // appliance and can refuse mid-window.
            throw new McpException($"Safeguard {plan.Action} failed for request {requestId}: " + ex.Message);
        }

        var projected = CloseAccessRequestPlanner.ProjectAccessRequest(postResponse.Body, allFields);
        return CloseAccessRequestPlanner.BuildSuccessEnvelope(plan.Action, projected, notices);
    }

    private static string BuildErrorMessage(string header, List<string> errors)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);
        foreach (var err in errors)
            sb.Append("  - ").AppendLine(err);
        return sb.ToString().TrimEnd();
    }
}
