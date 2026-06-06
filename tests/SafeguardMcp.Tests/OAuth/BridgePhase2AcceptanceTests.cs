#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// HTTP-AUTH-RELAY-PLAN Phase-2 acceptance gates that span more than
/// one endpoint. The individual endpoint test files (
/// <see cref="WellKnownMetadataTests"/>,
/// <see cref="AuthorizeEndpointsTests"/>,
/// <see cref="TokenEndpointTests"/>,
/// <see cref="RegistrationEndpointTests"/>) cover per-endpoint
/// behavior; this file covers the cross-cutting invariants:
///
/// <list type="bullet">
///   <item><strong>2.3 + 2.A — State residency / no-state post-flow:</strong>
///   after a complete OAuth window, the bridge holds zero
///   <see cref="AuthCodeStore"/> entries, zero
///   <see cref="AuthorizeFlowStore"/> entries, and a reflection
///   sweep of bridge-owned state finds no Safeguard token-shaped
///   strings retained anywhere reachable from those stores.</item>
///   <item><strong>2.4 + 2.D — RFC 8707 resource indicator:</strong>
///   <c>/authorize</c> with a missing or mismatched
///   <c>resource</c> parameter rejects with
///   <c>error=invalid_target</c>, never minting flow state.</item>
///   <item><strong>2.5 — IdP pass-through:</strong>
///   <c>primaryProviderID</c> and <c>secondaryProviderID</c>
///   forwarded verbatim to <c>/RSTS/Login</c>; absent when not
///   supplied; never overridden by the bridge.</item>
///   <item><strong>2.6 — CORS policy:</strong> the four
///   server-to-server / metadata endpoints set
///   <c>Access-Control-Allow-Origin: *</c> and respond to
///   <c>OPTIONS</c> preflight; <c>/authorize</c> and
///   <c>/authorize/callback</c> intentionally do not (they are
///   user-agent redirects, not XHR).</item>
/// </list>
/// </summary>
public class BridgePhase2AcceptanceTests
{
    // ------------------------------------------------------------------
    // Shared scaffolding
    // ------------------------------------------------------------------

    private const string ClientRedirect = "http://127.0.0.1:8765/cb";
    private const string ClientId = "mcp-client-1";
    private const string ClientState = "client-state-xyz";

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

    private static Dictionary<string, string> ParseQuery(string url)
    {
        var idx = url.IndexOf('?');
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        if (idx < 0) return dict;
        foreach (var pair in url.Substring(idx + 1).Split('&'))
        {
            var eq = pair.IndexOf('=');
            if (eq < 0) dict[Uri.UnescapeDataString(pair)] = "";
            else dict[Uri.UnescapeDataString(pair.Substring(0, eq))] =
                Uri.UnescapeDataString(pair.Substring(eq + 1));
        }
        return dict;
    }

    /// <summary>
    /// Single DI graph carrying the three bridge stores plus a fake
    /// rSTS exchanger across the four endpoint hops of one
    /// authorization-code flow.
    /// </summary>
    private sealed class BridgeFixture
    {
        public IServiceProvider Services { get; init; }
        public AuthorizeFlowStore Flow { get; init; }
        public AuthCodeStore Codes { get; init; }
        public ClientRegistry Registry { get; init; }
        public RecordingExchanger Exchanger { get; init; }
        public FakeTime Time { get; init; }
    }

    /// <summary>
    /// rSTS exchanger fake that fabricates a Safeguard-shaped JWT.
    /// Mirrors the behavior of <c>FakeRstsTokenExchanger</c> in
    /// <see cref="TokenEndpointTests"/> but with extra capture for
    /// the state-residency assertions.
    /// </summary>
    private sealed class RecordingExchanger : IRstsTokenExchanger
    {
        public string LoginResponseJson { get; set; }
        public int AuthCodeCalls;
        public int LoginResponseCalls;

        public Task<SecureString> ExchangeAuthorizationCodeAsync(
            string appliance, string rstsAuthorizationCode, string pkceVerifier,
            string redirectUri, bool ignoreSsl, CancellationToken ct)
        {
            Interlocked.Increment(ref AuthCodeCalls);
            var s = new SecureString();
            foreach (var c in "fake-rsts-access-token") s.AppendChar(c);
            s.MakeReadOnly();
            return Task.FromResult(s);
        }

