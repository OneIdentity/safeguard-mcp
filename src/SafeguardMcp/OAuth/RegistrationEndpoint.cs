using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SafeguardMcp.OAuth;

/// <summary>
/// Maps <c>POST /register</c> (HTTP-AUTH-RELAY-PLAN §2.2.f) — the
/// bridge's RFC 7591 dynamic client registration endpoint.
///
/// <para>
/// The MCP 2025-06-18 OAuth 2.1 client profile (Claude Desktop, VS
/// Code GitHub Copilot Chat, Cursor) requires Dynamic Client
/// Registration against unknown authorization servers; the bridge
/// cannot defer it. This handler is intentionally a public-client
/// stub — it issues a synthetic <c>client_id</c> only, never a
/// <c>client_secret</c>, since the client↔bridge leg is PKCE-only.
/// </para>
///
/// <para>
/// The single non-trivial defense this endpoint must mount is
/// strict <c>redirect_uri</c> validation: a registry entry is what
/// <c>/authorize</c> trusts when deciding where to bounce the
/// user-agent on success or error, so an attacker who can register
/// an arbitrary redirect_uri can turn the bridge into an open
/// redirect. Per plan §2.2.f, each <c>redirect_uri</c> must satisfy
/// at least one of:
/// </para>
///
/// <list type="bullet">
///   <item>The URI is an RFC 8252 §7.3 loopback redirect — http on
///   <c>127.0.0.1</c>, <c>[::1]</c>, or <c>localhost</c>, with any
///   port — for native MCP clients.</item>
///   <item>The URI's authority (case-insensitive host + scheme +
///   port) matches the registering request's <c>Origin</c> header,
///   for browser-hosted clients. Without an <c>Origin</c> header
///   only loopback URIs are accepted; this collapses the
///   open-redirect surface to operator-trusted developer machines.</item>
/// </list>
///
/// <para>
/// Validation failure surfaces as an RFC 7591 §3.2.2
/// <c>invalid_redirect_uri</c> 400. The synthesized
/// <c>client_id</c> is <c>mcp-client-&lt;uuid&gt;</c>; clients that
/// see a 401 from <c>/authorize</c> after a bridge restart simply
/// re-register transparently. Storage TTL is 30 days; the bridge
/// holds no secrets — only the redirect_uri allow-list bound to
/// each issued client_id.
/// </para>
/// </summary>
internal static class RegistrationEndpoint
{
    public const string RegistrationPath = "/register";

    private const string JsonContentTypePrefix = "application/json";
    private const string ResponseContentType = "application/json; charset=utf-8";

    // RFC 7591 doesn't fix a registration TTL; plan §2.2.f sets 30
    // days. Bridge restart loses all entries — clients re-register
    // on the next 401 from /authorize.
    public static readonly TimeSpan ClientRegistrationTtl = TimeSpan.FromDays(30);

