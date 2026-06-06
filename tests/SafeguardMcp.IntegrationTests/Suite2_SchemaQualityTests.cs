using Microsoft.Extensions.Configuration;
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
        _fixture.RequireAvailable();

        var schema = _fixture.Schema("/v4/Users", "POST");
        var requiredSection = ExtractSection(schema, "Required:");

        // Name must be required for user creation
        Assert.Contains("Name", requiredSection);

        // Auth provider reference — swagger may expose this as PrimaryAuthenticationProvider (object)
        // or PrimaryAuthenticationProviderId (integer). Either indicates the schema surfaces it.
        Assert.True(
            schema.Contains("PrimaryAuthenticationProvider", StringComparison.OrdinalIgnoreCase)
            || schema.Contains("AuthenticationProvider", StringComparison.OrdinalIgnoreCase),
            $"Expected schema to mention authentication provider reference. Schema output:\n{schema}");
    }

    [Fact]
    public void Schema_PostAssets_HasNameNetworkAddressAndPlatformId()
    {
        _fixture.RequireAvailable();

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
        _fixture.RequireAvailable();

        var schema = _fixture.Schema("/v4/AssetAccounts", "POST");
        var requiredSection = ExtractSection(schema, "Required:");

        Assert.Contains("Name", requiredSection);

        // The schema must reference the parent asset — either as a nested Asset object
        // or as an AssetId integer field
        Assert.True(
            schema.Contains("Asset", StringComparison.OrdinalIgnoreCase),
            $"Expected schema to mention Asset or AssetId. Schema output:\n{schema}");
    }

    [Fact]
    public void Schema_PostRoles_HasRequiredName()
    {
        _fixture.RequireAvailable();

        var schema = _fixture.Schema("/v4/Roles", "POST");
        var requiredSection = ExtractSection(schema, "Required:");

        Assert.Contains("Name", requiredSection);
    }

    [Fact]
    public void Schema_PostAccessPolicies_HasRoleIdAndNestedAccessRequestProperties()
    {
        _fixture.RequireAvailable();

        var schema = _fixture.Schema("/v4/AccessPolicies", "POST");
        var requestSection = ExtractSection(schema, "REQUEST BODY:");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("RoleId", requestSection);
        // Nested $ref types are rendered as "object<TypeName>" by AppendSchemaProperty.
        Assert.Contains("AccessRequestProperties (object", requestSection);
        Assert.Contains("AccessRequestProperties: Nested object", hintsSection);
    }

    [Fact]
    public void Schema_GetUsers_ResponseHasKeyFields()
    {
        _fixture.RequireAvailable();

        var schema = _fixture.Schema("/v4/Users", "GET");
        var responseSection = ExtractSection(schema, "RESPONSE BODY (GET):");

        Assert.Contains("Id", responseSection);
        Assert.Contains("Name", responseSection);
        Assert.Contains("AdminRoles", responseSection);
        Assert.Contains("Disabled", responseSection);
    }

    [Fact]
    public async Task Schema_NoConnection_ReturnsHelpfulGuidance()
    {
        // Execute without connecting should throw McpException with actionable guidance.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
            {
                ["Safeguard:IgnoreSsl"] = "true",
            })
            .Build();
        var catalogLoader = new SafeguardMcp.Catalog.CatalogLoader(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Catalog.CatalogLoader>());
        var catalogProvider = new SafeguardMcp.Catalog.CatalogProvider(
            catalogLoader, new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Catalog.CatalogProvider>());
        // Set SAFEGUARD_HOST so the "missing host" code path is bypassed
        // and the test exercises the "no bearer in request" path.
        var prevHost = Environment.GetEnvironmentVariable("SAFEGUARD_HOST");
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", "disconnected-host.example");
        try
        {
            using var disconnectedMgr = new SafeguardMcp.Tools.HttpRelaySafeguardSession(
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<SafeguardMcp.Tools.HttpRelaySafeguardSession>(),
                config, new Microsoft.AspNetCore.Http.HttpContextAccessor());
            var tool = new SafeguardMcp.Tools.SafeguardApiTool(disconnectedMgr, catalogProvider, config);

            var ex = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(
                () => tool.Safeguard_Execute(null, method: "GET", path: "/v4/Users"));

            Assert.Contains("authenticated", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SAFEGUARD_HOST", prevHost);
        }
    }

    [Fact]
    public void Schema_GetUserById_WithPathParameter_StillReturnsFieldInfo()
    {
        _fixture.RequireAvailable();

        var schema = _fixture.Schema("/v4/Users/{id}", "GET");
        var responseSection = ExtractSection(schema, "RESPONSE BODY (GET):");

        Assert.Contains("Id", responseSection);
        Assert.Contains("Name", responseSection);
    }

    [Fact]
    public void Schema_PostAssets_IncludesAgentHintsSectionWithPlatformGuidance()
    {
        _fixture.RequireAvailable();

        var schema = _fixture.Schema("/v4/Assets", "POST");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("PlatformId", hintsSection);
    }

    [Fact]
    public void Schema_PostAssets_PlatformIdHintPointsToPlatformsByDisplayName()
    {
        _fixture.RequireAvailable();

        var schema = _fixture.Schema("/v4/Assets", "POST");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("GET /v4/Platforms", hintsSection);
        Assert.Contains("DisplayName", hintsSection);
        Assert.Contains("appliance-specific", hintsSection);
    }

    [Fact]
    public void Schema_PostAssets_AssetPartitionIdHintExplainsDefaultPartition()
    {
        _fixture.RequireAvailable();

        var schema = _fixture.Schema("/v4/Assets", "POST");
        var hintsSection = ExtractSection(schema, "AGENT HINTS:");

        Assert.Contains("AssetPartitionId:", hintsSection);
        Assert.Contains("AssetPartitions", hintsSection);
    }

    [Fact]
    public void Schema_PostUsers_AuthProviderHintShowsLocalProviderPattern()
    {
        _fixture.RequireAvailable();

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
