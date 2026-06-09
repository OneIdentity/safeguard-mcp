namespace SafeguardMcp.Tools;

/// <summary>
/// The four sub-endpoints under <c>POST /v4/AccessRequests/{id}/...</c>
/// that an agent might be expected to call to "close" a request, plus
/// pseudo-actions used by the planner: <see cref="None"/> for terminal
/// states where no action is needed, and <see cref="NeedsAdmin"/> for
/// states where a non-PolicyAdmin requester has no viable endpoint and
/// the planner must return a diagnostic instead of guessing.
/// </summary>
internal enum CloseAction
{
    Cancel,
    CheckIn,
    Close,
    Acknowledge,
    None,
    NeedsAdmin,
    Unknown,
}

/// <summary>
/// Tags whether a row in <see cref="CloseAccessRequestStateMap"/> has
/// been observed on a real appliance (<see cref="Verified"/>) or is
/// carried over from safeguard-ps's mapping pending Tier-2/3
/// verification (<see cref="Inferred"/>). The planner surfaces
/// <c>Inferred</c> rows through an <c>inferred-not-verified-on-this-
/// appliance</c> notice so downstream consumers (agents, issue
/// reporters) know the difference.
/// </summary>
internal enum VerificationStatus
{
    Verified,
    Inferred,
}

/// <summary>
/// One row in the close-state dispatch table. <see cref="State"/> is
/// the literal string the appliance returns in
/// <c>AccessRequest.State</c> (matches the
/// <c>Pangaea.Data.Transfer.V2.AccessRequestWorkflow.AccessRequestState</c>
/// enum name verbatim).
/// </summary>
internal readonly record struct CloseStateRow(
    string State,
    CloseAction RequesterAction,
    VerificationStatus Status,
    string Note);

/// <summary>
/// Locked dispatch table from <c>AccessRequest.State</c> to the
/// sub-endpoint <c>Safeguard_CloseAccessRequest</c> POSTs on the
/// requester path. Built from:
///
///   * the PangaeaAppliance V4 controller's <c>UserAuthorization</c>
///     and <c>AllowIfState</c> attributes
///     (<c>src\Service\Core\Controllers\V4\Requests\AccessRequestsController.cs</c>),
///   * safeguard-ps's <c>Close-SafeguardAccessRequest</c> mapping
///     (<c>OneIdentity/safeguard-ps</c>, <c>src/requests.psm1</c>),
///   * Tier-1 / Tier-2 verification driven by
///     <c>CloseAccessRequestVerificationHarness</c> in the integration
///     test project.
///
/// Rows tagged <see cref="VerificationStatus.Inferred"/> are carried
/// over from safeguard-ps and have NOT yet been observed on a live
/// appliance from this codebase. When the verification harness runs
/// and the live response contradicts a row here, fix the row first --
/// do not silently absorb the divergence elsewhere.
/// </summary>
internal static class CloseAccessRequestStateMap
{
    /// <summary>
    /// Maximum length of the optional <c>comment</c> body the V4
    /// controller accepts on <c>Cancel</c> / <c>Close</c> /
    /// <c>Acknowledge</c> (see partial-class constant
    /// <c>MaxCommentLength = 255</c> inherited from V2 in
    /// <c>AccessRequestsController</c>). Longer comments are truncated
    /// by the planner with a notice rather than rejected outright so
    /// the agent's intent still reaches the appliance.
    /// </summary>
    internal const int MaxCommentLength = 255;

    /// <summary>
    /// Field projection used when <c>allFields=false</c>. Mirrors
    /// safeguard-ps's <c>$script:SgAccessRequestFields</c> so the
    /// MCP response matches the cmdlet output line-for-line.
    /// </summary>
    internal static readonly string[] SgAccessRequestFields =
    [
        "Id",
        "AccessRequestType",
        "State",
        "TicketNumber",
        "IsEmergency",
        "AssetId",
        "AssetName",
        "AssetNetworkAddress",
        "AccountId",
        "AccountDomainName",
        "AccountName",
    ];

