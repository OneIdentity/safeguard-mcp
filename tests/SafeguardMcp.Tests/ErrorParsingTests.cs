using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class ErrorParsingTests
{
    [Theory]
    [InlineData("Safeguard API error (HTTP 400): Bad Request", 400)]
    [InlineData("Safeguard API error (HTTP 401): Unauthorized", 401)]
    [InlineData("Safeguard API error (HTTP 404): Not Found", 404)]
    [InlineData("Request failed (HTTP 503): Service Unavailable", 503)]
    [InlineData("Something happened without a code", 0)]
    [InlineData("", 0)]
    public void ExtractStatusCode_ParsesFromMessage(string message, int expected)
    {
        Assert.Equal(expected, ApiToolHelpers.ExtractStatusCode(message));
    }

    [Theory]
    [InlineData("Error (HTTP 400): {\"Message\":\"Invalid field\"}", "{\"Message\":\"Invalid field\"}")]
    [InlineData("Simple error message", "Simple error message")]
    [InlineData("Prefix): detail here", "detail here")]
    public void ExtractErrorDetail_ExtractsAfterSeparator(string message, string expected)
    {
        Assert.Equal(expected, ApiToolHelpers.ExtractErrorDetail(message));
    }

    [Theory]
    [InlineData(400, "Check request body format. Use Safeguard_Schema to see required fields.")]
    [InlineData(401, "Token expired. Call Safeguard_Connect to re-authenticate.")]
    [InlineData(403, "Insufficient permissions for this operation.")]
    [InlineData(404, "Resource not found. Verify the ID exists using a GET call.")]
    [InlineData(409, "Conflict. GET the current state first, then retry.")]
    [InlineData(422, "Validation failed. Check property types match the schema.")]
    public void GetErrorHint_ReturnsHintForKnownCodes(int statusCode, string expected)
    {
        Assert.Equal(expected, ApiToolHelpers.GetErrorHint(statusCode));
    }

    [Theory]
    [InlineData(500)]
    [InlineData(200)]
    public void GetErrorHint_ReturnsNullForUnknownCodes(int statusCode)
    {
        Assert.Null(ApiToolHelpers.GetErrorHint(statusCode));
    }

    [Fact]
    public void GetErrorHint_ContextAware_ReturnsOrderbyHintForInvalidOrderByProperty()
    {
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid order by property - 'CreatedDate desc' is not a valid property name.",
            hasModelState: false);
        Assert.Contains("orderby=-Field", hint);
        Assert.Contains("not OData", hint);
    }

    [Fact]
    public void GetErrorHint_ContextAware_OrderbyHintWinsOverModelStateHint()
    {
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid order by property - 'Name desc' is not a valid property name.",
            hasModelState: true);
        Assert.Contains("orderby=-Field", hint);
    }

    [Fact]
    public void GetErrorHint_ContextAware_ReturnsModelStateHintWhenPresent()
    {
        var hint = ApiToolHelpers.GetErrorHint(400, "The request is invalid.", hasModelState: true);
        Assert.Equal("Fix the fields listed under 'Validation errors' and retry.", hint);
    }

    [Fact]
    public void GetErrorHint_ContextAware_FallsBackToGenericHintFor400()
    {
        var hint = ApiToolHelpers.GetErrorHint(400, "Some other 400 message", hasModelState: false);
        Assert.Equal("Check request body format. Use Safeguard_Schema to see required fields.", hint);
    }

    [Fact]
    public void GetErrorHint_ContextAware_FallsBackToGenericHintForOtherCodes()
    {
        Assert.Equal(ApiToolHelpers.GetErrorHint(401), ApiToolHelpers.GetErrorHint(401, "anything", false));
        Assert.Equal(ApiToolHelpers.GetErrorHint(404), ApiToolHelpers.GetErrorHint(404, null, false));
    }

    [Fact]
    public void GetErrorHint_ContextAware_FlatFkFieldUsesGenericGuidance()
    {
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid field property - 'AssetId' is not a valid property name.",
            hasModelState: false);
        Assert.Contains("Safeguard_Schema", hint);
        Assert.Contains("nested objects", hint);
    }

    [Fact]
    public void GetErrorHint_ContextAware_DottedFieldSuggestsChildEndpoint()
    {
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid field property - 'Profiles.Id' is not a valid property name.",
            hasModelState: false);
        Assert.Contains("to-one", hint);
        Assert.Contains("sub-resource endpoint", hint);
    }

    [Fact]
    public void GetErrorHint_ContextAware_InvalidFieldFallbackWinsOverModelState()
    {
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid field property - 'AssetId' is not a valid property name.",
            hasModelState: true);
        Assert.Contains("Safeguard_Schema", hint);
    }

    [Fact]
    public void FormatModelState_ReturnsNullForNonJsonBody()
    {
        Assert.Null(ApiToolHelpers.FormatModelState("Not JSON"));
        Assert.Null(ApiToolHelpers.FormatModelState(""));
        Assert.Null(ApiToolHelpers.FormatModelState(null));
    }

    [Fact]
    public void FormatModelState_ReturnsNullWhenModelStateAbsent()
    {
        const string body = "{\"Code\":70002,\"Message\":\"Invalid field property - 'AssetId' is not a valid property name.\"}";
        Assert.Null(ApiToolHelpers.FormatModelState(body));
    }

    [Fact]
    public void FormatModelState_ExtractsSingleFieldError()
    {
        const string body = "{\"Code\":70000,\"Message\":\"The request is invalid.\",\"ModelState\":{\"entity.AccountPasswordRuleId\":[\"The field AccountPasswordRuleId must be a valid non-zero database ID.\"]}}";
        var result = ApiToolHelpers.FormatModelState(body);
        Assert.Equal(
            "- entity.AccountPasswordRuleId: The field AccountPasswordRuleId must be a valid non-zero database ID.",
            result);
    }

    [Fact]
    public void FormatModelState_ExtractsMultipleFieldErrorsAndJoinsMessages()
    {
        const string body = "{\"ModelState\":{\"entity.Name\":[\"Required.\"],\"entity.Port\":[\"Out of range.\",\"Must be > 0.\"]}}";
        var result = ApiToolHelpers.FormatModelState(body);
        Assert.Contains("- entity.Name: Required.", result);
        Assert.Contains("- entity.Port: Out of range. Must be > 0.", result);
    }

    [Fact]
    public void FormatModelState_ReturnsNullForEmptyModelState()
    {
        const string body = "{\"ModelState\":{}}";
        Assert.Null(ApiToolHelpers.FormatModelState(body));
    }

    [Fact]
    public void FormatModelState_ReturnsNullForMalformedJson()
    {
        const string body = "{\"ModelState\":";
        Assert.Null(ApiToolHelpers.FormatModelState(body));
    }
}