    public static void Map(IEndpointRouteBuilder endpoints, BridgeOptions options)
    {
        if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
        if (options == null) throw new ArgumentNullException(nameof(options));

        endpoints.MapPost(RegistrationPath, ctx => HandleRegisterAsync(ctx, options));
        endpoints.MapMethods(RegistrationPath, new[] { "OPTIONS" }, ctx =>
        {
            SetCorsHeaders(ctx);
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
    }

    internal static async Task HandleRegisterAsync(HttpContext ctx, BridgeOptions options)
    {
        SetCorsHeaders(ctx);
        // RFC 7591 doesn't speak to caching, but registration responses
        // include a freshly minted client_id and must never be cached
        // by an intermediary or user-agent.
        ctx.Response.Headers.CacheControl = "no-store";
        ctx.Response.Headers.Pragma = "no-cache";

        var contentType = ctx.Request.ContentType;
        if (contentType == null
            || !contentType.StartsWith(JsonContentTypePrefix, StringComparison.OrdinalIgnoreCase))
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_client_metadata",
                "Content-Type must be application/json.");
            return;
        }

        string body;
        try
        {
            using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
            body = await reader.ReadToEndAsync(ctx.RequestAborted);
        }
        catch
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_client_metadata",
                "Registration body could not be read.");
            return;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_client_metadata",
                "Registration body must be a non-empty JSON object.");
            return;
        }

        // AOT rule: parse with JsonDocument only.
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_client_metadata",
                "Registration body could not be parsed as JSON.");
            return;
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                    "invalid_client_metadata",
                    "Registration body must be a JSON object.");
                return;
            }

            if (!doc.RootElement.TryGetProperty("redirect_uris", out var urisEl)
                || urisEl.ValueKind != JsonValueKind.Array
                || urisEl.GetArrayLength() == 0)
            {
                await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                    "invalid_redirect_uri",
                    "redirect_uris is required and must be a non-empty array of absolute URIs.");
                return;
            }

            // Parse + collect — fail on the first bad entry.
            var redirectUris = new List<string>(urisEl.GetArrayLength());
            foreach (var item in urisEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                        "invalid_redirect_uri",
                        "Every redirect_uris entry must be a string.");
                    return;
                }
                redirectUris.Add(item.GetString());
            }

            var origin = ctx.Request.Headers["Origin"].ToString();
            foreach (var redirectUri in redirectUris)
            {
                if (!IsAcceptableRedirectUri(redirectUri, origin, out var rejectReason))
                {
                    await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                        "invalid_redirect_uri",
                        rejectReason);
                    return;
                }
            }

            // Optional client_name pass-through (RFC 7591 §2). We
            // don't currently use it but echo for client UX.
            string clientName = null;
            if (doc.RootElement.TryGetProperty("client_name", out var nameEl)
                && nameEl.ValueKind == JsonValueKind.String)
            {
                clientName = nameEl.GetString();
            }

            var registry = ctx.RequestServices.GetRequiredService<ClientRegistry>();
            var now = (ctx.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System)
                .GetUtcNow();

            // mcp-client-<uuid>; the registry rejects collisions.
            // Guid.NewGuid() is cryptographically strong enough for
            // an opaque registration id (no auth power on its own —
            // the redirect_uri allow-list and PKCE binding do the
            // actual security work).
            var clientId = "mcp-client-" + Guid.NewGuid().ToString("N");
            var expiresAt = now.Add(ClientRegistrationTtl);
            if (!registry.TryAdd(clientId, redirectUris, expiresAt))
            {
                // Effectively impossible (128-bit Guid), but if the
                // generator ever collides we surface a 5xx rather
                // than overwrite a live registration.
                await WriteErrorAsync(ctx, StatusCodes.Status500InternalServerError,
                    "server_error",
                    "client_id collision; please retry.");
                return;
            }

            await WriteSuccessAsync(ctx, clientId, redirectUris, clientName, now, expiresAt);
        }
    }

    /// <summary>
    /// Returns true if <paramref name="redirectUri"/> is one of the
    /// two redirect-target shapes the bridge accepts:
    /// <list type="number">
    ///   <item>RFC 8252 §7.3 loopback: <c>http</c> scheme, host is
    ///   <c>127.0.0.1</c>, <c>[::1]</c>, or <c>localhost</c>, any
    ///   path, any port (RFC 8252 explicitly allows ephemeral
    ///   ports).</item>
    ///   <item>Same-origin: scheme + host + port equal the
    ///   <paramref name="origin"/> header value parsed as a URI
    ///   (case-insensitive host).</item>
    /// </list>
    /// Anything else — including <c>http</c> on a non-loopback host,
    /// missing scheme, fragment-bearing URIs, or origin-less
    /// non-loopback URIs — is rejected to keep this endpoint from
    /// becoming an open-redirect oracle (plan §2.2.f, task 2.E).
    /// </summary>
    internal static bool IsAcceptableRedirectUri(string redirectUri, string origin, out string rejectReason)
    {
        rejectReason = null;
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            rejectReason = "redirect_uri must not be empty.";
            return false;
        }

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            rejectReason = "redirect_uri must be an absolute URI.";
            return false;
        }

        // RFC 7591 §2 / §5 — fragment in redirect_uri is forbidden
        // by RFC 6749 §3.1.2; reject pre-emptively to avoid storing
        // an unrepresentable allow-list entry.
        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            rejectReason = "redirect_uri must not contain a fragment.";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            rejectReason = "redirect_uri scheme must be http (loopback only) or https.";
            return false;
        }

        if (IsLoopbackHost(uri))
        {
            // RFC 8252 §7.3 explicitly tolerates http on loopback.
            // Native clients pick an ephemeral port, so we allow any.
            return true;
        }

        // Non-loopback: require https and an Origin match.
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            rejectReason = "redirect_uri must use https unless it is a loopback address.";
            return false;
        }

        if (string.IsNullOrEmpty(origin))
        {
            rejectReason = "redirect_uri host must match the request Origin or be a loopback address; "
                + "no Origin header was supplied.";
            return false;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
            || (originUri.Scheme != Uri.UriSchemeHttp && originUri.Scheme != Uri.UriSchemeHttps))
        {
            rejectReason = "Origin header is not a valid http(s) absolute URI.";
            return false;
        }

        // Compare scheme + host + port. Hosts are case-insensitive
        // per RFC 3986 §3.2.2; ports are normalized by Uri (default
        // port for the scheme yields uri.Port == default).
        if (!string.Equals(uri.Scheme, originUri.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, originUri.Host, StringComparison.OrdinalIgnoreCase)
            || uri.Port != originUri.Port)
        {
            rejectReason = "redirect_uri host does not match the request Origin.";
            return false;
        }

        return true;
    }

    private static bool IsLoopbackHost(Uri uri)
    {
        // Uri normalizes [::1] to "::1" in Host; IsLoopback covers
        // 127.0.0.0/8 and the IPv6 loopback. "localhost" is also a
        // RFC 6761 reserved name that resolves to a loopback.
        if (uri.IsLoopback) return true;
        return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    // ---- response helpers -----------------------------------------

    private static async Task WriteSuccessAsync(
        HttpContext ctx,
        string clientId,
        IReadOnlyList<string> redirectUris,
        string clientName,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        ctx.Response.StatusCode = StatusCodes.Status201Created;
        ctx.Response.ContentType = ResponseContentType;

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("client_id", clientId);
            // RFC 7591 §3.2.1: client_id_issued_at, expressed as a
            // Unix timestamp (seconds).
            writer.WriteNumber("client_id_issued_at", now.ToUnixTimeSeconds());
            // RFC 7591 §3.2.1: 0 indicates no client_secret was
            // issued — the canonical "public client" signal.
            writer.WriteNumber("client_secret_expires_at", 0);
            // The bridge tracks its own 30-day allow-list TTL via
            // client_id expiration; surface it under the IETF
            // client_metadata convention so well-behaved clients can
            // re-register before expiry instead of waiting for a 401.
            writer.WriteNumber("client_id_expires_at", expiresAt.ToUnixTimeSeconds());
            writer.WriteString("token_endpoint_auth_method", "none");

            writer.WriteStartArray("redirect_uris");
            foreach (var uri in redirectUris)
                writer.WriteStringValue(uri);
            writer.WriteEndArray();

            writer.WriteStartArray("grant_types");
            writer.WriteStringValue("authorization_code");
            writer.WriteEndArray();

            writer.WriteStartArray("response_types");
            writer.WriteStringValue("code");
            writer.WriteEndArray();

            if (!string.IsNullOrEmpty(clientName))
                writer.WriteString("client_name", clientName);

            writer.WriteEndObject();
        }
        await ctx.Response.Body.WriteAsync(buffer.GetBuffer().AsMemory(0, (int)buffer.Length));
    }

    private static async Task WriteErrorAsync(HttpContext ctx, int statusCode, string error, string description)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = ResponseContentType;

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("error", error);
            if (!string.IsNullOrEmpty(description))
                writer.WriteString("error_description", description);
            writer.WriteEndObject();
        }
        await ctx.Response.Body.WriteAsync(buffer.GetBuffer().AsMemory(0, (int)buffer.Length));
    }

    private static void SetCorsHeaders(HttpContext ctx)
    {
        // Plan §2.6: /register is server-to-server, allow * for
        // pragmatism.
        ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
        ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        ctx.Response.Headers["Access-Control-Max-Age"] = "3600";
    }
}
