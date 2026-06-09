using System.ComponentModel;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.Tools;

/// <summary>
/// Composite tool that launches a Safeguard access request safely:
/// validates the inputs, pre-checks <c>/v4/Me/RequestEntitlements</c>
/// to catch the wrong-asset case (appliance error 90408) before any
/// write, and only then posts <c>NewAccessRequest</c>. The intent is
/// to replace agents' open-coded launch flows (which routinely lose
/// the asset-vs-account distinction) with one call that fails closed
/// when the entitlement does not match.
/// </summary>
[McpServerToolType]
internal sealed class SafeguardLaunchAccessRequestTool(ISafeguardSession session)
{
    [McpServerTool(Name = "Safeguard_LaunchAccessRequest", Title = "Launch Safeguard Access Request",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Submit a Safeguard access request after pre-validating the (account, asset, type) "
        + "combination against /v4/Me/RequestEntitlements. Catches the wrong-asset / wrong-type case "
        + "(appliance error 90408 'not authorized to use this request type') before any write hits the "
        + "appliance, so the agent never sees the misleading 'not authorized' message when the real "
        + "fault is that the policy is scoped to a different asset. "
        + "On success returns the new AccessRequest Id and State, plus a typed next-step pointer "
        + "(InitializeSession for session types, CheckOutPassword/CheckOutSshKey/etc. for credential "
        + "types). Does NOT auto-launch sessions or echo credentials. "
        + "Use Safeguard_Execute method=GET path=/v4/Me/RequestEntitlements to discover what you can "
        + "request before calling this tool.")]
    public async Task<string> Safeguard_LaunchAccessRequest(
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
        var inputs = new LaunchAccessRequestInputs
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

        var argErrors = LaunchAccessRequestPlanner.ValidateArgs(inputs);
        if (argErrors.Count > 0)
            throw new McpException(BuildErrorMessage("Validation failed before any appliance call.", argErrors));

        await session.EnsureReadyAsync(server, ct);

        // Pre-flight: read the user's entitlements scoped to the requested account+type.
        var canonicalType = LaunchAccessRequestPlanner.CanonicalAccessRequestType(accessRequestType);
        var query = LaunchAccessRequestPlanner.BuildEntitlementsQuery(accountId, canonicalType);
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

        var rows = LaunchAccessRequestPlanner.ParseEntitlements(entitlementsResponse.Body);
        var validation = LaunchAccessRequestPlanner.ValidateEntitlements(rows, accountId, canonicalType, assetId);
        if (!validation.Ok)
            throw new McpException(validation.ErrorMessage + "\nNo access request was submitted.");

        var resolvedAssetId = validation.ResolvedAssetId;

        // Build and POST the NewAccessRequest body.
        var body = LaunchAccessRequestPlanner.BuildNewAccessRequestJson(
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

        var (id, state) = LaunchAccessRequestPlanner.ExtractIdAndState(postResponse.Body);
        var summary = LaunchAccessRequestPlanner.BuildSuccessSummary(
            id, state, canonicalType, accountId, resolvedAssetId);

        var sb = new StringBuilder();
        sb.AppendLine(summary);
        sb.AppendLine();
        sb.AppendLine("AccessRequest body:");
        sb.Append(postResponse.Body ?? string.Empty);
        return sb.ToString();
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
