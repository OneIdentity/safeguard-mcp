using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.Tools;

/// <summary>
/// Per-request abstraction over an <see cref="ISafeguardConnection"/>.
/// The session owns connection lifetime so tool code never sees raw
/// <see cref="ISafeguardConnection"/> instances; callers receive the
/// connection only inside an <c>ExecuteWithConnectionAsync</c> callback.
///
/// Two implementations:
///   <list type="bullet">
///     <item><see cref="StdioSafeguardSession"/> — singleton in stdio
///     mode; caches a single device-code/PKCE-authenticated connection
///     for the process lifetime.</item>
///     <item><see cref="HttpRelaySafeguardSession"/> — scoped in HTTP
///     mode; reads the <c>Authorization: Bearer</c> header from the
///     current request and builds a transient
///     <see cref="ISafeguardConnection"/> via
///     <c>Safeguard.Connect(host, secureBearer, apiVersion=4, ignoreSsl)</c>.
///     The connection (and its SecureString copy of the bearer) is
///     disposed when the DI scope ends.</item>
///   </list>
/// No Safeguard access token is ever stored on this object outside the
/// SDK's <see cref="System.Security.SecureString"/>-managed copy inside
/// the active <see cref="ISafeguardConnection"/>.
/// </summary>
public interface ISafeguardSession
{
    /// <summary>The Safeguard appliance host this session targets.</summary>
    string Host { get; }

    /// <summary>
    /// Whether the outbound TLS connection to the appliance should skip
    /// certificate validation. Deployment-wide property — never
    /// per-caller — sourced from <c>SAFEGUARD_IGNORE_SSL</c> at startup.
    /// </summary>
    bool IgnoreSsl { get; }

    /// <summary>
    /// Ensures the session is ready to execute API calls. In stdio mode
    /// this drives device-code (or PKCE) login on first call and binds
    /// the host, optionally eliciting it from the MCP client when
    /// <c>SAFEGUARD_HOST</c> is unset. In HTTP mode this validates that
    /// the current request carries an <c>Authorization: Bearer</c>
    /// header and that <c>SAFEGUARD_HOST</c> is configured; it never
    /// stores the bearer outside the per-request scope.
    /// </summary>
    Task EnsureReadyAsync(
        McpServer server,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes <paramref name="work"/> with an authenticated
    /// <see cref="ISafeguardConnection"/>. The connection's lifetime is
    /// owned by the session; do not dispose it inside the callback.
    /// </summary>
    Task<T> ExecuteWithConnectionAsync<T>(
        Func<ISafeguardConnection, Task<T>> work,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches identity information about the caller from
    /// <c>GET /service/core/v4/Me</c> and returns a normalized
    /// <see cref="PrincipalInfo"/>. In HTTP mode this is the principal
    /// whose bearer accompanied the current request.
    /// </summary>
    Task<PrincipalInfo> GetPrincipalInfoAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out and disposes the active connection. In stdio mode this
    /// invalidates the cached connection; subsequent calls will need to
    /// re-authenticate. In HTTP mode this calls
    /// <see cref="ISafeguardConnection.LogOut"/> on the per-request
    /// connection, which posts to
    /// <c>/service/core/v4/Token/Logout</c> with the bearer and
    /// invalidates that bearer at the appliance.
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports whether the session currently has cached credentials
    /// (stdio) or a request bearer (HTTP). Used by status/connect tools
    /// to render a "not authenticated" hint without a round-trip.
    /// </summary>
    bool HasCredentials { get; }
}

/// <summary>
/// Display-only summary of the authenticated Safeguard principal.
/// Built from <c>GET /v4/Me</c> plus a no-verification JWT decode of
/// the bearer for expiry display. Never carries the access token.
/// </summary>
public sealed class PrincipalInfo
{
    public string DisplayName { get; set; }
    public string Name { get; set; }
    public string IdentityProvider { get; set; }
    public string ApplianceHost { get; set; }
    public DateTimeOffset? TokenExpiresAt { get; set; }
    public int TokenLifetimeMinutes { get; set; }
}
