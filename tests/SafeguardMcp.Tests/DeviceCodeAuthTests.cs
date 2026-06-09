#nullable disable

using System.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using OneIdentity.SafeguardDotNet;
using OneIdentity.SafeguardDotNet.Event;
using OneIdentity.SafeguardDotNet.Sps;
using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;
using DeviceCodeInfo = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeInfo;
using DeviceCodeLoginParameters = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLoginParameters;

namespace SafeguardMcp.Tests;

[CollectionDefinition("EnvVars", DisableParallelization = true)]
public class EnvVarCollection { }

[Collection("EnvVars")]
public class DeviceCodeAuthTests
{
    private const string TestHost = "safeguard.example.test";

    [Fact]
    public async Task EnsureAuthenticated_NoEnvVars_UsesDeviceCode()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", TestHost);
        var factory = new FakeConnectionFactory();
        using var manager = CreateManager(factory);

        await manager.EnsureReadyAsync(server: null, cancellationToken: CancellationToken.None);

        Assert.Equal(1, factory.DeviceCodeCallCount);
        Assert.Equal(0, factory.PkceCallCount);
        Assert.Equal(TestHost, factory.LastDeviceCodeHost);
    }

    [Fact]
    public async Task EnsureAuthenticated_AllPkceEnvVarsSet_UsesPkce()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", TestHost);
        Environment.SetEnvironmentVariable("SAFEGUARD_PROVIDER", "local");
        Environment.SetEnvironmentVariable("SAFEGUARD_USER", "admin");
        Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", "secret");

        var factory = new FakeConnectionFactory();
        using var manager = CreateManager(factory);

        await manager.EnsureReadyAsync(server: null, cancellationToken: CancellationToken.None);

        Assert.Equal(0, factory.DeviceCodeCallCount);
        Assert.Equal(1, factory.PkceCallCount);
        Assert.Equal(TestHost, factory.LastPkceHost);
        Assert.Equal("local", factory.LastPkceProvider);
        Assert.Equal("admin", factory.LastPkceUser);
    }

    [Fact]
    public async Task EnsureAuthenticated_PartialEnvVars_UsesDeviceCode()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", TestHost);
        Environment.SetEnvironmentVariable("SAFEGUARD_USER", "admin");
        Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", "secret");

        var factory = new FakeConnectionFactory();
        using var manager = CreateManager(factory);

        await manager.EnsureReadyAsync(server: null, cancellationToken: CancellationToken.None);

        Assert.Equal(1, factory.DeviceCodeCallCount);
        Assert.Equal(0, factory.PkceCallCount);
    }

    [Fact]
    public async Task EnsureAuthenticated_DeviceCodeDisplayCallback_LogsUrlAndCode()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", TestHost);
        var info = new DeviceCodeInfo
        {
            VerificationUri = "https://safeguard.example.test/RSTS/Login/DeviceCode",
            UserCode = "ABCD-1234",
            ExpiresIn = 600,
        };
        var factory = new FakeConnectionFactory { DeviceCodeInfoToFire = info };
        var capturingLogger = new CapturingLogger<StdioSafeguardSession>();
        using var manager = CreateManager(factory, sessionLogger: capturingLogger);

        await manager.EnsureReadyAsync(server: null, cancellationToken: CancellationToken.None);

        Assert.Contains(capturingLogger.Messages, m =>
            m.Contains("Device code for") &&
            m.Contains(info.VerificationUri) &&
            m.Contains(info.UserCode));
    }

    [Fact]
    public async Task EnsureAuthenticated_DeviceCodeFails_WrapsAsMcpException()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", TestHost);
        var factory = new FakeConnectionFactory
        {
            DeviceCodeException = new SafeguardDotNetException("device code grant disabled"),
        };
        using var manager = CreateManager(factory);

        var ex = await Assert.ThrowsAsync<McpException>(
            () => manager.EnsureReadyAsync(server: null, cancellationToken: CancellationToken.None));

        Assert.Contains("Authentication failed", ex.Message);
        Assert.Contains("PKCE fallback", ex.Message);
    }

    [Fact]
    public async Task EnsureAuthenticated_TlsFailure_NoElicitation_ThrowsWithIgnoreSslHint()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", TestHost);
        var factory = new FakeConnectionFactory
        {
            DeviceCodeException = new HttpRequestException(
                "The SSL connection could not be established",
                new System.Security.Authentication.AuthenticationException(
                    "The remote certificate is invalid according to the validation procedure.")),
        };
        using var manager = CreateManager(factory);

        var ex = await Assert.ThrowsAsync<McpException>(
            () => manager.EnsureReadyAsync(server: null, cancellationToken: CancellationToken.None));

        Assert.Contains("TLS certificate verification failed", ex.Message);
        Assert.Contains("SAFEGUARD_IGNORE_SSL", ex.Message);
        Assert.Equal(1, factory.DeviceCodeCallCount);
    }

    [Fact]
    public async Task EnsureAuthenticated_TlsFailure_AlreadyIgnoringSsl_DoesNotEnterTlsRetry()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", TestHost);
        Environment.SetEnvironmentVariable("SAFEGUARD_IGNORE_SSL", "true");
        var factory = new FakeConnectionFactory
        {
            DeviceCodeException = new HttpRequestException(
                "tls handshake failed",
                new System.Security.Authentication.AuthenticationException("bad cert")),
        };
        using var manager = CreateManager(factory);

        var ex = await Assert.ThrowsAsync<McpException>(
            () => manager.EnsureReadyAsync(server: null, cancellationToken: CancellationToken.None));

        Assert.Contains("Cannot connect", ex.Message);
        Assert.DoesNotContain("TLS certificate verification failed", ex.Message);
        Assert.Equal(1, factory.DeviceCodeCallCount);
    }

    private static StdioSafeguardSession CreateManager(
        FakeConnectionFactory factory,
        ILogger<StdioSafeguardSession> sessionLogger = null)
    {
        var config = new ConfigurationBuilder().Build();
        var catalogLoader = new CatalogLoader(NullLogger<CatalogLoader>.Instance);
        var catalogProvider = new CatalogProvider(catalogLoader, NullLogger<CatalogProvider>.Instance);
        var logger = sessionLogger ?? NullLogger<StdioSafeguardSession>.Instance;
        return new StdioSafeguardSession(logger, config, catalogProvider, factory);
    }

    private sealed class EnvVarScope : IDisposable
    {
        private readonly Dictionary<string, string> _saved = new();
        private static readonly string[] AuthVars =
        {
            "SAFEGUARD_HOST",
            "SAFEGUARD_PROVIDER",
            "SAFEGUARD_USER",
            "SAFEGUARD_PASSWORD",
            "SAFEGUARD_IGNORE_SSL",
        };

        public static EnvVarScope ClearSafeguardAuthVars()
        {
            var scope = new EnvVarScope();
            foreach (var name in AuthVars)
            {
                scope._saved[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, null);
            }
            return scope;
        }

        public void Dispose()
        {
            foreach (var kvp in _saved)
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => NullDisposable.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class FakeConnectionFactory : ISafeguardConnectionFactory
    {
        public int DeviceCodeCallCount;
        public int PkceCallCount;
        public string LastDeviceCodeHost;
        public string LastPkceHost;
        public string LastPkceProvider;
        public string LastPkceUser;
        public DeviceCodeInfo DeviceCodeInfoToFire;
        public Exception DeviceCodeException;

        public Task<ISafeguardConnection> ConnectDeviceCodeAsync(
            string host,
            DeviceCodeLoginParameters parameters,
            bool ignoreSsl,
            CancellationToken ct)
        {
            DeviceCodeCallCount++;
            LastDeviceCodeHost = host;
            if (DeviceCodeInfoToFire != null)
                parameters.DisplayCallback?.Invoke(DeviceCodeInfoToFire);
            if (DeviceCodeException != null)
                return Task.FromException<ISafeguardConnection>(DeviceCodeException);
            return Task.FromResult<ISafeguardConnection>(new FakeSafeguardConnection());
        }

        public ISafeguardConnection ConnectPkce(
            string host,
            string provider,
            string user,
            SecureString password,
            bool ignoreSsl)
        {
            PkceCallCount++;
            LastPkceHost = host;
            LastPkceProvider = provider;
            LastPkceUser = user;
            return new FakeSafeguardConnection();
        }

        public ISafeguardConnection ConnectWithAccessToken(
            string host,
            SecureString accessToken,
            int apiVersion,
            bool ignoreSsl)
            => throw new NotSupportedException(
                "Stdio session does not use bearer-token relay.");
    }

    private sealed class FakeSafeguardConnection : ISafeguardConnection
    {
        public int GetAccessTokenLifetimeRemaining() => 60;
        public void LogOut() { }
        public void Dispose() { }

        public IStreamingRequest Streaming => throw new NotSupportedException();
        public void RefreshAccessToken() => throw new NotSupportedException();
        public string InvokeMethod(Service service, Method method, string relativeUrl, string body = null,
            IDictionary<string, string> parameters = null, IDictionary<string, string> additionalHeaders = null,
            TimeSpan? timeout = null) => throw new NotSupportedException();
        public FullResponse InvokeMethodFull(Service service, Method method, string relativeUrl, string body = null,
            IDictionary<string, string> parameters = null, IDictionary<string, string> additionalHeaders = null,
            TimeSpan? timeout = null) => throw new NotSupportedException();
        public string InvokeMethodCsv(Service service, Method method, string relativeUrl, string body = null,
            IDictionary<string, string> parameters = null, IDictionary<string, string> additionalHeaders = null,
            TimeSpan? timeout = null) => throw new NotSupportedException();
        public FullResponse JoinSps(ISafeguardSessionsConnection spsConnection, string certificateChain, string sppAddress)
            => throw new NotSupportedException();
        public ISafeguardEventListener GetEventListener() => throw new NotSupportedException();
        public ISafeguardEventListener GetPersistentEventListener() => throw new NotSupportedException();
        public ISafeguardConnection GetManagementServiceConnection(string networkAddress) => throw new NotSupportedException();
        public SecureString GetAccessToken() => throw new NotSupportedException();
    }
}