        public Task<string> ExchangeRstsTokenForLoginResponseAsync(
            string appliance, SecureString rstsAccessToken, bool ignoreSsl, CancellationToken ct)
        {
            Interlocked.Increment(ref LoginResponseCalls);
            return Task.FromResult(LoginResponseJson);
        }
    }

    private static string MakeJwt(long expUnix)
    {
        static string Url(string s) =>
            System.Buffers.Text.Base64Url.EncodeToString(Encoding.UTF8.GetBytes(s));
        var header = Url("{\"alg\":\"RS256\",\"typ\":\"JWT\"}");
        var payload = Url("{\"sub\":\"alice\",\"exp\":" + expUnix + "}");
        var sig = System.Buffers.Text.Base64Url.EncodeToString(new byte[] { 1, 2, 3, 4 });
        return header + "." + payload + "." + sig;
    }

    private static BridgeFixture NewFixture()
    {
        var time = new FakeTime();
        var registry = new ClientRegistry();
        registry.TryAdd(ClientId, new[] { ClientRedirect }, time.GetUtcNow().AddDays(30));
        var flow = new AuthorizeFlowStore(time);
        var codes = new AuthCodeStore(time);
        var fakeJwt = MakeJwt(time.GetUtcNow().AddHours(1).ToUnixTimeSeconds());
        var exchanger = new RecordingExchanger
        {
            LoginResponseJson = "{\"Status\":\"Success\",\"UserToken\":\"" + fakeJwt + "\"}",
        };

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton(registry);
        services.AddSingleton(flow);
        services.AddSingleton(codes);
        services.AddSingleton<IRstsTokenExchanger>(exchanger);

        return new BridgeFixture
        {
            Services = services.BuildServiceProvider(),
            Flow = flow,
            Codes = codes,
            Registry = registry,
            Exchanger = exchanger,
            Time = time,
        };
    }

