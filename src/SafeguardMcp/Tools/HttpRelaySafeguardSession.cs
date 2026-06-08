using System.Net;
using System.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tools;

/// <summary>
/// Pure-relay session for HTTP mode. Registered scoped so each MCP HTTP
/// request gets its own session instance. Reads the
/// <c>Authorization: Bearer</c> header from the current
/// <see cref="HttpContext"/> the first time
/// <see cref="ExecuteWithConnectionAsync"/> is called, builds a
/// transient <see cref="ISafeguardConnection"/> via
/// <c>Safeguard.Connect(host, secureBearer, apiVersion=4, ignoreSsl)</c>,
/// and caches it for the rest of the request so concurrent SDK calls
/// within one MCP request only pay one TLS handshake. The connection
/// (and the SDK's <see cref="SecureString"/> copy of the bearer) is
/// disposed when the DI scope ends.
///
/// No Safeguard access token is ever assigned to a field on this class
/// or persisted anywhere: the token transits as
/// <c>request header → managed string → SecureString → SDK
/// connection</c>, and the managed and secure copies are released at
/// request end.
/// </summary>
internal sealed class HttpRelaySafeguardSession : ISafeguardSession, IDisposable
{
    private const int ApplianceApiVersion = 4;

    private readonly ILogger<HttpRelaySafeguardSession> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISafeguardConnectionFactory _factory;
    private readonly string _host;
    private readonly bool _ignoreSsl;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    private ISafeguardConnection _connection;

    public HttpRelaySafeguardSession(
        ILogger<HttpRelaySafeguardSession> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
        : this(logger, configuration, httpContextAccessor, new SafeguardConnectionFactory())
    {
    }

    public HttpRelaySafeguardSession(
        ILogger<HttpRelaySafeguardSession> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ISafeguardConnectionFactory factory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _factory = factory;
        _host = Environment.GetEnvironmentVariable("SAFEGUARD_HOST")?.Trim();
        _ignoreSsl = ResolveSslPolicy();
    }

    public string Host => _host;

    public bool IgnoreSsl => _ignoreSsl;

    public bool HasCredentials => TryGetBearer(out _);

    public Task EnsureReadyAsync(McpServer server, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_host))
        {
            throw new McpException(
                "Safeguard appliance host is not configured. Set the SAFEGUARD_HOST "
                + "environment variable before starting the MCP server in HTTP mode.");
        }

        if (!TryGetBearer(out _))
        {
            throw new McpException(
                "Not authenticated against Safeguard. Acquire a Safeguard user token "
                + "(e.g., `safeguard-mcp login`) and configure your client to send "
                + "`Authorization: Bearer <token>`.");
        }

        return Task.CompletedTask;
    }

    public async Task<T> ExecuteWithConnectionAsync<T>(
        Func<ISafeguardConnection, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOrCreateConnectionAsync(cancellationToken);
        try
        {
            return await work(connection);
        }
        catch (SafeguardDotNetException ex) when (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
        {
            // Pure relay: there is nothing to refresh — the bearer
            // belongs to the caller. Surface a hint to re-acquire.
            throw new McpException(
                "Safeguard token has expired or been revoked. Re-acquire "
                + "(`safeguard-mcp login` or your MCP client's OAuth flow) and retry.");
        }
    }

    public Task<PrincipalInfo> GetPrincipalInfoAsync(CancellationToken cancellationToken = default)
        => SessionHelpers.GetPrincipalInfoAsync(this, _host, cancellationToken);

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetOrCreateConnectionAsync(cancellationToken);
        try
        {
            // SDK's LogOut() posts to /service/core/v4/Token/Logout with
            // the bearer (DefaultApiVersion = 4) and zeroes the
            // SecureString copy. No hand-rolled HTTP.
            await Task.Run(connection.LogOut, cancellationToken);
        }
        finally
        {
            DisposeConnection();
        }
    }

    public void Dispose() => DisposeConnection();

    private async Task<ISafeguardConnection> GetOrCreateConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
            return _connection;

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection != null)
                return _connection;

            await EnsureReadyAsync(server: null, cancellationToken);

            if (!TryGetBearer(out var bearer))
            {
                throw new McpException(
                    "Not authenticated against Safeguard. Acquire a Safeguard user token "
                    + "(e.g., `safeguard-mcp login`) and configure your client to send "
                    + "`Authorization: Bearer <token>`.");
            }

            // The SecureString here lives only as long as the SDK
            // connection: the SDK takes a copy internally; we dispose
            // ours immediately to keep no second resident copy.
            using var secureBearer = SessionHelpers.ToSecureString(bearer);
            _connection = _factory.ConnectWithAccessToken(_host, secureBearer, ApplianceApiVersion, _ignoreSsl);

            _logger.LogDebug(
                "HTTP relay session bound to '{Host}' (ignoreSsl={IgnoreSsl}).",
                _host, _ignoreSsl);

            return _connection;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private bool TryGetBearer(out string bearer)
    {
        bearer = null;
        var ctx = _httpContextAccessor?.HttpContext;
        if (ctx == null)
            return false;

        if (!ctx.Request.Headers.TryGetValue("Authorization", out var values))
            return false;

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;
            var trimmed = value.Trim();
            if (trimmed.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = trimmed.Substring("Bearer ".Length).Trim();
                if (candidate.Length == 0)
                    continue;
                bearer = candidate;
                return true;
            }
        }

        return false;
    }

    private void DisposeConnection()
    {
        var connection = _connection;
        _connection = null;
        if (connection == null)
            return;
        try { connection.Dispose(); } catch { }
    }

    private bool ResolveSslPolicy()
    {
        var envVal = Environment.GetEnvironmentVariable("SAFEGUARD_IGNORE_SSL");
        if (bool.TryParse(envVal, out var ignore))
            return ignore;
        return bool.TryParse(_configuration["Safeguard:IgnoreSsl"], out var ignoreCfg) && ignoreCfg;
    }
}
