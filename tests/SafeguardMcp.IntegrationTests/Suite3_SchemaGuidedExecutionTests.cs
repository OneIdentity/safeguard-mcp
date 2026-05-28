using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace SafeguardMcp.IntegrationTests;

[Collection("AgentSimulation")]
public class Suite3_SchemaGuidedExecutionTests
{
    private readonly AgentSimulationFixture _fixture;

    public Suite3_SchemaGuidedExecutionTests(AgentSimulationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Execute_CreateUser_FullPipeline()
    {
        if (!_fixture.Available) return;

        var discoverResult = _fixture.Discover(search: "users", method: "POST");
        DiscoverAssertions.AssertFindsEndpoint(discoverResult, "POST", "/v4/Users");

        var schema = _fixture.Schema("/v4/Users", "POST");
        Assert.Contains("Name", schema);

        var builder = new SchemaBodyBuilder(_fixture);
        var body = await builder.BuildAsync(schema);

        var result = await _fixture.ExecuteAsync("POST", "/v4/Users", body: body);
        using var doc = JsonDocument.Parse(result);
        var userId = doc.RootElement.GetProperty("Id").GetInt32();
        _fixture.RegisterCleanup("DELETE", $"/v4/Users/{userId}");

        var getResult = await _fixture.ExecuteAsync("GET", $"/v4/Users/{userId}");
        Assert.Contains("McpTest_", getResult);
    }

    [Fact]
    public async Task Execute_CreateAsset_PlatformDiscoveryByName()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Assets", "POST");

        var builder = new SchemaBodyBuilder(_fixture)
            .WithOverride("NetworkAddress", "\"10.0.0.99\"");
        var body = await builder.BuildAsync(schema);

        using var bodyDoc = JsonDocument.Parse(body);
        Assert.True(bodyDoc.RootElement.TryGetProperty("PlatformId", out var platformIdProp));
        Assert.True(platformIdProp.GetInt32() > 0, "PlatformId should be discovered by name, not 0");

        var result = await _fixture.ExecuteAsync("POST", "/v4/Assets", body: body);
        using var doc = JsonDocument.Parse(result);
        var assetId = doc.RootElement.GetProperty("Id").GetInt32();
        _fixture.RegisterCleanup("DELETE", $"/v4/Assets/{assetId}");

        var getResult = await _fixture.ExecuteAsync("GET", $"/v4/Assets/{assetId}");
        using var getDoc = JsonDocument.Parse(getResult);
        Assert.Equal("10.0.0.99", getDoc.RootElement.GetProperty("NetworkAddress").GetString());
    }

    [Fact]
    public async Task Execute_CreateAccount_OnExistingAsset()
    {
        if (!_fixture.Available) return;

        var assetBody = JsonSerializer.Serialize(new
        {
            Name = $"McpTest_Asset_{Random.Shared.Next(10000, 99999)}",
            NetworkAddress = "10.0.0.100",
            PlatformId = await GetWindowsServerPlatformIdAsync(),
            AssetPartitionId = -1
        });
        var assetResult = await _fixture.ExecuteAsync("POST", "/v4/Assets", body: assetBody);
        using var assetDoc = JsonDocument.Parse(assetResult);
        var assetId = assetDoc.RootElement.GetProperty("Id").GetInt32();
        _fixture.RegisterCleanup("DELETE", $"/v4/Assets/{assetId}");

        var schema = _fixture.Schema("/v4/AssetAccounts", "POST");

        var builder = new SchemaBodyBuilder(_fixture)
            .WithOverride("Asset", $"{{\"Id\": {assetId}}}");
        var body = await builder.BuildAsync(schema);

        var result = await _fixture.ExecuteAsync("POST", "/v4/AssetAccounts", body: body);
        using var doc = JsonDocument.Parse(result);
        var accountId = doc.RootElement.GetProperty("Id").GetInt32();
        _fixture.RegisterCleanup("DELETE", $"/v4/AssetAccounts/{accountId}");

        var getResult = await _fixture.ExecuteAsync("GET", $"/v4/AssetAccounts/{accountId}");
        Assert.Contains("McpTest_", getResult);
    }