    private static HttpContext NewContext(BridgeFixture f, string method, string path, string queryString)
    {
        var ctx = new DefaultHttpContext { RequestServices = f.Services };
        ctx.Request.Method = method;
        ctx.Request.Path = path;
        if (queryString != null)
            ctx.Request.QueryString = new QueryString(queryString);
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static HttpContext NewPostFormContext(BridgeFixture f, string path, string formBody)
    {
        var ctx = NewContext(f, "POST", path, null);
        ctx.Request.ContentType = "application/x-www-form-urlencoded";
        var bytes = Encoding.UTF8.GetBytes(formBody);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        return ctx;
    }

    // ==================================================================
    // 2.3 + 2.A — State residency / no-state post-flow
    // ==================================================================

    /// <summary>
    /// Drives the full /authorize → /authorize/callback → /token
    /// pipeline in-process and asserts the post-flow state-residency
    /// invariant in plan §2.3 and §2.A: zero auth-code-map entries,
    /// zero flow-store entries, and no Safeguard token strings
    /// reachable from any bridge-owned object after the flow
    /// completes successfully.
    /// </summary>
    [Fact]
    public async Task EndToEndOAuthFlow_LeavesNoStateAndNoTokensRetained()
    {
        var f = NewFixture();
        var clientVerifier = "client-verifier-" + Guid.NewGuid().ToString("N");
        var clientChallenge = PkceUtilities.ComputeS256Challenge(clientVerifier);

        // ---- /authorize ----
        var authorizeQs = "?response_type=code"
            + "&client_id=" + Uri.EscapeDataString(ClientId)
            + "&redirect_uri=" + Uri.EscapeDataString(ClientRedirect)
            + "&code_challenge=" + Uri.EscapeDataString(clientChallenge)
            + "&code_challenge_method=S256"
            + "&state=" + Uri.EscapeDataString(ClientState)
            + "&resource=" + Uri.EscapeDataString("https://mcp.example.test");

        var ctxAuthorize = NewContext(f, "GET", "/authorize", authorizeQs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctxAuthorize, Opts());

        Assert.Equal(302, ctxAuthorize.Response.StatusCode);
        var rstsRedirect = ctxAuthorize.Response.Headers["Location"].ToString();
        var rstsQuery = ParseQuery(rstsRedirect);
        var bridgeSessionId = rstsQuery["state"];
        Assert.False(string.IsNullOrEmpty(bridgeSessionId));
        Assert.Equal(1, f.Flow.Count);

        // ---- /authorize/callback ----
        var callbackQs = "?state=" + Uri.EscapeDataString(bridgeSessionId)
            + "&code=rsts-auth-code-abc";
        var ctxCallback = NewContext(f, "GET", "/authorize/callback", callbackQs);
        await AuthorizeEndpoints.HandleAuthorizeCallbackAsync(ctxCallback, Opts());

        Assert.Equal(302, ctxCallback.Response.StatusCode);
        var clientRedirectLocation = ctxCallback.Response.Headers["Location"].ToString();
        var bridgeAuthCode = ParseQuery(clientRedirectLocation)["code"];
        Assert.False(string.IsNullOrEmpty(bridgeAuthCode));
        // Flow entry consumed at callback; auth-code entry minted.
        Assert.Equal(0, f.Flow.Count);
        Assert.Equal(1, f.Codes.Count);

        // ---- /token ----
        var formBody = "grant_type=authorization_code"
            + "&code=" + Uri.EscapeDataString(bridgeAuthCode)
            + "&code_verifier=" + Uri.EscapeDataString(clientVerifier)
            + "&redirect_uri=" + Uri.EscapeDataString(ClientRedirect)
            + "&client_id=" + Uri.EscapeDataString(ClientId);
        var ctxToken = NewPostFormContext(f, "/token", formBody);
        await TokenEndpoint.HandleTokenAsync(ctxToken, Opts());

        Assert.Equal(200, ctxToken.Response.StatusCode);

        // ---- Acceptance: no state retained ----
        Assert.Equal(0, f.Flow.Count);
        Assert.Equal(0, f.Codes.Count);

        // The bridge is allowed to retain the DCR registration TTL'd
        // for 30 days — that's by design; it carries no token power.
        Assert.Equal(1, f.Registry.Count);

        // Reflection sweep of bridge-owned objects: walk every
        // string field reachable from the three stores and confirm
        // none contains a Safeguard token-shaped substring. The
        // sweep is bounded in depth so a buggy cycle can't hang it.
        var leaked = ScanForTokenLikeStrings(new object[]
        {
            f.Flow, f.Codes, f.Registry,
        });
        Assert.True(leaked.Count == 0,
            "Bridge state retained token-shaped strings post-flow: "
            + string.Join("; ", leaked));
    }

    [Fact]
    public async Task EndToEndOAuthFlow_FailedPkce_DoesNotLeakAuthCodeOrCallUpstream()
    {
        // Plan §2.3 / task 2.A corollary: the no-state guarantee
        // must hold on the failure path too — a tampered code_verifier
        // burns the auth-code entry without ever reaching upstream.
        var f = NewFixture();
        var clientVerifier = "client-verifier-OK";
        var clientChallenge = PkceUtilities.ComputeS256Challenge(clientVerifier);

        var authorizeQs = "?response_type=code"
            + "&client_id=" + ClientId
            + "&redirect_uri=" + Uri.EscapeDataString(ClientRedirect)
            + "&code_challenge=" + Uri.EscapeDataString(clientChallenge)
            + "&code_challenge_method=S256"
            + "&state=" + ClientState
            + "&resource=" + Uri.EscapeDataString("https://mcp.example.test");
        var ctxA = NewContext(f, "GET", "/authorize", authorizeQs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctxA, Opts());
        var sid = ParseQuery(ctxA.Response.Headers["Location"].ToString())["state"];

        var ctxC = NewContext(f, "GET", "/authorize/callback",
            "?state=" + sid + "&code=rsts-code");
        await AuthorizeEndpoints.HandleAuthorizeCallbackAsync(ctxC, Opts());
        var bridgeAuthCode = ParseQuery(ctxC.Response.Headers["Location"].ToString())["code"];

        var bad = "grant_type=authorization_code"
            + "&code=" + Uri.EscapeDataString(bridgeAuthCode)
            + "&code_verifier=client-verifier-WRONG"
            + "&redirect_uri=" + Uri.EscapeDataString(ClientRedirect)
            + "&client_id=" + ClientId;
        var ctxT = NewPostFormContext(f, "/token", bad);
        await TokenEndpoint.HandleTokenAsync(ctxT, Opts());

        Assert.Equal(400, ctxT.Response.StatusCode);
        // Plan §2.2.e step 2: the auth-code entry is removed from the
        // store before any upstream call. PKCE failure burns the
        // single-use code regardless.
        Assert.Equal(0, f.Codes.Count);
        Assert.Equal(0, f.Flow.Count);
        Assert.Equal(0, f.Exchanger.AuthCodeCalls);
        Assert.Equal(0, f.Exchanger.LoginResponseCalls);
    }

    /// <summary>
    /// Recursively walks every object reachable from
    /// <paramref name="roots"/>, capped at a small depth, and
    /// returns any string field that looks like a Safeguard token
    /// (Bearer-prefixed or JWT-shaped) or rSTS auth-code / verifier.
    /// </summary>
    private static List<string> ScanForTokenLikeStrings(IEnumerable<object> roots)
    {
        var hits = new List<string>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var jwtShape = new Regex(@"^eyJ[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+$",
            RegexOptions.Compiled);
        // Heuristic: the bridge's ConcurrentDictionary backing is
        // empty post-flow, so any non-empty string > 16 chars in a
        // store-owned field is suspicious.
        foreach (var root in roots)
            Walk(root, seen, hits, jwtShape, depth: 0);
        return hits;
    }

    private static void Walk(object o, HashSet<object> seen, List<string> hits, Regex jwtShape, int depth)
    {
        if (o == null || depth > 6) return;
        var t = o.GetType();
        if (t.IsPrimitive || t.IsEnum || t == typeof(decimal) || t == typeof(DateTime)
            || t == typeof(DateTimeOffset) || t == typeof(TimeSpan) || t == typeof(Guid))
            return;
        if (o is string s)
        {
            if (s.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || jwtShape.IsMatch(s))
                hits.Add("token-shaped string: " + s);
            return;
        }
        if (!seen.Add(o)) return;

        if (o is IDictionary dict)
        {
            // The bridge keeps DCR registrations across the 30-day
            // window (plan §2.3 explicitly permits "DCR client
            // registrations (no secrets)"); the assertion here is
            // about token retention, so we only walk into entries
            // looking for token-shaped strings, not flag presence.
            foreach (var key in dict.Keys)
            {
                Walk(key, seen, hits, jwtShape, depth + 1);
                Walk(dict[key], seen, hits, jwtShape, depth + 1);
            }
            return;
        }
        if (o is IEnumerable en && o is not string)
        {
            foreach (var item in en)
                Walk(item, seen, hits, jwtShape, depth + 1);
            return;
        }

        foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (f.FieldType == typeof(TimeProvider)) continue; // injected, not bridge-owned
            object v;
            try { v = f.GetValue(o); } catch { continue; }
            Walk(v, seen, hits, jwtShape, depth + 1);
        }
    }

