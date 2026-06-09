using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SafeguardMcp.Tools;

/// <summary>
/// Inputs accepted by <c>Safeguard_CloseAccessRequest</c>. Mirrors the
/// shape of <see cref="OpenAccessRequestInputs"/>; the planner applies
/// defaults and validation.
/// </summary>
internal sealed class CloseAccessRequestInputs
{
    public string RequestId { get; init; }
    public string Comment { get; init; }
    public bool AllFields { get; init; }
}

/// <summary>
/// Result of <see cref="CloseAccessRequestPlanner.Plan"/>. Either
/// <see cref="Action"/> is a real sub-endpoint name (Cancel / CheckIn
/// / Close / Acknowledge) and the tool should POST to it, or
/// <see cref="Action"/> is <c>"None"</c> (terminal state, no-op), or
/// <see cref="Refusal"/> is non-null and the tool must return the
/// refusal envelope WITHOUT calling the appliance again.
/// </summary>
internal sealed class ClosePlan
{
    public string Action { get; set; }
    public string Body { get; set; }
    public string Refusal { get; set; }
    public List<Notice> Notices { get; init; } = new();
}

/// <summary>
/// Pure-logic planner for <c>Safeguard_CloseAccessRequest</c>. Owns
/// state -> sub-endpoint dispatch, comment truncation, and the
/// projection applied when <c>allFields=false</c>. No I/O.
/// </summary>
internal static class CloseAccessRequestPlanner
{
    /// <summary>
    /// Echo-side limit. Mirrors the controller's <c>MaxCommentLength</c>
    /// (255) on Cancel/Close/Acknowledge; the planner truncates rather
    /// than rejects so the agent's intent still reaches the appliance.
    /// </summary>
    internal const int MaxCommentLength = CloseAccessRequestStateMap.MaxCommentLength;

    /// <summary>
    /// Parses the optional <c>requestId</c> input. Returns
    /// <c>null</c>-or-empty rejection so callers can render the
    /// argument error in the same envelope as planner refusals.
    /// </summary>
    public static List<string> ValidateArgs(CloseAccessRequestInputs inputs)
    {
        var errors = new List<string>();
        if (inputs == null)
        {
            errors.Add("Inputs are required.");
            return errors;
        }
        if (string.IsNullOrWhiteSpace(inputs.RequestId))
            errors.Add("requestId is required.");
        return errors;
    }

    /// <summary>
    /// Truncates <paramref name="comment"/> to <see cref="MaxCommentLength"/>
    /// characters when needed and surfaces a notice that the original
    /// input was longer. Returns the original string when no truncation
    /// is needed; returns <c>null</c> when the input was null/empty.
    /// </summary>
    public static string TruncateComment(string comment, List<Notice> notices)
    {
        if (string.IsNullOrEmpty(comment))
            return null;
        if (comment.Length <= MaxCommentLength)
            return comment;

        if (notices != null)
        {
            notices.Add(new Notice(
                "comment_truncated",
                $"Comment truncated from {comment.Length} to {MaxCommentLength} characters before submission. "
                + "The appliance rejects longer comments outright (ModelError_70004_MaxLength); the tool truncates "
                + "so your intent still reaches the audit trail.",
                $"Shorten the comment to <= {MaxCommentLength} characters to avoid truncation."));
        }
        return comment.Substring(0, MaxCommentLength);
    }

