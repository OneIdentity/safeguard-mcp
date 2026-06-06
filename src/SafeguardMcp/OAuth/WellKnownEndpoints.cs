using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SafeguardMcp.OAuth;

/// <summary>
/// Maps the bridge's RFC 9728 / RFC 8414 well-known metadata endpoints
/// (HTTP-AUTH-RELAY-PLAN §2.2.a, §2.2.b) onto an
/// <see cref="IEndpointRouteBuilder"/>. Both endpoints:
/// <list type="bullet">
///   <item>Return <c>application/json</c>.</item>
///   <item>Set <c>Access-Control-Allow-Origin: *</c> per plan §2.6
///   (RFC 8414 §3.2 expects metadata to be publicly readable).</item>
///   <item>Implement an <c>OPTIONS</c> preflight that mirrors the
///   ACAO/ACAM headers and returns 204.</item>
///   <item>Send <c>Cache-Control: public, max-age=3600</c> — the
///   document is keyed on <c>MCP_PUBLIC_URL</c>, which is immutable
///   for the process lifetime.</item>
/// </list>
/// Safe to call only after <see cref="BridgeOptions.Parse"/> returns
/// an active result.
/// </summary>
internal static class WellKnownEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints, BridgeOptions options)
    {
        if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var protectedResourceJson = WellKnownMetadata.BuildProtectedResourceJson(options);
        var authServerJson = WellKnownMetadata.BuildAuthorizationServerJson(options);

        MapMetadata(endpoints, WellKnownMetadata.ProtectedResourcePath, protectedResourceJson);
        MapMetadata(endpoints, WellKnownMetadata.AuthorizationServerPath, authServerJson);
    }

    private static void MapMetadata(IEndpointRouteBuilder endpoints, string path, string body)
    {
        endpoints.MapGet(path, ctx => WriteMetadataAsync(ctx, body));
        endpoints.MapMethods(path, new[] { "OPTIONS" }, ctx =>
        {
            SetMetadataCorsHeaders(ctx);
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
    }

    private static Task WriteMetadataAsync(HttpContext ctx, string body)
    {
        SetMetadataCorsHeaders(ctx);
        ctx.Response.Headers.CacheControl = "public, max-age=3600";
        ctx.Response.ContentType = "application/json; charset=utf-8";
        return ctx.Response.WriteAsync(body);
    }

    private static void SetMetadataCorsHeaders(HttpContext ctx)
    {
        ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
        ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
    }
}
