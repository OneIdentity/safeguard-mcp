#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Verifies the bridge's RFC 7591 dynamic client registration
/// endpoint:
///
/// <list type="bullet">
///   <item>Issues a synthetic <c>mcp-client-&lt;uuid&gt;</c> client_id
///   on success (RFC 7591 §3.2.1) with no <c>client_secret</c> —
///   <c>client_secret_expires_at</c> = 0 — and persists the client's
///   redirect_uri allow-list in <see cref="ClientRegistry"/> with
///   the 30-day TTL.</item>
///   <item>Accepts both RFC 8252 §7.3 loopback redirects (any port,
///   <c>127.0.0.1</c> / <c>[::1]</c> / <c>localhost</c>) and
///   non-loopback redirects whose authority matches the request's
///   <c>Origin</c> header.</item>
///   <item><strong>Rejects</strong> any redirect_uri that is neither
///   loopback nor origin-matched — the open-redirect-rejection
///   acceptance gate. Specifically: non-loopback http anywhere,
///   https with a missing or mismatched Origin, multiple
///   redirect_uris where any single entry fails validation.</item>
///   <item>RFC 7591 §3.2.2 error shape on validation failure: HTTP
///   400 with <c>{"error": "invalid_redirect_uri" | "invalid_client_metadata"}</c>.</item>
///   <item>CORS preflight (<c>OPTIONS /register</c>) returns 204
///   with <c>*</c> origin.</item>
/// </list>
/// </summary>
public class RegistrationEndpointTests
{
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

    private static (HttpContext Ctx, ClientRegistry Reg, FakeTime Time) BuildContext(
        string body,
        string contentType = "application/json",
        string origin = null)
    {
        var time = new FakeTime();
        var registry = new ClientRegistry();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton(registry);

        var ctx = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        ctx.Request.Method = "POST";
        ctx.Request.Path = "/register";
        if (contentType != null)
            ctx.Request.ContentType = contentType;
        if (!string.IsNullOrEmpty(origin))
            ctx.Request.Headers["Origin"] = origin;
        if (body != null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            ctx.Request.Body = new MemoryStream(bytes);
            ctx.Request.ContentLength = bytes.Length;
        }
        ctx.Response.Body = new MemoryStream();
        return (ctx, registry, time);
    }

    private static JsonDocument ReadJsonResponse(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        using var reader = new StreamReader(ctx.Response.Body);
        var s = reader.ReadToEnd();
        return JsonDocument.Parse(s);
    }

    private static string Body(params string[] redirectUris)
    {
        var sb = new StringBuilder("{\"redirect_uris\":[");
        for (int i = 0; i < redirectUris.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(redirectUris[i]).Append('"');
        }
        sb.Append("]}");
        return sb.ToString();
    }

    // ---- happy paths -----------------------------------------------

    [Fact]
    public async Task Register_LoopbackIPv4_Accepts_AndPersists()
    {
        var (ctx, reg, time) = BuildContext(Body("http://127.0.0.1:8765/cb"));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status201Created, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        var root = json.RootElement;

        var clientId = root.GetProperty("client_id").GetString();
        Assert.StartsWith("mcp-client-", clientId);
        Assert.Equal(0, root.GetProperty("client_secret_expires_at").GetInt32());
        Assert.Equal(time.GetUtcNow().ToUnixTimeSeconds(), root.GetProperty("client_id_issued_at").GetInt64());
        Assert.Equal("none", root.GetProperty("token_endpoint_auth_method").GetString());

        var uris = root.GetProperty("redirect_uris");
        Assert.Equal(1, uris.GetArrayLength());
        Assert.Equal("http://127.0.0.1:8765/cb", uris[0].GetString());

        // 30-day TTL: registry must accept the redirect_uri now and
        // 29 days from now, but reject 31 days out.
        Assert.True(reg.IsValidRedirectUri(clientId, "http://127.0.0.1:8765/cb", time.GetUtcNow()));
        Assert.True(reg.IsValidRedirectUri(clientId, "http://127.0.0.1:8765/cb", time.GetUtcNow().AddDays(29)));
        Assert.False(reg.IsValidRedirectUri(clientId, "http://127.0.0.1:8765/cb", time.GetUtcNow().AddDays(31)));
    }

