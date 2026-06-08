using Microsoft.AspNetCore.Http;

namespace SafeguardMcp.OAuth;

/// <summary>
/// Resolves the bridge's per-request URL set from
/// <see cref="BridgeOptions"/> + the incoming <see cref="HttpContext"/>.
///
/// <para>
/// In the common deployment shape the bridge URLs are inferred from
/// <c>Request.Scheme</c> + <c>Request.Host</c> + <c>Request.PathBase</c>.
/// Operators who need to pin the published URLs (e.g., when the
/// rSTS <c>RelyingPartyApplication.Realm</c> was registered under a
/// hostname different from the one the request arrives on) can set
/// <see cref="BridgeOptions.OverridePublicUrl"/> /
/// <see cref="BridgeOptions.OverrideClientId"/> to short-circuit the
/// inference.
/// </para>
///
/// <para>
/// <c>Request.Scheme</c> and <c>Request.Host</c> reflect
/// <c>X-Forwarded-Proto</c> / <c>X-Forwarded-Host</c> when
/// <c>UseForwardedHeaders</c> has applied them; the trust list for
/// that middleware is configured in <c>Program.cs</c>.
/// </para>
/// </summary>
internal sealed class BridgeUrlResolver
{
    private readonly BridgeOptions _options;

    public BridgeUrlResolver(BridgeOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        _options = options;
    }

    public BridgeRequestUrls Resolve(HttpContext ctx)
    {
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        string publicUrl;
        if (!string.IsNullOrEmpty(_options.OverridePublicUrl))
        {
            publicUrl = _options.OverridePublicUrl;
        }
        else
        {
            var scheme = ctx.Request.Scheme;
            var host = ctx.Request.Host.HasValue ? ctx.Request.Host.Value : "localhost";
            var pathBase = ctx.Request.PathBase.HasValue ? ctx.Request.PathBase.Value : string.Empty;
            publicUrl = (scheme + "://" + host + pathBase).TrimEnd('/');
        }

        var clientId = !string.IsNullOrEmpty(_options.OverrideClientId)
            ? _options.OverrideClientId
            : publicUrl;

        return new BridgeRequestUrls(publicUrl, clientId);
    }
}

/// <summary>
/// Per-request set of fully-qualified bridge URLs. Built once at the
/// top of each handler and passed down so every endpoint sees a
/// consistent snapshot of the public URL — even if a future change
/// makes resolution sensitive to per-request data beyond
/// <c>Host</c>/<c>Scheme</c>.
/// </summary>
internal sealed class BridgeRequestUrls
{
    public string PublicUrl { get; }
    public string ClientId { get; }
    public string AuthorizeEndpoint { get; }
    public string AuthorizeCallbackEndpoint { get; }
    public string TokenEndpoint { get; }
    public string RegistrationEndpoint { get; }
    public string ProtectedResourceMetadataUrl { get; }
    public string AuthorizationServerMetadataUrl { get; }

    public BridgeRequestUrls(string publicUrl, string clientId)
    {
        if (string.IsNullOrEmpty(publicUrl)) throw new ArgumentException(nameof(publicUrl));
        if (string.IsNullOrEmpty(clientId)) throw new ArgumentException(nameof(clientId));
        PublicUrl = publicUrl;
        ClientId = clientId;
        AuthorizeEndpoint = publicUrl + "/authorize";
        AuthorizeCallbackEndpoint = publicUrl + "/authorize/callback";
        TokenEndpoint = publicUrl + "/token";
        RegistrationEndpoint = publicUrl + "/register";
        ProtectedResourceMetadataUrl = publicUrl + "/.well-known/oauth-protected-resource";
        AuthorizationServerMetadataUrl = publicUrl + "/.well-known/oauth-authorization-server";
    }
}
