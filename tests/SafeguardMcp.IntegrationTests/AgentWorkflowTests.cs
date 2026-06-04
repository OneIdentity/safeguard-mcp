using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Tests the full tool pipeline: Discover → Schema → Execute, mimicking how an AI agent
/// would actually use the server in practice.
/// </summary>
[Collection("Appliance")]
public class AgentWorkflowTests
{
    private readonly ApplianceFixture _fixture;

    public AgentWorkflowTests(ApplianceFixture fixture) => _fixture = fixture;

    [RequiresApplianceFact]
    public void Discover_ThenSchema_ForPostEndpoint()
    {
        // Step 1: Agent discovers asset-related endpoints
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);

        var postAssets = endpoints.ToArray()
            .FirstOrDefault(e => e.Path == "/v4/Assets" && e.Method == "POST");

        Assert.Equal("POST", postAssets.Method);

        // Step 2: Agent gets schema to understand the request body
        var schema = _fixture.CatalogProvider.GetSchema(
            postAssets.Method, postAssets.Service, postAssets.Path, _fixture.Host);

        Assert.NotNull(schema);
        Assert.True(schema.Value.Properties.Length > 0,
            "POST /v4/Assets schema should have properties");

        // Step 3: Agent can identify required/useful fields
        var propNames = schema.Value.Properties.Select(p => p.Name).ToList();
        Assert.Contains("Name", propNames);
    }

    [RequiresApplianceFact]
    public void Discover_ThenSchema_ForAccessRequests()
    {
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);

        var postAR = endpoints.ToArray()
            .FirstOrDefault(e => e.Path == "/v4/AccessRequests" && e.Method == "POST");

        Assert.Equal("POST", postAR.Method);

        var schema = _fixture.CatalogProvider.GetSchema(
            postAR.Method, postAR.Service, postAR.Path, _fixture.Host);

        Assert.NotNull(schema);

        var propNames = schema.Value.Properties.Select(p => p.Name).ToList();
        // Access requests need at minimum an account
        Assert.True(propNames.Count >= 3,
            $"POST /v4/AccessRequests should have multiple properties, got {propNames.Count}");
    }

    [RequiresApplianceFact]
    public async Task FullPipeline_DiscoverUsersEndpoint_GetSchema_ExecuteQuery()
    {
        // Simulate: "Show me all admin users"

        // 1. Discover
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);
        var getUsersEndpoint = endpoints.ToArray()
            .FirstOrDefault(e => e.Path == "/v4/Users" && e.Method == "GET");
        Assert.Equal("GET", getUsersEndpoint.Method);

        // 2. Schema (for response fields)
        var schema = _fixture.CatalogProvider.GetSchema(
            "GET", getUsersEndpoint.Service, getUsersEndpoint.Path, _fixture.Host);
        // GET may or may not have a response schema depending on swagger annotations

        // 3. Execute with intelligent query
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, OneIdentity.SafeguardDotNet.Service.Core, "GET", "Users",
            parameters: new Dictionary<string, string>
            {
                ["filter"] = "Name eq 'Admin'",
                ["fields"] = "Id,Name,AdminRoles"
            });

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("Admin", response.Body);
    }

    [RequiresApplianceFact]
    public async Task FullPipeline_CheckApplianceHealth()
    {
        // Simulate: "Is the appliance healthy?"

        // 1. Discover health endpoint
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);
        var healthEndpoint = endpoints.ToArray()
            .FirstOrDefault(e => e.Path.Contains("Health") && e.Method == "GET"
                && e.Service == "Appliance");

        Assert.NotNull(healthEndpoint.Path);

        // 2. Execute
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, OneIdentity.SafeguardDotNet.Service.Appliance, "GET",
            "ApplianceStatus/Health");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(response.Body));
    }

    [RequiresApplianceFact]
    public async Task FullPipeline_ListAssetAccounts_WithFieldProjection()
    {
        // Simulate: "List accounts with just names"
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, OneIdentity.SafeguardDotNet.Service.Core, "GET", "AssetAccounts",
            parameters: new Dictionary<string, string>
            {
                ["fields"] = "Id,Name",
                ["limit"] = "5"
            });

        Assert.Equal(200, (int)response.StatusCode);
        // Even if no accounts exist, we should get a valid empty array
        Assert.StartsWith("[", response.Body.TrimStart());
    }

    [RequiresApplianceFact]
    public async Task FullPipeline_CheckClusterStatus()
    {
        // Simulate: "What's the cluster state?"
        // Cluster/Members is under Core service, not Appliance
        var response = await _fixture.ConnectionManager.InvokeAsync(
            _fixture.Host, OneIdentity.SafeguardDotNet.Service.Core, "GET", "Cluster/Members");

        Assert.Equal(200, (int)response.StatusCode);
        // Should return an array of cluster members (at least one — this appliance)
        Assert.StartsWith("[", response.Body.TrimStart());
    }

    [RequiresApplianceFact]
    public void StaticCatalog_LoadsAndMatchesDynamic()
    {
        // The static catalog should be a subset of what the dynamic catalog provides
        var staticEndpoints = SafeguardCatalog.Endpoints;
        var dynamicEndpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);

        Assert.True(staticEndpoints.Length > 500,
            $"Static catalog should have 500+ endpoints, got {staticEndpoints.Length}");

        // Check a few known static entries exist in dynamic
        var staticUsers = staticEndpoints.FirstOrDefault(
            e => e.Path == "/v4/Users" && e.Method == "GET");
        var dynamicUsers = dynamicEndpoints.ToArray().FirstOrDefault(
            e => e.Path == "/v4/Users" && e.Method == "GET");

        Assert.Equal(staticUsers.Method, dynamicUsers.Method);
        Assert.Equal(staticUsers.Path, dynamicUsers.Path);
    }
}
