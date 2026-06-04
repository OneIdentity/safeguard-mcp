using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Tools;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Tests that verify we can authenticate and maintain a connection to a real appliance.
/// </summary>
[Collection("Appliance")]
public class ConnectionTests
{
    private readonly ApplianceFixture _fixture;

    public ConnectionTests(ApplianceFixture fixture) => _fixture = fixture;

    [RequiresApplianceFact]
    public void Connect_Authenticates_Successfully()
    {
        Assert.True(_fixture.Available, "Connection should have succeeded");
        Assert.Contains(_fixture.Host, _fixture.ConnectionManager.ConnectedHosts);
    }

    [RequiresApplianceFact]
    public async Task Execute_GetMe_ReturnsCurrentUser()
    {
        // SDK expects relative URL without /v4/ prefix
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, Service.Core, "GET", "Me");

        Assert.NotNull(response);
        Assert.Equal(200, (int)response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(response.Body),
            "GET Me should return a non-empty response body");
        // /v4/Me returns the user's profile with a Name field
        Assert.Contains("Name", response.Body);
    }

    [RequiresApplianceFact]
    public async Task Execute_GetApplianceStatus_RoutesToApplianceService()
    {
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, Service.Appliance, "GET", "ApplianceStatus");

        Assert.NotNull(response);
        Assert.Equal(200, (int)response.StatusCode);
        // ApplianceStatus response contains appliance state information
        Assert.False(string.IsNullOrWhiteSpace(response.Body));
    }

    [RequiresApplianceFact]
    public async Task Execute_GetUsers_ReturnsCollection()
    {
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, Service.Core, "GET", "Users",
            parameters: new Dictionary<string, string> { ["limit"] = "5" });

        Assert.NotNull(response);
        Assert.Equal(200, (int)response.StatusCode);
        // Should be a JSON array
        Assert.StartsWith("[", response.Body.TrimStart());
    }

    [RequiresApplianceFact]
    public async Task Execute_InvalidPath_ThrowsMcpException()
    {
        // The SDK throws SafeguardDotNetException on 404, which ConnectionManager
        // wraps into McpException
        var ex = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(() =>
            _fixture.ConnectionManager.InvokeAsync(
                _fixture.Host, Service.Core, "GET", "NonExistentEndpoint12345"));

        Assert.Contains("404", ex.Message);
    }
}