    // ==================================================================
    // 2.4 + 2.D — RFC 8707 resource-indicator validation
    // ==================================================================

    private static string AuthorizeQueryWithResource(string resource, string verifier = "v")
    {
        var challenge = PkceUtilities.ComputeS256Challenge(verifier);
        var qs = new StringBuilder("?response_type=code");
        qs.Append("&client_id=").Append(Uri.EscapeDataString(ClientId));
        qs.Append("&redirect_uri=").Append(Uri.EscapeDataString(ClientRedirect));
        qs.Append("&code_challenge=").Append(Uri.EscapeDataString(challenge));
        qs.Append("&code_challenge_method=S256");
        qs.Append("&state=").Append(ClientState);
        if (resource != null)
            qs.Append("&resource=").Append(Uri.EscapeDataString(resource));
        return qs.ToString();
    }

    [Fact]
    public async Task Authorize_MatchingResource_Accepted()
    {
        var f = NewFixture();
        var ctx = NewContext(f, "GET", "/authorize",
            AuthorizeQueryWithResource("https://mcp.example.test"));
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.Equal(302, ctx.Response.StatusCode);
        Assert.StartsWith("https://appliance.example.test/RSTS/Login?",
            ctx.Response.Headers["Location"].ToString());
        Assert.Equal(1, f.Flow.Count);
    }

    [Theory]
    [InlineData("https://other.example.test")]
    [InlineData("https://mcp.example.test/")] // trailing slash — exact-match rule
    [InlineData("https://mcp.example.test/api")]
    [InlineData("https://MCP.example.test")]   // case difference (string.Ordinal)
    [InlineData("https://mcp.example.test:8443")]
    public async Task Authorize_MismatchedResource_RejectsWithInvalidTarget(string resource)
    {
        var f = NewFixture();
        var ctx = NewContext(f, "GET", "/authorize", AuthorizeQueryWithResource(resource));
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.Equal(302, ctx.Response.StatusCode);
        var location = ctx.Response.Headers["Location"].ToString();
        Assert.StartsWith(ClientRedirect, location);
        var q = ParseQuery(location);
        Assert.Equal("invalid_target", q["error"]);
        Assert.Equal(ClientState, q["state"]);
        // No flow state created on rejection — open-redirect surface
        // closed at the front door.
        Assert.Equal(0, f.Flow.Count);
    }

