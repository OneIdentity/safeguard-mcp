using System.Net;
using System.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Catalog;
using DeviceCodeInfo = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeInfo;
using DeviceCodeLoginParameters = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLoginParameters;

namespace SafeguardMcp.Tools;

/// <summary>
/// Single-target stdio session. Caches one
/// <see cref="ISafeguardConnection"/> for the process lifetime,
/// re-using it across all tool calls. Authentication is via
/// device-code (default) or PKCE when <c>SAFEGUARD_PROVIDER</c> /
/// <c>SAFEGUARD_USER</c> / <c>SAFEGUARD_PASSWORD</c> are all set.
/// </summary>
internal sealed class StdioSafeguardSession : ISafeguardSession, IDisposable
{
    private readonly ILogger<StdioSafeguardSession> _logger;
    private readonly IConfiguration _configuration;
    private readonly CatalogProvider _catalogProvider;
    private readonly ISafeguardConnectionFactory _factory;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    private ISafeguardConnection _connection;
    private string _host;
    private bool _ignoreSsl;

    public StdioSafeguardSession(
        ILogger<StdioSafeguardSession> logger,
        IConfiguration configuration,
        CatalogProvider catalogProvider)
        : this(logger, configuration, catalogProvider, new SafeguardConnectionFactory())
    {
    }

    public StdioSafeguardSession(
        ILogger<StdioSafeguardSession> logger,
        IConfiguration configuration,
        CatalogProvider catalogProvider,
        ISafeguardConnectionFactory factory)
    {
        _logger = logger;
        _configuration = configuration;
        _catalogProvider = catalogProvider;
        _factory = factory;
        _host = Environment.GetEnvironmentVariable("SAFEGUARD_HOST")?.Trim();
        _ignoreSsl = ResolveSslPolicy();
    }

    public string Host => _host;

    public bool IgnoreSsl => _ignoreSsl;

    public bool HasCredentials => _connection != null;

    public async Task EnsureReadyAsync(McpServer server, CancellationToken cancellationToken = default)
    {
        if (_connection != null && TryEnsureTokenFresh())
            return;

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection != null && TryEnsureTokenFresh())
                return;

            if (_connection != null)
            {
                DisposeConnectionLocked();
            }

            if (string.IsNullOrWhiteSpace(_host))
                _host = await ElicitHostAsync(server, cancellationToken);

            await ConnectLockedAsync(server, cancellationToken);
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task<T> ExecuteWithConnectionAsync<T>(
        Func<ISafeguardConnection, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadyAsync(server: null, cancellationToken);
        return await work(_connection);
    }

    public Task<PrincipalInfo> GetPrincipalInfoAsync(CancellationToken cancellationToken = default)
        => SessionHelpers.GetPrincipalInfoAsync(this, _host, cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        DisposeConnectionLocked();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisposeConnectionLocked();
        _authLock.Dispose();
    }

    private void DisposeConnectionLocked()
    {
        var connection = _connection;
        _connection = null;
        if (connection == null)
            return;
        try { connection.LogOut(); } catch { }
        try { connection.Dispose(); } catch { }
    }

    private async Task ConnectLockedAsync(McpServer server, CancellationToken ct)
    {
        var envProvider = Environment.GetEnvironmentVariable("SAFEGUARD_PROVIDER");
        var envUser = Environment.GetEnvironmentVariable("SAFEGUARD_USER");
        var envPassword = Environment.GetEnvironmentVariable("SAFEGUARD_PASSWORD");

        try
        {
            if (!string.IsNullOrEmpty(envProvider) && !string.IsNullOrEmpty(envUser) && !string.IsNullOrEmpty(envPassword))
            {
                _logger.LogInformation("Using non-interactive PKCE authentication for '{Host}'.", _host);
                _connection = await Task.Run(() =>
                {
                    using var securePassword = SessionHelpers.ToSecureString(envPassword);
                    return _factory.ConnectPkce(_host, envProvider, envUser, securePassword, _ignoreSsl);
                }, ct);
            }
            else
            {
                _logger.LogInformation("Using device-code authentication for '{Host}'.", _host);
                var parameters = new DeviceCodeLoginParameters
                {
                    DisplayCallback = info => DisplayDeviceCode(server, _host, info, ct),
                };
                _connection = await _factory.ConnectDeviceCodeAsync(_host, parameters, _ignoreSsl, ct);
            }
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Net.Sockets.SocketException)
        {
            throw new McpException(
                $"Cannot connect to '{_host}': {ex.Message}. "
                + "Verify the hostname/IP is correct and the appliance is reachable. Call Safeguard_Connect with the correct host.");
        }
        catch (SafeguardDotNetException ex)
        {
            throw new McpException(
                $"Authentication failed for '{_host}': {ex.Message}. "
                + "If your Safeguard administrator has not enabled the Device Authorization Grant, "
                + "set SAFEGUARD_PROVIDER, SAFEGUARD_USER, and SAFEGUARD_PASSWORD to use the PKCE fallback.");
        }

        _logger.LogInformation(
            "Successfully connected to '{Host}'. Token expires in {Minutes} minutes.",
            _host, _connection.GetAccessTokenLifetimeRemaining());

        _ = Task.Run(() => _catalogProvider.LoadCatalogAsync(_host, _ignoreSsl), CancellationToken.None);
    }

