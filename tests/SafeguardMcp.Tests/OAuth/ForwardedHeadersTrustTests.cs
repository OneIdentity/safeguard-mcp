#nullable disable

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Locks down the trust boundary for <c>X-Forwarded-Host</c> /
/// <c>X-Forwarded-Proto</c>: the bridge derives its public URL from
/// <c>Request.Scheme</c> + <c>Request.Host</c> after
/// <c>UseForwardedHeaders</c> has run, so the wiring of
/// <see cref="ForwardedHeadersOptions.KnownIPNetworks"/> is what
/// keeps a malicious client from spoofing the published
/// authorization-server URL.
///
/// <para>
/// These tests instantiate <see cref="ForwardedHeadersMiddleware"/>
/// directly with the options shape Program.cs builds in production
/// (loopback + RFC1918 default + extra <c>BRIDGE_TRUSTED_PROXIES</c>),
/// then assert that headers from a trusted RemoteIp are honored and
/// headers from an untrusted RemoteIp are dropped.
/// </para>
/// </summary>
public class ForwardedHeadersTrustTests
{
    private static ForwardedHeadersOptions ProductionOptions(params string[] extraCidrs)
    {
        var opts = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto
                             | ForwardedHeaders.XForwardedHost,
            ForwardLimit = 2,
        };
        opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
        opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
        opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));
        foreach (var c in extraCidrs)
            opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(c));
        return opts;
    }

    private static async Task<HttpContext> RunMiddlewareAsync(
        ForwardedHeadersOptions opts,
        IPAddress remoteIp,
        string xForwardedHost,
        string xForwardedProto)
    {
        var middleware = new ForwardedHeadersMiddleware(
            next: _ => Task.CompletedTask,
            loggerFactory: NullLoggerFactory.Instance,
            options: Options.Create(opts));

        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = remoteIp;
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("internal-pod-ip:8080");
        if (xForwardedHost != null)
            ctx.Request.Headers["X-Forwarded-Host"] = xForwardedHost;
        if (xForwardedProto != null)
            ctx.Request.Headers["X-Forwarded-Proto"] = xForwardedProto;

        await middleware.Invoke(ctx);
        return ctx;
    }

    [Fact]
    public async Task TrustedRfc1918Proxy_HonorsForwardedHeaders()
    {
        var ctx = await RunMiddlewareAsync(
            ProductionOptions(),
            remoteIp: IPAddress.Parse("10.0.0.5"),
            xForwardedHost: "mcp.example.com",
            xForwardedProto: "https");

        Assert.Equal("https", ctx.Request.Scheme);
        Assert.Equal("mcp.example.com", ctx.Request.Host.Value);
    }

    [Fact]
    public async Task TrustedLoopbackProxy_HonorsForwardedHeaders()
    {
        // Loopback is always trusted (KnownProxies includes IPv4/IPv6
        // loopback by default). Validates the docker-run-on-host case.
        var ctx = await RunMiddlewareAsync(
            ProductionOptions(),
            remoteIp: IPAddress.Loopback,
            xForwardedHost: "mcp.example.com",
            xForwardedProto: "https");

        Assert.Equal("https", ctx.Request.Scheme);
        Assert.Equal("mcp.example.com", ctx.Request.Host.Value);
    }

    [Fact]
    public async Task UntrustedPublicProxy_IgnoresForwardedHeaders()
    {
        // Carrier-grade NAT (100.64.0.0/10) is not in the default
        // trust list. A request arriving from outside the trust list
        // must not let X-Forwarded-Host change the published URL.
        var ctx = await RunMiddlewareAsync(
            ProductionOptions(),
            remoteIp: IPAddress.Parse("100.64.5.5"),
            xForwardedHost: "evil.example.com",
            xForwardedProto: "https");

        Assert.Equal("http", ctx.Request.Scheme);
        Assert.Equal("internal-pod-ip:8080", ctx.Request.Host.Value);
    }

    [Fact]
    public async Task BridgeTrustedProxiesExtension_HonorsForwardedHeaders()
    {
        // Operator opted in to a CGN-style range via
        // BRIDGE_TRUSTED_PROXIES; that should let the X-Forwarded-*
        // headers from that range through.
        var ctx = await RunMiddlewareAsync(
            ProductionOptions("100.64.0.0/10"),
            remoteIp: IPAddress.Parse("100.64.5.5"),
            xForwardedHost: "mcp.example.com",
            xForwardedProto: "https");

        Assert.Equal("https", ctx.Request.Scheme);
        Assert.Equal("mcp.example.com", ctx.Request.Host.Value);
    }
}
