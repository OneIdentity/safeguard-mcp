#nullable disable

using System.Collections.Generic;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Phase 2 task 2.1 — verifies <see cref="BridgeOptions.Parse"/>
/// against HTTP-AUTH-RELAY-PLAN §2.1.
///
/// <list type="bullet">
///   <item>No bridge env vars → result is inactive, no error.</item>
///   <item><c>MCP_PUBLIC_URL</c> set but <c>RSTS_CLIENT_ID</c> missing
///   → fail-fast error naming the missing var.</item>
///   <item>Non-absolute URI in either env var → fail-fast error.</item>
///   <item>TTL out of range / unparseable → fail-fast error citing
///   the 1..300 bound and rSTS's 5-minute auth-code TTL.</item>
///   <item>Valid configuration → populated options with derived
///   endpoint URLs and normalized public URL (no trailing slash).</item>
/// </list>
/// </summary>
public class BridgeOptionsTests
{
    private static Func<string, string> Env(params (string Name, string Value)[] vars)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in vars)
            map[name] = value;
        return name => map.TryGetValue(name, out var v) ? v : null;
    }

    [Fact]
    public void Parse_NoEnvVars_ReturnsInactive()
    {
        var result = BridgeOptions.Parse(Env());
        Assert.False(result.IsActive);
        Assert.Null(result.Options);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Parse_McpPublicUrlBlank_TreatedAsUnset()
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "   "),
            ("RSTS_CLIENT_ID", "https://rsts.example.test/bridge")));
        Assert.False(result.IsActive);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.test")]
    [InlineData("/no-scheme")]
    public void Parse_McpPublicUrlNotAbsoluteHttp_FailsFast(string bad)
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", bad),
            ("RSTS_CLIENT_ID", "https://rsts.example.test/bridge"),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.False(result.IsActive);
        Assert.NotNull(result.Error);
        Assert.Contains("MCP_PUBLIC_URL", result.Error);
        Assert.Contains("absolute", result.Error);
    }

    [Fact]
    public void Parse_McpPublicUrlSetButRstsClientIdMissing_FailsFast()
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test"),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.NotNull(result.Error);
        Assert.Contains("RSTS_CLIENT_ID", result.Error);
        Assert.Contains("required", result.Error);
    }

    [Fact]
    public void Parse_RstsClientIdNotAbsoluteUri_FailsFast()
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test"),
            ("RSTS_CLIENT_ID", "bridge-realm"),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.NotNull(result.Error);
        Assert.Contains("RSTS_CLIENT_ID", result.Error);
        Assert.Contains("absolute URI", result.Error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("301")]
    [InlineData("not-a-number")]
    public void Parse_TtlOutOfRange_FailsFast(string ttl)
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test"),
            ("RSTS_CLIENT_ID", "https://rsts.example.test/bridge"),
            ("SAFEGUARD_HOST", "appliance.example.test"),
            ("BRIDGE_AUTH_CODE_TTL_SECONDS", ttl)));
        Assert.NotNull(result.Error);
        Assert.Contains("BRIDGE_AUTH_CODE_TTL_SECONDS", result.Error);
        Assert.Contains("300", result.Error);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("60")]
    [InlineData("300")]
    public void Parse_TtlInRange_Accepted(string ttl)
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test"),
            ("RSTS_CLIENT_ID", "https://rsts.example.test/bridge"),
            ("SAFEGUARD_HOST", "appliance.example.test"),
            ("BRIDGE_AUTH_CODE_TTL_SECONDS", ttl)));
        Assert.Null(result.Error);
        Assert.True(result.IsActive);
        Assert.Equal(int.Parse(ttl), result.Options.AuthCodeTtlSeconds);
    }

    [Fact]
    public void Parse_TtlOmitted_DefaultsTo60()
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test"),
            ("RSTS_CLIENT_ID", "https://rsts.example.test/bridge"),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.True(result.IsActive);
        Assert.Equal(60, result.Options.AuthCodeTtlSeconds);
    }

    [Fact]
    public void Parse_ValidConfig_ProducesDerivedEndpoints()
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test"),
            ("RSTS_CLIENT_ID", "https://rsts.example.test/bridge"),
            ("SAFEGUARD_HOST", "appliance.example.test"),
            ("SAFEGUARD_IGNORE_SSL", "true")));

        Assert.True(result.IsActive);
        var opts = result.Options;
        Assert.Equal("https://mcp.example.test", opts.McpPublicUrl);
        Assert.Equal("https://rsts.example.test/bridge", opts.RstsClientId);
        Assert.Equal("appliance.example.test", opts.SafeguardHost);
        Assert.True(opts.IgnoreSsl);

        Assert.Equal("https://mcp.example.test/authorize", opts.AuthorizeEndpoint);
        Assert.Equal("https://mcp.example.test/authorize/callback", opts.AuthorizeCallbackEndpoint);
        Assert.Equal("https://mcp.example.test/token", opts.TokenEndpoint);
        Assert.Equal("https://mcp.example.test/register", opts.RegistrationEndpoint);
        Assert.Equal(
            "https://mcp.example.test/.well-known/oauth-protected-resource",
            opts.ProtectedResourceMetadataUrl);
        Assert.Equal(
            "https://mcp.example.test/.well-known/oauth-authorization-server",
            opts.AuthorizationServerMetadataUrl);
    }

    [Fact]
    public void Parse_McpPublicUrlWithTrailingSlash_IsNormalized()
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test/"),
            ("RSTS_CLIENT_ID", "https://rsts.example.test/bridge"),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.True(result.IsActive);
        Assert.Equal("https://mcp.example.test", result.Options.McpPublicUrl);
        Assert.Equal("https://mcp.example.test/authorize", result.Options.AuthorizeEndpoint);
    }
}
