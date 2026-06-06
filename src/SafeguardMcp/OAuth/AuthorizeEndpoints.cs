using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SafeguardMcp.OAuth;

/// <summary>
/// Maps the bridge's user-agent OAuth endpoints
/// <c>GET /authorize</c> (plan §2.2.c) and
/// <c>GET /authorize/callback</c> (plan §2.2.d).
///
/// <para>
/// <c>/authorize</c> is the entry point for an MCP client's
/// browser-based PKCE authorization-code grant. The bridge does no
/// upstream token work; it validates the request, mints a fresh
/// bridge↔rSTS PKCE pair, stores the in-flight flow state keyed by
/// an opaque <c>bridge_session_id</c>, and 302-redirects the user-
/// agent to <c>https://&lt;SAFEGUARD_HOST&gt;/RSTS/Login</c>.
/// </para>
///
/// <para>
/// <c>/authorize/callback</c> receives the rSTS authorization code,
/// recovers the flow state by the rSTS <c>state</c> echo, mints an
/// opaque <c>bridge_auth_code</c>, persists the redemption record in
/// <see cref="AuthCodeStore"/>, and 302-redirects the user-agent
/// back to the MCP client's <c>redirect_uri</c> with that code and
/// the client's original <c>state</c>. <strong>No upstream token
/// work happens here</strong> — that's deferred to <c>POST /token</c>
/// (plan §2.2.e) where the client's PKCE check can fail before any
/// network call is made.
/// </para>
/// </summary>
internal static class AuthorizeEndpoints
{
    public const string AuthorizePath = "/authorize";
    public const string AuthorizeCallbackPath = "/authorize/callback";

    public static void Map(IEndpointRouteBuilder endpoints, BridgeOptions options)
    {
        if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
        if (options == null) throw new ArgumentNullException(nameof(options));

        endpoints.MapGet(AuthorizePath, ctx => HandleAuthorizeAsync(ctx, options));
        endpoints.MapGet(AuthorizeCallbackPath, ctx => HandleAuthorizeCallbackAsync(ctx, options));
    }

    // ---- /authorize ------------------------------------------------

    internal static async Task HandleAuthorizeAsync(HttpContext ctx, BridgeOptions options)
    {
        var q = ctx.Request.Query;

        var clientId = q["client_id"].ToString();
        var redirectUri = q["redirect_uri"].ToString();
        var responseType = q["response_type"].ToString();
        var codeChallenge = q["code_challenge"].ToString();
        var codeChallengeMethod = q["code_challenge_method"].ToString();
        var state = q["state"].ToString();
        var resource = q["resource"].ToString();
        var primaryProviderId = q["primaryProviderID"].ToString();
        var secondaryProviderId = q["secondaryProviderID"].ToString();

        var registry = ctx.RequestServices.GetRequiredService<ClientRegistry>();
        var now = ctx.RequestServices.GetService<TimeProvider>()?.GetUtcNow()
                  ?? TimeProvider.System.GetUtcNow();

        // RFC 6749 §4.1.2.1: if client_id or redirect_uri are invalid,
        // do NOT redirect — display the error to the resource owner.
        // Validating registry membership before anything else also
        // closes the open-redirect surface on this endpoint.
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
        {
            await WriteBadRequestAsync(ctx, "invalid_request",
                "client_id and redirect_uri are required.");
            return;
        }
        if (!registry.IsValidRedirectUri(clientId, redirectUri, now))
        {
            await WriteBadRequestAsync(ctx, "unauthorized_client",
                "client_id is unknown or redirect_uri does not match a registered URI for this client.");
            return;
        }

        // From here on, parameter errors get reported back to the
        // client by 302-redirect per RFC 6749 §4.1.2.1.
        if (!string.Equals(responseType, "code", StringComparison.Ordinal))
        {
            RedirectWithError(ctx, redirectUri, "unsupported_response_type",
                "response_type must be 'code'.", state);
            return;
        }
        if (string.IsNullOrEmpty(codeChallenge)
            || !string.Equals(codeChallengeMethod, "S256", StringComparison.Ordinal))
        {
            RedirectWithError(ctx, redirectUri, "invalid_request",
                "code_challenge is required and code_challenge_method must be 'S256'.", state);
            return;
        }
        if (string.IsNullOrEmpty(state))
        {
            RedirectWithError(ctx, redirectUri, "invalid_request",
                "state is required.", state);
            return;
        }
        // Plan §2.4 / RFC 8707: the bridge advertises MCP_PUBLIC_URL
        // as the protected resource; the client MUST name it as the
        // resource indicator. Strict match — any other value reveals
        // a misconfigured client trying to teleport tokens.
        if (!string.Equals(resource, options.McpPublicUrl, StringComparison.Ordinal))
        {
            RedirectWithError(ctx, redirectUri, "invalid_target",
                "resource parameter must equal the bridge's advertised MCP_PUBLIC_URL.", state);
            return;
        }

        var (verifier, challenge) = PkceUtilities.GenerateS256Pair();
        var bridgeSessionId = PkceUtilities.GenerateOpaqueToken();

        // Flow state is bounded by rSTS's own 5-minute auth-code
        // window — there is no point letting it linger longer than
        // upstream will accept a code redemption.
        var flowExpiresAt = now.AddSeconds(BridgeOptions.MaxAuthCodeTtlSeconds);

        var flow = new AuthorizeFlowStore.Entry(
            clientId: clientId,
            clientRedirectUri: redirectUri,
            clientState: state,
            clientPkceChallenge: codeChallenge,
            bridgeToRstsPkceVerifier: verifier,
            expiresAt: flowExpiresAt);

        var flowStore = ctx.RequestServices.GetRequiredService<AuthorizeFlowStore>();
        flowStore.Add(bridgeSessionId, flow);

        var rstsLoginUrl = BuildRstsLoginUrl(
            options,
            challenge,
            bridgeSessionId,
            primaryProviderId,
            secondaryProviderId);

        ctx.Response.Redirect(rstsLoginUrl);
    }