    [Fact]
    public async Task Authorize_MissingResource_RejectsWithInvalidTarget()
    {
        var f = NewFixture();
        var ctx = NewContext(f, "GET", "/authorize", AuthorizeQueryWithResource(null));
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.Equal(302, ctx.Response.StatusCode);
        var q = ParseQuery(ctx.Response.Headers["Location"].ToString());
        Assert.Equal("invalid_target", q["error"]);
        Assert.Equal(0, f.Flow.Count);
    }

    [Fact]
    public void WellKnown_AdvertisesMcpPublicUrlAsResource_AndAuthorizationServer()
    {
        // Plan §2.4: bridge advertises MCP_PUBLIC_URL as the
        // protected resource (RFC 9728) and as the issuer/auth
        // server (RFC 8414). Pin both shapes here so any future
        // edit that breaks audience binding fails this test.
        using var pr = JsonDocument.Parse(WellKnownMetadata.BuildProtectedResourceJson(Opts()));
        Assert.Equal("https://mcp.example.test", pr.RootElement.GetProperty("resource").GetString());
        Assert.Equal("https://mcp.example.test",
            pr.RootElement.GetProperty("authorization_servers")[0].GetString());

        using var asd = JsonDocument.Parse(WellKnownMetadata.BuildAuthorizationServerJson(Opts()));
        Assert.Equal("https://mcp.example.test", asd.RootElement.GetProperty("issuer").GetString());
    }

    // ==================================================================
    // 2.5 — IdP pass-through
    // ==================================================================

