using Xunit;

namespace SafeguardMcp.IntegrationTests;

[Collection("AgentSimulation")]
public class Suite1_DiscoveryQualityTests
{
    private readonly AgentSimulationFixture _fixture;

    public Suite1_DiscoveryQualityTests(AgentSimulationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Discover_ListUsers_FindsGetUsers()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "users");
        DiscoverAssertions.AssertFindsEndpoint(result, "GET", "/v4/Users");
    }

    [Fact]
    public void Discover_CreateAsset_FindsPostAssets()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "assets", method: "POST");
        DiscoverAssertions.AssertFindsEndpoint(result, "POST", "/v4/Assets");
    }

    [Fact]
    public void Discover_ManageEntitlements_FindsRolesEndpoint()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "entitlements");
        DiscoverAssertions.AssertFindsEndpoint(result, "GET", "/v4/Roles");
    }

    [Fact]
    public void Discover_CheckHealth_FindsApplianceHealth()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "health");
        DiscoverAssertions.AssertFindsEndpoint(result, "GET", "ApplianceStatus/Health");
    }

    [Fact]
    public void Discover_PasswordCheckout_FindsCheckoutPassword()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "password checkout");
        DiscoverAssertions.AssertFindsEndpoint(result, "POST", "CheckOutPassword");
    }

    [Fact]
    public void Discover_ManagedAccounts_FindsAssetAccounts()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "managed accounts");
        DiscoverAssertions.AssertFindsEndpoint(result, "GET", "/v4/AssetAccounts");
    }

    [Fact]
    public void Discover_AccessRequest_FindsAccessRequests()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "access request");
        DiscoverAssertions.AssertFindsEndpoint(result, "GET", "/v4/AccessRequests");
    }

    [Fact]
    public void Discover_ClusterStatus_FindsClusterMembers()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "cluster");
        DiscoverAssertions.AssertFindsEndpoint(result, "GET", "Cluster/Members");
    }

    [Fact]
    public void Discover_FilterByService_ReturnsOnlyApplianceEndpoints()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(service: "Appliance");

        Assert.Contains("Appliance", result);
        Assert.DoesNotContain("Core         /", result);
        Assert.DoesNotContain("Notification ", result);
    }

    [Fact]
    public void Discover_FilterByMethod_ReturnsOnlyDeleteEndpoints()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(method: "DELETE");

        Assert.Contains("DELETE ", result);
        Assert.DoesNotContain("GET    ", result);
        Assert.DoesNotContain("POST   ", result);
    }

    [Fact]
    public void Discover_NoResults_ReturnsHelpfulGuidance()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "xyzzy_nonexistent");

        Assert.True(
            string.IsNullOrWhiteSpace(result)
            || result.Length < 40
            || result.Contains("no", StringComparison.OrdinalIgnoreCase),
            $"Expected a no-results message or short empty output, but got:{Environment.NewLine}{result}");
    }

    [Fact]
    public void Discover_NarrowBroadSearch_FindsPostUsersWithoutBroadGetUsers()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "users", method: "POST");

        DiscoverAssertions.AssertFindsEndpoint(result, "POST", "/v4/Users");
        Assert.DoesNotContain("GET    Core         /v4/Users", result);
    }

    [Fact]
    public void Discover_BulkImport_FindsBatchCreate()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "batch");
        DiscoverAssertions.AssertFindsEndpoint(result, "POST", "BatchCreate");
    }

    [Fact]
    public void Discover_AuditTrail_FindsAuditLog()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "audit");
        DiscoverAssertions.AssertFindsEndpoint(result, "GET", "AuditLog");
    }

    [Fact]
    public void Discover_PrivilegedAccess_FindsAccessRequests()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "privileged access");
        DiscoverAssertions.AssertFindsEndpoint(result, "GET", "AccessRequests");
    }

    [Fact]
    public void Discover_UserGroups_FindsUserGroups()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "user group");
        DiscoverAssertions.AssertFindsEndpoint(result, "GET", "/v4/UserGroups");
    }

    [Fact]
    public void Discover_CheckoutSynonym_FindsCheckoutPassword()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "checkout");
        DiscoverAssertions.AssertFindsEndpoint(result, "POST", "CheckOutPassword");
    }

    [Fact]
    public void Discover_DiscoveryJobs_ReturnsDiscoveredEndpoints()
    {
        _fixture.RequireAvailable();

        var result = _fixture.Discover(search: "discovery");
        Assert.Contains("Discovered", result);
    }
}
