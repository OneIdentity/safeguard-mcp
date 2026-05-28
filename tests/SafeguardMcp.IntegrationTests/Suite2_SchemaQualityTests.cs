using Xunit;

namespace SafeguardMcp.IntegrationTests;

[Collection("AgentSimulation")]
public class Suite2_SchemaQualityTests
{
    private readonly AgentSimulationFixture _fixture;

    public Suite2_SchemaQualityTests(AgentSimulationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Schema_PostUsers_HasRequiredNameAndAuthProvider()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Users", "POST");
        var requiredSection = ExtractSection(schema, "Required:");

        Assert.Contains("Name", schema);
        Assert.Contains("PrimaryAuthenticationProvider", schema);
        Assert.Contains("Name", requiredSection);
        Assert.Contains("PrimaryAuthenticationProvider", requiredSection);
    }

    [Fact]
    public void Schema_PostAssets_HasNameNetworkAddressAndPlatformId()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Assets", "POST");
        var requestSection = ExtractSection(schema, "REQUEST BODY:");
        var requiredSection = ExtractSection(schema, "Required:");

        Assert.Contains("Name", requiredSection);
        Assert.Contains("NetworkAddress", requestSection);
        Assert.Contains("PlatformId", requestSection);
    }

    [Fact]
    public void Schema_PostAssetAccounts_HasRequiredNameAndNestedAssetReference()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/AssetAccounts", "POST");
        var requiredSection = ExtractSection(schema, "Required:");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("Name", requiredSection);
        Assert.Contains("Asset", schema);
        Assert.Contains("Asset: Nested object reference.", hintsSection);
        Assert.Contains("{\"Id\": <assetId>}", hintsSection);
    }

    [Fact]
    public void Schema_PostRoles_HasRequiredName()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Roles", "POST");
        var requiredSection = ExtractSection(schema, "Required:");

        Assert.Contains("Name", requiredSection);
    }

    [Fact]
    public void Schema_PostAccessPolicies_HasRoleIdAndNestedAccessRequestProperties()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/AccessPolicies", "POST");
        var requestSection = ExtractSection(schema, "REQUEST BODY:");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("RoleId", requestSection);
        Assert.Contains("AccessRequestProperties (object)", requestSection);
        Assert.Contains("AccessRequestProperties: Nested object", hintsSection);
    }

    [Fact]
    public void Schema_GetUsers_ResponseHasKeyFields()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Users", "GET");
        var responseSection = ExtractSection(schema, "RESPONSE BODY (GET):");

        Assert.Contains("Id", responseSection);
        Assert.Contains("Name", responseSection);
        Assert.Contains("AdminRoles", responseSection);
        Assert.Contains("Disabled", responseSection);
    }

    [Fact(Skip = "Cannot test disconnected state within connected fixture")]
    public void Schema_NoConnection_ReturnsHelpfulGuidance()
    {
    }

    [Fact]
    public void Schema_GetUserById_WithPathParameter_StillReturnsFieldInfo()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Users/{id}", "GET");
        var responseSection = ExtractSection(schema, "RESPONSE BODY (GET):");

        Assert.Contains("Id", responseSection);
        Assert.Contains("Name", responseSection);
    }

    [Fact]
    public void Schema_PostAssets_IncludesAgentHintsSectionWithPlatformGuidance()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Assets", "POST");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("PlatformId", hintsSection);
    }

    [Fact]
    public void Schema_PostAssets_PlatformIdHintPointsToPlatformsByDisplayName()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Assets", "POST");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("GET /v4/Platforms", hintsSection);
        Assert.Contains("DisplayName", hintsSection);
        Assert.Contains("appliance-specific", hintsSection);
    }

    [Fact]
    public void Schema_PostAssets_AssetPartitionIdHintExplainsDefaultPartition()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Assets", "POST");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("AssetPartitionId: Use -1 for the default asset partition.", hintsSection);
    }

    [Fact]
    public void Schema_PostUsers_AuthProviderHintShowsLocalProviderPattern()
    {
        if (!_fixture.Available) return;

        var schema = _fixture.Schema("/v4/Users", "POST");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("PrimaryAuthenticationProvider", hintsSection);
        Assert.Contains("{\"Id\": -1}", hintsSection);
    }

    private static string ExtractSection(string text, string header)
    {
        var lines = text.Replace("\r", string.Empty).Split('\n');
        var startIndex = -1;
        var headerIndent = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].TrimStart().StartsWith(header, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            startIndex = i + 1;
            headerIndent = LeadingWhitespaceCount(lines[i]);
            break;
        }

        if (startIndex < 0)
        {
            return string.Empty;
        }

        var section = new List<string>();
        for (var i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();
            if (trimmed.Length > 0
                && LeadingWhitespaceCount(line) <= headerIndent
                && IsSectionHeader(trimmed))
            {
                break;
            }

            section.Add(line);
        }

        return string.Join('\n', section).Trim();
    }

    private static int LeadingWhitespaceCount(string line)
    {
        var count = 0;
        while (count < line.Length && char.IsWhiteSpace(line[count]))
        {
            count++;
        }

        return count;
    }

    private static bool IsSectionHeader(string text)
        => text.EndsWith(":", StringComparison.Ordinal);
}