    [Fact]
    public async Task Execute_QueryWithFilters_ReturnsFilteredResults()
    {
        if (!_fixture.Available) return;

        var help = _fixture.QueryHelp(path: "/v4/Users");
        Assert.Contains("filter", help);

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "filter=Name icontains 'McpTest'&fields=Id,Name&orderby=Name&limit=5");

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            Assert.True(item.TryGetProperty("Id", out _));
            Assert.True(item.TryGetProperty("Name", out _));
        }
    }

    [Fact]
    public async Task Execute_UpdateUser_PutModifiedField()
    {
        if (!_fixture.Available) return;

        var createBody = JsonSerializer.Serialize(new
        {
            Name = $"McpTest_Update_{Random.Shared.Next(10000, 99999)}",
            PrimaryAuthenticationProvider = new { Id = -1 }
        });
        var createResult = await _fixture.ExecuteAsync("POST", "/v4/Users", body: createBody);
        using var createDoc = JsonDocument.Parse(createResult);
        var userId = createDoc.RootElement.GetProperty("Id").GetInt32();
        _fixture.RegisterCleanup("DELETE", $"/v4/Users/{userId}");

        var getResult = await _fixture.ExecuteAsync("GET", $"/v4/Users/{userId}");
        var modifiedBody = AddOrReplaceProperty(getResult, "Description", "\"Updated by agent test\"");

        await _fixture.ExecuteAsync("PUT", $"/v4/Users/{userId}", body: modifiedBody);

        var verifyResult = await _fixture.ExecuteAsync("GET", $"/v4/Users/{userId}");
        Assert.Contains("Updated by agent test", verifyResult);
    }

    [Fact]
    public async Task Execute_DeleteUser_ReturnsNotFoundAfter()
    {
        if (!_fixture.Available) return;

        var createBody = JsonSerializer.Serialize(new
        {
            Name = $"McpTest_Delete_{Random.Shared.Next(10000, 99999)}",
            PrimaryAuthenticationProvider = new { Id = -1 }
        });
        var createResult = await _fixture.ExecuteAsync("POST", "/v4/Users", body: createBody);
        using var doc = JsonDocument.Parse(createResult);
        var userId = doc.RootElement.GetProperty("Id").GetInt32();

        var discoverResult = _fixture.Discover(search: "users", method: "DELETE");
        DiscoverAssertions.AssertFindsEndpoint(discoverResult, "DELETE", "/v4/Users");

        await _fixture.ExecuteAsync("DELETE", $"/v4/Users/{userId}");

        try
        {
            var getResult = await _fixture.ExecuteAsync("GET", $"/v4/Users/{userId}");
            Assert.Contains("404", getResult);
        }
        catch (Exception ex)
        {
            Assert.Contains("404", ex.Message);
        }
    }

    private async Task<int> GetWindowsServerPlatformIdAsync()
    {
        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Platforms",
            query: "filter=DisplayName eq 'Windows Server'&fields=Id,DisplayName&limit=1");
        using var doc = JsonDocument.Parse(result);
        if (doc.RootElement.GetArrayLength() > 0)
        {
            return doc.RootElement[0].GetProperty("Id").GetInt32();
        }

        result = await _fixture.ExecuteAsync("GET", "/v4/Platforms", query: "fields=Id,DisplayName&limit=1");
        using var fallbackDoc = JsonDocument.Parse(result);
        return fallbackDoc.RootElement[0].GetProperty("Id").GetInt32();
    }

    private static string AddOrReplaceProperty(string json, string propertyName, string jsonValue)
    {
        var node = JsonNode.Parse(json).AsObject();
        node[propertyName] = JsonNode.Parse(jsonValue);
        return node.ToJsonString();
    }
}
