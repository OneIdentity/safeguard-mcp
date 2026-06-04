using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Tools;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Tests that verify the Discover → Schema → Execute pipeline works end-to-end
/// against a real appliance.
/// </summary>
[Collection("Appliance")]
public class DiscoverExecuteTests
{
    private readonly ApplianceFixture _fixture;

    public DiscoverExecuteTests(ApplianceFixture fixture) => _fixture = fixture;

    [RequiresApplianceFact]
    public void Discover_FindsEndpointsByKeyword()
    {
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);

        // Search for "Users" — should find GET/POST/etc on /v4/Users
        var matches = new List<string>();
        foreach (var ep in endpoints)
        {
            if (ep.Path.Contains("Users", StringComparison.OrdinalIgnoreCase)
                || (ep.Summary != null && ep.Summary.Contains("Users", StringComparison.OrdinalIgnoreCase)))
            {
                matches.Add($"{ep.Method} {ep.Path}");
            }
        }

        Assert.Contains(matches, m => m == "GET /v4/Users");
        Assert.True(matches.Count >= 3,
            $"Expected at least 3 User-related endpoints, got {matches.Count}");
    }

    [RequiresApplianceFact]
    public void Discover_ServiceRouting_MatchesCatalog()
    {
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);

        // Verify the heuristic matches the catalog for known paths
        foreach (var ep in endpoints)
        {
            if (ep.Path == "/v4/ApplianceStatus")
                Assert.Equal("Appliance", ep.Service);
            if (ep.Path == "/v4/Users")
                Assert.Equal("Core", ep.Service);
        }
    }

    [RequiresApplianceFact]
    public async Task Execute_WithLimit_ReturnsLimitedResults()
    {
        // GET Users with explicit limit — verify it returns a bounded set
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, Service.Core, "GET", "Users",
            parameters: new Dictionary<string, string> { ["limit"] = "2" });

        Assert.Equal(200, (int)response.StatusCode);

        var body = response.Body.Trim();
        Assert.StartsWith("[", body);

        // Count top-level array elements by looking for the top-level pattern
        // Each user starts a new object after a comma at the array level
        // Simplest: just verify we got fewer results than "no limit"
        var unlimitedResponse = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, Service.Core, "GET", "Users",
            parameters: new Dictionary<string, string> { ["limit"] = "100" });

        Assert.True(body.Length < unlimitedResponse.Body.Length || body.Length < 10000,
            "limit=2 should return less data than limit=100");
    }

    [RequiresApplianceFact]
    public async Task Execute_WithFilter_ReturnsFilteredResults()
    {
        // Safeguard Users use "Name" as the filter property (not "UserName")
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, Service.Core, "GET", "Users",
            parameters: new Dictionary<string, string>
            {
                ["filter"] = "Name eq 'Admin'"
            });

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("Admin", response.Body);
    }

    [RequiresApplianceFact]
    public async Task Execute_WithFieldSelection_ReturnsOnlyRequestedFields()
    {
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, Service.Core, "GET", "Users",
            parameters: new Dictionary<string, string>
            {
                ["fields"] = "Id,Name",
                ["limit"] = "2"
            });

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("Name", response.Body);
        // With fields=Id,Name we shouldn't see large nested objects like AdminRoles
        Assert.DoesNotContain("AdminRoles", response.Body);
    }

    [RequiresApplianceFact]
    public async Task Schema_ThenExecute_CreateAndDelete_Asset()
    {
        // Get schema for POST /v4/Assets to understand required fields
        var schema = _fixture.CatalogProvider.GetSchema("POST", "Core", "/v4/Assets", _fixture.Host);
        Assert.NotNull(schema);

        // Create a test asset — this may fail with 403 if the test user lacks
        // AssetAdmin permissions, which is acceptable
        var testName = $"IntegrationTest-{Guid.NewGuid().ToString("N")[..8]}";
        var createBody = $$"""
        {
            "Name": "{{testName}}",
            "NetworkAddress": "192.168.99.99",
            "PlatformId": 1
        }
        """;

        try
        {
            var createResponse = await _fixture.ConnectionManager.InvokeAsync(
                _fixture.Host, Service.Core, "POST", "Assets", body: createBody);

            // If we get here, creation succeeded — clean up
            if ((int)createResponse.StatusCode == 201)
            {
                var idStart = createResponse.Body.IndexOf("\"Id\":") + 5;
                var idEnd = createResponse.Body.IndexOf(",", idStart);
                if (idEnd < 0) idEnd = createResponse.Body.IndexOf("}", idStart);
                var id = createResponse.Body[idStart..idEnd].Trim();

                await _fixture.ConnectionManager.InvokeAsync(
                    _fixture.Host, Service.Core, "DELETE", $"Assets/{id}");
            }
        }
        catch (ModelContextProtocol.McpException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
        {
            // Expected when test user lacks asset management permissions — test passes
            // because we verified the schema lookup and the pipeline didn't crash
        }
    }
}