    private static string BuildRstsLoginUrl(
        BridgeOptions options,
        string codeChallenge,
        string bridgeSessionId,
        string primaryProviderId,
        string secondaryProviderId)
    {
        // Plan §2.1 fixes the front-channel URL; FACTS §rSTS §Endpoints
        // confirms /RSTS/Login is the canonical entry (the OAuth alias
        // /RSTS/oauth2/auth just 302s here).
        var sb = new StringBuilder("https://");
        sb.Append(options.SafeguardHost);
        sb.Append("/RSTS/Login");
        sb.Append("?response_type=code");
        AppendQuery(sb, "client_id", options.RstsClientId);
        AppendQuery(sb, "redirect_uri", options.AuthorizeCallbackEndpoint);
        AppendQuery(sb, "state", bridgeSessionId);
        AppendQuery(sb, "code_challenge", codeChallenge);
        AppendQuery(sb, "code_challenge_method", "S256");
        if (!string.IsNullOrEmpty(primaryProviderId))
            AppendQuery(sb, "primaryProviderID", primaryProviderId);
        if (!string.IsNullOrEmpty(secondaryProviderId))
            AppendQuery(sb, "secondaryProviderID", secondaryProviderId);
        return sb.ToString();
    }

    // ---- /authorize/callback ---------------------------------------

    internal static async Task HandleAuthorizeCallbackAsync(HttpContext ctx, BridgeOptions options)
    {
        var q = ctx.Request.Query;
        var bridgeSessionId = q["state"].ToString();
        var rstsCode = q["code"].ToString();
        var rstsError = q["error"].ToString();
        var rstsErrorDescription = q["error_description"].ToString();

        var flowStore = ctx.RequestServices.GetRequiredService<AuthorizeFlowStore>();

        if (string.IsNullOrEmpty(bridgeSessionId)
            || !flowStore.TryConsume(bridgeSessionId, out var flow))
        {
            // No or unknown state: the bridge has no client redirect
            // URI to return the user-agent to. Show a plain error so
            // an attacker cannot trigger an open redirect via this
            // endpoint either.
            await WriteBadRequestAsync(ctx, "invalid_request",
                "Missing, expired, or already-consumed authorize state.");
            return;
        }

        if (!string.IsNullOrEmpty(rstsError))
        {
            RedirectWithError(ctx, flow.ClientRedirectUri, rstsError,
                string.IsNullOrEmpty(rstsErrorDescription) ? null : rstsErrorDescription,
                flow.ClientState);
            return;
        }

        if (string.IsNullOrEmpty(rstsCode))
        {
            RedirectWithError(ctx, flow.ClientRedirectUri, "server_error",
                "rSTS callback returned neither code nor error.", flow.ClientState);
            return;
        }

        var now = ctx.RequestServices.GetService<TimeProvider>()?.GetUtcNow()
                  ?? TimeProvider.System.GetUtcNow();

        var bridgeAuthCode = PkceUtilities.GenerateOpaqueToken();
        var authCodeStore = ctx.RequestServices.GetRequiredService<AuthCodeStore>();
        authCodeStore.Add(bridgeAuthCode, new AuthCodeStore.Entry(
            rstsAuthCode: rstsCode,
            bridgeToRstsPkceVerifier: flow.BridgeToRstsPkceVerifier,
            clientPkceChallenge: flow.ClientPkceChallenge,
            clientRedirectUri: flow.ClientRedirectUri,
            clientId: flow.ClientId,
            expiresAt: now.AddSeconds(options.AuthCodeTtlSeconds)));

        var sb = new StringBuilder(flow.ClientRedirectUri);
        sb.Append(flow.ClientRedirectUri.Contains('?') ? '&' : '?');
        sb.Append("code=").Append(Uri.EscapeDataString(bridgeAuthCode));
        if (!string.IsNullOrEmpty(flow.ClientState))
            sb.Append("&state=").Append(Uri.EscapeDataString(flow.ClientState));

        ctx.Response.Redirect(sb.ToString());
    }

    // ---- helpers ---------------------------------------------------

    private static void RedirectWithError(
        HttpContext ctx, string redirectUri, string error, string description, string state)
    {
        var sb = new StringBuilder(redirectUri);
        sb.Append(redirectUri.Contains('?') ? '&' : '?');
        sb.Append("error=").Append(Uri.EscapeDataString(error));
        if (!string.IsNullOrEmpty(description))
            sb.Append("&error_description=").Append(Uri.EscapeDataString(description));
        if (!string.IsNullOrEmpty(state))
            sb.Append("&state=").Append(Uri.EscapeDataString(state));
        ctx.Response.Redirect(sb.ToString());
    }

    private static async Task WriteBadRequestAsync(HttpContext ctx, string error, string description)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.Response.WriteAsync(error + ": " + description);
    }

    private static void AppendQuery(StringBuilder sb, string name, string value)
    {
        sb.Append('&').Append(name).Append('=').Append(Uri.EscapeDataString(value));
    }
}
