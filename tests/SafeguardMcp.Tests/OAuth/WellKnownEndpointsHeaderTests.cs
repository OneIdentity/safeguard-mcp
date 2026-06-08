#nullable disable

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Locks down the well-known response headers that exist for cache
/// correctness in the inferred-URL world: <c>Cache-Control</c> bounds
/// staleness when DNS / ingress changes, and <c>Vary: Host</c> keeps a
/// shared cache from serving one host's metadata to another now that
/// <see cref="BridgeUrlResolver"/> can build per-host documents.
/// </summary>
public class WellKnownEndpointsHeaderTests
{
    private static BridgeOptions Opts() =>
        BridgeOptions.Parse(name => name switch
        {
            "SAFEGUARD_HOST" => "appliance.example.test",
            _ => null,
        }).Options;

    private static HttpContext NewCtx(string host = "mcp.example.test")
    {
        var services = new ServiceCollection();
        services.AddSingleton(Opts());
        services.AddSingleton<BridgeUrlResolver>();

        var ctx = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString(host);
        ctx.Response.Body = new System.IO.MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task WriteMetadata_SetsCacheControl5Minutes()
    {
        var ctx = NewCtx();
        await WellKnownEndpoints.WriteMetadataAsync(ctx,
            urls => WellKnownMetadata.BuildAuthorizationServerJson(urls));

        Assert.Equal("public, max-age=300", ctx.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task WriteMetadata_SetsVaryHost()
    {
        // Without Vary: Host, a CDN/proxy fronting two hostnames could
        // serve mcp-a.example.com's metadata document to mcp-b clients
        // — the resolver bakes Host into the document body.
        var ctx = NewCtx();
        await WellKnownEndpoints.WriteMetadataAsync(ctx,
            urls => WellKnownMetadata.BuildAuthorizationServerJson(urls));

        Assert.Equal("Host", ctx.Response.Headers["Vary"].ToString());
    }

    [Fact]
    public async Task WriteMetadata_SetsCorsHeaders()
    {
        // RFC 8414 §3.2: metadata is publicly readable. The MCP client
        // (browser-based or otherwise) must be able to fetch it
        // cross-origin to discover the bridge.
        var ctx = NewCtx();
        await WellKnownEndpoints.WriteMetadataAsync(ctx,
            urls => WellKnownMetadata.BuildProtectedResourceJson(urls));

        Assert.Equal("*", ctx.Response.Headers["Access-Control-Allow-Origin"].ToString());
        Assert.Equal("GET, OPTIONS", ctx.Response.Headers["Access-Control-Allow-Methods"].ToString());
        Assert.Equal("300", ctx.Response.Headers["Access-Control-Max-Age"].ToString());
    }

    [Fact]
    public async Task WriteMetadata_BodyReflectsRequestHost()
    {
        // The whole reason Vary: Host exists — confirm the rendered
        // document differs by Host so the cache directive is meaningful.
        var ctxA = NewCtx("mcp-a.example.test");
        await WellKnownEndpoints.WriteMetadataAsync(ctxA,
            urls => WellKnownMetadata.BuildAuthorizationServerJson(urls));
        var bodyA = ReadBody(ctxA);

        var ctxB = NewCtx("mcp-b.example.test");
        await WellKnownEndpoints.WriteMetadataAsync(ctxB,
            urls => WellKnownMetadata.BuildAuthorizationServerJson(urls));
        var bodyB = ReadBody(ctxB);

        Assert.Contains("mcp-a.example.test", bodyA);
        Assert.Contains("mcp-b.example.test", bodyB);
        Assert.NotEqual(bodyA, bodyB);
    }

    private static string ReadBody(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        using var sr = new System.IO.StreamReader(ctx.Response.Body);
        return sr.ReadToEnd();
    }
}
