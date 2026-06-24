using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class MisplacedQueryOptionsTests
{
    private static Dictionary<string, JsonElement> Args(string json)
        => JsonDocument.Parse(json).RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal);

    [Theory]
    [InlineData("parameters")]
    [InlineData("params")]
    [InlineData("queryParameters")]
    [InlineData("odata")]
    public void Rejects_OptionsNestedInContainerObject(string container)
    {
        var args = Args($$"""
        {
          "method": "GET",
          "path": "/v4/AuditLog/Logins",
          "{{container}}": { "filter": "Name eq 'TestAdmin'", "orderby": "-LogTime" }
        }
        """);

        var ex = Assert.Throws<McpException>(
            () => SafeguardApiTool.RejectMisplacedQueryOptions(args));

        Assert.Contains(container, ex.Message);
        Assert.Contains("query", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("query=\"filter=Name eq 'TestAdmin'&orderby=-LogTime\"", ex.Message);
        Assert.Contains("Safeguard_Reference topic=query-syntax", ex.Message);
    }

    [Theory]
    [InlineData("filter")]
    [InlineData("orderby")]
    [InlineData("fields")]
    [InlineData("count")]
    public void Rejects_TopLevelQueryOptionKey(string key)
    {
        var args = Args($$"""
        {
          "method": "GET",
          "path": "/v4/Users",
          "{{key}}": "anything"
        }
        """);

        var ex = Assert.Throws<McpException>(
            () => SafeguardApiTool.RejectMisplacedQueryOptions(args));

        Assert.Contains(key, ex.Message);
        Assert.Contains("Safeguard_Reference topic=query-syntax", ex.Message);
    }

    [Fact]
    public void Rejects_ContainerRegardlessOfCasing()
    {
        var args = Args("""
        {
          "method": "GET",
          "path": "/v4/Users",
          "Parameters": { "filter": "Name eq 'x'" }
        }
        """);

        Assert.Throws<McpException>(() => SafeguardApiTool.RejectMisplacedQueryOptions(args));
    }

    [Fact]
    public void Allows_CorrectQueryStringCall()
    {
        var args = Args("""
        {
          "method": "GET",
          "path": "/v4/AuditLog/Logins",
          "query": "filter=Name eq 'TestAdmin'&orderby=-LogTime"
        }
        """);

        SafeguardApiTool.RejectMisplacedQueryOptions(args);
    }

    [Fact]
    public void Allows_ContainerKeyThatIsNotAnObject()
    {
        // A scalar 'params' is not the documented misplacement shape (a nested
        // options object) and must not trip the guard.
        var args = Args("""
        {
          "method": "GET",
          "path": "/v4/Users",
          "params": "not-an-object"
        }
        """);

        SafeguardApiTool.RejectMisplacedQueryOptions(args);
    }

    [Fact]
    public void Allows_NullOrEmptyArguments()
    {
        SafeguardApiTool.RejectMisplacedQueryOptions((IDictionary<string, JsonElement>)null);
        SafeguardApiTool.RejectMisplacedQueryOptions(new Dictionary<string, JsonElement>());
    }

    // Wiring: proves the raw tool-call parameters object is read and rejected via
    // the same path Safeguard_Execute uses (context.Params -> Arguments -> guard).
    [Fact]
    public void RejectMisplacedQueryOptions_FromCallToolParams_RejectsNestedObject()
    {
        var parameters = new CallToolRequestParams
        {
            Name = "Safeguard_Execute",
            Arguments = Args("""
            {
              "method": "GET",
              "path": "/v4/AuditLog/Logins",
              "parameters": { "filter": "Name eq 'TestAdmin'", "orderby": "-LogTime" }
            }
            """),
        };

        var ex = Assert.Throws<McpException>(
            () => SafeguardApiTool.RejectMisplacedQueryOptions(parameters));

        Assert.Contains("parameters", ex.Message);
        Assert.Contains("query=\"filter=Name eq 'TestAdmin'&orderby=-LogTime\"", ex.Message);
        Assert.Contains("Safeguard_Reference topic=query-syntax", ex.Message);
    }

    [Fact]
    public void RejectMisplacedQueryOptions_FromCallToolParams_AllowsCorrectQuery()
    {
        var parameters = new CallToolRequestParams
        {
            Name = "Safeguard_Execute",
            Arguments = Args("""
            {
              "method": "GET",
              "path": "/v4/AuditLog/Logins",
              "query": "filter=Name eq 'TestAdmin'&orderby=-LogTime"
            }
            """),
        };

        SafeguardApiTool.RejectMisplacedQueryOptions(parameters);
    }

    [Fact]
    public void RejectMisplacedQueryOptions_FromCallToolParams_AllowsNullParamsOrArguments()
    {
        SafeguardApiTool.RejectMisplacedQueryOptions(default(CallToolRequestParams));
        SafeguardApiTool.RejectMisplacedQueryOptions(new CallToolRequestParams { Name = "Safeguard_Execute" });
    }
}
