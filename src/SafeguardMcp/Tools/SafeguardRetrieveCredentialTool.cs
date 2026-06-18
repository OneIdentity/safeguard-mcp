using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tools;

/// <summary>
/// Owns every Safeguard API path that returns plaintext credential
/// material. Dispatches by typed <c>kind</c> to the underlying
/// endpoint, then returns a two-block MCP response:
///
///   block 1 (audience=["assistant"]): JSON envelope, metadata only.
///     kind, subject ids, delivery flags, notices. No secret value.
///
///   block 2 (audience=["user"]): human-formatted plaintext.
///     The MCP spec (2025-06-18) defines the `audience` annotation
///     as an optional hint a host MAY use to route content — it does
///     not require hosts to filter on it. Hosts that honor the hint
///     can render this block directly to the human (secure pane,
///     copy-to-clipboard) without feeding it to the model; hosts that
///     ignore it will surface the plaintext to the LLM. Degrade-
///     gracefully is preferable to refusing to run, since credentials
///     can be rotated if the host failed to split audiences.
///
/// The companion to <see cref="SafeguardApiTool.Safeguard_Execute"/>'s
/// refuse-and-redirect guard: anything Execute refuses lives here.
/// </summary>
[McpServerToolType]
internal sealed class SafeguardRetrieveCredentialTool
{
    private readonly ISafeguardSession _session;
    private readonly ILogger<SafeguardRetrieveCredentialTool> _logger;

    public SafeguardRetrieveCredentialTool(
        ISafeguardSession session,
        ILogger<SafeguardRetrieveCredentialTool> logger)
    {
        _session = session;
        _logger = logger;
    }

    [McpServerTool(Name = "Safeguard_RetrieveCredential", Title = "Retrieve Safeguard Credential",
        ReadOnly = true, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Retrieve sensitive Safeguard credential material in a two-block MCP response: "
        + "block 1 (assistant audience) is metadata only and never the plaintext; "
        + "block 2 (user audience) carries the plaintext value (password / SSH key PEM / API secret / "
        + "TOTP code / personal-account password). The audience annotation is an optional host hint — "
        + "treat block 2 as potentially model-visible unless your host filters by audience, and rely on "
        + "appliance audit + rotation as the authoritative control. "
        + "Supported kinds: access-request-password, access-request-ssh-key, access-request-api-key, "
        + "access-request-totp, access-request-file (require accessRequestId); personal-account-password, "
        + "personal-account-password-history, personal-account-totp (require accountId); generated-password; "
        + "asset-account-api-secret-history (requires accountId AND apiKeyId). "
        + "These endpoints are NOT callable via Safeguard_Execute.")]
    public async Task<IList<ContentBlock>> Safeguard_RetrieveCredential(
        McpServer server,
        [Description("Typed credential kind. One of: access-request-password, access-request-ssh-key, "
            + "access-request-api-key, access-request-totp, access-request-file, "
            + "personal-account-password, personal-account-password-history, personal-account-totp, "
            + "generated-password, asset-account-api-secret-history.")]
        string kind,
        [Description("AccessRequest database id. Required for every access-request-* kind. "
            + "Note: Safeguard access-request ids are opaque dashed strings "
            + "(e.g. '3-4-6-8951-1-d7c2dff871514f669a61163dbd8548fa-0003'), not integers.")]
        string accessRequestId = null,
        [Description("Account database id. Required for personal-account-* kinds and "
            + "asset-account-api-secret-history.")]
        int? accountId = null,
        [Description("ApiKey database id. Required only for asset-account-api-secret-history.")]
        int? apiKeyId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(kind))
            throw new McpException("The 'kind' parameter is required. See the tool description for valid kinds.");

        var entry = SensitiveCredentialEndpoints.GetByKind(kind);
        if (entry == null)
        {
            throw new McpException(
                $"Unknown credential kind '{kind}'. See the tool description for valid kinds.");
        }

