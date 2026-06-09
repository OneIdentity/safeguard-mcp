using System;
using System.Text.Json;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class SafeguardRetrieveCredentialTwoBlockTests
{
    private const string Plaintext = "hunter2-not-really";

    [Fact]
    public void AssistantBlock_ContainsMetadataOnly_NoPlaintext_ForAccessRequestPassword()
    {
        var json = SafeguardRetrieveCredentialTool.BuildAssistantMetadataBlock(
            kind: "access-request-password",
            accessRequestId: 123,
            accountId: null,
            apiKeyId: null,
            retrievedAtUtc: DateTimeOffset.UtcNow);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("access-request-password", doc.RootElement.GetProperty("kind").GetString());
        Assert.Equal("retrieved", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(123, doc.RootElement.GetProperty("subject").GetProperty("accessRequestId").GetInt32());

        var delivery = doc.RootElement.GetProperty("delivery");
        Assert.Equal("user", delivery.GetProperty("block2_audience").GetString());

        Assert.DoesNotContain(Plaintext, json);
    }

    [Theory]
    [InlineData("access-request-password", "\"hunter2-not-really\"")]
    [InlineData("generated-password", "\"hunter2-not-really\"")]
    [InlineData("personal-account-password", "\"hunter2-not-really\"")]
    public void UserBlock_StringScalarKinds_ContainSecret(string kind, string body)
    {
        var text = SafeguardRetrieveCredentialTool.BuildUserAudienceBlock(
            kind: kind,
            body: body,
            accessRequestId: kind.StartsWith("access-request") ? 123 : null,
            accountId: kind.StartsWith("personal-account") ? 614 : null,
            apiKeyId: null);
        Assert.Contains(Plaintext, text);
    }

    [Fact]
    public void UserBlock_SshKey_ContainsPrivateKeyAndPublicKey()
    {
        var body = "{\"PrivateKey\":\"-----BEGIN RSA PRIVATE KEY-----secret-pem\","
                   + "\"PublicKey\":\"ssh-rsa AAAA...\","
                   + "\"Fingerprint\":\"ab:cd\",\"KeyType\":\"Rsa\",\"KeyLength\":2048}";
        var text = SafeguardRetrieveCredentialTool.BuildUserAudienceBlock(
            "access-request-ssh-key", body, accessRequestId: 9, accountId: null, apiKeyId: null);
        Assert.Contains("secret-pem", text);
        Assert.Contains("ssh-rsa AAAA", text);
        Assert.Contains("ab:cd", text);
    }

    [Fact]
    public void UserBlock_ApiKey_ContainsClientSecret()
    {
        var body = "[{\"Name\":\"k1\",\"Id\":1,\"ClientId\":\"client-a\",\"ClientSecret\":\"S3CR3T\"}]";
        var text = SafeguardRetrieveCredentialTool.BuildUserAudienceBlock(
            "access-request-api-key", body, accessRequestId: 42, accountId: null, apiKeyId: null);
        Assert.Contains("S3CR3T", text);
        Assert.Contains("client-a", text);
    }

    [Fact]
    public void UserBlock_Totp_ContainsMultipleCodesAndValidity()
    {
        var body = "[{\"Code\":\"111222\",\"Period\":30,\"TimeStamp\":\"2025-01-01T00:00:00Z\"},"
                 + "{\"Code\":\"333444\",\"Period\":30,\"TimeStamp\":\"2025-01-01T00:00:30Z\"}]";
        var text = SafeguardRetrieveCredentialTool.BuildUserAudienceBlock(
            "access-request-totp", body, accessRequestId: 77, accountId: null, apiKeyId: null);
        Assert.Contains("111222", text);
        Assert.Contains("333444", text);
        Assert.Contains("30 seconds", text);
    }

    [Fact]
    public void UserBlock_PasswordHistory_ContainsAllPasswords()
    {
        var body = "[{\"Password\":\"p1\",\"TimeStarted\":\"2025-01-01T00:00:00Z\",\"TimeEnded\":\"2025-01-02T00:00:00Z\"},"
                 + "{\"Password\":\"p2\",\"TimeStarted\":\"2025-01-02T00:00:00Z\",\"TimeEnded\":\"2025-01-03T00:00:00Z\"}]";
        var text = SafeguardRetrieveCredentialTool.BuildUserAudienceBlock(
            "personal-account-password-history", body, accessRequestId: null, accountId: 614, apiKeyId: null);
        Assert.Contains("p1", text);
        Assert.Contains("p2", text);
    }

    [Fact]
    public void UserBlock_ApiSecretHistory_ContainsAllClientSecrets()
    {
        var body = "[{\"ClientId\":\"c1\",\"ClientSecret\":\"old-s\",\"TimeStarted\":\"2025-01-01T00:00:00Z\"},"
                 + "{\"ClientId\":\"c1\",\"ClientSecret\":\"new-s\",\"TimeStarted\":\"2025-02-01T00:00:00Z\"}]";
        var text = SafeguardRetrieveCredentialTool.BuildUserAudienceBlock(
            "asset-account-api-secret-history", body, accessRequestId: null, accountId: 881, apiKeyId: 5);
        Assert.Contains("old-s", text);
        Assert.Contains("new-s", text);
    }

    [Fact]
    public void UserBlock_File_ReturnsRawBody()
    {
        var body = "FILE-CONTENT-BYTES-HERE";
        var text = SafeguardRetrieveCredentialTool.BuildUserAudienceBlock(
            "access-request-file", body, accessRequestId: 12, accountId: null, apiKeyId: null);
        Assert.Contains("FILE-CONTENT-BYTES-HERE", text);
    }
}
