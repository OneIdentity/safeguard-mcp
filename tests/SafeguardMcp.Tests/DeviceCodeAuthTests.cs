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
        var factory = new FakeConnectionFactory();
        var manager = CreateManager(factory);

        await manager.EnsureAuthenticatedAsync(server: null, host: TestHost, ct: CancellationToken.None, ignoreSsl: false);

        Assert.Equal(1, factory.DeviceCodeCallCount);
        Assert.Equal(0, factory.PkceCallCount);
        Assert.Equal(TestHost, factory.LastDeviceCodeHost);
    }

    [Fact]
    public async Task EnsureAuthenticated_AllPkceEnvVarsSet_UsesPkce()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        Environment.SetEnvironmentVariable("SAFEGUARD_PROVIDER", "local");
        Environment.SetEnvironmentVariable("SAFEGUARD_USER", "admin");
        Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", "secret");

        var factory = new FakeConnectionFactory();
        var manager = CreateManager(factory);

        await manager.EnsureAuthenticatedAsync(server: null, host: TestHost, ct: CancellationToken.None, ignoreSsl: false);

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
        Environment.SetEnvironmentVariable("SAFEGUARD_USER", "admin");
        Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", "secret");

        var factory = new FakeConnectionFactory();
        var manager = CreateManager(factory);

        await manager.EnsureAuthenticatedAsync(server: null, host: TestHost, ct: CancellationToken.None, ignoreSsl: false);

        Assert.Equal(1, factory.DeviceCodeCallCount);
        Assert.Equal(0, factory.PkceCallCount);
    }

    [Fact]
    public async Task EnsureAuthenticated_DeviceCodeDisplayCallback_LogsUrlAndCode()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        var info = new DeviceCodeInfo
        {
            VerificationUri = "https://safeguard.example.test/RSTS/Login/DeviceCode",
            UserCode = "ABCD-1234",
            ExpiresIn = 600,
        };
        var factory = new FakeConnectionFactory { DeviceCodeInfoToFire = info };
        var capturingLogger = new CapturingLogger<SafeguardConnectionManager>();
        var manager = CreateManager(factory, connectionLogger: capturingLogger);

        await manager.EnsureAuthenticatedAsync(server: null, host: TestHost, ct: CancellationToken.None, ignoreSsl: false);

        Assert.Contains(capturingLogger.Messages, m =>
            m.Contains("Device code for") &&
            m.Contains(info.VerificationUri) &&
            m.Contains(info.UserCode));
    }

    [Fact]
    public async Task EnsureAuthenticated_DeviceCodeFails_WrapsAsMcpException()
    {
        using var _scope = EnvVarScope.ClearSafeguardAuthVars();
        var factory = new FakeConnectionFactory
        {
            DeviceCodeException = new SafeguardDotNetException("device code grant disabled"),
        };
        var manager = CreateManager(factory);

        var ex = await Assert.ThrowsAsync<McpException>(
            () => manager.EnsureAuthenticatedAsync(server: null, host: TestHost, ct: CancellationToken.None, ignoreSsl: false));

        Assert.Contains("Authentication failed", ex.Message);
        Assert.Contains("PKCE fallback", ex.Message);
    }

    private static SafeguardConnectionManager CreateManager(
        FakeConnectionFactory factory,
        ILogger<SafeguardConnectionManager> connectionLogger = null)
    {
        var config = new ConfigurationBuilder().Build();
        var catalogLoader = new CatalogLoader(NullLogger<CatalogLoader>.Instance);
        var catalogProvider = new CatalogProvider(catalogLoader, NullLogger<CatalogProvider>.Instance);
        var logger = connectionLogger ?? NullLogger<SafeguardConnectionManager>.Instance;
        return new SafeguardConnectionManager(logger, config, catalogProvider, factory);
    }

    private sealed class EnvVarScope : IDisposable
    {
        private readonly Dictionary<string, string> _saved = new();
        private static readonly string[] AuthVars =
        {
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
