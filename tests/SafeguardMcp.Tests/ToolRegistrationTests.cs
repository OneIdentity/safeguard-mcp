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
    public void SafeguardExecute_Description_AdvertisesBulkReflexAndBatchSiblings()
    {
        var method = typeof(SafeguardApiTool).GetMethod(
            "Safeguard_Execute",
            BindingFlags.Public | BindingFlags.Instance)!;
        var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;

        Assert.NotNull(description);
        Assert.Contains("Bulk reflex", description);
        Assert.Contains("BatchDelete", description);
        // The six resources the appliance exposes Batch* on — confirmed against
        // pangaeaappliance/src/Service/Core/Controllers/**/*Controller_Batch.cs.
        Assert.Contains("/v4/Assets", description);
        Assert.Contains("/v4/AssetAccounts", description);
        Assert.Contains("/v4/Users", description);
        Assert.Contains("/v4/UserGroups", description);
        Assert.Contains("/v4/AccountGroups", description);
        Assert.Contains("/v4/AssetGroups", description);
    }
}
