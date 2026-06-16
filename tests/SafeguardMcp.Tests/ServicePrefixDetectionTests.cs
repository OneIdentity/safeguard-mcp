using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Tests for the /service/{name}/ prefix detection used by Execute, Schema, and QueryHelp
/// to short-circuit the malformed path before it reaches the upstream API.
/// </summary>
public class ServicePrefixDetectionTests
{
    [Theory]
    [InlineData("/service/appliance/v4/SystemTime", "/v4/SystemTime", "Appliance")]
    [InlineData("/service/core/v4/Me", "/v4/Me", "Core")]
    [InlineData("/service/notification/v4/Status", "/v4/Status", "Notification")]
    [InlineData("/service/a2a/v4/Credentials", "/v4/Credentials", "A2A")]
    [InlineData("/service/Appliance/v4/SystemTime", "/v4/SystemTime", "Appliance")]
    [InlineData("/SERVICE/APPLIANCE/v4/SystemTime", "/v4/SystemTime", "Appliance")]
    [InlineData("service/appliance/v4/SystemTime", "/v4/SystemTime", "Appliance")]
    [InlineData("  /service/appliance/v4/SystemTime  ", "/v4/SystemTime", "Appliance")]
    [InlineData("/service/appliance/v4/SystemTime?fields=Id", "/v4/SystemTime", "Appliance")]
    public void TryDetectServicePrefix_MatchesAndStrips(string input, string expectedStripped, string expectedService)
    {
        var hit = ApiToolHelpers.TryDetectServicePrefix(input, out var stripped, out var service);

        Assert.True(hit);
        Assert.Equal(expectedStripped, stripped);
        Assert.Equal(expectedService, service);
    }

    [Theory]
    [InlineData("/v4/SystemTime")]
    [InlineData("/v4/Users")]
    [InlineData("/services/appliance/v4/SystemTime")]   // plural
    [InlineData("/v4/service/appliance/foo")]            // service segment but not in position 0
    [InlineData("/service/notarealservice/v4/X")]        // unknown service
    [InlineData("/service")]                             // single segment
    [InlineData("")]
    public void TryDetectServicePrefix_DoesNotMatchInvalidShapes(string input)
    {
        var hit = ApiToolHelpers.TryDetectServicePrefix(input, out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryDetectServicePrefix_NullInput_ReturnsFalse()
    {
        var hit = ApiToolHelpers.TryDetectServicePrefix(null!, out _, out _);

        Assert.False(hit);
    }

    [Fact]
    public void TryDetectServicePrefix_PrefixOnlyNoPath_ReturnsRootStripped()
    {
        // /service/appliance with no /v4/... path is still a malformed call we
        // should catch and redirect; stripped path is "/" so the directive can
        // tell the agent that /v4/... is required.
        var hit = ApiToolHelpers.TryDetectServicePrefix("/service/appliance", out var stripped, out var service);

        Assert.True(hit);
        Assert.Equal("/", stripped);
        Assert.Equal("Appliance", service);
    }

    [Fact]
    public void BuildServicePrefixDirective_NamesCorrectedPathAndService()
    {
        var directive = ApiToolHelpers.BuildServicePrefixDirective(
            "/service/appliance/v4/SystemTime", "/v4/SystemTime", "Appliance");

        Assert.Contains("path=\"/v4/SystemTime\"", directive);
        Assert.Contains("Appliance", directive);
        Assert.Contains("/service/{name}/", directive);
    }
}
