using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Test-only stand-in that exposes both the new
/// <see cref="ISafeguardSession"/> surface (which production code now
/// depends on) and a backward-compatible facade matching the old
/// <c>SafeguardConnectionManager</c> API so existing integration test
/// call sites compile unchanged. Internally delegates to
/// <see cref="StdioSafeguardSession"/> and <see cref="SafeguardInvoker"/>.
/// Single-target by design — multi-host stdio has been removed.
/// </summary>
internal sealed class TestConnectionManager : ISafeguardSession, IDisposable
{
    private readonly StdioSafeguardSession _session;

    public TestConnectionManager(
        ILogger<StdioSafeguardSession> logger,
        IConfiguration configuration,
        CatalogProvider catalogProvider,
        ISafeguardConnectionFactory factory = null)
    {
        _session = factory != null
            ? new StdioSafeguardSession(logger, configuration, catalogProvider, factory)
            : new StdioSafeguardSession(logger, configuration, catalogProvider);
    }

    public ISafeguardSession Session => _session;

    // -------- ISafeguardSession passthrough --------
    public string Host => _session.Host;
    public bool IgnoreSsl => _session.IgnoreSsl;
    public bool HasCredentials => _session.HasCredentials;

    public Task EnsureReadyAsync(McpServer server, CancellationToken cancellationToken = default)
        => _session.EnsureReadyAsync(server, cancellationToken);

    public Task<T> ExecuteWithConnectionAsync<T>(
        Func<ISafeguardConnection, Task<T>> work,
        CancellationToken cancellationToken = default)
        => _session.ExecuteWithConnectionAsync(work, cancellationToken);

    public Task<PrincipalInfo> GetPrincipalInfoAsync(CancellationToken cancellationToken = default)
        => _session.GetPrincipalInfoAsync(cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default)
        => _session.LogoutAsync(cancellationToken);

    // -------- Backward-compatible facade --------

    /// <summary>Single-element list (single-target). Empty if not yet connected.</summary>
    public IReadOnlyList<string> ConnectedHosts
        => _session.HasCredentials && !string.IsNullOrWhiteSpace(_session.Host)
            ? new[] { _session.Host }
            : Array.Empty<string>();

    public Task<string> EnsureAuthenticatedAsync(McpServer server, string host, CancellationToken ct, bool? ignoreSsl = null)
        => EnsureAndReturnHostAsync(server, ct);

    public Task<string> EnsureHostConfiguredAsync(McpServer server, string host, CancellationToken ct)
        => EnsureAndReturnHostAsync(server, ct);

    private async Task<string> EnsureAndReturnHostAsync(McpServer server, CancellationToken ct)
    {
        await _session.EnsureReadyAsync(server, ct);
        return _session.Host;
    }

    public Task<FullResponse> InvokeAsync(
        string host, Service service, string method, string relativeUrl,
        string body = null, IDictionary<string, string> parameters = null, CancellationToken ct = default)
        => SafeguardInvoker.InvokeAsync(_session, service, method, relativeUrl, body, parameters, ct);

    public Task<string> InvokeCsvAsync(
        string host, Service service, Method method, string relativeUrl,
        IDictionary<string, string> parameters = null, CancellationToken ct = default)
        => SafeguardInvoker.InvokeCsvAsync(_session, service, relativeUrl, parameters, ct);

    public int GetTokenLifetimeMinutes(string host)
    {
        if (!_session.HasCredentials)
            return -1;
        try
        {
            return _session.ExecuteWithConnectionAsync(
                c => Task.FromResult(c.GetAccessTokenLifetimeRemaining()),
                CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
            return -1;
        }
    }

    public void Dispose() => _session.Dispose();
}

/// <summary>
/// Test-only extension methods that re-add the host-parameterized
/// signatures the old <see cref="CatalogProvider"/> exposed.
/// Lets pre-existing integration tests compile without rewriting every
/// call site — the host argument is intentionally ignored (the
/// provider is single-target now).
/// </summary>
internal static class CatalogProviderTestExtensions
{
    public static ReadOnlySpan<ApiEndpoint> GetEndpoints(this CatalogProvider provider, string host)
        => provider.GetEndpoints();

    public static ApiSchema? GetSchema(this CatalogProvider provider, string method, string service, string path, string host)
        => provider.GetSchema(method, service, path);

    public static ApiSchema? GetResponseSchema(this CatalogProvider provider, string method, string service, string path, string host)
        => provider.GetResponseSchema(method, service, path);

    public static bool HasDynamicCatalog(this CatalogProvider provider, string host)
        => provider.HasDynamicCatalog;
}