    private bool TryEnsureTokenFresh()
    {
        if (_connection == null)
            return false;
        try
        {
            var minutesLeft = _connection.GetAccessTokenLifetimeRemaining();
            if (minutesLeft < 5)
            {
                _logger.LogInformation("Token for '{Host}' expires in {Minutes} min — refreshing.", _host, minutesLeft);
                _connection.RefreshAccessToken();
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check/refresh token for '{Host}' — forcing re-authentication.", _host);
            return false;
        }
    }

    private bool ResolveSslPolicy()
    {
        var envVal = Environment.GetEnvironmentVariable("SAFEGUARD_IGNORE_SSL");
        if (bool.TryParse(envVal, out var ignore))
            return ignore;
        return bool.TryParse(_configuration["Safeguard:IgnoreSsl"], out var ignoreCfg) && ignoreCfg;
    }

    private async Task<string> ElicitHostAsync(McpServer server, CancellationToken ct)
    {
        if (server?.ClientCapabilities?.Elicitation?.Form == null)
        {
            throw new McpException(
                "Safeguard server address is required and the MCP client does not support elicitation. "
                + "Set the SAFEGUARD_HOST environment variable to the appliance DNS name or IP address before starting the server.");
        }

        _logger.LogInformation("Eliciting Safeguard server address from user...");

        var result = await server.ElicitAsync(new ElicitRequestParams
        {
            Mode = "form",
            Message = "Enter the DNS name or IP address of the Safeguard server to connect to.",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                {
                    ["host"] = new ElicitRequestParams.StringSchema
                    {
                        Title = "Safeguard Server",
                        Description = "DNS name or IP address of the Safeguard appliance"
                    }
                },
                Required = ["host"]
            }
        }, ct);

        var isAccepted = result.Action != null
            && result.Action.StartsWith("accept", StringComparison.OrdinalIgnoreCase);

        if (!isAccepted || result.Content == null
            || !result.Content.TryGetValue("host", out var hostElement))
        {
            throw new McpException("Safeguard server address is required to proceed.");
        }

        var resolvedHost = hostElement.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(resolvedHost))
            throw new McpException("Safeguard server address is required to proceed.");

        _logger.LogInformation("Host configured: '{Host}'", resolvedHost);
        return resolvedHost;
    }

    private void DisplayDeviceCode(McpServer server, string host, DeviceCodeInfo info, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(info.VerificationUriComplete)
            ? info.VerificationUri
            : info.VerificationUriComplete;

        _logger.LogInformation(
            "Device code for '{Host}': open {Url} and enter code {Code} (expires in {Seconds}s).",
            host, url, info.UserCode, info.ExpiresIn);

        if (server?.ClientCapabilities?.Elicitation?.Form == null)
        {
            _logger.LogWarning(
                "MCP client did not advertise form elicitation for '{Host}'; emitting device-code on stderr fallback.",
                host);
            Console.Error.WriteLine(
                $"SAFEGUARD_DEVICE_CODE: host={host} url={url} code={info.UserCode} expires_in={info.ExpiresIn}");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await server.ElicitAsync(new ElicitRequestParams
                {
                    Mode = "form",
                    Message =
                        $"To finish signing in to Safeguard '{host}', open {url} in a browser "
                        + $"and confirm code {info.UserCode}. The code expires in {info.ExpiresIn} seconds. "
                        + "Press Enter (accept) to dismiss this dialog once you have signed in.",
                    RequestedSchema = new ElicitRequestParams.RequestSchema
                    {
                        Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                        {
                            ["acknowledged"] = new ElicitRequestParams.BooleanSchema
                            {
                                Title = "Signed in",
                                Description = "Leave as-is and press Enter after completing the browser sign-in.",
                                Default = true
                            }
                        },
                        Required = []
                    }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not surface device-code prompt via MCP elicitation; user must follow the URL/code from the log.");
            }
        }, CancellationToken.None);
    }
}
