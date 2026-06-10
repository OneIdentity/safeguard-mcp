using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Locks in that <see cref="SafeguardApiTool.TryParseErrorBody"/> recovers
/// the appliance JSON from every body shape we have observed, and that the
/// smart-hint branches in <see cref="ApiToolHelpers"/> fire end-to-end on
/// the parsed message. The wrapper-prose case in particular guards against
/// the regression that silently disabled the entire hint surface when the
/// SDK's <c>Exception.Message</c> was passed in instead of
/// <c>SafeguardDotNetException.Response</c>.
/// </summary>
public class ErrorHintParsingTests
{
    [Fact]
    public void TryParseErrorBody_PureJson_ParsesCodeAndMessage()
    {
        const string body = "{\"Code\":70002,\"Message\":\"Invalid field property - 'Foo' is not a valid property name.\"}";

        var ok = SafeguardApiTool.TryParseErrorBody(body, out var apiMessage, out var apiCode, out var inner);

        Assert.True(ok);
        Assert.Equal("Invalid field property - 'Foo' is not a valid property name.", apiMessage);
        Assert.Equal("70002", apiCode);
        Assert.Null(inner);
    }

    [Fact]
    public void TryParseErrorBody_LeadingWhitespace_ParsesCodeAndMessage()
    {
        const string body = "   \r\n  {\"Code\":70001,\"Message\":\"Invalid order by property - '-Name' is not a valid property name.\"}";

        var ok = SafeguardApiTool.TryParseErrorBody(body, out var apiMessage, out var apiCode, out _);

        Assert.True(ok);
        Assert.Equal("70001", apiCode);
        Assert.Contains("Invalid order by property", apiMessage);
    }

    [Fact]
    public void TryParseErrorBody_WrapperProsePrefix_ParsesEmbeddedJson()
    {
        // Mirrors the legacy SDK Exception.Message shape we may still see
        // when SafeguardDotNetException.Response is empty and we fall back
        // to .Message.
        const string body = "Error returned from Safeguard API, Error: BadRequest "
            + "{\"Code\":70009,\"Message\":\"Invalid filter property - 'Bar' is not a valid property name.\"}";

        var ok = SafeguardApiTool.TryParseErrorBody(body, out var apiMessage, out var apiCode, out _);

        Assert.True(ok);
        Assert.Equal("70009", apiCode);
        Assert.Contains("Invalid filter property", apiMessage);
    }

    [Fact]
    public void TryParseErrorBody_HtmlBody_ReturnsFalse()
    {
        const string body = "<html><head><title>500</title></head><body>Internal Server Error</body></html>";

        var ok = SafeguardApiTool.TryParseErrorBody(body, out var apiMessage, out var apiCode, out var inner);

        Assert.False(ok);
        Assert.Null(apiMessage);
        Assert.Null(apiCode);
        Assert.Null(inner);
    }

    [Fact]
    public void TryParseErrorBody_EmptyBody_ReturnsFalse()
    {
        Assert.False(SafeguardApiTool.TryParseErrorBody("", out _, out _, out _));
        Assert.False(SafeguardApiTool.TryParseErrorBody(null, out _, out _, out _));
        Assert.False(SafeguardApiTool.TryParseErrorBody("   ", out _, out _, out _));
    }

    [Fact]
    public void TryParseErrorBody_JsonArray_ReturnsFalse()
    {
        // Only object-shaped bodies are appliance error envelopes; arrays
        // are valid 2xx response bodies and must not be misread as errors.
        const string body = "[{\"Code\":70002}]";

        var ok = SafeguardApiTool.TryParseErrorBody(body, out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void Pipeline_70002InvalidFieldProperty_FiresPropertyHintAndSuggestsCandidate()
    {
        const string body = "Error returned from Safeguard API, Error: BadRequest "
            + "{\"Code\":70002,\"Message\":\"Invalid field property - 'Naem' is not a valid property name.\"}";

        Assert.True(SafeguardApiTool.TryParseErrorBody(body, out var apiMessage, out _, out _));

        var paths = new[]
        {
            new ApiSchemaPropertyPath("Id", "integer", null, false, false),
            new ApiSchemaPropertyPath("Name", "string", null, false, false),
            new ApiSchemaPropertyPath("Description", "string", null, false, false),
        };
        var ctx = new ErrorContext("core", "GET", "/v4/Assets");

        var hint = ApiToolHelpers.GetErrorHint(400, apiMessage, hasModelState: false, ctx, paths);

        Assert.NotNull(hint);
        Assert.Contains("'Naem'", hint);
        Assert.Contains("`Name`", hint);
    }

    [Fact]
    public void Pipeline_70001InvalidOrderByProperty_FiresOrderByHint()
    {
        const string body = "Error returned from Safeguard API, Error: BadRequest "
            + "{\"Code\":70001,\"Message\":\"Invalid order by property - 'Naem' is not a valid property name.\"}";

        Assert.True(SafeguardApiTool.TryParseErrorBody(body, out var apiMessage, out _, out _));

        var paths = new[]
        {
            new ApiSchemaPropertyPath("Name", "string", null, false, false),
        };
        var ctx = new ErrorContext("core", "GET", "/v4/Assets");

        var hint = ApiToolHelpers.GetErrorHint(400, apiMessage, hasModelState: false, ctx, paths);

        Assert.NotNull(hint);
        Assert.Contains("`Name`", hint);
        Assert.Contains("descending: `-Name`", hint);
    }

    [Fact]
    public void Pipeline_70009InvalidFilterProperty_FiresFilterHint()
    {
        const string body = "Error returned from Safeguard API, Error: BadRequest "
            + "{\"Code\":70009,\"Message\":\"Invalid filter property - 'Naem' is not a valid property name.\"}";

        Assert.True(SafeguardApiTool.TryParseErrorBody(body, out var apiMessage, out _, out _));

        var paths = new[]
        {
            new ApiSchemaPropertyPath("Name", "string", null, false, false),
        };
        var ctx = new ErrorContext("core", "GET", "/v4/Assets");

        var hint = ApiToolHelpers.GetErrorHint(400, apiMessage, hasModelState: false, ctx, paths);

        Assert.NotNull(hint);
        Assert.Contains("filter property", hint);
        Assert.Contains("`Name`", hint);
    }

    [Fact]
    public void Pipeline_90408_FiresWrongAssetHint()
    {
        const string body = "Error returned from Safeguard API, Error: BadRequest "
            + "{\"Code\":90408,\"Message\":\"User is not authorized to use this request type.\"}";

        Assert.True(SafeguardApiTool.TryParseErrorBody(body, out var apiMessage, out var apiCode, out _));
        Assert.Equal("90408", apiCode);

        var ctx = new ErrorContext("core", "POST", "/v4/AccessRequests");
        var hint = ApiToolHelpers.GetErrorHint(400, apiMessage, hasModelState: false, ctx, paths: null);

        Assert.NotNull(hint);
        Assert.Contains("90408", hint);
        Assert.Contains("RequestEntitlements", hint);
        Assert.Contains("Safeguard_OpenAccessRequest", hint);
    }
}
