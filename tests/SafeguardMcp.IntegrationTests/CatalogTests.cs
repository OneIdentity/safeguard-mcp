using SafeguardMcp.Catalog;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Tests that verify the dynamic catalog loads from the appliance's live swagger endpoints.
/// </summary>
[Collection("Appliance")]
public class CatalogTests
{
    private readonly ApplianceFixture _fixture;

    public CatalogTests(ApplianceFixture fixture) => _fixture = fixture;

    [RequiresApplianceFact]
    public void DynamicCatalog_LoadsFromAppliance()
    {
        Assert.True(_fixture.CatalogProvider.HasDynamicCatalog(_fixture.Host),
            "Dynamic catalog should be loaded after connection");
    }

    [RequiresApplianceFact]
    public void DynamicCatalog_ContainsEndpoints()
    {
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);
        Assert.True(endpoints.Length > 500,
            $"Expected at least 500 endpoints from swagger, got {endpoints.Length}");
    }

    [RequiresApplianceFact]
    public void DynamicCatalog_ContainsKnownCoreEndpoints()
    {
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);

        var hasUsers = false;
        var hasAssets = false;
        var hasAccessRequests = false;

        foreach (var ep in endpoints)
        {
            if (ep.Path == "/v4/Users" && ep.Method == "GET") hasUsers = true;
            if (ep.Path == "/v4/Assets" && ep.Method == "GET") hasAssets = true;
            if (ep.Path == "/v4/AccessRequests" && ep.Method == "GET") hasAccessRequests = true;
        }

        Assert.True(hasUsers, "Should have GET /v4/Users");
        Assert.True(hasAssets, "Should have GET /v4/Assets");
        Assert.True(hasAccessRequests, "Should have GET /v4/AccessRequests");
    }

    [RequiresApplianceFact]
    public void DynamicCatalog_ContainsApplianceEndpoints()
    {
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);

        var hasApplianceStatus = false;
        foreach (var ep in endpoints)
        {
            if (ep.Path == "/v4/ApplianceStatus" && ep.Method == "GET")
            {
                hasApplianceStatus = true;
                Assert.Equal("Appliance", ep.Service);
                break;
            }
        }

        Assert.True(hasApplianceStatus, "Should have GET /v4/ApplianceStatus");
    }

    [RequiresApplianceFact]
    public void DynamicCatalog_HasSchemas()
    {
        // POST /v4/Assets should have a request schema
        var schema = _fixture.CatalogProvider.GetSchema("POST", "Core", "/v4/Assets", _fixture.Host);
        Assert.NotNull(schema);
        Assert.NotEmpty(schema.Value.Properties);
    }

    [RequiresApplianceFact]
    public void DynamicCatalog_SchemaHasRequiredFields()
    {
        var schema = _fixture.CatalogProvider.GetSchema("POST", "Core", "/v4/Assets", _fixture.Host);
        Assert.NotNull(schema);

        // Assets typically require a Name and PlatformId at minimum
        var hasName = schema.Value.Properties.Any(p =>
            p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasName, "POST /v4/Assets schema should have a Name property");
    }

    [RequiresApplianceFact]
    public void DynamicCatalog_EndpointsHaveSummaries()
    {
        var endpoints = _fixture.CatalogProvider.GetEndpoints(_fixture.Host);

        // Most endpoints should have summaries from swagger
        var withSummary = 0;
        var total = endpoints.Length;
        foreach (var ep in endpoints)
        {
            if (!string.IsNullOrWhiteSpace(ep.Summary))
                withSummary++;
        }

        var ratio = (double)withSummary / total;
        Assert.True(ratio > 0.8,
            $"Expected >80% of endpoints to have summaries, got {ratio:P0} ({withSummary}/{total})");
    }
}
