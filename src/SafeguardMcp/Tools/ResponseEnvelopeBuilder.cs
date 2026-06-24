using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SafeguardMcp.Tools;

/// <summary>
/// Stable identifiers for notices surfaced in the response envelope's meta.notices.
/// Agents key off these strings; do not rename without coordinating consumers.
/// </summary>
internal static class NoticeKinds
{
    internal const string AutoLimitApplied = "auto_limit_applied";
    internal const string DefaultFieldsApplied = "default_fields_applied";
    internal const string DefaultOrderbyApplied = "default_orderby_applied";
    internal const string AutoWindowApplied = "auto_window_applied";
    internal const string EmptyAuditResult = "empty_audit_result";
    internal const string CountOnlyResponse = "count_only_response";
    internal const string PagingMoreAvailable = "paging_more_available";
    internal const string BodyTruncatedRecords = "body_truncated_records";
    internal const string BodyTruncatedChars = "body_truncated_chars";
    internal const string RecordTooLargeForCap = "record_too_large_for_cap";
    internal const string CsvSaved = "csv_saved";
    internal const string WorkflowRecipeSuggested = "workflow_recipe_suggested";
    internal const string SensitiveEndpointRedirected = "sensitive_endpoint_redirected";
    internal const string UncatalogedSensitiveShape = "uncataloged_sensitive_shape";

    // Auto-approve-wait outcomes attached by Safeguard_OpenAccessRequest after
    // the brief post-submission wait. Exactly one of these accompanies every
    // successful submission so the agent can branch without parsing the
    // access-request payload's State field directly.
    internal const string AutoApprovedReady = "auto_approved_ready";
    internal const string PendingApprovalCheckBack = "pending_approval_check_back";
    internal const string PendingScheduled = "pending_scheduled";
    internal const string PendingAccountAction = "pending_account_action";
    internal const string TerminatedBeforeReady = "terminated_before_ready";

    // Attached to successful POST .../InitializeSession responses. Tells the agent to
    // present BOTH the manual launch command (and/or ConnectionUri) AND an explicit
    // offer to launch the session on the user's behalf — never auto-launching. The
    // credential is injected by Safeguard at the proxy and never enters agent context.
    internal const string SessionTokenIssuedOfferToLaunch = "session_token_issued_offer_to_launch";
}

internal sealed class Notice
{
    internal string Kind { get; }
    internal string Message { get; }
    internal string Suggestion { get; }

    internal Notice(string kind, string message, string suggestion = null)
    {
        Kind = kind;
        Message = message;
        Suggestion = suggestion;
    }
}

internal sealed class PagingInfo
{
    internal int Page { get; init; }
    internal int? Limit { get; init; }
    internal int Returned { get; init; }
    /// <summary>"explicit" (caller passed limit), "auto" (MCP injected), or "none".</summary>
    internal string LimitSource { get; init; }
    internal bool More { get; init; }
    internal string Next { get; init; }
}

internal sealed class TruncationInfo
{
    internal bool Applied { get; init; }
    /// <summary>"records_dropped" or "record_too_large".</summary>
    internal string Kind { get; init; }
    internal int? ReturnedRecords { get; init; }
    internal int? TotalRecords { get; init; }
}

/// <summary>
/// Builds the structured response envelope used by Safeguard_Execute.
/// Always operates against raw JSON body strings via JsonDocument/Utf8JsonWriter
/// (no JsonSerializer.Deserialize&lt;T&gt;, no JsonNode, no dynamic — AOT compatible).
/// </summary>
internal static class ResponseEnvelopeBuilder
{
    /// <summary>
    /// Wrap a JSON response body in the standard envelope:
    ///   { "data": &lt;body&gt;, "meta": { "count": N?, "notices": [...], "paging": {...}?, "truncation": {...}? } }
    /// When <paramref name="count"/> is supplied (a count=true response), the value is surfaced in
    /// meta.count and data is left null rather than overloading data with a bare integer.
    /// </summary>
    internal static string BuildJsonEnvelope(
        string body,
        IReadOnlyList<Notice> notices,
        PagingInfo paging,
        TruncationInfo truncation,
        long? count = null)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();

            writer.WritePropertyName("data");
            WriteRawDataValue(writer, body);

            writer.WritePropertyName("meta");
            writer.WriteStartObject();

            if (count.HasValue)
                writer.WriteNumber("count", count.Value);

