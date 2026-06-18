using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ModelContextProtocol.Server;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class ToolRegistrationTests
{
    [Fact]
    public void SafeguardApiTool_DoesNotRegisterSafeguardStatus()
    {
        var registeredNames = typeof(SafeguardApiTool)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        Assert.DoesNotContain("Safeguard_Status", registeredNames);
        Assert.Contains("Safeguard_Connect", registeredNames);
        Assert.Contains("Safeguard_Disconnect", registeredNames);
    }

    [Fact]
    public void SafeguardApiTool_HasNoSafeguardStatusMethod()
    {
        var method = typeof(SafeguardApiTool).GetMethod(
            "Safeguard_Status",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.Null(method);
    }

    [Fact]
    public void SafeguardExecute_Description_PointsToBatchGuidance()
    {
        var method = typeof(SafeguardApiTool).GetMethod(
            "Safeguard_Execute",
            BindingFlags.Public | BindingFlags.Instance)!;
        var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;

        Assert.NotNull(description);
        // Bulk/Batch lore was relocated to the common-patterns resource (Stage 2); the
        // always-on description only points the agent at it via Discover search='Batch'.
        Assert.Contains("Batch", description);
        Assert.Contains("Discover", description);
    }

    [Fact]
    public void CommonPatternsResource_DocumentsBatchEndpoints()
    {
        var content = SafeguardMcp.Catalog.EmbeddedResources.Load("common-patterns.md");

        Assert.Contains("BatchDelete", content);
        // The six resources the appliance exposes Batch* on — confirmed against
        // pangaeaappliance/src/Service/Core/Controllers/**/*Controller_Batch.cs.
        Assert.Contains("Assets", content);
        Assert.Contains("AssetAccounts", content);
        Assert.Contains("Users", content);
        Assert.Contains("UserGroups", content);
        Assert.Contains("AccountGroups", content);
        Assert.Contains("AssetGroups", content);
    }
}
