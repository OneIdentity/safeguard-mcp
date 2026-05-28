using System.Text.Json;
using Xunit;

namespace SafeguardMcp.IntegrationTests;

[Collection("AgentSimulation")]
public class Suite6_EdgeCasesTests
{
    private readonly AgentSimulationFixture _fixture;

    public Suite6_EdgeCasesTests(AgentSimulationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EdgeCase_EmptySearchResults_ReturnsEmptyArray()
    {
        if (!_fixture.Available) return;

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "filter=Name eq 'IMPOSSIBLE_VALUE_12345'");

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task EdgeCase_SpecialCharactersInFilter_ReturnsValidJson()
    {
        if (!_fixture.Available) return;

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "filter=Name eq 'O''Brien'");

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task EdgeCase_CountQuery_ReturnsIntegerOrValidArray()
    {
        if (!_fixture.Available) return;

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "count=true");

        using var doc = JsonDocument.Parse(result);
        Assert.True(
            doc.RootElement.ValueKind is JsonValueKind.Number or JsonValueKind.Array,
            $"Expected count response to be a number or array, got: {result}");

        if (doc.RootElement.ValueKind == JsonValueKind.Number)
        {
            Assert.True(doc.RootElement.TryGetInt64(out _), $"Expected integer count value, got: {result}");
        }
    }

    [Fact]
    public async Task EdgeCase_LargeLimit_ReturnsValidJsonArray()
    {
        if (!_fixture.Available) return;

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "limit=99999");

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task EdgeCase_AllQueryParameters_ReturnsValidJsonArray()
    {
        if (!_fixture.Available) return;

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "fields=Id,Name&filter=Name isw 'McpTest'&orderby=Name&limit=5&page=0");

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task EdgeCase_CsvFormat_ReturnsCommaSeparatedData()
    {
        if (!_fixture.Available) return;

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "fields=Id,Name&limit=5",
            format: "csv");

        Assert.True(
            result.Contains(',')
            && (result.Contains('\n') || result.Contains('\r') || result.Contains("Id,Name", StringComparison.OrdinalIgnoreCase)),
            $"Expected CSV output with commas and row/header delimiters, got: {result}");
    }

    [Fact(Skip = "Cannot test disconnected state within connected fixture")]
    public Task EdgeCase_SchemaBeforeConnect_IsSkipped()
        => Task.CompletedTask;

    [Fact(Skip = "Cannot test disconnected state within connected fixture")]
    public Task EdgeCase_ExecuteBeforeConnect_IsSkipped()
        => Task.CompletedTask;

    [Fact]
    public async Task EdgeCase_QuickSearchParam_ReturnsJsonArrayWithResults()
    {
        if (!_fixture.Available) return;

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "q=admin");

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() > 0, "Expected at least one result for q=admin.");
    }

    [Fact]
    public async Task EdgeCase_BatchEndpointDiscovery_ContainsBatchCreate()
    {
        if (!_fixture.Available) return;

        var result = _fixture.Discover(search: "batch");
        if (!result.Contains("BatchCreate", StringComparison.OrdinalIgnoreCase))
        {
            result = _fixture.Discover(search: "bulk");
        }

        Assert.True(
            result.Contains("BatchCreate", StringComparison.OrdinalIgnoreCase),
            $"Expected discovery output to contain BatchCreate, got: {result}");

        await Task.CompletedTask;
    }
}
