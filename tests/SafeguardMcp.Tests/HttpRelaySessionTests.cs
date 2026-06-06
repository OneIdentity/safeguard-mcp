#nullable disable

using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OneIdentity.SafeguardDotNet;
using OneIdentity.SafeguardDotNet.Event;
using OneIdentity.SafeguardDotNet.Sps;
using SafeguardMcp.Tools;
using DeviceCodeLoginParameters = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLoginParameters;

namespace SafeguardMcp.Tests;

/// <summary>
/// Phase 1 verification tests for <see cref="HttpRelaySafeguardSession"/>.
/// Covers tasks 1.A (two concurrent sessions with distinct bearers never
/// cross-contaminate) and 1.B (after the session is disposed no Safeguard
/// access-token state is retained on the heap).
/// </summary>
[Collection("EnvVars")]
public class HttpRelaySessionTests
{
    private const string TestHost = "safeguard.example.test";

    public HttpRelaySessionTests()
    {
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", TestHost);
        Environment.SetEnvironmentVariable("SAFEGUARD_IGNORE_SSL", "false");
    }

    // -------- 1.A: two-bearer concurrency --------

    [Fact]
    public async Task TwoConcurrentSessions_WithDistinctBearers_NeverCrossContaminate()
    {
        var factory = new RecordingConnectionFactory();
        var config = new ConfigurationBuilder().Build();

        const int iterations = 32;
        var tasks = new Task[iterations * 2];

        for (var i = 0; i < iterations; i++)
        {
            var idA = i;
            tasks[i * 2] = Task.Run(async () =>
            {
                var bearer = $"token-A-{idA}";
                using var session = BuildSession(config, factory, bearer);
                await session.ExecuteWithConnectionAsync(c =>
                {
                    var observed = ((RecordingConnection)c).BearerLiteral;
                    Assert.Equal(bearer, observed);
                    return Task.FromResult(0);
                });
            });

            var idB = i;
            tasks[i * 2 + 1] = Task.Run(async () =>
            {
                var bearer = $"token-B-{idB}";
                using var session = BuildSession(config, factory, bearer);
                await session.ExecuteWithConnectionAsync(c =>
                {
                    var observed = ((RecordingConnection)c).BearerLiteral;
                    Assert.Equal(bearer, observed);
                    return Task.FromResult(0);
                });
            });
        }

        await Task.WhenAll(tasks);

        // Every connection saw exactly the bearer it was constructed with.
        Assert.Equal(iterations * 2, factory.Connections.Count);
        Assert.Equal(
            factory.Connections.Count,
            factory.Connections.Select(c => c.BearerLiteral).Distinct().Count());
    }

    // -------- 1.B: no-state heap-shape --------

    [Fact]
    public async Task AfterDispose_SessionHasNoCachedConnectionAndNoBearerState()
    {
        var factory = new RecordingConnectionFactory();
        var config = new ConfigurationBuilder().Build();
        var session = BuildSession(config, factory, "bearer-heap-shape-1");

        await session.ExecuteWithConnectionAsync(_ => Task.FromResult(0));
        Assert.True(session.HasCredentials);

        session.Dispose();

        // Reflectively verify no ISafeguardConnection field remains set —
        // pure relay means nothing should outlive the request scope.
        var connectionField = typeof(HttpRelaySafeguardSession).GetField(
            "_connection", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(connectionField);
        Assert.Null(connectionField.GetValue(session));

        // No string/SecureString-typed instance field should hold a token.
        foreach (var f in typeof(HttpRelaySafeguardSession).GetFields(
            BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (f.FieldType == typeof(string))
            {
                var v = (string)f.GetValue(session);
                if (v != null)
                    Assert.DoesNotContain("bearer-heap-shape", v);
            }
            else if (f.FieldType == typeof(SecureString))
            {
                Assert.Null(f.GetValue(session));
            }
        }
    }

    [Fact]
    public async Task NoHttpContext_ExecuteThrowsNotAuthenticated()
    {
        var factory = new RecordingConnectionFactory();
        var config = new ConfigurationBuilder().Build();
        using var session = new HttpRelaySafeguardSession(
            NullLogger<HttpRelaySafeguardSession>.Instance,
            config,
            new HttpContextAccessor(),
            factory);

        var ex = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(
            () => session.ExecuteWithConnectionAsync(_ => Task.FromResult(0)));
        Assert.Contains("Not authenticated", ex.Message);
        Assert.Empty(factory.Connections);
    }

    // -------- helpers --------

    private static HttpRelaySafeguardSession BuildSession(
        IConfiguration config,
        RecordingConnectionFactory factory,
        string bearer)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Authorization"] = $"Bearer {bearer}";
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        return new HttpRelaySafeguardSession(
            NullLogger<HttpRelaySafeguardSession>.Instance,
            config,
            accessor,
            factory);
    }

    private static string SecureStringToLiteral(SecureString secure)
    {
        if (secure == null) return null;
        var ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(secure);
            return Marshal.PtrToStringUni(ptr);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
        }
    }

    private sealed class RecordingConnectionFactory : ISafeguardConnectionFactory
    {
        public List<RecordingConnection> Connections { get; } = new();
        private readonly object _lock = new();

        public Task<ISafeguardConnection> ConnectDeviceCodeAsync(
            string host, DeviceCodeLoginParameters parameters, bool ignoreSsl, CancellationToken ct)
            => throw new NotSupportedException();

        public ISafeguardConnection ConnectPkce(
            string host, string provider, string user, SecureString password, bool ignoreSsl)
            => throw new NotSupportedException();

        public ISafeguardConnection ConnectWithAccessToken(
            string host, SecureString accessToken, int apiVersion, bool ignoreSsl)
        {
            // Snapshot the bearer the SDK would have received.
            var literal = SecureStringToLiteral(accessToken);
            var conn = new RecordingConnection(host, literal);
            lock (_lock)
                Connections.Add(conn);
            return conn;
        }
    }

    private sealed class RecordingConnection : ISafeguardConnection
    {
        public string Host { get; }
        public string BearerLiteral { get; }

        public RecordingConnection(string host, string bearerLiteral)
        {
            Host = host;
            BearerLiteral = bearerLiteral;
        }

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
