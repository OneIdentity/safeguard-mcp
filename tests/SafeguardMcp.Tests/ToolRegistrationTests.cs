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
}
