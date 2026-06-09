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
    /// with <c>Safeguard_Enum name="AccessRequestType"</c>.
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
                + "Use Safeguard_Enum name=\"AccessRequestType\" to list valid values.");
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
    /// Formats the final response after a successful POST. Strips no
    /// information — the raw <c>AccessRequest</c> payload is returned
    /// verbatim — but prepends a single line of guidance pointing at the
    /// correct follow-up endpoint for the request type so the agent can
    /// complete the launch use-case without guessing.
    /// </summary>
    public static string BuildSuccessSummary(
        string accessRequestId,
        string state,
        string accessRequestType,
        int accountId,
        int assetId)
    {
        var sb = new StringBuilder();
        sb.Append("Access request created. Id=").Append(accessRequestId ?? "<unknown>")
            .Append(", State=").Append(state ?? "<unknown>")
            .Append(", AccountId=").Append(accountId.ToString(CultureInfo.InvariantCulture))
            .Append(", AssetId=").Append(assetId.ToString(CultureInfo.InvariantCulture))
            .Append(", Type=").Append(CanonicalAccessRequestType(accessRequestType))
            .AppendLine(".");

        sb.AppendLine("Next steps:");
        sb.AppendLine("  1. Poll GET /v4/AccessRequests/" + (accessRequestId ?? "{id}") + " until State is Available/Approved (auto-approve policies skip Pending).");

        var canonical = CanonicalAccessRequestType(accessRequestType);
        switch (canonical)
        {
            case "RemoteDesktop":
            case "RemoteDesktopApplication":
            case "Ssh":
            case "Telnet":
                sb.AppendLine("  2. POST /v4/AccessRequests/" + (accessRequestId ?? "{id}") + "/InitializeSession with body {} to get the connection artifact.");
                sb.AppendLine("     RDP: response.RdpConnectionFile is the literal .rdp file content — save and run mstsc against it.");
                sb.AppendLine("     SSH/Telnet: response.SshConnectionString / TelnetConnectionString carries the proxied connection.");
                sb.AppendLine("  3. Do NOT hand-build the .rdp file; let the appliance generate it so policy-driven RDP settings stay in sync.");
                break;
            case "Password":
                sb.AppendLine("  2. POST /v4/AccessRequests/" + (accessRequestId ?? "{id}") + "/CheckOutPassword to retrieve the password.");
                sb.AppendLine("     Treat the returned password as sensitive: do not echo it in summaries, logs, or follow-up tool arguments.");
                break;
            case "SshKey":
                sb.AppendLine("  2. POST /v4/AccessRequests/" + (accessRequestId ?? "{id}") + "/CheckOutSshKey to retrieve the SSH private key.");
                sb.AppendLine("     Treat the returned key as sensitive.");
                break;
            case "ApiKey":
                sb.AppendLine("  2. POST /v4/AccessRequests/" + (accessRequestId ?? "{id}") + "/CheckOutApiKeys to retrieve the API key(s).");
                break;
            case "File":
                sb.AppendLine("  2. POST /v4/AccessRequests/" + (accessRequestId ?? "{id}") + "/CheckOutFile to retrieve the file content.");
                break;
        }
        sb.Append("  ").Append(canonical == "Password" || canonical == "SshKey" || canonical == "ApiKey" || canonical == "File" ? "3" : "4")
            .Append(". POST /v4/AccessRequests/").Append(accessRequestId ?? "{id}").Append("/CheckIn when done.");
        return sb.ToString();
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
