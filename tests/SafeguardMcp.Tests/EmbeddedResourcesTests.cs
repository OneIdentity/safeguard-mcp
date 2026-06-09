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
    public void QuerySyntax_DoesNotAdvertiseNotInOperator()
    {
        var content = QuerySyntaxResource.GetQuerySyntax();
        // not_in is not a real Safeguard operator. Make sure the reference doc
        // never lists it as one. (It is fine to mention it in negative form,
        // e.g. "There is no not_in operator", so we only assert it does not
        // appear in the operators table or as a `filter=Id not_in` example.)
        Assert.DoesNotContain("| not_in ", content);
        Assert.DoesNotContain("Not in list", content);
    }

    [Fact]
    public void QuerySyntax_DocumentsNegationAndNullLiteral()
    {
        var content = QuerySyntaxResource.GetQuerySyntax();
        Assert.Contains("not (Id in [4,5,6])", content);
        Assert.Contains("Description eq null", content);
        Assert.Contains("Description ne null", content);
    }

    [Fact]
    public void QuerySyntax_ListsEveryAuthoritativeBinaryOperator()
    {
        var content = QuerySyntaxResource.GetQuerySyntax();
        string[] expected =
        {
            "eq", "ne", "gt", "ge", "lt", "le",
            "contains", "icontains", "ieq",
            "sw", "isw", "ew", "iew",
            "in",
        };
        foreach (var op in expected)
        {
            Assert.Contains($"| {op} ", content);
        }
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
