#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Verifies <c>POST /token</c> behavior and pins two acceptance gates:
///
/// <list type="bullet">
///   <item><strong>Successful exchange</strong> — <c>POST /token</c>
///   exchanges a stored bridge_auth_code for a Safeguard user token
///   via the SDK helpers (mocked here), returns RFC 6749 §5.1 JSON,
///   and never stores token material anywhere outside the response
///   body.</item>
///   <item><strong>PKCE binding</strong>
///   (<see cref="Token_TamperedCodeVerifier_RejectsAsInvalidGrant_NoUpstreamCall"/>)
///   — a manipulated <c>code_verifier</c> rejects with
///   <c>invalid_grant</c> <em>and the upstream exchanger is never
///   called</em>, because PKCE validation happens before any
///   network I/O.</item>
///   <item><strong>Single-use replay gate</strong>
///   (<see cref="Token_AuthCodeReplay_SecondCallRejected"/>) — a
///   second <c>POST /token</c> with a previously-redeemed
///   <c>code</c> rejects with <c>invalid_grant</c>; the upstream
///   exchanger is called exactly once across both attempts.</item>
/// </list>
/// </summary>
public class TokenEndpointTests
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

    /// <summary>
    /// Builds a Safeguard-shaped JWT (header.payload.signature) with
    /// a synthetic <c>exp</c> claim. Signature bytes are arbitrary —
    /// the bridge never verifies them; the appliance is the
    /// authority on signature.
    /// </summary>
    private static string MakeJwt(long expUnix)
    {
        static string Url(string s) =>
            System.Buffers.Text.Base64Url.EncodeToString(Encoding.UTF8.GetBytes(s));
        var header = Url("{\"alg\":\"RS256\",\"typ\":\"JWT\"}");
        var payload = Url("{\"sub\":\"alice\",\"exp\":" + expUnix + "}");
        var sig = System.Buffers.Text.Base64Url.EncodeToString(new byte[] { 1, 2, 3, 4 });
        return header + "." + payload + "." + sig;
    }

    private sealed class FakeRstsTokenExchanger : IRstsTokenExchanger
    {
        public string LoginResponseJsonToReturn { get; set; }
        public Exception ThrowOnAuthorizationCode { get; set; }
        public Exception ThrowOnLoginResponse { get; set; }
        public int AuthorizationCodeCalls;
        public int LoginResponseCalls;
        public string LastRstsAuthCode;
        public string LastPkceVerifier;
        public string LastRedirectUri;

        public Task<SecureString> ExchangeAuthorizationCodeAsync(
            string appliance, string rstsAuthorizationCode, string pkceVerifier,
            string redirectUri, bool ignoreSsl, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref AuthorizationCodeCalls);
            LastRstsAuthCode = rstsAuthorizationCode;
            LastPkceVerifier = pkceVerifier;
            LastRedirectUri = redirectUri;
            if (ThrowOnAuthorizationCode != null) throw ThrowOnAuthorizationCode;
            var s = new SecureString();
            foreach (var c in "fake-rsts-access-token") s.AppendChar(c);
            s.MakeReadOnly();
            return Task.FromResult(s);
        }

        public Task<string> ExchangeRstsTokenForLoginResponseAsync(
            string appliance, SecureString rstsAccessToken, bool ignoreSsl,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref LoginResponseCalls);
            if (ThrowOnLoginResponse != null) throw ThrowOnLoginResponse;
            return Task.FromResult(LoginResponseJsonToReturn);
        }
    }

    private sealed class Fixture
    {
        public HttpContext Ctx;
        public AuthCodeStore Codes;
        public FakeRstsTokenExchanger Exchanger;
        public FakeTime Time;
    }

    /// <summary>
    /// Seeds an <see cref="AuthCodeStore"/> with one redeemable
    /// entry whose PKCE challenge matches a known verifier, builds
    /// a POST /token HttpContext with the requested form body, and
    /// wires a fake exchanger that returns a synthesized Safeguard
    /// LoginResponse with a JWT user token expiring 1 hour from
    /// "now".
    /// </summary>
    private static Fixture Seed(string bridgeAuthCode, string codeVerifier, string formBody)
    {
        var time = new FakeTime();
        var codes = new AuthCodeStore(time);
        var challenge = PkceUtilities.ComputeS256Challenge(codeVerifier);
        codes.Add(bridgeAuthCode, new AuthCodeStore.Entry(
            rstsAuthCode: "rsts-auth-code-xyz",
            bridgeToRstsPkceVerifier: "bridge-rsts-verifier",
            clientPkceChallenge: challenge,
            clientRedirectUri: ClientRedirect,
            clientId: ClientId,
            expiresAt: time.GetUtcNow().AddSeconds(60)));

        var fakeJwt = MakeJwt(time.GetUtcNow().AddHours(1).ToUnixTimeSeconds());
        var exchanger = new FakeRstsTokenExchanger
        {
            LoginResponseJsonToReturn = "{\"Status\":\"Success\",\"UserToken\":\"" + fakeJwt + "\"}",
        };

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton(codes);
        services.AddSingleton<IRstsTokenExchanger>(exchanger);

        var ctx = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        ctx.Request.Method = "POST";
        ctx.Request.Path = "/token";
        ctx.Request.ContentType = "application/x-www-form-urlencoded";
        var bytes = Encoding.UTF8.GetBytes(formBody);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        ctx.Response.Body = new MemoryStream();

        return new Fixture { Ctx = ctx, Codes = codes, Exchanger = exchanger, Time = time };
    }

    private static string ReadBody(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        using var r = new StreamReader(ctx.Response.Body);
        return r.ReadToEnd();
    }

    private static string Form(string code, string verifier)
    {
        return "grant_type=authorization_code"
            + "&code=" + Uri.EscapeDataString(code)
            + "&code_verifier=" + Uri.EscapeDataString(verifier)
            + "&redirect_uri=" + Uri.EscapeDataString(ClientRedirect)
            + "&client_id=" + Uri.EscapeDataString(ClientId);
    }

    // ---- happy path -----------------------------------------------

    [Fact]
    public async Task Token_HappyPath_Returns200WithBearerAndExpiresIn()
    {
        var f = Seed("auth-code-1", "verifier-abc", Form("auth-code-1", "verifier-abc"));

        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());

        Assert.Equal(200, f.Ctx.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", f.Ctx.Response.ContentType);
        Assert.Equal("no-store", f.Ctx.Response.Headers.CacheControl.ToString());
        Assert.Equal("no-cache", f.Ctx.Response.Headers.Pragma.ToString());

        var body = ReadBody(f.Ctx);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal("Bearer", root.GetProperty("token_type").GetString());
        var accessToken = root.GetProperty("access_token").GetString();
        Assert.False(string.IsNullOrEmpty(accessToken));
        // Token came back as the JWT minted by the fake LoginResponse.
        Assert.StartsWith("eyJ", accessToken);
        var expiresIn = root.GetProperty("expires_in").GetInt64();
        // ~3600s from the synthesized 1h-future exp claim.
        Assert.InRange(expiresIn, 3500L, 3600L);

        // Single-use: store empty after a successful redemption.
        Assert.Equal(0, f.Codes.Count);

        // Upstream was called with the *stored* rSTS code/verifier and
        // the bridge's callback URL — never the client-facing values.
        Assert.Equal(1, f.Exchanger.AuthorizationCodeCalls);
        Assert.Equal(1, f.Exchanger.LoginResponseCalls);
        Assert.Equal("rsts-auth-code-xyz", f.Exchanger.LastRstsAuthCode);
        Assert.Equal("bridge-rsts-verifier", f.Exchanger.LastPkceVerifier);
        Assert.Equal("https://mcp.example.test/authorize/callback", f.Exchanger.LastRedirectUri);
    }

    // ---- 2.B: PKCE-binding tamper test ----------------------------

    [Fact]
    public async Task Token_TamperedCodeVerifier_RejectsAsInvalidGrant_NoUpstreamCall()
    {
        var f = Seed("auth-code-1", "verifier-abc",
            Form("auth-code-1", "verifier-WRONG"));

        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());

        Assert.Equal(400, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());

        // Acceptance: PKCE failure happens before any upstream call.
        Assert.Equal(0, f.Exchanger.AuthorizationCodeCalls);
        Assert.Equal(0, f.Exchanger.LoginResponseCalls);

        // A failed PKCE attempt still consumes the single-use code —
        // a follow-up replay with the correct verifier would also
        // fail (standard OAuth single-use semantics).
        Assert.Equal(0, f.Codes.Count);
    }

    // ---- 2.C: Auth-code replay rejection test ---------------------

    [Fact]
    public async Task Token_AuthCodeReplay_SecondCallRejected()
    {
        // First call: happy path consumes the auth code.
        var f1 = Seed("auth-code-1", "verifier-abc", Form("auth-code-1", "verifier-abc"));
        await TokenEndpoint.HandleTokenAsync(f1.Ctx, Opts());
        Assert.Equal(200, f1.Ctx.Response.StatusCode);

        // Reuse the same DI graph for the replay so the same
        // AuthCodeStore and exchanger see both requests.
        var ctx2 = new DefaultHttpContext { RequestServices = f1.Ctx.RequestServices };
        ctx2.Request.Method = "POST";
        ctx2.Request.Path = "/token";
        ctx2.Request.ContentType = "application/x-www-form-urlencoded";
        var body = Encoding.UTF8.GetBytes(Form("auth-code-1", "verifier-abc"));
        ctx2.Request.Body = new MemoryStream(body);
        ctx2.Request.ContentLength = body.Length;
        ctx2.Response.Body = new MemoryStream();

        await TokenEndpoint.HandleTokenAsync(ctx2, Opts());

        Assert.Equal(400, ctx2.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(ctx2));
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());

        // Exchanger called exactly once across both attempts.
        Assert.Equal(1, f1.Exchanger.AuthorizationCodeCalls);
        Assert.Equal(1, f1.Exchanger.LoginResponseCalls);
    }

    // ---- form / request validation --------------------------------

    [Fact]
    public async Task Token_WrongContentType_400InvalidRequest()
    {
        var f = Seed("auth-code-1", "v", Form("auth-code-1", "v"));
        f.Ctx.Request.ContentType = "application/json";
        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());
        Assert.Equal(400, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("invalid_request", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Token_MissingGrantType_400InvalidRequest()
    {
        var f = Seed("auth-code-1", "v",
            "code=auth-code-1&code_verifier=v&redirect_uri="
            + Uri.EscapeDataString(ClientRedirect) + "&client_id=" + ClientId);
        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());
        Assert.Equal(400, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("invalid_request", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Token_WrongGrantType_400UnsupportedGrantType()
    {
        var f = Seed("auth-code-1", "v",
            "grant_type=client_credentials&code=auth-code-1&code_verifier=v&redirect_uri="
            + Uri.EscapeDataString(ClientRedirect) + "&client_id=" + ClientId);
        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());
        Assert.Equal(400, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("unsupported_grant_type", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Token_UnknownCode_400InvalidGrant()
    {
        var f = Seed("auth-code-1", "v", Form("auth-code-DIFFERENT", "v"));
        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());
        Assert.Equal(400, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, f.Exchanger.AuthorizationCodeCalls);
    }

    [Fact]
    public async Task Token_RedirectUriMismatch_400InvalidGrant()
    {
        var f = Seed("auth-code-1", "v",
            "grant_type=authorization_code&code=auth-code-1&code_verifier=v"
            + "&redirect_uri=" + Uri.EscapeDataString("http://evil.example.com/cb")
            + "&client_id=" + ClientId);
        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());
        Assert.Equal(400, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, f.Exchanger.AuthorizationCodeCalls);
        // Mismatch detected after consume — single-use semantics still hold.
        Assert.Equal(0, f.Codes.Count);
    }

    [Fact]
    public async Task Token_ClientIdMismatch_400InvalidGrant()
    {
        var f = Seed("auth-code-1", "v",
            "grant_type=authorization_code&code=auth-code-1&code_verifier=v"
            + "&redirect_uri=" + Uri.EscapeDataString(ClientRedirect)
            + "&client_id=mcp-client-OTHER");
        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());
        Assert.Equal(400, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, f.Exchanger.AuthorizationCodeCalls);
    }

    // ---- upstream failure mapping ---------------------------------

    [Fact]
    public async Task Token_LoginResponseStatusNotSuccess_400InvalidGrant()
    {
        var f = Seed("auth-code-1", "v", Form("auth-code-1", "v"));
        f.Exchanger.LoginResponseJsonToReturn =
            "{\"Status\":\"Failure\",\"Message\":\"Account is disabled.\"}";

        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());

        Assert.Equal(400, f.Ctx.Response.StatusCode);
        var body = ReadBody(f.Ctx);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
        // Appliance Message must not be reflected to anonymous callers.
        Assert.DoesNotContain("Account is disabled", body);
    }

    [Fact]
    public async Task Token_LoginResponseMissingUserToken_502ServerError()
    {
        var f = Seed("auth-code-1", "v", Form("auth-code-1", "v"));
        f.Exchanger.LoginResponseJsonToReturn = "{\"Status\":\"Success\"}";

        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());

        Assert.Equal(502, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("server_error", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Token_RstsExchangeThrowsSafeguardDotNet400_InvalidGrant()
    {
        var f = Seed("auth-code-1", "v", Form("auth-code-1", "v"));
        f.Exchanger.ThrowOnAuthorizationCode = new SafeguardDotNetException(
            "rSTS rejected the code.", HttpStatusCode.BadRequest, "invalid_grant");

        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());

        Assert.Equal(400, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
        // Stage-2 not attempted after Stage-1 failure.
        Assert.Equal(0, f.Exchanger.LoginResponseCalls);
    }

    [Fact]
    public async Task Token_RstsExchangeThrowsSafeguardDotNet500_ServerError()
    {
        var f = Seed("auth-code-1", "v", Form("auth-code-1", "v"));
        f.Exchanger.ThrowOnAuthorizationCode = new SafeguardDotNetException(
            "rSTS down.", HttpStatusCode.InternalServerError, "boom");

        await TokenEndpoint.HandleTokenAsync(f.Ctx, Opts());

        Assert.Equal(502, f.Ctx.Response.StatusCode);
        using var doc = JsonDocument.Parse(ReadBody(f.Ctx));
        Assert.Equal("server_error", doc.RootElement.GetProperty("error").GetString());
    }

    // ---- OPTIONS preflight ----------------------------------------

    [Fact]
    public void TokenEndpoint_DerivesPositiveExpiresIn_FromJwtExp()
    {
        var now = new DateTimeOffset(2026, 6, 5, 0, 0, 0, TimeSpan.Zero);
        var jwt = MakeJwt(now.AddMinutes(30).ToUnixTimeSeconds());
        Assert.InRange(TokenEndpoint.TryDeriveExpiresInSeconds(jwt, now), 1700L, 1800L);
    }

    [Fact]
    public void TokenEndpoint_ExpiresInClampsToZero_ForAlreadyExpiredJwt()
    {
        var now = new DateTimeOffset(2026, 6, 5, 0, 0, 0, TimeSpan.Zero);
        var jwt = MakeJwt(now.AddMinutes(-5).ToUnixTimeSeconds());
        Assert.Equal(0L, TokenEndpoint.TryDeriveExpiresInSeconds(jwt, now));
    }

    [Fact]
    public void TokenEndpoint_ExpiresInZero_ForNonJwt()
    {
        Assert.Equal(0L, TokenEndpoint.TryDeriveExpiresInSeconds("not-a-jwt", DateTimeOffset.UtcNow));
        Assert.Equal(0L, TokenEndpoint.TryDeriveExpiresInSeconds("", DateTimeOffset.UtcNow));
        Assert.Equal(0L, TokenEndpoint.TryDeriveExpiresInSeconds("a.b.c", DateTimeOffset.UtcNow));
    }
}
