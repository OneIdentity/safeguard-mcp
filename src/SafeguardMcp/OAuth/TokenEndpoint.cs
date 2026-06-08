using System.Buffers.Text;
using System.IO;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.OAuth;

/// <summary>
/// Maps <c>POST /token</c> — the bridge's RFC 6749 authorization-code
/// redemption endpoint.
///
/// <para>
/// The handler runs the redemption in a strict order so the
/// state-residency invariant (no token material persists in any
/// bridge data structure) holds even under upstream latency or
/// partial failure:
/// </para>
/// <list type="number">
///   <item>Parse the form body. Bad content type / unreadable body
///   → <c>400 invalid_request</c>.</item>
///   <item>Consume the bridge_auth_code from
///   <see cref="AuthCodeStore"/>. This is the "delete in-memory
///   entry before any network call" step: a missing entry — whether
///   expired, never minted, or already consumed by a prior
///   <c>/token</c> attempt — yields <c>400 invalid_grant</c>. That
///   single rule covers RFC 6749 §4.1.3 replay rejection.</item>
///   <item>Verify <c>client_id</c> and <c>redirect_uri</c> match
///   the values captured at <c>/authorize</c> — exact, case-sensitive
///   string compare per RFC 6749 §3.1.2.3 / §4.1.3. Mismatch →
///   <c>invalid_grant</c>.</item>
///   <item>Verify PKCE: <c>BASE64URL(SHA256(code_verifier))</c> equals
///   the challenge captured at <c>/authorize</c>. Mismatch →
///   <c>invalid_grant</c>. Auth codes are single-use, so a failed
///   PKCE attempt legitimately burns the code (standard OAuth).</item>
///   <item>Call <see cref="IRstsTokenExchanger.ExchangeAuthorizationCodeAsync"/>
///   with the stored rSTS authorization code and the stored
///   bridge↔rSTS PKCE verifier and the bridge's own callback
///   URL — never the client-facing values, which the bridge does
///   not forward.</item>
///   <item>Call
///   <see cref="IRstsTokenExchanger.ExchangeRstsTokenForLoginResponseAsync"/>
///   to Stage-2 exchange the rSTS access token for the Safeguard
///   user token. Parse the raw JSON body with
///   <see cref="JsonDocument"/> (AOT-safe), require
///   <c>Status == "Success"</c>, extract <c>UserToken</c>.</item>
///   <item>Write the RFC 6749 §5.1 success body via
///   <see cref="Utf8JsonWriter"/>: <c>access_token</c>,
///   <c>token_type=Bearer</c>, and an optional <c>expires_in</c>
///   derived from the user token's JWT <c>exp</c> claim (display-
///   only — Safeguard's signing cert remains the authority on the
///   appliance side).</item>
/// </list>
///
/// <para>
/// Upstream errors from either SDK call are translated into
/// RFC 6749 error responses; appliance/rSTS response bodies are
/// <strong>not</strong> reflected to the caller.
/// </para>
///
/// <para>
/// Token material — the rSTS <see cref="SecureString"/> and the
/// Safeguard user token <see cref="string"/> — exists only as a
/// stack local in this handler. Neither is logged, neither is
/// stored anywhere; only the bridge's HTTP response carries the
/// Safeguard user token, and only to the originating client.
/// </para>
/// </summary>
internal static class TokenEndpoint
{
    public const string TokenPath = "/token";

    private const string FormContentTypePrefix = "application/x-www-form-urlencoded";
    private const string ResponseContentType = "application/json; charset=utf-8";
    // RFC 6749 §5.1 — token responses must not be cached.
    private const string CacheControlNoStore = "no-store";
    private const string PragmaNoCache = "no-cache";

    public static void Map(IEndpointRouteBuilder endpoints, BridgeOptions options)
    {
        if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
        if (options == null) throw new ArgumentNullException(nameof(options));

        endpoints.MapPost(TokenPath, ctx => HandleTokenAsync(ctx, options));
        endpoints.MapMethods(TokenPath, new[] { "OPTIONS" }, HandleOptionsAsync);
    }

    internal static Task HandleOptionsAsync(HttpContext ctx)
    {
        SetCorsHeaders(ctx);
        ctx.Response.StatusCode = StatusCodes.Status204NoContent;
        return Task.CompletedTask;
    }

