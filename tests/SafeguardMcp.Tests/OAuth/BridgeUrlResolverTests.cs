#nullable disable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Verifies <see cref="BridgeUrlResolver"/> derives the bridge's
/// per-request URL set correctly from <see cref="BridgeOptions"/> +
/// the inbound <see cref="HttpContext"/>.
///
/// <list type="bullet">
///   <item>Override pinned: resolver returns <c>OverridePublicUrl</c>
///   regardless of the inbound Host.</item>
///   <item>No override: resolver builds the URL from
///   <c>Request.Scheme</c> + <c>Request.Host</c>.</item>
///   <item>PathBase respected: derived endpoint URLs include the
///   path prefix.</item>
///   <item>Scheme reflects what the framework set on
///   <c>Request.Scheme</c> (which in production equals
///   <c>X-Forwarded-Proto</c> after <c>UseForwardedHeaders</c>).</item>
///   <item>OverrideClientId pins the rSTS Realm independently of the
///   public URL inference.</item>
/// </list>
/// </summary>
public class BridgeUrlResolverTests
{
    private static BridgeOptions Options(string mcpPublicUrl = null, string rstsClientId = null)
    {
        var r = BridgeOptions.Parse(name => name switch
        {
            "MCP_PUBLIC_URL" => mcpPublicUrl,
            "RSTS_CLIENT_ID" => rstsClientId,
            "SAFEGUARD_HOST" => "appliance.example.test",
            _ => null,
        });
        Assert.True(r.IsActive);
        return r.Options;
    }

    private static HttpContext Request(string scheme, string host, string pathBase = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = scheme;
        ctx.Request.Host = new HostString(host);
        if (!string.IsNullOrEmpty(pathBase))
            ctx.Request.PathBase = pathBase;
        return ctx;
    }

    [Fact]
    public void Resolve_OverrideSet_ReturnsOverrideRegardlessOfHost()
    {
        var resolver = new BridgeUrlResolver(Options(mcpPublicUrl: "https://pinned.example.test"));
        var urls = resolver.Resolve(Request("http", "any-other-host:9000"));

        Assert.Equal("https://pinned.example.test", urls.PublicUrl);
        Assert.Equal("https://pinned.example.test/authorize", urls.AuthorizeEndpoint);
        Assert.Equal("https://pinned.example.test/authorize/callback", urls.AuthorizeCallbackEndpoint);
        Assert.Equal("https://pinned.example.test/token", urls.TokenEndpoint);
        Assert.Equal("https://pinned.example.test/register", urls.RegistrationEndpoint);
        Assert.Equal("https://pinned.example.test/.well-known/oauth-protected-resource",
            urls.ProtectedResourceMetadataUrl);
        Assert.Equal("https://pinned.example.test/.well-known/oauth-authorization-server",
            urls.AuthorizationServerMetadataUrl);
    }

    [Fact]
    public void Resolve_NoOverride_InfersFromSchemeAndHost()
    {
        var resolver = new BridgeUrlResolver(Options());
        var urls = resolver.Resolve(Request("https", "mcp.example.com"));

        Assert.Equal("https://mcp.example.com", urls.PublicUrl);
        Assert.Equal("https://mcp.example.com/authorize", urls.AuthorizeEndpoint);
        Assert.Equal("https://mcp.example.com/.well-known/oauth-authorization-server",
            urls.AuthorizationServerMetadataUrl);
    }

    [Fact]
    public void Resolve_NoOverride_RespectsPathBase()
    {
        var resolver = new BridgeUrlResolver(Options());
        var urls = resolver.Resolve(Request("https", "mcp.example.com", "/safeguard"));

        Assert.Equal("https://mcp.example.com/safeguard", urls.PublicUrl);
        Assert.Equal("https://mcp.example.com/safeguard/authorize", urls.AuthorizeEndpoint);
        Assert.Equal("https://mcp.example.com/safeguard/token", urls.TokenEndpoint);
    }

    [Fact]
    public void Resolve_NoOverride_HonorsRequestScheme()
    {
        // ASP.NET Core's UseForwardedHeaders middleware writes
        // X-Forwarded-Proto into Request.Scheme before the resolver
        // ever runs, so the unit-test-level contract we need is that
        // the resolver uses whatever Request.Scheme the middleware
        // already produced.
        var resolver = new BridgeUrlResolver(Options());

        var http = resolver.Resolve(Request("http", "mcp.example.com:8080"));
        Assert.Equal("http://mcp.example.com:8080", http.PublicUrl);

        var https = resolver.Resolve(Request("https", "mcp.example.com"));
        Assert.Equal("https://mcp.example.com", https.PublicUrl);
    }

    [Fact]
    public void Resolve_ClientIdDefaultsToInferredPublicUrl()
    {
        var resolver = new BridgeUrlResolver(Options());
        var urls = resolver.Resolve(Request("https", "mcp.example.com"));
        Assert.Equal("https://mcp.example.com", urls.ClientId);
    }

    [Fact]
    public void Resolve_OverrideClientId_PinsRealmIndependentOfPublicUrl()
    {
        // OverrideClientId without OverridePublicUrl: ClientId is
        // pinned, public URL is still inferred.
        var resolver = new BridgeUrlResolver(Options(rstsClientId: "https://rsts.example.test/realm"));
        var urls = resolver.Resolve(Request("https", "mcp.example.com"));

        Assert.Equal("https://mcp.example.com", urls.PublicUrl);
        Assert.Equal("https://rsts.example.test/realm", urls.ClientId);
    }
}
