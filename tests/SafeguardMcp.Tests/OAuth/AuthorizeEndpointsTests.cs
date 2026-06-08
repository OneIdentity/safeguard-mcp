#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Verifies the behavior of the bridge's <c>/authorize</c> and
/// <c>/authorize/callback</c> handlers:
///
/// <list type="bullet">
///   <item><c>/authorize</c> rejects unknown client_ids and
///   mismatched redirect_uris with a plain 400 (RFC 6749 §4.1.2.1
///   "do not redirect").</item>
///   <item><c>/authorize</c> redirects other validation failures to
///   the client redirect_uri with <c>error=</c>+<c>state=</c>.</item>
///   <item><c>/authorize</c> on success 302-redirects to
///   <c>https://&lt;SAFEGUARD_HOST&gt;/RSTS/Login</c> with the
///   bridge↔rSTS PKCE challenge (S256-hashed verifier) and a
///   bridge-minted opaque <c>state</c>.</item>
///   <item><c>/authorize/callback</c> mints a single-use opaque
///   bridge_auth_code, persists exactly the credentials
///   <c>/token</c> will need, and redirects to the client's
///   <c>redirect_uri</c> with that code and the client's original
///   <c>state</c> — <strong>without</strong> doing any upstream
///   token exchange.</item>
///   <item><c>/authorize/callback</c> rejects an unknown state with
///   a plain 400 — no open redirect via this endpoint.</item>
/// </list>
/// </summary>
public class AuthorizeEndpointsTests
{
    private const string ClientRedirect = "http://127.0.0.1:8765/cb";
    private const string ClientId = "mcp-client-1";

    private static BridgeOptions Opts()
    {
        var r = BridgeOptions.Parse(name => name switch
        {
            "MCP_PUBLIC_URL" => "https://mcp.example.test",
            "RSTS_CLIENT_ID" => "https://rsts.example.test/bridge",
            "SAFEGUARD_HOST" => "appliance.example.test",
            _ => null,
        });
        Assert.True(r.IsActive);
        return r.Options;
    }

    private static (HttpContext Ctx, AuthorizeFlowStore Flow, AuthCodeStore Codes, ClientRegistry Reg, FakeTime Time)
        BuildContext(string query)
    {
        var time = new FakeTime();
        var reg = new ClientRegistry();
        reg.TryAdd(ClientId, new[] { ClientRedirect }, time.GetUtcNow().AddDays(30));

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton(reg);
        var flow = new AuthorizeFlowStore(time);
        services.AddSingleton(flow);
        var codes = new AuthCodeStore(time);
        services.AddSingleton(codes);

        var ctx = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        ctx.Request.Method = "GET";
        ctx.Request.Path = "/authorize";
        ctx.Request.QueryString = new QueryString(query);
        ctx.Response.Body = new MemoryStream();
        return (ctx, flow, codes, reg, time);
    }

    private static string ReadBody(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        using var r = new StreamReader(ctx.Response.Body);
        return r.ReadToEnd();
    }

    private static Dictionary<string, string> ParseQuery(string url)
    {
        var idx = url.IndexOf('?');
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        if (idx < 0) return dict;
        foreach (var pair in url.Substring(idx + 1).Split('&'))
        {
            var eq = pair.IndexOf('=');
            if (eq < 0) dict[Uri.UnescapeDataString(pair)] = "";
            else dict[Uri.UnescapeDataString(pair.Substring(0, eq))] = Uri.UnescapeDataString(pair.Substring(eq + 1));
        }
        return dict;
    }

    private static string ValidAuthorizeQuery(string verifier)
    {
        var challenge = PkceUtilities.ComputeS256Challenge(verifier);
        var qs = new StringBuilder("?");
        qs.Append("response_type=code");
        qs.Append("&client_id=").Append(Uri.EscapeDataString(ClientId));
        qs.Append("&redirect_uri=").Append(Uri.EscapeDataString(ClientRedirect));
        qs.Append("&code_challenge=").Append(Uri.EscapeDataString(challenge));
        qs.Append("&code_challenge_method=S256");
        qs.Append("&state=").Append("client-state-xyz");
        qs.Append("&resource=").Append(Uri.EscapeDataString("https://mcp.example.test"));
        return qs.ToString();
    }

    [Fact]
    public async Task Authorize_HappyPath_RedirectsToRstsLogin_AndStoresFlow()
    {
        var (ctx, flow, _, _, _) = BuildContext(ValidAuthorizeQuery("verifier-abc-123"));
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.Equal(302, ctx.Response.StatusCode);
        var location = ctx.Response.Headers["Location"].ToString();
        Assert.StartsWith("https://appliance.example.test/RSTS/Login?", location);

        var q = ParseQuery(location);
        Assert.Equal("code", q["response_type"]);
        Assert.Equal("https://rsts.example.test/bridge", q["client_id"]);
        Assert.Equal("https://mcp.example.test/authorize/callback", q["redirect_uri"]);
        Assert.Equal("S256", q["code_challenge_method"]);
        Assert.False(string.IsNullOrEmpty(q["code_challenge"]));
        // The bridge-minted state is the bridge_session_id used to
        // recover flow state at callback. It must NOT equal the
        // client's state — otherwise the bridge would leak the
        // client's state to rSTS unnecessarily.
        Assert.NotEqual("client-state-xyz", q["state"]);
        Assert.False(string.IsNullOrEmpty(q["state"]));
        Assert.Equal(1, flow.Count);
    }

