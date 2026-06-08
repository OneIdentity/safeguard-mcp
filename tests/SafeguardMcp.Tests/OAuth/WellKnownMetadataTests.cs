#nullable disable

using System.Collections.Generic;
using System.Text.Json;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Verifies the JSON bodies returned from the well-known metadata
/// endpoints (RFC 9728 protected resource + RFC 8414 authorization
/// server). Parses the rendered text with <see cref="JsonDocument"/>
/// (the same primitive used at runtime per the project's
/// JSON-handling constraint) and asserts every field's value matches
/// what an MCP client expects.
/// </summary>
public class WellKnownMetadataTests
{
    private static BridgeRequestUrls SampleOptions()
    {
        return new BridgeRequestUrls("https://mcp.example.test", "https://mcp.example.test");
    }

    [Fact]
    public void ProtectedResource_AdvertisesMcpPublicUrlAsResourceAndAuthServer()
    {
        var json = WellKnownMetadata.BuildProtectedResourceJson(SampleOptions());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("https://mcp.example.test", root.GetProperty("resource").GetString());

        var authServers = root.GetProperty("authorization_servers");
        Assert.Equal(JsonValueKind.Array, authServers.ValueKind);
        Assert.Equal(1, authServers.GetArrayLength());
        Assert.Equal("https://mcp.example.test", authServers[0].GetString());

        var methods = StringArray(root.GetProperty("bearer_methods_supported"));
        Assert.Contains("header", methods);
    }

    [Fact]
    public void AuthorizationServer_HasEveryFieldFromPlan_2_2_b()
    {
        var json = WellKnownMetadata.BuildAuthorizationServerJson(SampleOptions());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("https://mcp.example.test", root.GetProperty("issuer").GetString());
        Assert.Equal("https://mcp.example.test/authorize", root.GetProperty("authorization_endpoint").GetString());
        Assert.Equal("https://mcp.example.test/token", root.GetProperty("token_endpoint").GetString());
        Assert.Equal("https://mcp.example.test/register", root.GetProperty("registration_endpoint").GetString());

        Assert.Equal(new[] { "code" }, StringArray(root.GetProperty("response_types_supported")));
        Assert.Equal(new[] { "authorization_code" }, StringArray(root.GetProperty("grant_types_supported")));
        Assert.Equal(new[] { "S256" }, StringArray(root.GetProperty("code_challenge_methods_supported")));
        Assert.Equal(new[] { "none" }, StringArray(root.GetProperty("token_endpoint_auth_methods_supported")));
    }

    [Fact]
    public void AuthorizationServer_DoesNotAdvertiseUnsupportedFeatures()
    {
        // Negative-property test: the bridge does not issue refresh tokens,
        // does not implement device-code, password, or client-credentials
        // grants, and does not implement implicit. None of those tokens
        // must appear in grant_types_supported / response_types_supported.
        var json = WellKnownMetadata.BuildAuthorizationServerJson(SampleOptions());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var grants = StringArray(root.GetProperty("grant_types_supported"));
        Assert.DoesNotContain("refresh_token", grants);
        Assert.DoesNotContain("password", grants);
        Assert.DoesNotContain("client_credentials", grants);
        Assert.DoesNotContain("urn:ietf:params:oauth:grant-type:device_code", grants);

        var responses = StringArray(root.GetProperty("response_types_supported"));
        Assert.DoesNotContain("token", responses);
        Assert.DoesNotContain("id_token", responses);
    }

    [Fact]
    public void Both_AreValidJson()
    {
        var opts = SampleOptions();
        JsonDocument.Parse(WellKnownMetadata.BuildProtectedResourceJson(opts)).Dispose();
        JsonDocument.Parse(WellKnownMetadata.BuildAuthorizationServerJson(opts)).Dispose();
    }

    private static string[] StringArray(JsonElement el)
    {
        Assert.Equal(JsonValueKind.Array, el.ValueKind);
        var list = new List<string>(el.GetArrayLength());
        foreach (var e in el.EnumerateArray())
            list.Add(e.GetString());
        return list.ToArray();
    }
}
