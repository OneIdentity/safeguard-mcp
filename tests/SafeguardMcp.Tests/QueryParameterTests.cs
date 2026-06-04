using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class QueryParameterTests
{
    [Fact]
    public void ParseQueryParameters_SimpleParameters()
    {
        var result = ApiToolHelpers.ParseQueryParameters("filter=Name eq 'test'&limit=50");

        Assert.Equal(2, result.Count);
        Assert.Equal("Name eq 'test'", result["filter"]);
        Assert.Equal("50", result["limit"]);
    }

    [Fact]
    public void ParseQueryParameters_WithLeadingQuestionMark()
    {
        var result = ApiToolHelpers.ParseQueryParameters("?fields=Id,Name&orderby=-CreatedDate");

        Assert.Equal(2, result.Count);
        Assert.Equal("Id,Name", result["fields"]);
        Assert.Equal("-CreatedDate", result["orderby"]);
    }

    [Fact]
    public void ParseQueryParameters_UrlEncoded()
    {
        var result = ApiToolHelpers.ParseQueryParameters("filter=Name%20eq%20%27admin%27");

        Assert.Single(result);
        Assert.Equal("Name eq 'admin'", result["filter"]);
    }

    [Fact]
    public void ParseQueryParameters_EmptyAndNull()
    {
        Assert.Empty(ApiToolHelpers.ParseQueryParameters(""));
        Assert.Empty(ApiToolHelpers.ParseQueryParameters(null));
        Assert.Empty(ApiToolHelpers.ParseQueryParameters("   "));
    }

    [Fact]
    public void ParseQueryParameters_KeyWithoutValue()
    {
        var result = ApiToolHelpers.ParseQueryParameters("count=true&flag");

        Assert.Equal(2, result.Count);
        Assert.Equal("true", result["count"]);
        Assert.Equal("", result["flag"]);
    }

    [Fact]
    public void ParseQueryParameters_CaseInsensitiveLookup()
    {
        var result = ApiToolHelpers.ParseQueryParameters("Filter=Name eq 'x'&LIMIT=10");

        Assert.Equal("Name eq 'x'", result["filter"]);
        Assert.Equal("10", result["limit"]);
    }
}