            writer.WritePropertyName("notices");
            writer.WriteStartArray();
            if (notices != null)
            {
                foreach (var notice in notices)
                {
                    writer.WriteStartObject();
                    writer.WriteString("kind", notice.Kind);
                    writer.WriteString("message", notice.Message ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(notice.Suggestion))
                        writer.WriteString("suggestion", notice.Suggestion);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();

            if (paging != null)
            {
                writer.WritePropertyName("paging");
                writer.WriteStartObject();
                writer.WriteNumber("page", paging.Page);
                if (paging.Limit.HasValue)
                    writer.WriteNumber("limit", paging.Limit.Value);
                else
                    writer.WriteNull("limit");
                writer.WriteNumber("returned", paging.Returned);
                writer.WriteString("limitSource", paging.LimitSource ?? "none");
                writer.WriteBoolean("more", paging.More);
                if (!string.IsNullOrWhiteSpace(paging.Next))
                    writer.WriteString("next", paging.Next);
                writer.WriteEndObject();
            }

            if (truncation != null && truncation.Applied)
            {
                writer.WritePropertyName("truncation");
                writer.WriteStartObject();
                writer.WriteBoolean("applied", true);
                if (!string.IsNullOrWhiteSpace(truncation.Kind))
                    writer.WriteString("kind", truncation.Kind);
                if (truncation.ReturnedRecords.HasValue)
                    writer.WriteNumber("returnedRecords", truncation.ReturnedRecords.Value);
                if (truncation.TotalRecords.HasValue)
                    writer.WriteNumber("totalRecords", truncation.TotalRecords.Value);
                writer.WriteEndObject();
            }

            writer.WriteEndObject(); // meta
            writer.WriteEndObject(); // root
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// CSV path: return the CSV bytes unchanged when there are no notices, else append a
    /// single line of the form "# Safeguard meta: {json}" so consumers that ignore lines
    /// starting with '#' (or that read only the leading rows) parse the CSV cleanly while
    /// agents that read the whole stream can still recover the meta.
    /// </summary>
    internal static string BuildCsvWithMeta(
        string csv,
        IReadOnlyList<Notice> notices,
        PagingInfo paging,
        TruncationInfo truncation)
    {
        var safeCsv = csv ?? string.Empty;
        var hasNotices = notices != null && notices.Count > 0;
        var hasPaging = paging != null;
        var hasTruncation = truncation != null && truncation.Applied;
        if (!hasNotices && !hasPaging && !hasTruncation)
            return safeCsv;

        var metaJson = BuildMetaOnlyJson(notices, paging, truncation);
        var sb = new StringBuilder(safeCsv);
        if (sb.Length > 0 && sb[^1] != '\n')
            sb.Append('\n');
        sb.Append("# Safeguard meta: ").Append(metaJson).Append('\n');
        return sb.ToString();
    }

    private static string BuildMetaOnlyJson(
        IReadOnlyList<Notice> notices,
        PagingInfo paging,
        TruncationInfo truncation)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("notices");
            writer.WriteStartArray();
            if (notices != null)
            {
                foreach (var notice in notices)
                {
                    writer.WriteStartObject();
                    writer.WriteString("kind", notice.Kind);
                    writer.WriteString("message", notice.Message ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(notice.Suggestion))
                        writer.WriteString("suggestion", notice.Suggestion);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
            if (paging != null)
            {
                writer.WritePropertyName("paging");
                writer.WriteStartObject();
                writer.WriteNumber("page", paging.Page);
                if (paging.Limit.HasValue)
                    writer.WriteNumber("limit", paging.Limit.Value);
                writer.WriteNumber("returned", paging.Returned);
                writer.WriteString("limitSource", paging.LimitSource ?? "none");
                writer.WriteBoolean("more", paging.More);
                if (!string.IsNullOrWhiteSpace(paging.Next))
                    writer.WriteString("next", paging.Next);
                writer.WriteEndObject();
            }
            if (truncation != null && truncation.Applied)
            {
                writer.WritePropertyName("truncation");
                writer.WriteStartObject();
                writer.WriteBoolean("applied", true);
                if (!string.IsNullOrWhiteSpace(truncation.Kind))
                    writer.WriteString("kind", truncation.Kind);
                if (truncation.ReturnedRecords.HasValue)
                    writer.WriteNumber("returnedRecords", truncation.ReturnedRecords.Value);
                if (truncation.TotalRecords.HasValue)
                    writer.WriteNumber("totalRecords", truncation.TotalRecords.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>Write the body as the JSON value of "data". Falls back to a string when not parseable.</summary>
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
    /// Compute the next-page query string, preserving every other parameter the caller sent
    /// (and the auto-injected limit). Bumps page to page+1 (page is 0-indexed in Safeguard).
    /// Returns null when no limit is in scope (we don't suggest a page hop the caller can't act on).
    /// </summary>
    internal static string BuildNextQueryString(IDictionary<string, string> parameters, int currentPage, int? limit)
    {
        if (!limit.HasValue)
            return null;

        var ordered = new List<KeyValuePair<string, string>>();
        var seenPage = false;
        var seenLimit = false;
        if (parameters != null)
        {
            foreach (var kv in parameters)
            {
                if (string.Equals(kv.Key, "page", StringComparison.OrdinalIgnoreCase))
                {
                    ordered.Add(new KeyValuePair<string, string>(kv.Key, (currentPage + 1).ToString(CultureInfo.InvariantCulture)));
                    seenPage = true;
                }
                else if (string.Equals(kv.Key, "limit", StringComparison.OrdinalIgnoreCase))
                {
                    ordered.Add(new KeyValuePair<string, string>(kv.Key, limit.Value.ToString(CultureInfo.InvariantCulture)));
                    seenLimit = true;
                }
                else
                {
                    ordered.Add(kv);
                }
            }
        }
        if (!seenPage)
            ordered.Add(new KeyValuePair<string, string>("page", (currentPage + 1).ToString(CultureInfo.InvariantCulture)));
        if (!seenLimit)
            ordered.Add(new KeyValuePair<string, string>("limit", limit.Value.ToString(CultureInfo.InvariantCulture)));

        var sb = new StringBuilder();
        var first = true;
        foreach (var kv in ordered)
        {
            if (!first) sb.Append('&');
            first = false;
            sb.Append(Uri.EscapeDataString(kv.Key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(kv.Value ?? string.Empty));
        }
        return sb.ToString();
    }
}
