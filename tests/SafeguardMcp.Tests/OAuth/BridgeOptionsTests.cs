#nullable disable

using System.Collections.Generic;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Verifies <see cref="BridgeOptions.Parse"/> startup behavior.
///
/// <list type="bullet">
///   <item>No bridge env vars in HTTP mode → result is active with no
///   override pinning (URLs inferred per-request by
///   <see cref="BridgeUrlResolver"/>).</item>
///   <item><c>BRIDGE_DISABLED=true</c> → result is inactive,
///   regardless of any other vars.</item>
///   <item><c>MCP_PUBLIC_URL</c> + <c>RSTS_CLIENT_ID</c> set →
///   overrides populated and validated.</item>
///   <item><c>MCP_PUBLIC_URL</c> set without <c>RSTS_CLIENT_ID</c> →
///   accepted; resolver defaults <c>client_id</c> to the public URL
///   override.</item>
///   <item>Non-absolute URI override → fail-fast error.</item>
///   <item>TTL out of range / unparseable → fail-fast error citing
///   the 1..300 bound and rSTS's 5-minute auth-code TTL.</item>
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
    public void Parse_NoBridgeEnvVars_ReturnsActiveWithNoOverrides()
    {
        var result = BridgeOptions.Parse(Env(
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.True(result.IsActive);
        Assert.Null(result.Error);
        Assert.Null(result.Options.OverridePublicUrl);
        Assert.Null(result.Options.OverrideClientId);
    }

    [Fact]
    public void Parse_McpPublicUrlBlank_TreatedAsUnset()
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "   "),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.True(result.IsActive);
        Assert.Null(result.Error);
        Assert.Null(result.Options.OverridePublicUrl);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.test")]
    [InlineData("/no-scheme")]
    public void Parse_McpPublicUrlNotAbsoluteHttp_FailsFast(string bad)
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", bad),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.False(result.IsActive);
        Assert.NotNull(result.Error);
        Assert.Contains("MCP_PUBLIC_URL", result.Error);
        Assert.Contains("absolute", result.Error);
    }

    [Fact]
    public void Parse_McpPublicUrlSetWithoutRstsClientId_FallsBackToPublicUrl()
    {
        // New behavior: RSTS_CLIENT_ID is no longer required when
        // MCP_PUBLIC_URL is set. Resolver defaults client_id to the
        // public URL override.
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test"),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.True(result.IsActive);
        Assert.Null(result.Error);
        Assert.Equal("https://mcp.example.test", result.Options.OverridePublicUrl);
        Assert.Null(result.Options.OverrideClientId);
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
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.True(result.IsActive);
        Assert.Equal(60, result.Options.AuthCodeTtlSeconds);
    }

    [Fact]
    public void Parse_OverridesPopulatedFromEnv()
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test"),
            ("RSTS_CLIENT_ID", "https://rsts.example.test/bridge"),
            ("SAFEGUARD_HOST", "appliance.example.test"),
            ("SAFEGUARD_IGNORE_SSL", "true")));

        Assert.True(result.IsActive);
        var opts = result.Options;
        Assert.Equal("https://mcp.example.test", opts.OverridePublicUrl);
        Assert.Equal("https://rsts.example.test/bridge", opts.OverrideClientId);
        Assert.Equal("appliance.example.test", opts.SafeguardHost);
        Assert.True(opts.IgnoreSsl);
    }

    [Fact]
    public void Parse_McpPublicUrlWithTrailingSlash_IsNormalized()
    {
        var result = BridgeOptions.Parse(Env(
            ("MCP_PUBLIC_URL", "https://mcp.example.test/"),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.True(result.IsActive);
        Assert.Equal("https://mcp.example.test", result.Options.OverridePublicUrl);
    }

    [Fact]
    public void Parse_BridgeDisabledTrue_ReturnsInactive()
    {
        // BRIDGE_DISABLED=true wins regardless of other vars.
        var result = BridgeOptions.Parse(Env(
            ("BRIDGE_DISABLED", "true"),
            ("MCP_PUBLIC_URL", "https://mcp.example.test"),
            ("RSTS_CLIENT_ID", "https://mcp.example.test"),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.False(result.IsActive);
        Assert.Null(result.Error);
        Assert.Null(result.Options);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("FALSE")]
    [InlineData("0")]
    [InlineData("")]
    public void Parse_BridgeDisabledFalsy_ReturnsActive(string value)
    {
        var result = BridgeOptions.Parse(Env(
            ("BRIDGE_DISABLED", value),
            ("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.True(result.IsActive);
    }
}