        var providedArgs = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["accessRequestId"] = !string.IsNullOrWhiteSpace(accessRequestId),
            ["accountId"] = accountId.HasValue,
            ["apiKeyId"] = apiKeyId.HasValue,
        };

        var missing = new List<string>();
        foreach (var required in entry.Requires)
        {
            if (!providedArgs.TryGetValue(required, out var present) || !present)
                missing.Add(required);
        }
        if (missing.Count > 0)
        {
            throw new McpException(
                $"kind '{kind}' requires: {string.Join(", ", entry.Requires)}. "
                + $"Missing: {string.Join(", ", missing)}.");
        }

        // Reject extraneous ids that the kind doesn't expect — keeps callers honest
        // and protects against ambiguous arg sets (e.g., apiKeyId passed to a
        // non-asset-account kind).
        foreach (var (name, present) in providedArgs)
        {
            if (present && !ContainsIgnoreCase(entry.Requires, name))
            {
                throw new McpException(
                    $"kind '{kind}' does not accept '{name}'. Required args: "
                    + (entry.Requires.Count == 0 ? "(none)" : string.Join(", ", entry.Requires)) + ".");
            }
        }

        var endpoint = entry.Endpoints[0];
        var (method, path) = ResolvePath(entry, endpoint, accessRequestId, accountId, apiKeyId);

        await _session.EnsureReadyAsync(server, ct);

        FullResponse response;
        try
        {
            response = await SafeguardInvoker.InvokeAsync(
                _session, Service.Core, method, ToSdkRelativeUrl(path),
                body: method == "POST" ? "{}" : null,
                parameters: null,
                ct);
        }
        catch (McpException ex)
        {
            // Surface the underlying appliance error to the assistant block;
            // there is no plaintext to protect when the call failed.
            throw new McpException(
                $"Safeguard_RetrieveCredential ({kind}): underlying call {method} {path} failed: {ex.Message}");
        }

        var body = response.Body ?? string.Empty;
        var retrievedAtUtc = System.DateTimeOffset.UtcNow;

        // Audit log: never plaintext. Subject ids only.
        var subjectIds = new List<string>();
        if (!string.IsNullOrEmpty(accessRequestId)) subjectIds.Add($"accessRequestId={accessRequestId}");
        if (accountId.HasValue) subjectIds.Add($"accountId={accountId.Value}");
        if (apiKeyId.HasValue) subjectIds.Add($"apiKeyId={apiKeyId.Value}");
        _logger.LogInformation(
            "Safeguard_RetrieveCredential issued: kind={Kind} subject={Subject} callerSessionId={SessionId} retrievedAt={RetrievedAt}",
            kind,
            string.Join(",", subjectIds),
            server?.SessionId ?? string.Empty,
            retrievedAtUtc.ToString("O", CultureInfo.InvariantCulture));

        var assistantJson = BuildAssistantMetadataBlock(kind, accessRequestId, accountId, apiKeyId, retrievedAtUtc);
        var userText = BuildUserAudienceBlock(kind, body, accessRequestId, accountId, apiKeyId);

        return new List<ContentBlock>
        {
            new TextContentBlock
            {
                Text = assistantJson,
                Annotations = new Annotations { Audience = new List<Role> { Role.Assistant } },
            },
            new TextContentBlock
            {
                Text = userText,
                Annotations = new Annotations { Audience = new List<Role> { Role.User } },
            },
        };
    }

    private static bool ContainsIgnoreCase(IReadOnlyList<string> list, string value)
    {
        for (int i = 0; i < list.Count; i++)
            if (string.Equals(list[i], value, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static (string Method, string Path) ResolvePath(
        SensitiveCredentialEndpoints.KindEntry entry,
        SensitiveCredentialEndpoints.EndpointBinding endpoint,
        string accessRequestId,
        int? accountId,
        int? apiKeyId)
    {
        // The catalog kinds each map to a single concrete path template. Build the
        // path from the typed parameters rather than introspecting the regex (which
        // is the path *matcher*, not a template), so the kind→path direction stays
        // explicit and testable.
        switch (entry.Kind)
        {
            case "access-request-password":
                return ("POST", $"/v4/AccessRequests/{accessRequestId}/CheckOutPassword");
            case "access-request-ssh-key":
                return ("POST", $"/v4/AccessRequests/{accessRequestId}/CheckOutSshKey");
            case "access-request-api-key":
                return ("POST", $"/v4/AccessRequests/{accessRequestId}/CheckOutApiKeys");
            case "access-request-totp":
                return ("POST", $"/v4/AccessRequests/{accessRequestId}/CheckOutTotp");
            case "access-request-file":
                return ("POST", $"/v4/AccessRequests/{accessRequestId}/CheckOutFile");
            case "personal-account-password":
                return ("GET", $"/v4/Me/EnterpriseAccounts/{accountId}/Password");
            case "personal-account-password-history":
                return ("GET", $"/v4/Me/EnterpriseAccounts/{accountId}/Passwords");
            case "personal-account-totp":
                return ("GET", $"/v4/Me/EnterpriseAccounts/{accountId}/TotpAuthenticator/Values");
            case "generated-password":
                return ("POST", "/v4/Me/EnterpriseAccounts/GeneratePassword");
            case "asset-account-api-secret-history":
                return ("GET", $"/v4/AssetAccounts/{accountId}/ApiKeys/{apiKeyId}/ClientSecrets");
        }
        throw new McpException($"No dispatch path is configured for kind '{entry.Kind}'.");
    }

    private static string ToSdkRelativeUrl(string path)
    {
        var trimmed = path.TrimStart('/');
        if (trimmed.StartsWith("v4/", System.StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[3..];
        return trimmed;
    }

    internal static string BuildAssistantMetadataBlock(
        string kind,
        string accessRequestId,
        int? accountId,
        int? apiKeyId,
        System.DateTimeOffset retrievedAtUtc)
    {
        using var buffer = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("kind", kind);
            writer.WriteString("status", "retrieved");
            writer.WriteString("retrievedAt", retrievedAtUtc.ToString("O", CultureInfo.InvariantCulture));

            writer.WritePropertyName("subject");
            writer.WriteStartObject();
            if (!string.IsNullOrEmpty(accessRequestId)) writer.WriteString("accessRequestId", accessRequestId);
            if (accountId.HasValue) writer.WriteNumber("accountId", accountId.Value);
            if (apiKeyId.HasValue) writer.WriteNumber("apiKeyId", apiKeyId.Value);
            writer.WriteEndObject();

            writer.WritePropertyName("delivery");
            writer.WriteStartObject();
            writer.WriteString("block2_audience", "user");
            writer.WriteString("audience_honored_by_host", "unknown");
            writer.WriteEndObject();

            writer.WritePropertyName("notices");
            writer.WriteStartArray();
            writer.WriteStringValue(
                "Plaintext value is in the user-audience block (block 2). MCP hosts that do not "
                + "honor 'audience' annotations will render that block to the LLM transcript; if "
                + "you can see the plaintext here, your host did not split audiences.");
            writer.WriteStringValue(
                "Do NOT echo any retrieved credential into follow-up tool arguments, summaries, "
                + "or logs. Treat block 2 as opaque user-only material.");
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    internal static string BuildUserAudienceBlock(
        string kind,
        string body,
        string accessRequestId,
        int? accountId,
        int? apiKeyId)
    {
        var sb = new StringBuilder();
        switch (kind)
        {
            case "access-request-password":
                AppendAccessRequestHeader(sb, "Password", accessRequestId);
                sb.AppendLine();
                sb.Append(UnquoteIfJsonString(body));
                break;
            case "access-request-ssh-key":
                AppendAccessRequestHeader(sb, "SSH key (private key + identity)", accessRequestId);
                sb.AppendLine();
                AppendSshKeyBody(sb, body);
                break;
            case "access-request-api-key":
                AppendAccessRequestHeader(sb, "API key(s)", accessRequestId);
                sb.AppendLine();
                AppendApiKeyResultsBody(sb, body);
                break;
            case "access-request-totp":
                AppendAccessRequestHeader(sb, "TOTP code window", accessRequestId);
                sb.AppendLine();
                AppendTotpBody(sb, body);
                break;
            case "access-request-file":
                AppendAccessRequestHeader(sb, "File content", accessRequestId);
                sb.AppendLine();
                sb.Append(body);
                break;
            case "personal-account-password":
                sb.Append("Personal account password (accountId=").Append(accountId).AppendLine(")").AppendLine();
                sb.Append(UnquoteIfJsonString(body));
                break;
            case "personal-account-password-history":
                sb.Append("Personal account password history (accountId=").Append(accountId).AppendLine(")").AppendLine();
                AppendPasswordHistoryBody(sb, body);
                break;
            case "personal-account-totp":
                sb.Append("Personal account TOTP code window (accountId=").Append(accountId).AppendLine(")").AppendLine();
                AppendTotpBody(sb, body);
                break;
            case "generated-password":
                sb.AppendLine("Generated password (not assigned to any account):").AppendLine();
                sb.Append(UnquoteIfJsonString(body));
                break;
            case "asset-account-api-secret-history":
                sb.Append("API client secret history (accountId=").Append(accountId)
                    .Append(", apiKeyId=").Append(apiKeyId).AppendLine(")").AppendLine();
                AppendApiSecretHistoryBody(sb, body);
                break;
            default:
                sb.Append(body);
                break;
        }
        return sb.ToString().TrimEnd();
    }

    private static void AppendAccessRequestHeader(StringBuilder sb, string label, string accessRequestId)
    {
        sb.Append(label).Append(" for access request ").Append(accessRequestId).AppendLine(".");
    }

    private static string UnquoteIfJsonString(string body)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
                return doc.RootElement.GetString() ?? string.Empty;
        }
        catch (JsonException) { }
        return body;
    }

    private static void AppendSshKeyBody(StringBuilder sb, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            AppendStringPropertyLine(sb, root, "KeyType", "Key type");
            AppendIntPropertyLine(sb, root, "KeyLength", "Key length");
            AppendStringPropertyLine(sb, root, "Fingerprint", "Fingerprint (MD5)");
            AppendStringPropertyLine(sb, root, "FingerprintSha256", "Fingerprint (SHA256)");
            AppendStringPropertyLine(sb, root, "SshKeyFormat", "Format");
            if (root.TryGetProperty("PublicKey", out var pub) && pub.ValueKind == JsonValueKind.String)
                sb.AppendLine("Public key:").AppendLine(pub.GetString()).AppendLine();
            if (root.TryGetProperty("Passphrase", out var pass) && pass.ValueKind == JsonValueKind.String
                && !string.IsNullOrEmpty(pass.GetString()))
                sb.Append("Passphrase: ").AppendLine(pass.GetString());
            if (root.TryGetProperty("PrivateKey", out var pk) && pk.ValueKind == JsonValueKind.String)
                sb.AppendLine("Private key:").AppendLine(pk.GetString());
        }
        catch (JsonException)
        {
            sb.Append(body);
        }
    }

    private static void AppendApiKeyResultsBody(StringBuilder sb, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                sb.Append(body);
                return;
            }
            int idx = 0;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                idx++;
                sb.Append("API key ").Append(idx).AppendLine(":");
                AppendStringPropertyLine(sb, item, "Name", "  Name");
                AppendIntPropertyLine(sb, item, "Id", "  Id");
                AppendStringPropertyLine(sb, item, "ClientId", "  ClientId");
                AppendStringPropertyLine(sb, item, "ClientSecretId", "  ClientSecretId");
                AppendStringPropertyLine(sb, item, "ClientSecret", "  ClientSecret");
                sb.AppendLine();
            }
        }
        catch (JsonException)
        {
            sb.Append(body);
        }
    }

    private static void AppendTotpBody(StringBuilder sb, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                sb.Append(body);
                return;
            }
            int idx = 0;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                idx++;
                sb.Append("Code ").Append(idx).Append(": ");
                if (item.TryGetProperty("Code", out var code) && code.ValueKind == JsonValueKind.String)
                    sb.Append(code.GetString());
                if (item.TryGetProperty("Period", out var period) && period.ValueKind == JsonValueKind.Number)
                    sb.Append("   (valid for ").Append(period.GetInt32()).Append(" seconds");
                if (item.TryGetProperty("TimeStamp", out var ts) && ts.ValueKind == JsonValueKind.String)
                    sb.Append(" from ").Append(ts.GetString());
                sb.AppendLine(")");
            }
        }
        catch (JsonException)
        {
            sb.Append(body);
        }
    }

    private static void AppendPasswordHistoryBody(StringBuilder sb, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                sb.Append(body);
                return;
            }
            int idx = 0;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                idx++;
                sb.Append("Entry ").Append(idx).AppendLine(":");
                AppendStringPropertyLine(sb, item, "TimeStarted", "  TimeStarted");
                AppendStringPropertyLine(sb, item, "TimeEnded", "  TimeEnded");
                AppendStringPropertyLine(sb, item, "Password", "  Password");
                sb.AppendLine();
            }
        }
        catch (JsonException)
        {
            sb.Append(body);
        }
    }

    private static void AppendApiSecretHistoryBody(StringBuilder sb, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                sb.Append(body);
                return;
            }
            int idx = 0;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                idx++;
                sb.Append("Entry ").Append(idx).AppendLine(":");
                AppendStringPropertyLine(sb, item, "ClientId", "  ClientId");
                AppendStringPropertyLine(sb, item, "ClientSecretId", "  ClientSecretId");
                AppendStringPropertyLine(sb, item, "ClientSecret", "  ClientSecret");
                AppendStringPropertyLine(sb, item, "TimeStarted", "  TimeStarted");
                AppendStringPropertyLine(sb, item, "TimeEnded", "  TimeEnded");
                sb.AppendLine();
            }
        }
        catch (JsonException)
        {
            sb.Append(body);
        }
    }

    private static void AppendStringPropertyLine(StringBuilder sb, JsonElement el, string name, string label)
    {
        if (el.ValueKind != JsonValueKind.Object) return;
        if (!el.TryGetProperty(name, out var prop)) return;
        if (prop.ValueKind == JsonValueKind.String)
        {
            var v = prop.GetString();
            if (!string.IsNullOrEmpty(v))
                sb.Append(label).Append(": ").AppendLine(v);
        }
        else if (prop.ValueKind == JsonValueKind.Number)
        {
            sb.Append(label).Append(": ").AppendLine(prop.GetRawText());
        }
    }

    private static void AppendIntPropertyLine(StringBuilder sb, JsonElement el, string name, string label)
    {
        if (el.ValueKind != JsonValueKind.Object) return;
        if (el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number)
            sb.Append(label).Append(": ").AppendLine(prop.GetInt32().ToString(CultureInfo.InvariantCulture));
    }
}
