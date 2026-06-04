using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tests;

public class EmbeddedResourcesTests
{
    [Fact]
    public void ApiOverview_LoadsFromEmbeddedResource()
    {
        var content = ApiOverviewResource.GetApiOverview();
        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.StartsWith("# Safeguard API Service Map", content);
        Assert.Contains("## Core Service", content);
        Assert.Contains("## Notification Service", content);
    }

    [Fact]
    public void QuerySyntax_LoadsFromEmbeddedResource()
    {
        var content = QuerySyntaxResource.GetQuerySyntax();
        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.StartsWith("# Safeguard API Query Syntax Reference", content);
        Assert.Contains("Not OData", content);
        Assert.Contains("Sensitive Credential Material", content);
    }

    [Fact]
    public void CommonPatterns_LoadsFromEmbeddedResource()
    {
        var content = CommonPatternsResource.GetCommonPatterns();
        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.StartsWith("# Common Safeguard API Patterns", content);
        Assert.Contains("Lookup by Name", content);
    }

    [Fact]
    public void Terminology_LoadsFromEmbeddedResource()
    {
        var content = TerminologyResource.GetTerminologyMap();
        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.StartsWith("# Safeguard Terminology Map", content);
        Assert.Contains("/v4/Roles", content);
    }

    [Fact]
    public void Resources_AreCachedAfterFirstAccess()
    {
        var first = QuerySyntaxResource.GetQuerySyntax();
        var second = QuerySyntaxResource.GetQuerySyntax();
        Assert.Same(first, second);
    }
}