    /// <summary>
    /// Decides which sub-endpoint, if any,
    /// <c>Safeguard_CloseAccessRequest</c> should POST given the
    /// request's current state and the caller's role. Returns a plan
    /// with either an <see cref="ClosePlan.Action"/> name to dispatch
    /// or a <see cref="ClosePlan.Refusal"/> message to surface in the
    /// envelope.
    /// </summary>
    public static ClosePlan Plan(
        string state,
        bool callerIsRequester,
        bool callerIsPolicyAdmin,
        string truncatedComment,
        string requestId)
    {
        var plan = new ClosePlan();

        if (!callerIsRequester && !callerIsPolicyAdmin)
        {
            plan.Refusal = $"You didn't request '{requestId}' and you are not a policy admin.";
            return plan;
        }

        // PolicyAdmin acting on someone else's request: only Close from
        // PendingReview is supported. This diverges from safeguard-ps
        // (which sends Close unconditionally) in favour of the V4
        // controller's documented contract.
        if (callerIsPolicyAdmin && !callerIsRequester)
        {
            if (string.Equals(state, "PendingReview", StringComparison.OrdinalIgnoreCase))
            {
                plan.Action = "Close";
                plan.Body = BuildCommentBody(truncatedComment);
                return plan;
            }

            plan.Refusal = $"Close requires the request to be in PendingReview state. "
                + $"Current state is '{state}'. Only the requester can act on this state "
                + "(via Cancel/CheckIn/Acknowledge as appropriate); have them run "
                + "Safeguard_CloseAccessRequest, or wait for the appliance's expiry/reclaim sweep.";
            return plan;
        }

        // Requester path (possibly also PolicyAdmin acting on own request).
        var row = CloseAccessRequestStateMap.Find(state);
        if (row == null)
        {
            plan.Refusal = $"Unknown access-request state '{state}'. The planner does not pick a fallback action; "
                + "re-fetch GET /v4/AccessRequests/{id} to confirm the state, or run "
                + "Safeguard_Enum name=\"AccessRequestState\" to see the recognised values.";
            return plan;
        }

        var r = row.Value;

        if (r.Status == VerificationStatus.Inferred)
        {
            plan.Notices.Add(new Notice(
                "inferred-not-verified-on-this-appliance",
                $"State '{r.State}' -> '{ActionName(r.RequesterAction)}' is inherited from safeguard-ps "
                + "and has not been verified against this appliance by the close-action verification harness. "
                + (string.IsNullOrEmpty(r.Note) ? string.Empty : r.Note),
                "Run CloseAccessRequestVerificationTests against this appliance to upgrade the row to Verified."));
        }

        switch (r.RequesterAction)
        {
            case CloseAction.Cancel:
                plan.Action = "Cancel";
                plan.Body = BuildCommentBody(truncatedComment);
                return plan;

            case CloseAction.CheckIn:
                plan.Action = "CheckIn";
                plan.Body = null; // CheckIn ignores body; planner drops comment.
                if (!string.IsNullOrEmpty(truncatedComment))
                {
                    plan.Notices.Add(new Notice(
                        "comment_ignored",
                        "CheckIn does not accept a comment body; the supplied comment was not sent.",
                        "Use Cancel/Close/Acknowledge (per the request's state) to attach a comment."));
                }
                return plan;

            case CloseAction.Acknowledge:
                plan.Action = "Acknowledge";
                plan.Body = BuildCommentBody(truncatedComment);
                return plan;

            case CloseAction.None:
                plan.Action = "None";
                return plan;

            case CloseAction.NeedsAdmin:
                if (callerIsPolicyAdmin) // requester AND PolicyAdmin
                {
                    if (string.Equals(state, "PendingReview", StringComparison.OrdinalIgnoreCase))
                    {
                        plan.Action = "Close";
                        plan.Body = BuildCommentBody(truncatedComment);
                        return plan;
                    }
                    plan.Refusal = $"State '{state}' has no requester-callable sub-endpoint (Cancel/CheckIn/Acknowledge "
                        + "return 4xx here per the controller's AllowIfState attribute), and Close requires PendingReview. "
                        + "Wait for the appliance to transition the request (typically to PendingReview or Closed) and retry.";
                    return plan;
                }
                plan.Refusal = $"State '{state}' has no sub-endpoint the requester can call: "
                    + "Cancel/CheckIn/Acknowledge return 4xx here per the controller's AllowIfState attribute, "
                    + "and Close requires PolicyAdmin. Wait for the appliance's expiry/reclaim sweep, "
                    + "or ask a PolicyAdmin to run Safeguard_CloseAccessRequest.";
                return plan;

            default:
                plan.Refusal = $"State '{state}' has no mapped action. This is a planner bug; "
                    + "file an issue with the state name and the appliance version.";
                return plan;
        }
    }