    /// <summary>
    /// The locked dispatch table. Keep the order grouped by action so
    /// reviewers can scan the divergence row in isolation.
    /// </summary>
    internal static readonly CloseStateRow[] Rows =
    [
        // Cancel: pre-availability states the requester can always abort.
        new("New",                    CloseAction.Cancel,      VerificationStatus.Verified, ""),
        new("PendingApproval",        CloseAction.Cancel,      VerificationStatus.Verified, ""),
        new("Approved",               CloseAction.Cancel,      VerificationStatus.Verified, ""),
        new("PendingTimeRequested",   CloseAction.Cancel,      VerificationStatus.Inferred, "Requires a scheduled (not immediate) policy to reach."),
        new("RequestAvailable",       CloseAction.Cancel,      VerificationStatus.Verified, ""),
        new("PendingAccountRestored", CloseAction.Cancel,      VerificationStatus.Inferred, "Requires account-discovery workflow to reach."),
        new("PendingPasswordReset",   CloseAction.Cancel,      VerificationStatus.Inferred, "Requires a check-in-triggered reset to reach."),

        // CheckIn: post-checkout states with an active artifact.
        new("PasswordCheckedOut",     CloseAction.CheckIn,     VerificationStatus.Verified, ""),
        new("SshKeyCheckedOut",       CloseAction.CheckIn,     VerificationStatus.Inferred, "Requires an SshKey-type entitlement to reach."),
        new("SessionInitialized",     CloseAction.CheckIn,     VerificationStatus.Inferred, "Requires SPS link or built-in session proxy to reach."),

        // Divergence: safeguard-ps mapped these to Close, but the V4
        // controller requires PolicyAdmin for Close. For non-admin
        // requesters the planner returns a diagnostic naming the state
        // rather than guessing (Cancel/CheckIn return 4xx on at least
        // the Tier-1-verified PendingReview row). PolicyAdmin acting
        // on a request that reaches PendingReview falls into the
        // admin-on-other path and dispatches Close.
        new("RequestCheckedIn",       CloseAction.NeedsAdmin,  VerificationStatus.Inferred, "Reached after CheckIn from PasswordCheckedOut on auto-reset policies; planner refuses for non-PolicyAdmin requester pending Tier-1 verification of which sub-endpoint, if any, the requester can call."),
        new("Terminated",             CloseAction.NeedsAdmin,  VerificationStatus.Inferred, "Substate of Reclaimed reached after Cancel/Deny/Revoke. Non-admin requester has no viable endpoint per the controller's PolicyAdmin-only Close attribute."),
        new("PendingReview",          CloseAction.NeedsAdmin,  VerificationStatus.Verified, "Requires PolicyAdmin to close per the controller's Close attribute; Tier-1 confirms Cancel/CheckIn/Acknowledge return 4xx for the requester here."),
        new("PendingAccountSuspended",CloseAction.NeedsAdmin,  VerificationStatus.Inferred, "Requires account-discovery workflow to reach; non-admin requester has no viable endpoint per the controller's Close attribute."),

        // Acknowledge: substates of Reclaimed/Closed awaiting requester
        // acknowledgement before they transition to Complete.
        new("Expired",                CloseAction.Acknowledge, VerificationStatus.Inferred, "Substate of Reclaimed reached after the request's duration elapsed."),
        new("PendingAcknowledgment",  CloseAction.Acknowledge, VerificationStatus.Inferred, "Substate of Closed reached when policy sets RequireRequesterAcknowledgement."),

        // Terminal states: no-op.
        new("Closed",                 CloseAction.None,        VerificationStatus.Verified, ""),
        new("Complete",               CloseAction.None,        VerificationStatus.Verified, ""),
        new("Reclaimed",              CloseAction.None,        VerificationStatus.Verified, ""),
    ];

    /// <summary>
    /// Looks up a row by state name (case-insensitive). Returns
    /// <c>null</c> for unknown states so the planner can emit a
    /// diagnostic naming the state instead of falling through.
    /// </summary>
    internal static CloseStateRow? Find(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;
        for (int i = 0; i < Rows.Length; i++)
        {
            if (Rows[i].State.Equals(state, StringComparison.OrdinalIgnoreCase))
                return Rows[i];
        }
        return null;
    }
}
