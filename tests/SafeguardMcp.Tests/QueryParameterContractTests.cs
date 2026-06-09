using System.Text;
using System.Text.Json;
using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class QueryParameterContractTests
{
    private static List<ParamInfo> Collect(string json)
    {
        var doc = JsonDocument.Parse(json);
        var infos = new List<ParamInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CatalogLoader.CollectParameters(doc.RootElement, infos, seen);
        return infos;
    }

    [Theory]
    [InlineData("Log time range start. Default 1 day before endDate. (Preferred over 'filter').", true)]
    [InlineData("Log time range end. (Preferred over 'filter').", true)]
    [InlineData("User Id. (Preferred over filter).", true)]
    [InlineData("Asset Id. (preferred over FILTER).", true)]
    [InlineData("Filter expression for narrowing results.", false)]
    [InlineData("", false)]
    public void IsPreferredOverFilter_AcceptsBothQuotedAndUnquotedForms(string description, bool expected)
    {
        Assert.Equal(expected, CatalogLoader.IsPreferredOverFilter(description));
    }

    [Fact]
    public void CollectParameters_CapturesDescriptionTypeRequiredAndPreferred()
    {
        const string json = """
        {
          "parameters": [
            {
              "name": "startDate",
              "in": "query",
              "required": false,
              "description": "Log time range start. Default 1 day before endDate. (Preferred over 'filter').",
              "schema": { "type": "string", "format": "date-time" }
            },
            {
              "name": "userId",
              "in": "query",
              "required": false,
              "description": "User Id. (Preferred over filter).",
              "schema": { "type": "integer" }
            },
            {
              "name": "count",
              "in": "query",
              "description": "If true, returns the total count as a single integer; fields and orderby are ignored.",
              "schema": { "type": "boolean" }
            },
            {
              "name": "id",
              "in": "path",
              "required": true,
              "description": "Numeric identifier of the entity.",
              "schema": { "type": "integer" }
            },
            {
              "name": "Authorization",
              "in": "header",
              "description": "Auth header — must be ignored by CollectParameters.",
              "schema": { "type": "string" }
            }
          ]
        }
        """;

        var infos = Collect(json);

        Assert.Equal(4, infos.Count);
        Assert.DoesNotContain(infos, p => string.Equals(p.Name, "Authorization", StringComparison.OrdinalIgnoreCase));

        var startDate = infos.Single(p => p.Name == "startDate");
        Assert.Equal("query", startDate.In);
        Assert.Equal("string", startDate.Type);
        Assert.False(startDate.Required);
        Assert.True(startDate.PreferredOverFilter);
        Assert.Contains("Default 1 day before endDate", startDate.Description);

        var userId = infos.Single(p => p.Name == "userId");
        Assert.True(userId.PreferredOverFilter);
        Assert.Equal("integer", userId.Type);

        var count = infos.Single(p => p.Name == "count");
        Assert.False(count.PreferredOverFilter);
        Assert.Equal("boolean", count.Type);

        var id = infos.Single(p => p.Name == "id");
        Assert.Equal("path", id.In);
        Assert.True(id.Required);
    }

    [Fact]
    public void CollectParameters_OperationLevelWinsOverPathLevelOnNameCollision()
    {
        const string pathLevel = """
        {
          "parameters": [
            { "name": "fields", "in": "query", "description": "PATH-LEVEL", "schema": { "type": "string" } }
          ]
        }
        """;
        const string opLevel = """
        {
          "parameters": [
            { "name": "fields", "in": "query", "description": "OPERATION-LEVEL", "schema": { "type": "string" } }
          ]
        }
        """;

        var infos = new List<ParamInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Mirrors ParseSwaggerPaths ordering: operation-level first, then path-level.
        CatalogLoader.CollectParameters(JsonDocument.Parse(opLevel).RootElement, infos, seen);
        CatalogLoader.CollectParameters(JsonDocument.Parse(pathLevel).RootElement, infos, seen);

        var fields = Assert.Single(infos);
        Assert.Equal("OPERATION-LEVEL", fields.Description);
    }

    [Fact]
    public void AppendParameterDetails_RendersPreferredOtherPathAndDefaults()
    {
        var endpoint = new ApiEndpoint(
            Service: "Core",
            Method: "GET",
            Path: "/v4/AuditLog/Logins",
            Summary: "Gets a set of audit log login entries.",
            Params: "count, endDate, fields, filter, limit, orderby, page, q, startDate, userId",
            HasBody: false,
            ParamInfos: new[]
            {
                new ParamInfo("startDate", "query", "string",
                    "Log time range start. Default 1 day before endDate. (Preferred over 'filter').",
                    false, true),
                new ParamInfo("endDate", "query", "string",
                    "Log time range end. Default now. (Preferred over 'filter').",
                    false, true),
                new ParamInfo("userId", "query", "integer",
                    "User Id. (Preferred over filter).",
                    false, true),
                new ParamInfo("filter", "query", "string", "Filter expression.", false, false),
                new ParamInfo("page", "query", "integer", "0-indexed page.", false, false),
                new ParamInfo("limit", "query", "integer", "Page size.", false, false),
            });

        var sb = new StringBuilder();
        SafeguardApiTool.AppendParameterDetails(sb, endpoint);
        var output = sb.ToString();

        Assert.Contains("Preferred params:", output);
        Assert.Contains("startDate (string) [preferred]", output);
        Assert.Contains("endDate (string) [preferred]", output);
        Assert.Contains("userId (integer) [preferred]", output);
        Assert.Contains("Other params:", output);
        Assert.Contains("filter", output);
        Assert.Contains("page", output);
        Assert.Contains("limit", output);
        Assert.Contains("Defaults:", output);
        Assert.Contains("startDate = 1 day before endDate", output);
        Assert.Contains("endDate = now", output);

        // The "[preferred]" tag must not bleed onto non-preferred params.
        var preferredLineEnd = output.IndexOf("Other params:", StringComparison.Ordinal);
        Assert.True(preferredLineEnd > 0);
        var otherParamsLine = output.Substring(preferredLineEnd);
        Assert.DoesNotContain("[preferred]", otherParamsLine);
    }

    [Fact]
    public void AppendParameterDetails_RendersPathParamsSeparately()
    {
        var endpoint = new ApiEndpoint(
            Service: "Core",
            Method: "GET",
            Path: "/v4/AssetAccounts/{id}/ChangePassword",
            Summary: "Change password on an asset account.",
            Params: "",
            HasBody: false,
            ParamInfos: new[]
            {
                new ParamInfo("id", "path", "integer", "Account id.", true, false),
            });

        var sb = new StringBuilder();
        SafeguardApiTool.AppendParameterDetails(sb, endpoint);
        var output = sb.ToString();

        Assert.Contains("Path params:", output);
        Assert.Contains("{id}", output);
        Assert.DoesNotContain("Preferred params:", output);
        Assert.DoesNotContain("Other params:", output);
        Assert.DoesNotContain("Defaults:", output);
    }

    [Fact]
    public void AppendParameterDetails_NoOpForEndpointWithoutParamInfos()
    {
        var endpoint = new ApiEndpoint(
            Service: "Core",
            Method: "GET",
            Path: "/v4/Me",
            Summary: "Current user.",
            Params: "",
            HasBody: false,
            ParamInfos: Array.Empty<ParamInfo>());

        var sb = new StringBuilder();
        SafeguardApiTool.AppendParameterDetails(sb, endpoint);
        Assert.Equal(string.Empty, sb.ToString());
    }
}
