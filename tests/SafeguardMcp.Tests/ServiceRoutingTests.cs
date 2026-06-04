using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class ServiceRoutingTests
{
    [Theory]
    [InlineData("/v4/ApplianceStatus", Service.Appliance)]
    [InlineData("/v4/ApplianceStatus/Health", Service.Appliance)]
    [InlineData("/v4/Backups", Service.Appliance)]
    [InlineData("/v4/Backups/123/Download", Service.Appliance)]
    [InlineData("/v4/NetworkInterfaces", Service.Appliance)]
    [InlineData("/v4/NetworkDiagnostics", Service.Appliance)]
    [InlineData("/v4/DiagnosticPackage", Service.Appliance)]
    [InlineData("/v4/Patches", Service.Appliance)]
    [InlineData("/v4/Patches/1/Install", Service.Appliance)]
    public void ResolveServiceHeuristic_Appliance(string path, Service expected)
    {
        Assert.Equal(expected, ApiToolHelpers.ResolveServiceHeuristic(path));
    }

    [Theory]
    [InlineData("/v4/Status", Service.Notification)]
    [InlineData("/v4/Status/Availability", Service.Notification)]
    [InlineData("/v4/Status/Cluster", Service.Notification)]
    [InlineData("/v4/Status/ClusterPatch", Service.Notification)]
    [InlineData("/v4/Status/Maintenance", Service.Notification)]
    public void ResolveServiceHeuristic_Notification(string path, Service expected)
    {
        Assert.Equal(expected, ApiToolHelpers.ResolveServiceHeuristic(path));
    }

    [Theory]
    [InlineData("/v4/Users", Service.Core)]
    [InlineData("/v4/Assets", Service.Core)]
    [InlineData("/v4/AssetAccounts", Service.Core)]
    [InlineData("/v4/Roles", Service.Core)]
    [InlineData("/v4/AccessRequests", Service.Core)]
    [InlineData("/v4/AccessPolicies", Service.Core)]
    [InlineData("/v4/AuditLog", Service.Core)]
    [InlineData("/v4/Me", Service.Core)]
    [InlineData("/v4/A2ARegistrations", Service.Core)]
    public void ResolveServiceHeuristic_Core(string path, Service expected)
    {
        Assert.Equal(expected, ApiToolHelpers.ResolveServiceHeuristic(path));
    }

    [Fact]
    public void ResolveServiceHeuristic_PatchPolicies_IsCore_NotAppliance()
    {
        // PatchPolicies is a Core endpoint, not Appliance — the heuristic must not match "Patch" in it
        Assert.Equal(Service.Core, ApiToolHelpers.ResolveServiceHeuristic("/v4/PatchPolicies"));
    }

    [Theory]
    [InlineData("Core", Service.Core)]
    [InlineData("Appliance", Service.Appliance)]
    [InlineData("Notification", Service.Notification)]
    [InlineData("CORE", Service.Core)]
    [InlineData("appliance", Service.Appliance)]
    [InlineData("notification", Service.Notification)]
    [InlineData("Unknown", Service.Core)]
    [InlineData("", Service.Core)]
    public void ParseServiceName_ParsesCorrectly(string input, Service expected)
    {
        Assert.Equal(expected, ApiToolHelpers.ParseServiceName(input));
    }

    [Theory]
    [InlineData(Service.Core, "Core")]
    [InlineData(Service.Appliance, "Appliance")]
    [InlineData(Service.Notification, "Notification")]
    public void GetServiceName_ReturnsCorrectString(Service service, string expected)
    {
        Assert.Equal(expected, ApiToolHelpers.GetServiceName(service));
    }
}
