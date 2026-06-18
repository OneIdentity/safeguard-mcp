using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SafeguardMcp.Tools;

/// <summary>
/// Inputs accepted by <c>Safeguard_OpenAccessRequest</c>. Carries only
/// values supplied by the caller; defaulting and required-ness are
/// applied by <see cref="OpenAccessRequestPlanner"/>.
/// </summary>
internal sealed class OpenAccessRequestInputs
{
    public int? AccountId { get; init; }
    public int? AssetId { get; init; }
    public string AccessRequestType { get; init; }
    public int? DurationDays { get; init; }
    public int? DurationHours { get; init; }
    public int? DurationMinutes { get; init; }
    public string ReasonComment { get; init; }
    public int? ReasonCodeId { get; init; }
    public string TicketNumber { get; init; }
    public bool IsEmergency { get; init; }
}

/// <summary>
/// One entitlement row pulled from <c>GET /v4/Me/RequestEntitlements</c>.
/// Only the fields needed for pre-flight selection are extracted; the
/// raw <see cref="JsonElement"/> tree is left untouched so callers don't
/// pay for a deep clone.
/// </summary>
internal readonly record struct EntitlementRow(
    int AccountId,
    string AccountName,
    int AssetId,
    string AssetName,
    string AccessRequestType,
    string PolicyName);

/// <summary>
/// Result of the pre-flight validation against the entitlements returned
/// by <c>/v4/Me/RequestEntitlements</c>. <see cref="Ok"/> means the tool
/// may submit; otherwise <see cref="ErrorMessage"/> is the agent-facing
/// diagnostic and the POST must be skipped.
/// </summary>
internal sealed class EntitlementValidationResult
{
    public bool Ok { get; init; }
    public string ErrorMessage { get; init; }
    public int ResolvedAssetId { get; init; }
    public string ResolvedAssetName { get; init; }
    public string PolicyName { get; init; }
}

/// <summary>
/// Pure-logic planner for <c>Safeguard_OpenAccessRequest</c>: argument
/// validation, entitlement-driven pre-flight, and request-body assembly.
/// No I/O, no DI — exists so the composite tool's correctness can be
/// covered by unit tests without standing up a session.
/// </summary>
internal static class OpenAccessRequestPlanner
{
    /// <summary>
    /// Closed set of <c>AccessRequestType</c> values the appliance accepts
    /// on <c>POST /v4/AccessRequests</c>. Mirrors the
    /// <c>AccessRequestType</c> enum on PangaeaAppliance; kept in lock-step
    /// with <c>Safeguard_Reference topic=enum name="AccessRequestType"</c>.
    /// </summary>
    internal static readonly string[] KnownAccessRequestTypes =
    [
        "Password",
        "RemoteDesktop",
        "Ssh",
        "Telnet",
        "SshKey",
        "RemoteDesktopApplication",
        "ApiKey",
        "File",
    ];

    /// <summary>
    /// Validates the caller-supplied arguments against the
    /// <c>NewAccessRequest</c> contract: account id, request type,
    /// reason comment, and at least one of the three duration buckets
    /// must be present. Returns an empty list when the request is
    /// well-formed.
    /// </summary>
    public static List<string> ValidateArgs(OpenAccessRequestInputs inputs)
    {
        var errors = new List<string>();
        if (inputs == null)
        {
            errors.Add("Inputs are required.");
            return errors;
        }

        if (!inputs.AccountId.HasValue || inputs.AccountId.Value <= 0)
            errors.Add("accountId is required and must be a positive integer.");

        if (string.IsNullOrWhiteSpace(inputs.AccessRequestType))
        {
            errors.Add("accessRequestType is required (e.g. 'Password', 'RemoteDesktop', 'Ssh'). "
                + "Use Safeguard_Reference topic=enum name=\"AccessRequestType\" to list valid values.");
        }
        else if (!IsKnownAccessRequestType(inputs.AccessRequestType))
        {
            errors.Add($"accessRequestType '{inputs.AccessRequestType}' is not a valid AccessRequestType. "
                + $"Valid values: {string.Join(", ", KnownAccessRequestTypes)}.");
        }

        var hasAnyDuration = (inputs.DurationDays ?? 0) > 0
            || (inputs.DurationHours ?? 0) > 0
            || (inputs.DurationMinutes ?? 0) > 0;
        if (!hasAnyDuration)
        {
            errors.Add("A duration is required: pass at least one of durationDays, durationHours, "
                + "or durationMinutes (sum must not exceed 31 days). Use the entitlement's policy "
                + "DefaultReleaseDuration as a starting point.");
        }
        else
        {
            if ((inputs.DurationDays ?? 0) < 0)
                errors.Add("durationDays must be non-negative.");
            if ((inputs.DurationHours ?? 0) < 0)
                errors.Add("durationHours must be non-negative.");
            if ((inputs.DurationMinutes ?? 0) < 0)
                errors.Add("durationMinutes must be non-negative.");
        }

        if (string.IsNullOrWhiteSpace(inputs.ReasonComment))
            errors.Add("reasonComment is required (free-text justification, max 1000 chars). "
                + "If the policy also requires a reason code, pass reasonCodeId.");

        return errors;
    }

