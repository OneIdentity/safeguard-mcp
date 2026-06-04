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