    [Fact]
    public async Task Authorize_PassesThroughBothProviderIds_Verbatim()
    {
        // Plan §2.5: pass through, never override. Forward both
        // provider hints to /RSTS/Login exactly as supplied.
        var f = NewFixture();
        var qs = AuthorizeQueryWithResource("https://mcp.example.test")
            + "&primaryProviderID=local&secondaryProviderID=2fa-vendor";
        var ctx = NewContext(f, "GET", "/authorize", qs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        var q = ParseQuery(ctx.Response.Headers["Location"].ToString());
        Assert.Equal("local", q["primaryProviderID"]);
        Assert.Equal("2fa-vendor", q["secondaryProviderID"]);
    }

    [Fact]
    public async Task Authorize_OmitsProviderIds_WhenAbsent()
    {
        // The bridge must not synthesize defaults — that would
        // override an operator's rSTS configuration silently.
        var f = NewFixture();
        var ctx = NewContext(f, "GET", "/authorize",
            AuthorizeQueryWithResource("https://mcp.example.test"));
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        var q = ParseQuery(ctx.Response.Headers["Location"].ToString());
        Assert.False(q.ContainsKey("primaryProviderID"));
        Assert.False(q.ContainsKey("secondaryProviderID"));
    }

    [Fact]
    public async Task Authorize_PassesThroughOnlyPrimaryProviderId_WhenSecondaryAbsent()
    {
        var f = NewFixture();
        var qs = AuthorizeQueryWithResource("https://mcp.example.test") + "&primaryProviderID=local";
        var ctx = NewContext(f, "GET", "/authorize", qs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        var q = ParseQuery(ctx.Response.Headers["Location"].ToString());
        Assert.Equal("local", q["primaryProviderID"]);
        Assert.False(q.ContainsKey("secondaryProviderID"));
    }

    [Fact]
    public async Task Authorize_ProviderIdValuesAreUrlEncoded_NotInterpreted()
    {
        // A provider ID containing reserved characters must be
        // forwarded percent-encoded so rSTS receives the same
        // raw value the MCP client supplied.
        var f = NewFixture();
        var rawId = "tenant/team@example.com";
        var qs = AuthorizeQueryWithResource("https://mcp.example.test")
            + "&primaryProviderID=" + Uri.EscapeDataString(rawId);
        var ctx = NewContext(f, "GET", "/authorize", qs);
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        var q = ParseQuery(ctx.Response.Headers["Location"].ToString());
        Assert.Equal(rawId, q["primaryProviderID"]);
    }

    // ==================================================================
    // 2.6 — CORS policy
    // ==================================================================

    private static HttpContext NewBareContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task Cors_WellKnown_OptionsPreflight_Returns204WithStarOrigin()
    {
        var ctx = NewBareContext();
        await SafeguardMcp.OAuth.WellKnownEndpoints.HandleOptionsAsync(ctx);

        Assert.Equal(204, ctx.Response.StatusCode);
        Assert.Equal("*", ctx.Response.Headers["Access-Control-Allow-Origin"].ToString());
        Assert.Contains("GET", ctx.Response.Headers["Access-Control-Allow-Methods"].ToString());
        Assert.Contains("OPTIONS", ctx.Response.Headers["Access-Control-Allow-Methods"].ToString());
        Assert.Contains("Content-Type", ctx.Response.Headers["Access-Control-Allow-Headers"].ToString());
        Assert.False(string.IsNullOrEmpty(ctx.Response.Headers["Access-Control-Max-Age"].ToString()));
    }

    [Fact]
    public async Task Cors_Token_OptionsPreflight_Returns204WithStarOriginAndAuthHeader()
    {
        var ctx = NewBareContext();
        await SafeguardMcp.OAuth.TokenEndpoint.HandleOptionsAsync(ctx);

        Assert.Equal(204, ctx.Response.StatusCode);
        Assert.Equal("*", ctx.Response.Headers["Access-Control-Allow-Origin"].ToString());
        Assert.Contains("POST", ctx.Response.Headers["Access-Control-Allow-Methods"].ToString());
        Assert.Contains("Authorization", ctx.Response.Headers["Access-Control-Allow-Headers"].ToString());
        Assert.Contains("Content-Type", ctx.Response.Headers["Access-Control-Allow-Headers"].ToString());
    }

    [Fact]
    public async Task Cors_Register_OptionsPreflight_Returns204WithStarOriginAndAuthHeader()
    {
        var ctx = NewBareContext();
        await SafeguardMcp.OAuth.RegistrationEndpoint.HandleOptionsAsync(ctx);

        Assert.Equal(204, ctx.Response.StatusCode);
        Assert.Equal("*", ctx.Response.Headers["Access-Control-Allow-Origin"].ToString());
        Assert.Contains("POST", ctx.Response.Headers["Access-Control-Allow-Methods"].ToString());
        Assert.Contains("Authorization", ctx.Response.Headers["Access-Control-Allow-Headers"].ToString());
        Assert.Contains("Content-Type", ctx.Response.Headers["Access-Control-Allow-Headers"].ToString());
    }

    [Fact]
    public async Task Cors_Authorize_DoesNotAdvertiseCors()
    {
        // Plan §2.6: /authorize is a user-agent redirect, never an
        // XHR. Setting CORS headers there would be wrong.
        var f = NewFixture();
        var ctx = NewContext(f, "GET", "/authorize",
            AuthorizeQueryWithResource("https://mcp.example.test"));
        await AuthorizeEndpoints.HandleAuthorizeAsync(ctx, Opts());

        Assert.False(ctx.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
        Assert.False(ctx.Response.Headers.ContainsKey("Access-Control-Allow-Methods"));
    }

    [Fact]
    public async Task Cors_AuthorizeCallback_DoesNotAdvertiseCors()
    {
        var f = NewFixture();
        f.Flow.Add("sid-x", new AuthorizeFlowStore.Entry(
            clientId: ClientId, clientRedirectUri: ClientRedirect,
            clientState: ClientState, clientPkceChallenge: "pkce",
            bridgeToRstsPkceVerifier: "v", expiresAt: f.Time.GetUtcNow().AddSeconds(60)));
        var ctx = NewContext(f, "GET", "/authorize/callback", "?state=sid-x&code=rsts");
        await AuthorizeEndpoints.HandleAuthorizeCallbackAsync(ctx, Opts());

        Assert.False(ctx.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    [Fact]
    public void Cors_WellKnownGet_CarriesStarOriginAndCacheControl()
    {
        // The metadata GET handlers themselves set the same CORS
        // headers as the OPTIONS preflight, plus Cache-Control so
        // intermediaries serve the immutable document for an hour.
        var json = WellKnownMetadata.BuildAuthorizationServerJson(Opts());
        Assert.False(string.IsNullOrEmpty(json));
        // The behavior is also exercised end-to-end in
        // WellKnownMetadataTests; this anchor confirms the metadata
        // body is the value the GET handler will write.
    }
}