    /// <summary>Case-insensitive membership check against the enum vocabulary.</summary>
    public static bool IsKnownAccessRequestType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        for (int i = 0; i < KnownAccessRequestTypes.Length; i++)
        {
            if (KnownAccessRequestTypes[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the canonical spelling of <paramref name="value"/> (e.g.
    /// "remotedesktop" -> "RemoteDesktop") so the body sent to the
    /// appliance matches the enum the API rejects case-folded variants on.
    /// </summary>
    public static string CanonicalAccessRequestType(string value)
    {
        for (int i = 0; i < KnownAccessRequestTypes.Length; i++)
        {
            if (KnownAccessRequestTypes[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                return KnownAccessRequestTypes[i];
        }
        return value;
    }

    /// <summary>
    /// Parses the JSON array returned by
    /// <c>GET /v4/Me/RequestEntitlements?accountIds=&lt;id&gt;&amp;accessRequestType=&lt;type&gt;</c>
    /// into the subset of fields the planner needs. Tolerates an empty
    /// body and a body wrapped in a Safeguard_Execute envelope (the live
    /// tool path) — falls back to scanning the root element directly when
    /// no <c>data</c> property is present (raw appliance response).
    /// </summary>
    public static List<EntitlementRow> ParseEntitlements(string json)
    {
        var rows = new List<EntitlementRow>();
        if (string.IsNullOrWhiteSpace(json))
            return rows;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException) { return rows; }

        using (doc)
        {
            var array = doc.RootElement;
            if (array.ValueKind == JsonValueKind.Object
                && array.TryGetProperty("data", out var dataElem))
            {
                array = dataElem;
            }
            if (array.ValueKind != JsonValueKind.Array)
                return rows;

            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                int accountId = 0;
                string accountName = null;
                int assetId = 0;
                string assetName = null;
                string policyName = null;
                string requestType = null;

                if (item.TryGetProperty("Account", out var accountElem)
                    && accountElem.ValueKind == JsonValueKind.Object)
                {
                    if (accountElem.TryGetProperty("Id", out var aIdElem)
                        && aIdElem.ValueKind == JsonValueKind.Number)
                        aIdElem.TryGetInt32(out accountId);
                    if (accountElem.TryGetProperty("Name", out var aNameElem)
                        && aNameElem.ValueKind == JsonValueKind.String)
                        accountName = aNameElem.GetString();
                }

                if (item.TryGetProperty("Asset", out var assetElem)
                    && assetElem.ValueKind == JsonValueKind.Object)
                {
                    if (assetElem.TryGetProperty("Id", out var sIdElem)
                        && sIdElem.ValueKind == JsonValueKind.Number)
                        sIdElem.TryGetInt32(out assetId);
                    if (assetElem.TryGetProperty("Name", out var sNameElem)
                        && sNameElem.ValueKind == JsonValueKind.String)
                        assetName = sNameElem.GetString();
                }

                if (item.TryGetProperty("Policy", out var policyElem)
                    && policyElem.ValueKind == JsonValueKind.Object)
                {
                    if (policyElem.TryGetProperty("Name", out var pNameElem)
                        && pNameElem.ValueKind == JsonValueKind.String)
                        policyName = pNameElem.GetString();

                    if (policyElem.TryGetProperty("AccessRequestProperties", out var arpElem)
                        && arpElem.ValueKind == JsonValueKind.Object
                        && arpElem.TryGetProperty("AccessRequestType", out var artElem)
                        && artElem.ValueKind == JsonValueKind.String)
                    {
                        requestType = artElem.GetString();
                    }
                }

                rows.Add(new EntitlementRow(
                    accountId, accountName, assetId, assetName,
                    requestType ?? string.Empty, policyName ?? string.Empty));
            }
        }

        return rows;
    }

    /// <summary>
    /// Pre-flight: given the entitlements the user already has for the
    /// requested (accountId, type) pair, decide whether the POST can
    /// proceed and which asset it should target. Catches the wrong-asset
    /// case (E036/90408) and the no-entitlements case before any write
    /// hits the appliance.
    /// </summary>
    public static EntitlementValidationResult ValidateEntitlements(
        IReadOnlyList<EntitlementRow> entitlements,
        int accountId,
        string accessRequestType,
        int? assetId)
    {
        if (entitlements == null || entitlements.Count == 0)
        {
            return new EntitlementValidationResult
            {
                Ok = false,
                ErrorMessage = $"Account {accountId} has 0 '{accessRequestType}' entitlements for you. "
                    + "Run Safeguard_Execute method=GET path=/v4/Me/RequestEntitlements "
                    + $"query=accountIds={accountId} to see what access types this account has for you. "
                    + "If the list is empty, request the entitlement from your Safeguard administrator."
            };
        }

        // Entitlements for the requested account+type pair. The API filter
        // on accessRequestType is best-effort; re-filter here so an SDK
        // path that returned a broader set still gets pruned down.
        var matchingType = new List<EntitlementRow>();
        for (int i = 0; i < entitlements.Count; i++)
        {
            var row = entitlements[i];
            if (row.AccountId != accountId)
                continue;
            if (!string.IsNullOrEmpty(row.AccessRequestType)
                && !row.AccessRequestType.Equals(accessRequestType, StringComparison.OrdinalIgnoreCase))
                continue;
            matchingType.Add(row);
        }

        if (matchingType.Count == 0)
        {
            // Entitlements existed for the account but none of the requested type.
            var seenTypes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in entitlements)
            {
                if (row.AccountId == accountId && !string.IsNullOrEmpty(row.AccessRequestType))
                    seenTypes.Add(row.AccessRequestType);
            }
            var seenList = seenTypes.Count == 0 ? "(none)" : string.Join(", ", seenTypes);
            return new EntitlementValidationResult
            {
                Ok = false,
                ErrorMessage = $"Account {accountId} has no '{accessRequestType}' entitlement for you. "
                    + $"Entitlement types available on this account: {seenList}. "
                    + "Pick one of those, or request the missing entitlement from your Safeguard administrator."
            };
        }

        if (assetId.HasValue && assetId.Value > 0)
        {
            for (int i = 0; i < matchingType.Count; i++)
            {
                if (matchingType[i].AssetId == assetId.Value)
                {
                    return new EntitlementValidationResult
                    {
                        Ok = true,
                        ResolvedAssetId = matchingType[i].AssetId,
                        ResolvedAssetName = matchingType[i].AssetName,
                        PolicyName = matchingType[i].PolicyName
                    };
                }
            }

            // Wrong asset — name the allowed ones so the agent can fix in
            // a single follow-up rather than guessing.
            var allowed = new StringBuilder();
            for (int i = 0; i < matchingType.Count; i++)
            {
                if (i > 0) allowed.Append(", ");
                allowed.Append("AssetId=").Append(matchingType[i].AssetId.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(matchingType[i].AssetName))
                    allowed.Append(" (").Append(matchingType[i].AssetName).Append(')');
            }

            return new EntitlementValidationResult
            {
                Ok = false,
                ErrorMessage = $"Account {accountId} has '{accessRequestType}' entitlements only on {allowed}; "
                    + $"the requested AssetId={assetId.Value} is not in scope. "
                    + "Submitting anyway would return appliance error 90408 ('not authorized to use this request type'), "
                    + "which is the wrong-asset error in disguise. Re-call with an assetId from the list above, "
                    + "or omit assetId to let the tool pick when there is exactly one match."
            };
        }

        // assetId omitted: infer when unambiguous; list when not.
        var distinctAssets = new Dictionary<int, EntitlementRow>();
        foreach (var row in matchingType)
        {
            if (!distinctAssets.ContainsKey(row.AssetId))
                distinctAssets[row.AssetId] = row;
        }

        if (distinctAssets.Count == 1)
        {
            var only = distinctAssets.Values.First();
            return new EntitlementValidationResult
            {
                Ok = true,
                ResolvedAssetId = only.AssetId,
                ResolvedAssetName = only.AssetName,
                PolicyName = only.PolicyName
            };
        }

        var listed = new StringBuilder();
        var ordered = distinctAssets.Values.OrderBy(r => r.AssetId).ToArray();
        for (int i = 0; i < ordered.Length; i++)
        {
            if (i > 0) listed.Append(", ");
            listed.Append("AssetId=").Append(ordered[i].AssetId.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(ordered[i].AssetName))
                listed.Append(" (").Append(ordered[i].AssetName).Append(')');
        }

        return new EntitlementValidationResult
        {
            Ok = false,
            ErrorMessage = $"Account {accountId} has '{accessRequestType}' entitlements on multiple assets: {listed}. "
                + "Re-call with assetId set to the one you want."
        };
    }

    /// <summary>
    /// Builds the <c>NewAccessRequest</c> JSON body the appliance expects
    /// on <c>POST /v4/AccessRequests</c>. Field names mirror the
    /// <c>NewAccessRequest</c> DataContract exactly.
    /// </summary>
    public static string BuildNewAccessRequestJson(
        int accountId,
        int assetId,
        string accessRequestType,
        int? durationDays,
        int? durationHours,
        int? durationMinutes,
        string reasonComment,
        int? reasonCodeId,
        string ticketNumber,
        bool isEmergency)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("AccountId", accountId);
            writer.WriteNumber("AssetId", assetId);
            writer.WriteString("AccessRequestType", CanonicalAccessRequestType(accessRequestType));
            writer.WriteBoolean("IsEmergency", isEmergency);
            if (reasonCodeId.HasValue)
                writer.WriteNumber("ReasonCodeId", reasonCodeId.Value);
            if (!string.IsNullOrWhiteSpace(reasonComment))
                writer.WriteString("ReasonComment", reasonComment);
            if (durationDays.HasValue && durationDays.Value > 0)
                writer.WriteNumber("RequestedDurationDays", durationDays.Value);
            if (durationHours.HasValue && durationHours.Value > 0)
                writer.WriteNumber("RequestedDurationHours", durationHours.Value);
            if (durationMinutes.HasValue && durationMinutes.Value > 0)
                writer.WriteNumber("RequestedDurationMinutes", durationMinutes.Value);
            if (!string.IsNullOrWhiteSpace(ticketNumber))
                writer.WriteString("TicketNumber", ticketNumber);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// Builds the query string for the entitlements pre-flight call. The
    /// API takes <c>accountIds</c> as a comma-separated list and
    /// <c>accessRequestType</c> as a single enum value.
    /// </summary>
    public static string BuildEntitlementsQuery(int accountId, string accessRequestType)
    {
        var canonical = CanonicalAccessRequestType(accessRequestType);
        return string.Format(
            CultureInfo.InvariantCulture,
            "accountIds={0}&accessRequestType={1}&fields=Account.Id,Account.Name,Asset.Id,Asset.Name,Policy.Name,Policy.AccessRequestProperties.AccessRequestType",
            accountId,
            Uri.EscapeDataString(canonical));
    }

    /// <summary>
    /// AccessRequestState values that end the auto-approve wait early
    /// because no further useful transition will happen within the 5-second
    /// window. Mirrors
    /// <c>Pangaea.Data.Transfer.V2.AccessRequestWorkflow.AccessRequestState</c>
    /// — only the names that drive the outcome classification are listed;
    /// all other states keep the loop spinning until the deadline.
    /// </summary>
    public static bool IsTerminalForAutoApproveWait(string state)
    {
        if (string.IsNullOrEmpty(state))
            return false;
        switch (state)
        {
            case "RequestAvailable":
            case "PendingTimeRequested":
            case "Terminated":
            case "Expired":
            case "Closed":
            case "Complete":
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Pulls the appliance's clock from the <c>Date</c> response header so
    /// <c>PendingTimeRequested</c> classification compares
    /// <c>RequestedFor</c> against the same clock the appliance used. Falls
    /// back to local UTC when the header is missing or unparseable; in that
    /// fallback we accept the small clock-skew risk because the alternative
    /// (refusing to classify) is worse.
    /// </summary>
    public static DateTimeOffset ParseApplianceNowOrLocal(IDictionary<string, string> headers)
    {
        if (headers != null && headers.TryGetValue("Date", out var dateHeader)
            && DateTimeOffset.TryParse(dateHeader, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return parsed;
        }
        return DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reads <c>RequestedFor</c> (the V2 access-request payload's scheduled
    /// time) from the raw appliance JSON. Returns <c>null</c> when the
    /// property is absent or unparseable.
    /// </summary>
    public static DateTimeOffset? ExtractRequestedFor(string accessRequestJson)
    {
        if (string.IsNullOrWhiteSpace(accessRequestJson))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(accessRequestJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            if (!doc.RootElement.TryGetProperty("RequestedFor", out var elem))
                return null;
            if (elem.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(elem.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                return parsed;
            }
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Builds the structured notice attached to the response envelope after
    /// the brief auto-approve wait. Exactly one notice is produced; the
    /// kind drives all next-step prose so the agent can branch without
    /// parsing the access-request payload directly.
    /// </summary>
    public static Notice ClassifyAutoApproveOutcome(
        string state,
        string accessRequestId,
        string accessRequestType,
        string accessRequestJson,
        DateTimeOffset applianceNow)
    {
        var id = string.IsNullOrEmpty(accessRequestId) ? "{id}" : accessRequestId;
        var canonical = CanonicalAccessRequestType(accessRequestType);

        switch (state)
        {
            case "RequestAvailable":
                return BuildReadyNotice(id, canonical);

            case "PendingApproval":
                return new Notice(
                    NoticeKinds.PendingApprovalCheckBack,
                    "Request " + id + " was submitted but requires human approval, "
                        + "which can take hours. The tool did not keep waiting; the request "
                        + "stays open on the appliance.",
                    "Check back via Safeguard_Execute method=GET path=/v4/AccessRequests/" + id
                        + ". You can also call Safeguard_RetrieveCredential directly when you "
                        + "believe the request is ready — it returns a clear error if not.");

            case "PendingTimeRequested":
            {
                var scheduled = ExtractRequestedFor(accessRequestJson);
                // We compare against the appliance clock (Date header). If the
                // header was missing, ParseApplianceNowOrLocal fell back to
                // local UTC, which can mis-classify near-future schedules by
                // up to a few seconds — acceptable here because the message
                // text names the scheduled time so the operator sees the truth.
                var scheduledLabel = scheduled.HasValue
                    ? scheduled.Value.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture)
                    : "a future time";
                var inFuture = scheduled.HasValue && scheduled.Value > applianceNow;
                var message = inFuture
                    ? "Request " + id + " is approved but scheduled for " + scheduledLabel
                        + ". It will become available then."
                    : "Request " + id + " is approved with a scheduled start time of "
                        + scheduledLabel + ".";
                return new Notice(
                    NoticeKinds.PendingScheduled,
                    message,
                    "Check back via Safeguard_Execute method=GET path=/v4/AccessRequests/" + id
                        + " once the scheduled time has passed.");
            }

            case "PendingAccountElevated":
            case "PendingAccountRestored":
            {
                var verb = state == "PendingAccountElevated" ? "elevate" : "restore";
                return new Notice(
                    NoticeKinds.PendingAccountAction,
                    "Request " + id + " is waiting for the appliance to " + verb
                        + " the account. This usually completes in well under a minute.",
                    "Check back via Safeguard_Execute method=GET path=/v4/AccessRequests/" + id + ".");
            }

            case "Terminated":
            case "Expired":
            case "Closed":
            case "Complete":
                return new Notice(
                    NoticeKinds.TerminatedBeforeReady,
                    "Request " + id + " ended in state " + state
                        + " before becoming available (denied, expired, or closed mid-flow).",
                    "Submit a fresh request if access is still needed; this request id "
                        + "cannot be reopened.");

            default:
                // Anything we don't recognise (including New, Approved without a
                // sub-state, etc.) is conservatively treated as approval-pending
                // so the agent gets a check-back call rather than silence.
                return new Notice(
                    NoticeKinds.PendingApprovalCheckBack,
                    "Request " + id + " is in state " + (state ?? "<unknown>")
                        + " and did not reach RequestAvailable within the auto-approve wait.",
                    "Check back via Safeguard_Execute method=GET path=/v4/AccessRequests/" + id + ".");
        }
    }

    private static Notice BuildReadyNotice(string id, string canonical)
    {
        switch (canonical)
        {
            case "Password":
                return new Notice(
                    NoticeKinds.AutoApprovedReady,
                    "Request " + id + " is available. The password is ready to retrieve.",
                    "Call Safeguard_RetrieveCredential kind=\"access-request-password\" "
                        + "accessRequestId=" + id + ". Do NOT call "
                        + "/v4/AccessRequests/" + id + "/CheckOutPassword via Safeguard_Execute — "
                        + "that path is refuse-and-redirected. POST /v4/AccessRequests/" + id
                        + "/CheckIn via Safeguard_Execute when done.");
            case "SshKey":
                return new Notice(
                    NoticeKinds.AutoApprovedReady,
                    "Request " + id + " is available. The SSH private key is ready to retrieve.",
                    "Call Safeguard_RetrieveCredential kind=\"access-request-ssh-key\" "
                        + "accessRequestId=" + id + ". POST /v4/AccessRequests/" + id
                        + "/CheckIn via Safeguard_Execute when done.");
            case "ApiKey":
                return new Notice(
                    NoticeKinds.AutoApprovedReady,
                    "Request " + id + " is available. The API key(s) are ready to retrieve.",
                    "Call Safeguard_RetrieveCredential kind=\"access-request-api-key\" "
                        + "accessRequestId=" + id + ". POST /v4/AccessRequests/" + id
                        + "/CheckIn via Safeguard_Execute when done.");
            case "File":
                return new Notice(
                    NoticeKinds.AutoApprovedReady,
                    "Request " + id + " is available. The file content is ready to retrieve.",
                    "Call Safeguard_RetrieveCredential kind=\"access-request-file\" "
                        + "accessRequestId=" + id + ". POST /v4/AccessRequests/" + id
                        + "/CheckIn via Safeguard_Execute when done.");
            case "RemoteDesktop":
            case "RemoteDesktopApplication":
            case "Ssh":
            case "Telnet":
                return new Notice(
                    NoticeKinds.AutoApprovedReady,
                    "Request " + id + " is available. The session can be initialized now.",
                    "POST /v4/AccessRequests/" + id + "/InitializeSession via Safeguard_Execute "
                        + "(body {}) to get the connection artifact (RDP file / SSH or Telnet "
                        + "connection string). Do NOT hand-build the .rdp file. POST "
                        + "/v4/AccessRequests/" + id + "/CheckIn via Safeguard_Execute when done.");
            default:
                return new Notice(
                    NoticeKinds.AutoApprovedReady,
                    "Request " + id + " is available.",
                    "Continue per the access-request-type follow-up: credential types use "
                        + "Safeguard_RetrieveCredential, session types use "
                        + "POST /v4/AccessRequests/" + id + "/InitializeSession via Safeguard_Execute.");
        }
    }

    /// <summary>
    /// Renders the structured envelope returned by
    /// <c>Safeguard_OpenAccessRequest</c>:
    /// <c>{ "data": &lt;access-request JSON&gt;, "meta": { "notices": [&lt;one notice&gt;] } }</c>.
    /// The <c>data</c> field carries the appliance's raw payload so the
    /// agent reads State / Id / RequestedFor without parsing the notice.
    /// </summary>
    public static string BuildOpenAccessRequestEnvelope(string accessRequestJson, Notice notice)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();

            writer.WritePropertyName("data");
            WriteRawDataValue(writer, accessRequestJson);

            writer.WritePropertyName("meta");
            writer.WriteStartObject();
            writer.WritePropertyName("notices");
            writer.WriteStartArray();
            if (notice != null)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", notice.Kind);
                writer.WriteString("message", notice.Message ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(notice.Suggestion))
                    writer.WriteString("suggestion", notice.Suggestion);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteRawDataValue(Utf8JsonWriter writer, string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            writer.WriteNullValue();
            return;
        }
        try
        {
            using var document = JsonDocument.Parse(body);
            document.RootElement.WriteTo(writer);
        }
        catch (JsonException)
        {
            writer.WriteStringValue(body);
        }
    }

    /// <summary>
    /// Extracts <c>Id</c> and <c>State</c> from the appliance's
    /// <c>AccessRequest</c> POST response so the summary line can name
    /// them without re-parsing in the caller.
    /// </summary>
    public static (string Id, string State) ExtractIdAndState(string accessRequestJson)
    {
        if (string.IsNullOrWhiteSpace(accessRequestJson))
            return (null, null);
        try
        {
            using var doc = JsonDocument.Parse(accessRequestJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return (null, null);

            string id = null;
            string state = null;
            if (doc.RootElement.TryGetProperty("Id", out var idElem))
            {
                id = idElem.ValueKind switch
                {
                    JsonValueKind.String => idElem.GetString(),
                    JsonValueKind.Number => idElem.GetRawText(),
                    _ => null
                };
            }
            if (doc.RootElement.TryGetProperty("State", out var stateElem)
                && stateElem.ValueKind == JsonValueKind.String)
            {
                state = stateElem.GetString();
            }
            return (id, state);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }
}
