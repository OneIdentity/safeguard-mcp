#nullable disable

using System.Net;
using System.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using OneIdentity.SafeguardDotNet;
using OneIdentity.SafeguardDotNet.Event;
using OneIdentity.SafeguardDotNet.Sps;
using SafeguardMcp.Tools;
using DeviceCodeLoginParameters = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLoginParameters;

namespace SafeguardMcp.Tests;

/// <summary>
/// Pins the exact error-mapping wording presented to MCP clients so
/// it cannot drift silently:
///
/// <list type="bullet">
///   <item><b>Missing bearer (HTTP):</b> "Not authenticated against
///   Safeguard. Acquire a Safeguard user token (e.g.,
///   <c>safeguard-mcp login</c>) and configure your client to send
///   <c>Authorization: Bearer &lt;token&gt;</c>."</item>
///   <item><b>401 from appliance (HTTP):</b> "Safeguard token has
///   expired or been revoked. Re-acquire (<c>safeguard-mcp login</c> or
///   your MCP client's OAuth flow) and retry."</item>
/// </list>
///
/// The wording is exercised through the real runtime paths
/// (<see cref="HttpRelaySafeguardSession.EnsureReadyAsync"/> and
/// <see cref="HttpRelaySafeguardSession.ExecuteWithConnectionAsync{T}"/>)
/// so a future refactor that swaps the strings out is caught.
/// </summary>
[Collection("EnvVars")]
public class ErrorMappingWordingTests
{
    private const string TestHost = "safeguard.example.test";

    private const string MissingBearerExpected =
        "Not authenticated against Safeguard. Acquire a Safeguard user token "
        + "(e.g., `safeguard-mcp login`) and configure your client to send "
        + "`Authorization: Bearer <token>`.";

    private const string Expired401Expected =
        "Safeguard token has expired or been revoked. Re-acquire "
        + "(`safeguard-mcp login` or your MCP client's OAuth flow) and retry.";

    public ErrorMappingWordingTests()
    {
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", TestHost);
        Environment.SetEnvironmentVariable("SAFEGUARD_IGNORE_SSL", "false");
    }

    [Fact]
    public async Task EnsureReadyAsync_WithoutBearer_UsesExactMissingBearerWording()
    {
        var config = new ConfigurationBuilder().Build();
        using var session = new HttpRelaySafeguardSession(
            NullLogger<HttpRelaySafeguardSession>.Instance,
            config,
            new HttpContextAccessor(),
            new ThrowingConnectionFactory());

        var ex = await Assert.ThrowsAsync<McpException>(
            () => session.EnsureReadyAsync(server: null));
        Assert.Equal(MissingBearerExpected, ex.Message);
    }

    [Fact]
    public async Task ExecuteWithConnectionAsync_WithoutBearer_UsesExactMissingBearerWording()
    {
        var config = new ConfigurationBuilder().Build();
        using var session = new HttpRelaySafeguardSession(
            NullLogger<HttpRelaySafeguardSession>.Instance,
            config,
            new HttpContextAccessor(),
            new ThrowingConnectionFactory());

        var ex = await Assert.ThrowsAsync<McpException>(
            () => session.ExecuteWithConnectionAsync(_ => Task.FromResult(0)));
        Assert.Equal(MissingBearerExpected, ex.Message);
    }

    [Fact]
    public async Task ExecuteWithConnectionAsync_When401FromAppliance_UsesExactExpiredWording()
    {
        var config = new ConfigurationBuilder().Build();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Authorization"] = "Bearer expired.token.value";
        var accessor = new HttpContextAccessor { HttpContext = ctx };

        using var session = new HttpRelaySafeguardSession(
            NullLogger<HttpRelaySafeguardSession>.Instance,
            config,
            accessor,
            new ThrowingConnectionFactory());

        var ex = await Assert.ThrowsAsync<McpException>(
            () => session.ExecuteWithConnectionAsync<int>(_ =>
                throw new SafeguardDotNetException("simulated", HttpStatusCode.Unauthorized, response: "")));
        Assert.Equal(Expired401Expected, ex.Message);
    }

    private sealed class ThrowingConnectionFactory : ISafeguardConnectionFactory
    {
        public Task<ISafeguardConnection> ConnectDeviceCodeAsync(
            string host, DeviceCodeLoginParameters parameters, bool ignoreSsl, CancellationToken ct)
            => throw new NotSupportedException();

        public ISafeguardConnection ConnectPkce(
            string host, string provider, string user, SecureString password, bool ignoreSsl)
            => throw new NotSupportedException();

        public ISafeguardConnection ConnectWithAccessToken(
            string host, SecureString accessToken, int apiVersion, bool ignoreSsl)
            => new NoopConnection();
    }

    private sealed class NoopConnection : ISafeguardConnection
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
