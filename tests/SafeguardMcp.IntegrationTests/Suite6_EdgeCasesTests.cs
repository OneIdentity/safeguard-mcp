using System.Text.Json;
using Microsoft.Extensions.Configuration;
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
        _fixture.RequireAvailable();

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "filter=Name eq 'IMPOSSIBLE_VALUE_12345'&limit=100");

        var json = ExtractJsonBody(result);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task EdgeCase_SpecialCharactersInFilter_ReturnsValidJson()
    {
        _fixture.RequireAvailable();

        // Valid filter with special characters (hyphens, spaces) in string value
        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "filter=Name eq 'Test-User 123'&limit=100");

        var json = ExtractJsonBody(result);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task EdgeCase_InvalidFilterSyntax_ReturnsHelpfulError()
    {
        _fixture.RequireAvailable();

        // Single quotes within filter values break Safeguard's filter parser
        var ex = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(async () =>
            await _fixture.ExecuteAsync(
                "GET",
                "/v4/Users",
                query: "filter=Name eq 'O''Brien'&limit=100"));

        Assert.Contains("400", ex.Message);
        Assert.Contains("filter", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EdgeCase_CountQuery_ReturnsIntegerOrValidArray()
    {
        _fixture.RequireAvailable();

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "count=true&limit=100");

        var json = ExtractJsonBody(result);
        using var doc = JsonDocument.Parse(json);
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
        _fixture.RequireAvailable();

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
        _fixture.RequireAvailable();

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
        _fixture.RequireAvailable();

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

    [Fact]
    public async Task EdgeCase_SchemaBeforeConnect_IsSkipped()
    {
        // Schema without a connection should return guidance mentioning Connect.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
            {
                ["Safeguard:IgnoreSsl"] = "true",
            })
            .Build();
        var catalogLoader = new SafeguardMcp.Catalog.CatalogLoader(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Catalog.CatalogLoader>());
        var catalogProvider = new SafeguardMcp.Catalog.CatalogProvider(
            catalogLoader, new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Catalog.CatalogProvider>());
        using var disconnectedMgr = new SafeguardMcp.Tools.HttpRelaySafeguardSession(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Tools.HttpRelaySafeguardSession>(),
            config, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        var tool = new SafeguardMcp.Tools.SafeguardApiTool(disconnectedMgr, catalogProvider, config);

        var schema = tool.Safeguard_Schema(path: "/v4/Users", method: "POST");

        // Without a connection, schema should guide the user to connect first
        Assert.Contains("Connect", schema, StringComparison.OrdinalIgnoreCase);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task EdgeCase_ExecuteBeforeConnect_IsSkipped()
    {
        // Execute without connecting should throw with guidance to call Connect first.
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
            {
                ["Safeguard:IgnoreSsl"] = "true",
            })
            .Build();
        var catalogLoader = new SafeguardMcp.Catalog.CatalogLoader(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Catalog.CatalogLoader>());
        var catalogProvider = new SafeguardMcp.Catalog.CatalogProvider(
            catalogLoader, new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Catalog.CatalogProvider>());
        var prevHost = Environment.GetEnvironmentVariable("SAFEGUARD_HOST");
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", "disconnected-host.example");
        try
        {
            using var disconnectedMgr = new SafeguardMcp.Tools.HttpRelaySafeguardSession(
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Tools.HttpRelaySafeguardSession>(),
                config, new Microsoft.AspNetCore.Http.HttpContextAccessor());
            var tool = new SafeguardMcp.Tools.SafeguardApiTool(disconnectedMgr, catalogProvider, config);

            var ex = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(
                () => tool.Safeguard_Execute(null, method: "GET", path: "/v4/Users"));

            Assert.Contains("authenticated", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SAFEGUARD_HOST", prevHost);
        }
    }

    [Fact]
    public async Task EdgeCase_QuickSearchParam_ReturnsJsonArrayWithResults()
    {
        _fixture.RequireAvailable();

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Users",
            query: "q=admin&limit=100");

        var json = ExtractJsonBody(result);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() > 0, "Expected at least one result for q=admin.");
    }

    [Fact]
    public async Task EdgeCase_BatchEndpointDiscovery_ContainsBatchCreate()
    {
        _fixture.RequireAvailable();

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

    /// <summary>
    /// Peels the <c>data</c> field off the Safeguard_Execute envelope so tests can assert
    /// against the raw API body (array, scalar, or object) returned by the appliance.
    /// </summary>
    private static string ExtractJsonBody(string response)
        => EnvelopeTestHelpers.UnwrapData(response);
}
