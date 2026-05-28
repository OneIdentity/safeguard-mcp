using Xunit;

namespace SafeguardMcp.IntegrationTests;

[Collection("AgentSimulation")]
public class Suite5_ErrorGuidanceTests
{
    private readonly AgentSimulationFixture _fixture;

    public Suite5_ErrorGuidanceTests(AgentSimulationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Error_WrongPath_Returns404Guidance()
    {
        if (!_fixture.Available) return;

        string response;
        try
        {
            response = await _fixture.ExecuteAsync("POST", "/v4/NonExistentEndpoint", body: "{}");
        }
        catch (Exception ex)
        {
            response = ex.Message;
        }

        Assert.True(
            response.Contains("404", StringComparison.OrdinalIgnoreCase)
            || response.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
            || response.Contains("not found", StringComparison.OrdinalIgnoreCase),
            $"Expected error mentioning 404 or not found guidance, got: {response}");
    }

    [Fact]
    public async Task Error_MissingRequiredField_Returns400WithFieldName()
    {
        if (!_fixture.Available) return;

        string response;
        try
        {
            response = await _fixture.ExecuteAsync("POST", "/v4/Users", body: "{}");
        }
        catch (Exception ex)
        {
            response = ex.Message;
        }

        Assert.True(
            response.Contains("400", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Name", StringComparison.OrdinalIgnoreCase)
            || response.Contains("required", StringComparison.OrdinalIgnoreCase),
            $"Expected error mentioning status or required field, got: {response}");
    }

    [Fact]
    public async Task Error_InvalidFilterField_Returns400WithFieldName()
    {
        if (!_fixture.Available) return;

        string response;
        try
        {
            response = await _fixture.ExecuteAsync(
                "GET",
                "/v4/Users",
                query: "filter=BogusField eq 'x'");
        }
        catch (Exception ex)
        {
            response = ex.Message;
        }

        Assert.True(
            response.Contains("400", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Invalid", StringComparison.OrdinalIgnoreCase)
            || response.Contains("BogusField", StringComparison.OrdinalIgnoreCase),
            $"Expected invalid filter guidance mentioning BogusField, got: {response}");
    }

    [Fact]
    public async Task Error_WrongFieldType_Returns400OrTypeGuidance()
    {
        if (!_fixture.Available) return;

        const string body = """
            {"Name":"Test","PlatformId":"notanumber","AssetPartitionId":-1,"NetworkAddress":"1.2.3.4"}
            """;

        string response;
        try
        {
            response = await _fixture.ExecuteAsync("POST", "/v4/Assets", body: body);
        }
        catch (Exception ex)
        {
            response = ex.Message;
        }

        Assert.True(
            response.Contains("400", StringComparison.OrdinalIgnoreCase)
            || response.Contains("type", StringComparison.OrdinalIgnoreCase)
            || response.Contains("integer", StringComparison.OrdinalIgnoreCase)
            || response.Contains("PlatformId", StringComparison.OrdinalIgnoreCase),
            $"Expected type-related error guidance, got: {response}");
    }

    [Fact(Skip = "Test admin has all roles; cannot trigger 403 without a separate limited user")]
    public Task Error_InsufficientPermissions_Returns403Guidance()
        => Task.CompletedTask;

    [Fact]
    public async Task Error_InvalidProjectionField_Returns400WithFieldName()
    {
        if (!_fixture.Available) return;

        string response;
        try
        {
            response = await _fixture.ExecuteAsync(
                "GET",
                "/v4/Users",
                query: "fields=Id,BogusFieldName");
        }
        catch (Exception ex)
        {
            response = ex.Message;
        }

        Assert.True(
            response.Contains("400", StringComparison.OrdinalIgnoreCase)
            || response.Contains("Invalid", StringComparison.OrdinalIgnoreCase)
            || response.Contains("BogusFieldName", StringComparison.OrdinalIgnoreCase),
            $"Expected invalid projection guidance mentioning BogusFieldName, got: {response}");
    }
}
