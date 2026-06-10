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
/// carried over from safeguard-ps's mapping pending live verification
/// (<see cref="Inferred"/>). Inferred rows are typically those that
/// require additional setup to reach (SshKey checkout, SPS sessions,
/// expiry/acknowledgement). The planner surfaces <c>Inferred</c> rows
/// through an <c>inferred-not-verified-on-this-appliance</c> notice so
/// downstream consumers (agents, issue reporters) know the difference.
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
/// enum name verbatim). <see cref="RequesterAction"/> is the
/// sub-endpoint the planner POSTs when the caller is the requester
/// (and is not also acting as PolicyAdmin on someone else's request);
/// <see cref="AdminAction"/> is the sub-endpoint the planner POSTs
/// when the caller is PolicyAdmin acting on someone else's request,
/// or when the caller is both requester AND PolicyAdmin and the
/// requester branch resolves to <see cref="CloseAction.NeedsAdmin"/>.
/// The two axes are explicit so the matrix can encode both
/// safeguard-ps's per-state requester map AND the controller's
/// "PolicyAdmin can Close from any state the workflow permits"
/// behavior without conflating them.
/// </summary>
internal readonly record struct CloseStateRow(
    string State,
    CloseAction RequesterAction,
    CloseAction AdminAction,
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
///   * live-appliance verification driven by
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
    ///
    /// Two axes per row: <c>RequesterAction</c> is what the planner
    /// dispatches when the caller is acting as the requester (mirrors
    /// safeguard-ps's per-state map); <c>AdminAction</c> is what the
    /// planner dispatches when the caller is PolicyAdmin acting on
    /// someone else's request (or the requester+PolicyAdmin caller on
    /// a NeedsAdmin row). The admin column is <c>Close</c> for every
    /// non-terminal state -- the V4 controller (CloseAsync) gates on
    /// <c>[UserAuthorization(PolicyAdmin)]</c> + cluster-state
    /// <c>[AllowIfState(WithQuorum)]</c> only; there is NO workflow-
    /// state filter at the controller layer. The state machine's
    /// <c>.Permit(CloseRequest, ...)</c> entries (PendingReview,
    /// PendingPasswordReset, PendingAccountDemoted,
    /// PendingAccountSuspended, PendingAcknowledgement, plus
    /// PendingReview itself) accept Close from those workflow states,
    /// and safeguard-ps (<c>requests.psm1:1841-1899</c>) dispatches
    /// Close unconditionally for the admin branch. Terminal states
    /// (Closed/Complete/Reclaimed) keep <c>AdminAction = None</c> to
    /// match safeguard-ps's no-op there.
    /// </summary>
    internal static readonly CloseStateRow[] Rows =
    [
        // Cancel: pre-availability states the requester can always abort.
        new("New",                    CloseAction.Cancel,      CloseAction.Close, VerificationStatus.Verified, ""),
        new("PendingApproval",        CloseAction.Cancel,      CloseAction.Close, VerificationStatus.Verified, ""),
        new("Approved",               CloseAction.Cancel,      CloseAction.Close, VerificationStatus.Verified, ""),
        new("PendingTimeRequested",   CloseAction.Cancel,      CloseAction.Close, VerificationStatus.Inferred, "Requires a scheduled (not immediate) policy to reach."),
        new("RequestAvailable",       CloseAction.Cancel,      CloseAction.Close, VerificationStatus.Verified, ""),
        new("PendingAccountRestored", CloseAction.Cancel,      CloseAction.Close, VerificationStatus.Inferred, "Requires account-discovery workflow to reach."),
        new("PendingPasswordReset",   CloseAction.Cancel,      CloseAction.Close, VerificationStatus.Inferred, "Requires a check-in-triggered reset to reach. PolicyAdmin Close from this state has been observed against a live appliance; pending re-run by the verification harness to upgrade Status to Verified."),

        // CheckIn: post-checkout states with an active artifact.
        new("PasswordCheckedOut",     CloseAction.CheckIn,     CloseAction.Close, VerificationStatus.Verified, ""),
        new("SshKeyCheckedOut",       CloseAction.CheckIn,     CloseAction.Close, VerificationStatus.Inferred, "Requires an SshKey-type entitlement to reach."),
        new("SessionInitialized",     CloseAction.CheckIn,     CloseAction.Close, VerificationStatus.Inferred, "Requires SPS link or built-in session proxy to reach."),

        // NeedsAdmin: the requester has no viable sub-endpoint here
        // (Cancel/CheckIn/Acknowledge return 4xx per the V4
        // controller's per-action AllowIfState filters), but a
        // PolicyAdmin caller can dispatch Close. The controller's
        // CloseAsync method has NO workflow-state filter -- the state
        // machine's .Permit(CloseRequest, ...) entries accept Close
        // from each of these states.
        new("RequestCheckedIn",       CloseAction.NeedsAdmin,  CloseAction.Close, VerificationStatus.Inferred, "Reached after CheckIn from PasswordCheckedOut on auto-reset policies. Admin-side Close is permitted by the state machine; pending verification harness re-run to upgrade Status to Verified."),
        new("Terminated",             CloseAction.NeedsAdmin,  CloseAction.Close, VerificationStatus.Inferred, "Substate of Reclaimed reached after Cancel/Deny/Revoke. Admin-side Close is permitted by the state machine; pending verification harness re-run to upgrade Status to Verified."),
        new("PendingReview",          CloseAction.NeedsAdmin,  CloseAction.Close, VerificationStatus.Verified, "Live verification confirms Cancel/CheckIn/Acknowledge return 4xx for the requester here; admin-side Close succeeds."),
        new("PendingAccountSuspended",CloseAction.NeedsAdmin,  CloseAction.Close, VerificationStatus.Inferred, "Requires account-discovery workflow to reach. Admin-side Close is permitted by State.PendingAccountSuspended.cs:43 .Permit(CloseRequest, ...); pending verification harness re-run to upgrade Status to Verified."),
        new("PendingAccountDemoted",  CloseAction.NeedsAdmin,  CloseAction.Close, VerificationStatus.Inferred, "Requires account-discovery workflow to reach. Admin-side Close is permitted by State.PendingAccountDemoted.cs:43 .Permit(CloseRequest, ...); pending verification harness run to upgrade Status to Verified."),

        // Acknowledge: substates of Reclaimed/Closed awaiting requester
        // acknowledgement before they transition to Complete.
        new("Expired",                CloseAction.Acknowledge, CloseAction.Close, VerificationStatus.Inferred, "Substate of Reclaimed reached after the request's duration elapsed."),
        new("PendingAcknowledgment",  CloseAction.Acknowledge, CloseAction.Close, VerificationStatus.Inferred, "Substate of Closed reached when policy sets RequireRequesterAcknowledgement. Admin-side Close is permitted by State.PendingAcknowledgement.cs:20 .Permit(CloseRequest, ...)."),

        // Terminal states: no-op for both axes. safeguard-ps
        // requests.psm1:1841-1899 also no-ops these for admin callers.
        new("Closed",                 CloseAction.None,        CloseAction.None,  VerificationStatus.Verified, ""),
        new("Complete",               CloseAction.None,        CloseAction.None,  VerificationStatus.Verified, ""),
        new("Reclaimed",              CloseAction.None,        CloseAction.None,  VerificationStatus.Verified, ""),
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
