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
using SafeguardMcp;
using SafeguardMcp.Tools;
using DeviceCodeLoginParameters = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLoginParameters;

namespace SafeguardMcp.Tests;

/// <summary>
/// Pins the exact error-mapping wording presented to MCP clients so
/// it cannot drift silently. Every HTTP-mode auth-failure message
/// must lead with the MCP client's OAuth flow (the documented primary
/// per <c>README.md</c>) and reference <c>safeguard-mcp login</c>
/// only as a fallback for clients without OAuth discovery and for
/// scripts/CI. The tests assert both substrings are present AND that
/// the OAuth-flow phrase precedes the CLI fallback so future drift
/// can't quietly invert the recommendation.
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

    private const string MissingBearerExpected = HttpModeMessages.NotAuthenticated;

    private const string Expired401Expected = HttpModeMessages.TokenExpired;

    private const string PrimaryPhrase = "MCP client's OAuth flow";
    private const string FallbackPhrase = "safeguard-mcp login";

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
        AssertOAuthFlowLeadsCliFallback(ex.Message);
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
        AssertOAuthFlowLeadsCliFallback(ex.Message);
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
        AssertOAuthFlowLeadsCliFallback(ex.Message);
    }

    private static void AssertOAuthFlowLeadsCliFallback(string message)
    {
        var primary = message.IndexOf(PrimaryPhrase, StringComparison.Ordinal);
        var fallback = message.IndexOf(FallbackPhrase, StringComparison.Ordinal);
        Assert.True(primary >= 0, $"Expected '{PrimaryPhrase}' in: {message}");
        Assert.True(fallback >= 0, $"Expected '{FallbackPhrase}' in: {message}");
        Assert.True(
            primary < fallback,
            $"Expected '{PrimaryPhrase}' (primary) to precede '{FallbackPhrase}' (fallback) in: {message}");
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
