using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SafeguardMcp.OAuth;

/// <summary>
/// Maps the bridge's RFC 9728 / RFC 8414 well-known metadata endpoints
/// onto an <see cref="IEndpointRouteBuilder"/>. Both endpoints:
/// <list type="bullet">
///   <item>Return <c>application/json</c>.</item>
///   <item>Set <c>Access-Control-Allow-Origin: *</c> — RFC 8414 §3.2
///   expects metadata to be publicly readable.</item>
///   <item>Implement an <c>OPTIONS</c> preflight that mirrors the
///   ACAO/ACAM headers and returns 204.</item>
///   <item>Send <c>Cache-Control: public, max-age=300</c> with
///   <c>Vary: Host</c>. The 5-minute window keeps the document fresh
///   if DNS or ingress hostnames change; <c>Vary: Host</c> keeps a
///   shared cache (CDN, proxy) from serving one host's metadata to
///   another, which matters now that <see cref="BridgeUrlResolver"/>
///   may infer the public URL from the inbound <c>Host</c> header.</item>
/// </list>
/// Safe to call only after <see cref="BridgeOptions.Parse"/> returns
/// an active result and <see cref="BridgeUrlResolver"/> has been
/// registered in DI.
/// </summary>
internal static class WellKnownEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));

        MapMetadata(endpoints, WellKnownMetadata.ProtectedResourcePath,
            urls => WellKnownMetadata.BuildProtectedResourceJson(urls));
        MapMetadata(endpoints, WellKnownMetadata.AuthorizationServerPath,
            urls => WellKnownMetadata.BuildAuthorizationServerJson(urls));
    }

    private static void MapMetadata(IEndpointRouteBuilder endpoints, string path, Func<BridgeRequestUrls, string> render)
    {
        endpoints.MapGet(path, ctx => WriteMetadataAsync(ctx, render));
        endpoints.MapMethods(path, new[] { "OPTIONS" }, HandleOptionsAsync);
    }

    internal static Task HandleOptionsAsync(HttpContext ctx)
    {
        SetMetadataCorsHeaders(ctx);
        ctx.Response.StatusCode = StatusCodes.Status204NoContent;
        return Task.CompletedTask;
    }

    private static Task WriteMetadataAsync(HttpContext ctx, Func<BridgeRequestUrls, string> render)
    {
        SetMetadataCorsHeaders(ctx);
        ctx.Response.Headers.CacheControl = "public, max-age=300";
        ctx.Response.Headers["Vary"] = "Host";
        ctx.Response.ContentType = "application/json; charset=utf-8";

        var resolver = ctx.RequestServices.GetRequiredService<BridgeUrlResolver>();
        var urls = resolver.Resolve(ctx);
        return ctx.Response.WriteAsync(render(urls));
    }

    private static void SetMetadataCorsHeaders(HttpContext ctx)
    {
        ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
        ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        // Match the body's 5-minute Cache-Control window.
        ctx.Response.Headers["Access-Control-Max-Age"] = "300";
    }
}