    internal static async Task HandleTokenAsync(HttpContext ctx, BridgeOptions options)
    {
        SetCorsHeaders(ctx);
        ctx.Response.Headers.CacheControl = CacheControlNoStore;
        ctx.Response.Headers.Pragma = PragmaNoCache;

        // Step 1 — parse form.
        var contentType = ctx.Request.ContentType;
        if (contentType == null
            || !contentType.StartsWith(FormContentTypePrefix, StringComparison.OrdinalIgnoreCase))
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_request",
                "Content-Type must be application/x-www-form-urlencoded.");
            return;
        }

        IFormCollection form;
        try
        {
            form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);
        }
        catch
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_request",
                "Token request body could not be parsed as application/x-www-form-urlencoded.");
            return;
        }

        var grantType = form["grant_type"].ToString();
        var code = form["code"].ToString();
        var codeVerifier = form["code_verifier"].ToString();
        var redirectUri = form["redirect_uri"].ToString();
        var clientId = form["client_id"].ToString();

        if (string.IsNullOrEmpty(grantType))
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_request", "grant_type is required.");
            return;
        }
        if (!string.Equals(grantType, "authorization_code", StringComparison.Ordinal))
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "unsupported_grant_type",
                "Only the 'authorization_code' grant type is supported.");
            return;
        }
        if (string.IsNullOrEmpty(code)
            || string.IsNullOrEmpty(codeVerifier)
            || string.IsNullOrEmpty(redirectUri)
            || string.IsNullOrEmpty(clientId))
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_request",
                "code, code_verifier, redirect_uri, and client_id are required.");
            return;
        }

        // Step 2 — single-use consume. This is also the replay gate:
        // any second redemption attempt — whether by an attacker or
        // a buggy client — finds no entry and falls through to
        // invalid_grant below.
        var authCodeStore = ctx.RequestServices.GetRequiredService<AuthCodeStore>();
        if (!authCodeStore.TryConsume(code, out var entry))
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_grant",
                "Authorization code is invalid, expired, or has already been redeemed.");
            return;
        }

        // Step 3 — client_id / redirect_uri must match what was
        // captured at /authorize. RFC 6749 §3.1.2.3 / §4.1.3.
        if (!string.Equals(clientId, entry.ClientId, StringComparison.Ordinal)
            || !string.Equals(redirectUri, entry.ClientRedirectUri, StringComparison.Ordinal))
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_grant",
                "client_id or redirect_uri does not match the original authorization request.");
            return;
        }

        // Step 4 — PKCE binding. Hash the verifier and compare in
        // constant time to the challenge captured at /authorize. A
        // bad verifier here means the redeeming party is not the
        // same client that initiated /authorize, so the exchange
        // must fail before any upstream call.
        string actualChallenge;
        try
        {
            actualChallenge = PkceUtilities.ComputeS256Challenge(codeVerifier);
        }
        catch
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_grant", "code_verifier is malformed.");
            return;
        }
        if (!FixedTimeEquals(actualChallenge, entry.ClientPkceChallenge))
        {
            await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                "invalid_grant", "code_verifier does not match the original code_challenge.");
            return;
        }

        var exchanger = ctx.RequestServices.GetRequiredService<IRstsTokenExchanger>();
        var ct = ctx.RequestAborted;

        // Step 5 + 6 — upstream Stage 1 then Stage 2. Token material
        // never escapes this method.
        SecureString rstsAccessToken = null;
        try
        {
            try
            {
                rstsAccessToken = await exchanger.ExchangeAuthorizationCodeAsync(
                    options.SafeguardHost,
                    entry.RstsAuthCode,
                    entry.BridgeToRstsPkceVerifier,
                    options.AuthorizeCallbackEndpoint,
                    options.IgnoreSsl,
                    ct);
            }
            catch (SafeguardDotNetException ex)
            {
                await WriteRfc6749ErrorFromUpstreamAsync(ctx, ex,
                    "rSTS rejected the authorization-code exchange.");
                return;
            }

            string loginResponseJson;
            try
            {
                loginResponseJson = await exchanger.ExchangeRstsTokenForLoginResponseAsync(
                    options.SafeguardHost,
                    rstsAccessToken,
                    options.IgnoreSsl,
                    ct);
            }
            catch (SafeguardDotNetException ex)
            {
                await WriteRfc6749ErrorFromUpstreamAsync(ctx, ex,
                    "Safeguard LoginResponse exchange failed.");
                return;
            }

            // Parse with JsonDocument only — AOT rule.
            string safeguardUserToken;
            using (var doc = SafelyParse(loginResponseJson))
            {
                if (doc == null)
                {
                    await WriteErrorAsync(ctx, StatusCodes.Status502BadGateway,
                        "server_error",
                        "LoginResponse body could not be parsed as JSON.");
                    return;
                }
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    await WriteErrorAsync(ctx, StatusCodes.Status502BadGateway,
                        "server_error", "LoginResponse JSON was not an object.");
                    return;
                }
                if (!root.TryGetProperty("Status", out var statusEl)
                    || statusEl.ValueKind != JsonValueKind.String
                    || !string.Equals(statusEl.GetString(), "Success", StringComparison.Ordinal))
                {
                    await WriteErrorAsync(ctx, StatusCodes.Status400BadRequest,
                        "invalid_grant",
                        "Safeguard rejected the LoginResponse exchange.");
                    return;
                }
                if (!root.TryGetProperty("UserToken", out var userTokenEl)
                    || userTokenEl.ValueKind != JsonValueKind.String)
                {
                    await WriteErrorAsync(ctx, StatusCodes.Status502BadGateway,
                        "server_error",
                        "LoginResponse JSON did not include a UserToken string.");
                    return;
                }
                safeguardUserToken = userTokenEl.GetString();
            }
            if (string.IsNullOrEmpty(safeguardUserToken))
            {
                await WriteErrorAsync(ctx, StatusCodes.Status502BadGateway,
                    "server_error", "LoginResponse UserToken was empty.");
                return;
            }

            var now = (ctx.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System)
                .GetUtcNow();
            var expiresInSeconds = TryDeriveExpiresInSeconds(safeguardUserToken, now);

            await WriteSuccessAsync(ctx, safeguardUserToken, expiresInSeconds);
        }
        finally
        {
            rstsAccessToken?.Dispose();
        }
    }

    // ---- response helpers -----------------------------------------

    private static async Task WriteSuccessAsync(HttpContext ctx, string accessToken, long expiresInSeconds)
    {
        ctx.Response.StatusCode = StatusCodes.Status200OK;
        ctx.Response.ContentType = ResponseContentType;

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("access_token", accessToken);
            writer.WriteString("token_type", "Bearer");
            // expires_in is derived from the JWT exp claim. Omit when
            // we can't derive a positive value so we never advertise
            // an already-expired token.
            if (expiresInSeconds > 0)
                writer.WriteNumber("expires_in", expiresInSeconds);
            writer.WriteEndObject();
        }
        await ctx.Response.Body.WriteAsync(buffer.GetBuffer().AsMemory(0, (int)buffer.Length));
    }

    private static async Task WriteErrorAsync(HttpContext ctx, int statusCode, string error, string description)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = ResponseContentType;
        ctx.Response.Headers.CacheControl = CacheControlNoStore;
        ctx.Response.Headers.Pragma = PragmaNoCache;

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

    private static Task WriteRfc6749ErrorFromUpstreamAsync(
        HttpContext ctx, SafeguardDotNetException ex, string fallbackDescription)
    {
        // Map upstream HTTP status to RFC 6749 shape. We intentionally
        // do NOT leak the appliance/rSTS response body — bridge
        // callers are anonymous, and rSTS error bodies have leaked
        // deployment detail before. The exception is logged where
        // SafeguardMcp.Logging sanitization scrubs bearer/token
        // patterns; the response carries only the category.
        var status = ex.HttpStatusCode;
        if (status.HasValue && ((int)status.Value == 400 || (int)status.Value == 401 || (int)status.Value == 403))
            return WriteErrorAsync(ctx, StatusCodes.Status400BadRequest, "invalid_grant", fallbackDescription);
        return WriteErrorAsync(ctx, StatusCodes.Status502BadGateway, "server_error", fallbackDescription);
    }

    private static void SetCorsHeaders(HttpContext ctx)
    {
        // /token is server-to-server, advertise * for pragmatism.
        ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
        ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        ctx.Response.Headers["Access-Control-Max-Age"] = "3600";
    }

    private static JsonDocument SafelyParse(string json)
    {
        try { return JsonDocument.Parse(json); }
        catch (JsonException) { return null; }
    }

    /// <summary>
    /// Constant-time string comparison so an attacker cannot use
    /// per-character timing to recover the stored PKCE challenge.
    /// </summary>
    private static bool FixedTimeEquals(string a, string b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }

    /// <summary>
    /// Extracts the JWT <c>exp</c> claim from the Safeguard user
    /// token without verifying the signature (display-only, plan
    /// §"Why not local JWT validation"). Returns 0 if the token is
    /// not a parseable JWT or <c>exp</c> is already past — the
    /// caller omits <c>expires_in</c> in that case so the response
    /// never advertises a non-positive lifetime.
    /// </summary>
    internal static long TryDeriveExpiresInSeconds(string jwt, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(jwt)) return 0;
        var firstDot = jwt.IndexOf('.');
        if (firstDot <= 0) return 0;
        var secondDot = jwt.IndexOf('.', firstDot + 1);
        if (secondDot <= firstDot + 1) return 0;
        var payloadChars = jwt.AsSpan(firstDot + 1, secondDot - firstDot - 1);

        try
        {
            // Base64Url.DecodeFromChars throws FormatException on
            // malformed input — wrapped because the JWT payload is
            // attacker-influenced.
            var payloadBytes = Base64Url.DecodeFromChars(payloadChars);
            using var doc = SafelyParse(Encoding.UTF8.GetString(payloadBytes));
            if (doc == null) return 0;
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return 0;
            if (!doc.RootElement.TryGetProperty("exp", out var expEl)) return 0;
            if (expEl.ValueKind != JsonValueKind.Number) return 0;
            if (!expEl.TryGetInt64(out var expSeconds)) return 0;
            var nowSeconds = now.ToUnixTimeSeconds();
            var delta = expSeconds - nowSeconds;
            return delta > 0 ? delta : 0;
        }
        catch
        {
            return 0;
        }
    }
}