    [Fact]
    public async Task Authorize_HappyPath_PassesThroughPrimaryProviderId()
    {
        var qs = ValidAuthorizeQuery("verifier") + "&primaryProviderID=local&secondaryProviderID=2fa";
        var (ctx, _, _, _, _) = BuildContext(qs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        var q = ParseQuery(ctx.Response.Headers["Location"].ToString());
        Assert.Equal("local", q["primaryProviderID"]);
        Assert.Equal("2fa", q["secondaryProviderID"]);
    }

    [Fact]
    public async Task Authorize_MissingClientId_Returns400()
    {
        var (ctx, flow, _, _, _) = BuildContext("?redirect_uri=" + Uri.EscapeDataString(ClientRedirect));
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.Equal(400, ctx.Response.StatusCode);
        Assert.Equal(0, flow.Count);
    }

    [Fact]
    public async Task Authorize_UnknownClientId_Returns400_NotRedirect()
    {
        // RFC 6749 §4.1.2.1: do NOT redirect when client_id is not
        // registered — even if the supplied redirect_uri looks
        // plausible.
        var qs = "?client_id=unknown&redirect_uri=" + Uri.EscapeDataString(ClientRedirect);
        var (ctx, flow, _, _, _) = BuildContext(qs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.Equal(400, ctx.Response.StatusCode);
        Assert.False(ctx.Response.Headers.ContainsKey("Location"));
        Assert.Equal(0, flow.Count);
    }

    [Fact]
    public async Task Authorize_RedirectUriMismatch_Returns400()
    {
        var qs = "?client_id=" + ClientId + "&redirect_uri=" + Uri.EscapeDataString("http://evil.example.com/cb");
        var (ctx, flow, _, _, _) = BuildContext(qs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.Equal(400, ctx.Response.StatusCode);
        Assert.Equal(0, flow.Count);
    }

    [Fact]
    public async Task Authorize_BadCodeChallengeMethod_RedirectsWithError()
    {
        var qs = ValidAuthorizeQuery("v").Replace("code_challenge_method=S256", "code_challenge_method=plain");
        var (ctx, flow, _, _, _) = BuildContext(qs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.Equal(302, ctx.Response.StatusCode);
        var loc = ctx.Response.Headers["Location"].ToString();
        Assert.StartsWith(ClientRedirect, loc);
        var q = ParseQuery(loc);
        Assert.Equal("invalid_request", q["error"]);
        Assert.Equal("client-state-xyz", q["state"]);
        Assert.Equal(0, flow.Count);
    }

    [Fact]
    public async Task Authorize_ResourceMismatch_RedirectsWithInvalidTarget()
    {
        var qs = ValidAuthorizeQuery("v").Replace(
            "resource=" + Uri.EscapeDataString("https://mcp.example.test"),
            "resource=" + Uri.EscapeDataString("https://otherapi.example.test"));
        var (ctx, flow, _, _, _) = BuildContext(qs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.Equal(302, ctx.Response.StatusCode);
        var q = ParseQuery(ctx.Response.Headers["Location"].ToString());
        Assert.Equal("invalid_target", q["error"]);
        Assert.Equal("client-state-xyz", q["state"]);
        Assert.Equal(0, flow.Count);
    }

    [Fact]
    public async Task Authorize_UsesS256HashOfFreshVerifier()
    {
        // Run /authorize twice and confirm the persisted
        // bridge↔rSTS verifier hashes to the challenge sent
        // upstream and changes between requests (fresh randomness).
        var (ctx1, flow1, _, _, _) = BuildContext(ValidAuthorizeQuery("v"));
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx1, Opts());
        var challenge1 = ParseQuery(ctx1.Response.Headers["Location"].ToString())["code_challenge"];
        var sid1 = ParseQuery(ctx1.Response.Headers["Location"].ToString())["state"];
        Assert.True(flow1.TryConsume(sid1, out var entry1));
        Assert.Equal(challenge1, PkceUtilities.ComputeS256Challenge(entry1.BridgeToRstsPkceVerifier));

        var (ctx2, flow2, _, _, _) = BuildContext(ValidAuthorizeQuery("v"));
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx2, Opts());
        var challenge2 = ParseQuery(ctx2.Response.Headers["Location"].ToString())["code_challenge"];
        Assert.NotEqual(challenge1, challenge2);
    }

    // ---- /authorize/callback ---------------------------------------

    private static (HttpContext Ctx, string BridgeSessionId, AuthorizeFlowStore Flow, AuthCodeStore Codes, FakeTime Time)
        SeedFlowAndBuildCallback(string queryAppend)
    {
        var time = new FakeTime();
        var reg = new ClientRegistry();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton(reg);
        var flow = new AuthorizeFlowStore(time);
        services.AddSingleton(flow);
        var codes = new AuthCodeStore(time);
        services.AddSingleton(codes);

        var sid = "bridge-session-" + Guid.NewGuid().ToString("N");
        flow.Add(sid, new AuthorizeFlowStore.Entry(
            clientId: ClientId,
            clientRedirectUri: ClientRedirect,
            clientState: "client-state-xyz",
            clientPkceChallenge: "client-pkce-challenge",
            bridgeToRstsPkceVerifier: "bridge-rsts-verifier",
            expiresAt: time.GetUtcNow().AddSeconds(300)));

        var ctx = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        ctx.Request.Path = "/authorize/callback";
        ctx.Request.QueryString = new QueryString("?state=" + Uri.EscapeDataString(sid) + queryAppend);
        ctx.Response.Body = new MemoryStream();
        return (ctx, sid, flow, codes, time);
    }

    [Fact]
    public async Task Callback_HappyPath_MintsCodeAndRedirectsToClient()
    {
        var (ctx, sid, flow, codes, _) = SeedFlowAndBuildCallback("&code=rsts-auth-code-abc");
        await AuthorizeEndpoints.HandleAuthorizeCallbackAsync(ctx, Opts());

        Assert.Equal(302, ctx.Response.StatusCode);
        var loc = ctx.Response.Headers["Location"].ToString();
        Assert.StartsWith(ClientRedirect, loc);
        var q = ParseQuery(loc);
        Assert.Equal("client-state-xyz", q["state"]);
        Assert.False(string.IsNullOrEmpty(q["code"]));
        // bridge_auth_code is the bridge's own opaque token, not
        // the upstream rSTS auth code passed through.
        Assert.NotEqual("rsts-auth-code-abc", q["code"]);

        // Flow consumed; auth-code entry persisted with the exact
        // material /token will need.
        Assert.Equal(0, flow.Count);
        Assert.Equal(1, codes.Count);
        Assert.True(codes.TryConsume(q["code"], out var entry));
        Assert.Equal("rsts-auth-code-abc", entry.RstsAuthCode);
        Assert.Equal("bridge-rsts-verifier", entry.BridgeToRstsPkceVerifier);
        Assert.Equal("client-pkce-challenge", entry.ClientPkceChallenge);
        Assert.Equal(ClientRedirect, entry.ClientRedirectUri);
        Assert.Equal(ClientId, entry.ClientId);
    }

    [Fact]
    public async Task Callback_RstsError_RedirectsErrorToClient()
    {
        var (ctx, _, flow, codes, _) = SeedFlowAndBuildCallback(
            "&error=access_denied&error_description=" + Uri.EscapeDataString("user cancelled"));
        await AuthorizeEndpoints.HandleAuthorizeCallbackAsync(ctx, Opts());

        Assert.Equal(302, ctx.Response.StatusCode);
        var q = ParseQuery(ctx.Response.Headers["Location"].ToString());
        Assert.Equal("access_denied", q["error"]);
        Assert.Equal("user cancelled", q["error_description"]);
        Assert.Equal("client-state-xyz", q["state"]);
        // Flow entry was consumed; no auth-code minted.
        Assert.Equal(0, flow.Count);
        Assert.Equal(0, codes.Count);
    }

    [Fact]
    public async Task Callback_UnknownState_Returns400_NoRedirect()
    {
        // No flow seeded under this state — the bridge has no
        // client_redirect_uri to send the user-agent to, and must
        // not become an open redirect.
        var time = new FakeTime();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton(new ClientRegistry());
        services.AddSingleton(new AuthorizeFlowStore(time));
        services.AddSingleton(new AuthCodeStore(time));

        var ctx = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        ctx.Request.QueryString = new QueryString("?state=unknown&code=anything");
        ctx.Response.Body = new MemoryStream();

        await AuthorizeEndpoints.HandleAuthorizeCallbackAsync(ctx, Opts());

        Assert.Equal(400, ctx.Response.StatusCode);
        Assert.False(ctx.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task Callback_StateReplay_FailsSecondTime()
    {
        var (ctx1, sid, flow, _, _) = SeedFlowAndBuildCallback("&code=rsts-auth-code");
        await AuthorizeEndpoints.HandleAuthorizeCallbackAsync(ctx1, Opts());
        Assert.Equal(302, ctx1.Response.StatusCode);

        // Reuse the same state on a second callback; flow store
        // already consumed → must 400.
        var ctx2 = new DefaultHttpContext { RequestServices = ctx1.RequestServices };
        ctx2.Request.QueryString = new QueryString("?state=" + Uri.EscapeDataString(sid) + "&code=replay");
        ctx2.Response.Body = new MemoryStream();
        await AuthorizeEndpoints.HandleAuthorizeCallbackAsync(ctx2, Opts());
        Assert.Equal(400, ctx2.Response.StatusCode);
    }
}
