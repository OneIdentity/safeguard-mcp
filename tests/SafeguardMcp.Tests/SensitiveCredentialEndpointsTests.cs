using System.Linq;
using System.Text.Json;
using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class SensitiveCredentialEndpointsTests
{
    [Theory]
    [InlineData("POST", "/v4/AccessRequests/123/CheckOutPassword", "access-request-password", "accessRequestId", "123")]
    [InlineData("POST", "/v4/AccessRequests/9/CheckOutSshKey", "access-request-ssh-key", "accessRequestId", "9")]
    [InlineData("POST", "/v4/AccessRequests/42/CheckOutApiKeys", "access-request-api-key", "accessRequestId", "42")]
    [InlineData("POST", "/v4/AccessRequests/77/CheckOutTotp", "access-request-totp", "accessRequestId", "77")]
    [InlineData("POST", "/v4/AccessRequests/12/CheckOutFile", "access-request-file", "accessRequestId", "12")]
    [InlineData("POST", "/v4/AccessRequests/3-4-6-8951-1-d7c2dff871514f669a61163dbd8548fa-0003/CheckOutPassword",
        "access-request-password", "accessRequestId", "3-4-6-8951-1-d7c2dff871514f669a61163dbd8548fa-0003")]
    [InlineData("GET", "/v4/Me/EnterpriseAccounts/614/Password", "personal-account-password", "accountId", "614")]
    [InlineData("GET", "/v4/Me/EnterpriseAccounts/614/Passwords", "personal-account-password-history", "accountId", "614")]
    [InlineData("GET", "/v4/Me/EnterpriseAccounts/614/TotpAuthenticator/Values", "personal-account-totp", "accountId", "614")]
    public void TryMatch_ReturnsCorrectKindAndArgument_ForCanonicalPaths(
        string method, string path, string expectedKind, string argName, string argValue)
    {
        var m = SensitiveCredentialEndpoints.TryMatch(method, path);
        Assert.NotNull(m);
        Assert.Equal(expectedKind, m.Entry.Kind);
        Assert.Single(m.ExtractedArguments);
        Assert.Equal(argName, m.ExtractedArguments[0].Key);
        Assert.Equal(argValue, m.ExtractedArguments[0].Value);
    }

    [Fact]
    public void TryMatch_AssetAccountApiSecretHistory_ExtractsBothIds()
    {
        var m = SensitiveCredentialEndpoints.TryMatch(
            "GET", "/v4/AssetAccounts/881/ApiKeys/5/ClientSecrets");
        Assert.NotNull(m);
        Assert.Equal("asset-account-api-secret-history", m.Entry.Kind);
        Assert.Equal(2, m.ExtractedArguments.Count);
        Assert.Equal("accountId", m.ExtractedArguments[0].Key);
        Assert.Equal("881", m.ExtractedArguments[0].Value);
        Assert.Equal("apiKeyId", m.ExtractedArguments[1].Key);
        Assert.Equal("5", m.ExtractedArguments[1].Value);
    }

    [Fact]
    public void TryMatch_GeneratedPassword_NoIdsExtracted()
    {
        var m = SensitiveCredentialEndpoints.TryMatch(
            "POST", "/v4/Me/EnterpriseAccounts/GeneratePassword");
        Assert.NotNull(m);
        Assert.Equal("generated-password", m.Entry.Kind);
        Assert.Empty(m.ExtractedArguments);
    }

    [Theory]
    [InlineData("POST", "/v4/AccessRequests")]
    [InlineData("POST", "/v4/AccessRequests/123/CheckIn")]
    [InlineData("GET", "/v4/AssetAccounts/881")]
    [InlineData("GET", "/v4/Me")]
    [InlineData("POST", "/v4/AccessRequests/123/InitializeSession")]
    public void TryMatch_NonSensitivePaths_ReturnNull(string method, string path)
    {
        Assert.Null(SensitiveCredentialEndpoints.TryMatch(method, path));
    }

    [Fact]
    public void GetByKind_UnknownKind_ReturnsNull()
    {
        Assert.Null(SensitiveCredentialEndpoints.GetByKind("not-a-real-kind"));
    }

    [Fact]
    public void RedirectEnvelope_ContainsStructuredNextCall_ForAccessRequestPassword()
    {
        var match = SensitiveCredentialEndpoints.TryMatch("POST", "/v4/AccessRequests/123/CheckOutPassword");
        Assert.NotNull(match);
        var json = SafeguardApiTool.BuildSensitiveEndpointRedirectEnvelope(
            "POST", "/v4/AccessRequests/123/CheckOutPassword", match);

        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal("sensitive_endpoint_redirected", data.GetProperty("error").GetString());
        Assert.Equal("/v4/AccessRequests/123/CheckOutPassword", data.GetProperty("refusedPath").GetString());

        var next = data.GetProperty("next_call");
        Assert.Equal("Safeguard_RetrieveCredential", next.GetProperty("tool").GetString());
        var args = next.GetProperty("arguments");
        Assert.Equal("access-request-password", args.GetProperty("kind").GetString());
        Assert.Equal(123, args.GetProperty("accessRequestId").GetInt32());

        var notices = doc.RootElement.GetProperty("meta").GetProperty("notices");
        Assert.True(notices.GetArrayLength() > 0);
        Assert.Equal("sensitive_endpoint_redirected",
            notices[0].GetProperty("kind").GetString());
    }

    [Fact]
    public void RedirectEnvelope_AssetAccountApiSecretHistory_IncludesBothArgs()
    {
        var match = SensitiveCredentialEndpoints.TryMatch(
            "GET", "/v4/AssetAccounts/881/ApiKeys/5/ClientSecrets");
        var json = SafeguardApiTool.BuildSensitiveEndpointRedirectEnvelope(
            "GET", "/v4/AssetAccounts/881/ApiKeys/5/ClientSecrets", match);

        using var doc = JsonDocument.Parse(json);
        var args = doc.RootElement.GetProperty("data").GetProperty("next_call").GetProperty("arguments");
        Assert.Equal("asset-account-api-secret-history", args.GetProperty("kind").GetString());
        Assert.Equal(881, args.GetProperty("accountId").GetInt32());
        Assert.Equal(5, args.GetProperty("apiKeyId").GetInt32());
    }

    [Fact]
    public void RedirectEnvelope_AccessRequestPassword_PassesDashedIdAsString()
    {
        const string requestId = "3-4-6-8951-1-d7c2dff871514f669a61163dbd8548fa-0003";
        var path = "/v4/AccessRequests/" + requestId + "/CheckOutPassword";
        var match = SensitiveCredentialEndpoints.TryMatch("POST", path);
        Assert.NotNull(match);
        var json = SafeguardApiTool.BuildSensitiveEndpointRedirectEnvelope("POST", path, match);

        using var doc = JsonDocument.Parse(json);
        var args = doc.RootElement.GetProperty("data").GetProperty("next_call").GetProperty("arguments");
        Assert.Equal("access-request-password", args.GetProperty("kind").GetString());
        Assert.Equal(requestId, args.GetProperty("accessRequestId").GetString());
    }
}