    /// <summary>
    /// Projects an <c>AccessRequest</c> JSON object to the
    /// <see cref="CloseAccessRequestStateMap.SgAccessRequestFields"/>
    /// set when <paramref name="allFields"/> is false. Returns the
    /// input verbatim when <paramref name="allFields"/> is true or the
    /// input is not parseable.
    /// </summary>
    public static string ProjectAccessRequest(string accessRequestJson, bool allFields)
    {
        if (allFields || string.IsNullOrWhiteSpace(accessRequestJson))
            return accessRequestJson;
        JsonDocument doc;
        try { doc = JsonDocument.Parse(accessRequestJson); }
        catch (JsonException) { return accessRequestJson; }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return accessRequestJson;

            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
            {
                writer.WriteStartObject();
                foreach (var field in CloseAccessRequestStateMap.SgAccessRequestFields)
                {
                    if (doc.RootElement.TryGetProperty(field, out var elem))
                    {
                        writer.WritePropertyName(field);
                        elem.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
        }
    }

    /// <summary>
    /// Extracts <c>State</c>, <c>RequesterId</c>, and <c>Id</c> from
    /// the appliance's <c>GET /v4/AccessRequests/{id}</c> response so
    /// the planner can dispatch without re-parsing.
    /// </summary>
    public static (string State, int RequesterId, string Id) ExtractGate(string accessRequestJson)
    {
        if (string.IsNullOrWhiteSpace(accessRequestJson))
            return (null, 0, null);
        try
        {
            using var doc = JsonDocument.Parse(accessRequestJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return (null, 0, null);

            string state = null;
            int requesterId = 0;
            string id = null;
            if (doc.RootElement.TryGetProperty("State", out var sElem)
                && sElem.ValueKind == JsonValueKind.String)
                state = sElem.GetString();
            if (doc.RootElement.TryGetProperty("RequesterId", out var rElem)
                && rElem.ValueKind == JsonValueKind.Number)
                rElem.TryGetInt32(out requesterId);
            if (doc.RootElement.TryGetProperty("Id", out var iElem))
            {
                id = iElem.ValueKind switch
                {
                    JsonValueKind.String => iElem.GetString(),
                    JsonValueKind.Number => iElem.GetRawText(),
                    _ => null,
                };
            }
            return (state, requesterId, id);
        }
        catch (JsonException)
        {
            return (null, 0, null);
        }
    }

    /// <summary>
    /// Extracts <c>Id</c> and <c>AdminRoles</c> from <c>GET /v4/Me</c>.
    /// Returns <c>(0, false)</c> on unparseable input.
    /// </summary>
    public static (int Id, bool IsPolicyAdmin) ExtractMe(string meJson)
    {
        if (string.IsNullOrWhiteSpace(meJson))
            return (0, false);
        try
        {
            using var doc = JsonDocument.Parse(meJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return (0, false);
            int id = 0;
            bool isPolicyAdmin = false;
            if (doc.RootElement.TryGetProperty("Id", out var idElem)
                && idElem.ValueKind == JsonValueKind.Number)
                idElem.TryGetInt32(out id);
            if (doc.RootElement.TryGetProperty("AdminRoles", out var rolesElem)
                && rolesElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in rolesElem.EnumerateArray())
                {
                    if (role.ValueKind == JsonValueKind.String
                        && string.Equals(role.GetString(), "PolicyAdmin", StringComparison.OrdinalIgnoreCase))
                    {
                        isPolicyAdmin = true;
                        break;
                    }
                }
            }
            return (id, isPolicyAdmin);
        }
        catch (JsonException)
        {
            return (0, false);
        }
    }

    /// <summary>
    /// Renders the success envelope:
    ///   { ok: true, action: "...", request: &lt;projection&gt;, notices: [...] }
    /// </summary>
    public static string BuildSuccessEnvelope(
        string action,
        string requestJson,
        IReadOnlyList<Notice> notices)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteBoolean("ok", true);
            writer.WriteString("action", action ?? string.Empty);
            writer.WritePropertyName("request");
            WriteRawValue(writer, requestJson);
            WriteNotices(writer, notices);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// Renders the refusal envelope:
    ///   { ok: false, error: "...", notices: [...] }
    /// </summary>
    public static string BuildRefusalEnvelope(string error, IReadOnlyList<Notice> notices)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteBoolean("ok", false);
            writer.WriteString("error", error ?? string.Empty);
            WriteNotices(writer, notices);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static string ActionName(CloseAction a) => a switch
    {
        CloseAction.Cancel => "Cancel",
        CloseAction.CheckIn => "CheckIn",
        CloseAction.Close => "Close",
        CloseAction.Acknowledge => "Acknowledge",
        CloseAction.None => "None",
        CloseAction.NeedsAdmin => "NeedsAdmin",
        _ => "Unknown",
    };

    private static string BuildCommentBody(string comment)
    {
        if (string.IsNullOrEmpty(comment))
            return null;
        // Cancel / Close / Acknowledge take [FromBody] string -- a JSON
        // string literal, not an object wrapper.
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStringValue(comment);
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteRawValue(Utf8JsonWriter writer, string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            writer.WriteNullValue();
            return;
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.WriteTo(writer);
        }
        catch (JsonException)
        {
            writer.WriteStringValue(json);
        }
    }

    private static void WriteNotices(Utf8JsonWriter writer, IReadOnlyList<Notice> notices)
    {
        writer.WritePropertyName("notices");
        writer.WriteStartArray();
        if (notices != null)
        {
            foreach (var n in notices)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", n.Kind);
                writer.WriteString("message", n.Message ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(n.Suggestion))
                    writer.WriteString("suggestion", n.Suggestion);
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();
    }
}
