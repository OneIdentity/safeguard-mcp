using System.ComponentModel;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.Tools;

/// <summary>
/// Composite tool that opens a Safeguard access request safely:
/// validates the inputs, pre-checks <c>/v4/Me/RequestEntitlements</c>
/// to catch the wrong-asset case (appliance error 90408) before any
/// write, and only then posts <c>NewAccessRequest</c>. The intent is
/// to replace agents' open-coded request flows (which routinely lose
/// the asset-vs-account distinction) with one call that fails closed
/// when the entitlement does not match.
/// </summary>
[McpServerToolType]
internal sealed class SafeguardOpenAccessRequestTool(ISafeguardSession session)
{
    [McpServerTool(Name = "Safeguard_OpenAccessRequest", Title = "Open Safeguard Access Request",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Submit a Safeguard access request, pre-validating the (account, asset, type) combination "
        + "against /v4/Me/RequestEntitlements so a policy scoped to a different asset fails fast instead of "
        + "returning the misleading 90408 'not authorized'. Waits up to 5s for auto-approval so common cases "
        + "(self-approve, no-approval, emergency) return ready in one call; otherwise returns immediately. "
        + "Returns { data, meta }; meta.notices[0].kind names the next step (auto_approved_ready, "
        + "pending_approval_check_back, pending_scheduled, pending_account_action, terminated_before_ready). "
        + "Never auto-launches sessions or echoes credentials. "
        + "Call GET /v4/Me/RequestEntitlements first to see what you can request. "
        + "See safeguard://common-patterns for the notice-kind lifecycle and session-launch convention.")]
    public async Task<string> Safeguard_OpenAccessRequest(
        McpServer server,
        [Description("Database id of the account to request access to. Required. For domain-controller "
            + "/ linked-account policies, pass the linked account's id as returned by RequestEntitlements.")]
        int accountId,
        [Description("AccessRequestType: Password, RemoteDesktop, Ssh, Telnet, SshKey, "
            + "RemoteDesktopApplication, ApiKey, or File. Required. Case-insensitive; canonicalized "
            + "before submission. Use Safeguard_Enum name=\"AccessRequestType\" to list values.")]
        string accessRequestType,
        [Description("Database id of the asset. Optional: when omitted and the (account, type) pair has "
            + "exactly one entitlement, the tool infers it; if there are multiple entitlements the tool "
            + "fails fast with the candidate list so you can pick.")]
        int? assetId = null,
        [Description("Requested duration in days (0-31).")]
        int? durationDays = null,
        [Description("Requested duration in hours (0-23).")]
        int? durationHours = null,
        [Description("Requested duration in minutes (0-59). Sum of days+hours+minutes must be > 0 and not exceed 31 days.")]
        int? durationMinutes = null,
        [Description("Free-text justification (max 1000 chars). Required.")]
        string reasonComment = null,
        [Description("Database id of a pre-defined reason code on the policy. Required only when the "
            + "policy sets RequireReasonCode.")]
        int? reasonCodeId = null,
        [Description("Help-desk ticket number. Required only when the policy sets RequireServiceTicket.")]
        string ticketNumber = null,
        [Description("Whether to request emergency access (subject to policy). Default false.")]
        bool isEmergency = false,
        CancellationToken ct = default)
    {
        var inputs = new OpenAccessRequestInputs
        {
            AccountId = accountId,
            AssetId = assetId,
            AccessRequestType = accessRequestType,
            DurationDays = durationDays,
            DurationHours = durationHours,
            DurationMinutes = durationMinutes,
            ReasonComment = reasonComment,
            ReasonCodeId = reasonCodeId,
            TicketNumber = ticketNumber,
            IsEmergency = isEmergency,
        };

        var argErrors = OpenAccessRequestPlanner.ValidateArgs(inputs);
        if (argErrors.Count > 0)
            throw new McpException(BuildErrorMessage("Validation failed before any appliance call.", argErrors));

        await session.EnsureReadyAsync(server, ct);

        // Pre-flight: read the user's entitlements scoped to the requested account+type.
        var canonicalType = OpenAccessRequestPlanner.CanonicalAccessRequestType(accessRequestType);
        var query = OpenAccessRequestPlanner.BuildEntitlementsQuery(accountId, canonicalType);
        var parameters = ApiToolHelpers.ParseQueryParameters(query);

        FullResponse entitlementsResponse;
        try
        {
            entitlementsResponse = await SafeguardInvoker.InvokeAsync(
                session, Service.Core, "GET", "Me/RequestEntitlements",
                body: null, parameters, ct);
        }
        catch (McpException ex)
        {
            throw new McpException("Pre-flight entitlement check failed: " + ex.Message
                + "\nNo access request was submitted.");
        }

        var rows = OpenAccessRequestPlanner.ParseEntitlements(entitlementsResponse.Body);
        var validation = OpenAccessRequestPlanner.ValidateEntitlements(rows, accountId, canonicalType, assetId);
        if (!validation.Ok)
            throw new McpException(validation.ErrorMessage + "\nNo access request was submitted.");

        var resolvedAssetId = validation.ResolvedAssetId;

        // Build and POST the NewAccessRequest body.
        var body = OpenAccessRequestPlanner.BuildNewAccessRequestJson(
            accountId,
            resolvedAssetId,
            canonicalType,
            durationDays,
            durationHours,
            durationMinutes,
            reasonComment,
            reasonCodeId,
            ticketNumber,
            isEmergency);

        FullResponse postResponse;
        try
        {
            postResponse = await SafeguardInvoker.InvokeAsync(
                session, Service.Core, "POST", "AccessRequests",
                body, parameters: null, ct);
        }
        catch (McpException ex)
        {
            // The pre-flight should have caught 90408; if the appliance
            // still returned it, surface the original message intact so
            // the user can act on it.
            throw new McpException("Access request submission failed: " + ex.Message
                + "\nThe pre-flight entitlement check passed but the appliance rejected the request. "
                + "Use Safeguard_Execute method=GET path=/v4/Me/RequestEntitlements to re-check the live state.");
        }

        var (initialId, initialState) = OpenAccessRequestPlanner.ExtractIdAndState(postResponse.Body);

        var (finalBody, finalState, finalHeaders) = await WaitForAutoApproveAsync(
            initialId, postResponse.Body, initialState, postResponse.Headers, ct);

        var applianceNow = OpenAccessRequestPlanner.ParseApplianceNowOrLocal(finalHeaders);
        var notice = OpenAccessRequestPlanner.ClassifyAutoApproveOutcome(
            finalState, initialId, canonicalType, finalBody, applianceNow);

        return OpenAccessRequestPlanner.BuildOpenAccessRequestEnvelope(finalBody, notice);
    }