    [Theory]
    [InlineData("http://localhost:8080/cb")]
    [InlineData("http://127.0.0.1/cb")]
    [InlineData("http://[::1]:1234/cb")]
    [InlineData("http://localhost/cb")]
    public async Task Register_LoopbackVariants_AllAccepted(string redirectUri)
    {
        var (ctx, _, _) = BuildContext(Body(redirectUri));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status201Created, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Register_HttpsWithMatchingOrigin_Accepted()
    {
        var (ctx, _, _) = BuildContext(
            Body("https://app.example.test/cb"),
            origin: "https://app.example.test");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status201Created, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Register_OriginMatch_IsCaseInsensitiveOnHost()
    {
        var (ctx, _, _) = BuildContext(
            Body("https://APP.example.test/cb"),
            origin: "https://app.example.test");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status201Created, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Register_MultipleLoopbackUris_AllAccepted_AllPersisted()
    {
        var (ctx, reg, time) = BuildContext(
            Body("http://127.0.0.1:8000/cb", "http://localhost:9000/oauth"));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status201Created, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        var clientId = json.RootElement.GetProperty("client_id").GetString();
        Assert.True(reg.IsValidRedirectUri(clientId, "http://127.0.0.1:8000/cb", time.GetUtcNow()));
        Assert.True(reg.IsValidRedirectUri(clientId, "http://localhost:9000/oauth", time.GetUtcNow()));
    }

    [Fact]
    public async Task Register_EchoesClientNameWhenSupplied()
    {
        var body = "{\"redirect_uris\":[\"http://127.0.0.1:8000/cb\"],\"client_name\":\"My MCP Client\"}";
        var (ctx, _, _) = BuildContext(body);
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status201Created, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        Assert.Equal("My MCP Client", json.RootElement.GetProperty("client_name").GetString());
    }

    // ---- open-redirect rejection -----------------------------------

    [Fact]
    public async Task Register_NonLoopbackHttp_RejectedAsInvalidRedirectUri()
    {
        // http on a non-loopback host must be rejected — that's the
        // classic open-redirect vector.
        var (ctx, reg, _) = BuildContext(Body("http://attacker.example.com/cb"));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        Assert.Equal("invalid_redirect_uri", json.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, reg.Count);
    }

    [Fact]
    public async Task Register_HttpsWithoutOrigin_Rejected()
    {
        // An https redirect_uri with no Origin to compare against
        // could redirect anywhere; reject.
        var (ctx, reg, _) = BuildContext(Body("https://attacker.example.com/cb"));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        Assert.Equal("invalid_redirect_uri", json.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, reg.Count);
    }

    [Fact]
    public async Task Register_HttpsWithMismatchedOrigin_Rejected()
    {
        // The canonical open-redirect attack — claim to be an
        // extension of trusted-origin.com but redirect to attacker.com.
        var (ctx, reg, _) = BuildContext(
            Body("https://attacker.example.com/cb"),
            origin: "https://trusted.example.com");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        Assert.Equal("invalid_redirect_uri", json.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, reg.Count);
    }

    [Fact]
    public async Task Register_HttpsWithMismatchedPort_Rejected()
    {
        // The whole authority must match; same host different port
        // is a different origin per RFC 6454.
        var (ctx, reg, _) = BuildContext(
            Body("https://app.example.test:8443/cb"),
            origin: "https://app.example.test");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        Assert.Equal(0, reg.Count);
    }

    [Fact]
    public async Task Register_HttpsWithMismatchedScheme_Rejected()
    {
        var (ctx, reg, _) = BuildContext(
            Body("https://app.example.test/cb"),
            origin: "http://app.example.test");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        Assert.Equal(0, reg.Count);
    }

    [Fact]
    public async Task Register_AnyBadEntryPoisonsTheBatch_NoneRegistered()
    {
        // Even if the first redirect_uri is fine, a single bad
        // entry must reject the whole registration so we never
        // half-register a client whose allow-list mixes trusted +
        // attacker redirect targets.
        var (ctx, reg, _) = BuildContext(
            Body("http://127.0.0.1:8000/cb", "http://attacker.example.com/cb"));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        Assert.Equal("invalid_redirect_uri", json.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, reg.Count);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("not-a-uri")]
    [InlineData("")]
    public async Task Register_NonHttpScheme_OrMalformed_Rejected(string redirectUri)
    {
        var (ctx, reg, _) = BuildContext(Body(redirectUri));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        Assert.Equal(0, reg.Count);
    }

    [Fact]
    public async Task Register_RedirectUriWithFragment_Rejected()
    {
        // RFC 6749 §3.1.2 forbids fragments on redirect_uri.
        var (ctx, reg, _) = BuildContext(Body("http://127.0.0.1:8000/cb#x"));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        Assert.Equal(0, reg.Count);
    }

    // ---- malformed-request handling --------------------------------

    [Fact]
    public async Task Register_MissingRedirectUris_RejectedAsInvalidRedirectUri()
    {
        var (ctx, reg, _) = BuildContext("{}");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        Assert.Equal("invalid_redirect_uri", json.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, reg.Count);
    }

    [Fact]
    public async Task Register_EmptyRedirectUrisArray_Rejected()
    {
        var (ctx, _, _) = BuildContext("{\"redirect_uris\":[]}");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Register_NonArrayRedirectUris_Rejected()
    {
        var (ctx, _, _) = BuildContext("{\"redirect_uris\":\"http://127.0.0.1:8000/cb\"}");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Register_NonStringRedirectUriEntry_Rejected()
    {
        var (ctx, _, _) = BuildContext("{\"redirect_uris\":[42]}");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Register_MalformedJson_Rejected()
    {
        var (ctx, _, _) = BuildContext("{not json");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        Assert.Equal("invalid_client_metadata", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Register_NonObjectRoot_Rejected()
    {
        var (ctx, _, _) = BuildContext("[]");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Register_EmptyBody_Rejected()
    {
        var (ctx, _, _) = BuildContext("");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Register_WrongContentType_Rejected()
    {
        var (ctx, _, _) = BuildContext(
            Body("http://127.0.0.1:8000/cb"),
            contentType: "application/x-www-form-urlencoded");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
        using var json = ReadJsonResponse(ctx);
        Assert.Equal("invalid_client_metadata", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Register_MissingContentType_Rejected()
    {
        var (ctx, _, _) = BuildContext(
            Body("http://127.0.0.1:8000/cb"),
            contentType: null);
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status400BadRequest, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Register_AcceptsContentTypeWithCharset()
    {
        var (ctx, _, _) = BuildContext(
            Body("http://127.0.0.1:8000/cb"),
            contentType: "application/json; charset=utf-8");
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        Assert.Equal(StatusCodes.Status201Created, ctx.Response.StatusCode);
    }

    // ---- response shape --------------------------------------------

    [Fact]
    public async Task Register_Response_AdvertisesPublicClientShape()
    {
        var (ctx, _, _) = BuildContext(Body("http://127.0.0.1:8000/cb"));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());
        using var json = ReadJsonResponse(ctx);
        var root = json.RootElement;

        // grant_types ⊇ ["authorization_code"], response_types ⊇ ["code"].
        var grants = root.GetProperty("grant_types");
        Assert.Equal(JsonValueKind.Array, grants.ValueKind);
        Assert.Contains(grants.EnumerateArray(), e => e.GetString() == "authorization_code");

        var responses = root.GetProperty("response_types");
        Assert.Contains(responses.EnumerateArray(), e => e.GetString() == "code");

        // No client_secret on a public client.
        Assert.False(root.TryGetProperty("client_secret", out _));
    }

    [Fact]
    public async Task Register_Response_HasNoStoreCacheControl()
    {
        var (ctx, _, _) = BuildContext(Body("http://127.0.0.1:8000/cb"));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal("no-store", ctx.Response.Headers.CacheControl.ToString());
        Assert.Equal("no-cache", ctx.Response.Headers.Pragma.ToString());
    }

    [Fact]
    public async Task Register_Response_HasCorsAllowAll()
    {
        var (ctx, _, _) = BuildContext(Body("http://127.0.0.1:8000/cb"));
        await RegistrationEndpoint.HandleRegisterAsync(ctx, Opts());

        Assert.Equal("*", ctx.Response.Headers["Access-Control-Allow-Origin"].ToString());
        Assert.Contains("POST", ctx.Response.Headers["Access-Control-Allow-Methods"].ToString());
    }

    // ---- IsAcceptableRedirectUri unit-level coverage ---------------

    [Theory]
    [InlineData("http://127.0.0.1/cb", null, true)]
    [InlineData("http://127.0.0.1:65000/cb", null, true)]
    [InlineData("http://localhost:8080/cb", null, true)]
    [InlineData("http://[::1]/cb", null, true)]
    [InlineData("https://example.com/cb", "https://example.com", true)]
    [InlineData("https://example.com:443/cb", "https://example.com", true)]
    [InlineData("https://example.com/cb", "https://example.com:8443", false)]
    [InlineData("https://example.com/cb", null, false)]
    [InlineData("http://example.com/cb", null, false)]
    [InlineData("http://example.com/cb", "https://example.com", false)]
    [InlineData("https://example.com/cb", "http://example.com", false)]
    [InlineData("ftp://example.com/cb", null, false)]
    [InlineData("not-a-uri", null, false)]
    public void IsAcceptableRedirectUri_TruthTable(string redirectUri, string origin, bool expected)
    {
        var actual = RegistrationEndpoint.IsAcceptableRedirectUri(redirectUri, origin, out _);
        Assert.Equal(expected, actual);
    }
}
