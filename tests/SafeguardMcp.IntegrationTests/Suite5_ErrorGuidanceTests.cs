using Microsoft.Extensions.Configuration;
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
        _fixture.RequireAvailable();

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
        _fixture.RequireAvailable();

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
        _fixture.RequireAvailable();

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
        _fixture.RequireAvailable();

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

    [Fact]
    public async Task Error_InsufficientPermissions_Returns403Guidance()
    {
        _fixture.RequireAvailable();

        // Create a limited user (Auditor only — cannot create/modify users)
        var limitedUserName = "McpTest_Limited";
        var password = Environment.GetEnvironmentVariable("SPP_PASSWORD");

        var createBody = System.Text.Json.JsonSerializer.Serialize(new
        {
            Name = limitedUserName,
            AdminRoles = new[] { "Auditor" },
            PrimaryAuthenticationProvider = new { Id = -1 },
        });

        var createResponse = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, OneIdentity.SafeguardDotNet.Service.Core, "POST", "Users", body: createBody);
        using var doc = System.Text.Json.JsonDocument.Parse(createResponse.Body);
        var limitedUserId = doc.RootElement.GetProperty("Id").GetInt32();

        try
        {
            // Set the password so we can authenticate as this user
            var passwordBody = System.Text.Json.JsonSerializer.Serialize(password);
            await _fixture.ConnectionManager.InvokeAsync(
                _fixture.Host, OneIdentity.SafeguardDotNet.Service.Core, "PUT",
                $"Users/{limitedUserId}/Password", body: passwordBody);

            // Authenticate as the limited user
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Safeguard:IgnoreSsl"] = Environment.GetEnvironmentVariable("SAFEGUARD_IGNORE_SSL") ?? "false",
                })
                .Build();
            var catalogLoader = new SafeguardMcp.Catalog.CatalogLoader(
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Catalog.CatalogLoader>());
            var catalogProvider = new SafeguardMcp.Catalog.CatalogProvider(
                catalogLoader, new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Catalog.CatalogProvider>());
            using var limitedMgr = new SafeguardMcp.Tools.SafeguardConnectionManager(
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Tools.SafeguardConnectionManager>(),
                config, catalogProvider);

            Environment.SetEnvironmentVariable("SAFEGUARD_USER", limitedUserName);
            Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", password);
            await limitedMgr.EnsureAuthenticatedAsync(null, _fixture.Host, System.Threading.CancellationToken.None);

            // Attempt to create a user — Auditor role does not have UserAdmin permission
            var tool = new SafeguardMcp.Tools.SafeguardApiTool(limitedMgr, catalogProvider, config);
            string response;
            try
            {
                response = await tool.Safeguard_Execute(null,
                    method: "POST", path: "/v4/Users",
                    body: "{\"Name\":\"ShouldFail\"}", host: _fixture.Host);
            }
            catch (Exception ex)
            {
                response = ex.Message;
            }

            Assert.True(
                response.Contains("403", StringComparison.OrdinalIgnoreCase)
                || response.Contains("permission", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Insufficient", StringComparison.OrdinalIgnoreCase)
                || response.Contains("Authorization", StringComparison.OrdinalIgnoreCase),
                $"Expected 403/permission error, got: {response}");
        }
        finally
        {
            // Restore env vars for the main fixture's user
            Environment.SetEnvironmentVariable("SAFEGUARD_USER", "McpTest_Admin");
            Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", password);

            // Clean up the limited user
            try
            {
                await _fixture.ConnectionManager.InvokeAsync(
                    _fixture.Host, OneIdentity.SafeguardDotNet.Service.Core, "DELETE", $"Users/{limitedUserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Failed to delete limited user: {ex.Message}");
            }
        }
    }

    [Fact]
    public async Task Error_InvalidProjectionField_Returns400WithFieldName()
    {
        _fixture.RequireAvailable();

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