    // Polls GET /v4/AccessRequests/{id} every 250 ms for a hard 5-second
    // cap. No back-off, no caller-tunable cadence, no parameter: the point
    // is "wait briefly for auto-approve, then stop." Worst case is ~20 GET
    // probes per submission. Stops on the first terminal-or-actionable
    // state per OpenAccessRequestPlanner.IsTerminalForAutoApproveWait.
    private async Task<(string Body, string State, IDictionary<string, string> Headers)> WaitForAutoApproveAsync(
        string accessRequestId,
        string initialBody,
        string initialState,
        IDictionary<string, string> initialHeaders,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(accessRequestId)
            || OpenAccessRequestPlanner.IsTerminalForAutoApproveWait(initialState ?? string.Empty))
        {
            return (initialBody, initialState, initialHeaders);
        }

        var body = initialBody;
        var state = initialState;
        var headers = initialHeaders;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        var relativeUrl = "AccessRequests/" + accessRequestId;
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            FullResponse poll;
            try
            {
                poll = await SafeguardInvoker.InvokeAsync(
                    session, Service.Core, "GET", relativeUrl,
                    body: null, parameters: null, ct);
            }
            catch (McpException)
            {
                // GET probe failed mid-wait — stop and surface what we have.
                break;
            }

            body = poll.Body;
            headers = poll.Headers;
            var (_, polled) = OpenAccessRequestPlanner.ExtractIdAndState(body);
            if (!string.IsNullOrEmpty(polled))
                state = polled;
            if (OpenAccessRequestPlanner.IsTerminalForAutoApproveWait(state ?? string.Empty))
                break;
        }
        return (body, state, headers);
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
