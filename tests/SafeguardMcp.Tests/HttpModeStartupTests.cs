#nullable disable

using System.Collections.Generic;

namespace SafeguardMcp.Tests;

/// <summary>
/// Verifies the HTTP-mode startup lockdowns from
/// <see cref="HttpModeStartup"/>:
///
/// <list type="bullet">
///   <item><c>SAFEGUARD_HOST</c> missing → fail-fast with a clear
///   message naming the variable.</item>
///   <item>Any of <c>SAFEGUARD_PROVIDER</c> / <c>SAFEGUARD_USER</c> /
///   <c>SAFEGUARD_PASSWORD</c> present → fail-fast naming every
///   offending variable and explaining the impersonation risk.</item>
///   <item>Valid environment (only <c>SAFEGUARD_HOST</c> set) →
///   returns <c>null</c> (no error).</item>
/// </list>
///
/// The validator is exercised via its
/// <see cref="HttpModeStartup.ValidateEnvironment(System.Func{string,string})"/>
/// overload so the tests do not mutate process-wide environment state.
/// </summary>
public class HttpModeStartupTests
{
    private static Func<string, string> Env(params (string Name, string Value)[] vars)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in vars)
            map[name] = value;
        return name => map.TryGetValue(name, out var v) ? v : null;
    }

    [Fact]
    public void ValidateEnvironment_WithOnlyHostSet_ReturnsNull()
    {
        var error = HttpModeStartup.ValidateEnvironment(Env(("SAFEGUARD_HOST", "appliance.example.test")));
        Assert.Null(error);
    }

    [Fact]
    public void ValidateEnvironment_WithoutHost_ReturnsHostRequiredError()
    {
        var error = HttpModeStartup.ValidateEnvironment(Env());
        Assert.NotNull(error);
        Assert.Contains("SAFEGUARD_HOST is required", error);
        Assert.Contains("--http", error);
    }

    [Fact]
    public void ValidateEnvironment_WithBlankHost_ReturnsHostRequiredError()
    {
        var error = HttpModeStartup.ValidateEnvironment(Env(("SAFEGUARD_HOST", "   ")));
        Assert.NotNull(error);
        Assert.Contains("SAFEGUARD_HOST is required", error);
    }

    [Theory]
    [InlineData("SAFEGUARD_PROVIDER")]
    [InlineData("SAFEGUARD_USER")]
    [InlineData("SAFEGUARD_PASSWORD")]
    public void ValidateEnvironment_WithSingleForbiddenVar_FailsFastNamingIt(string forbidden)
    {
        var error = HttpModeStartup.ValidateEnvironment(Env(
            ("SAFEGUARD_HOST", "appliance.example.test"),
            (forbidden, "value")));

        Assert.NotNull(error);
        Assert.Contains(forbidden, error);
        Assert.Contains("HTTP mode forbids", error);
        // The other two forbidden vars should NOT appear in the message.
        foreach (var name in HttpModeStartup.ForbiddenEnvVars)
        {
            if (name == forbidden) continue;
            Assert.DoesNotContain(name, error);
        }
    }

    [Fact]
    public void ValidateEnvironment_WithAllForbiddenVars_NamesEveryOne()
    {
        var error = HttpModeStartup.ValidateEnvironment(Env(
            ("SAFEGUARD_HOST", "appliance.example.test"),
            ("SAFEGUARD_PROVIDER", "p"),
            ("SAFEGUARD_USER", "u"),
            ("SAFEGUARD_PASSWORD", "x")));

        Assert.NotNull(error);
        foreach (var name in HttpModeStartup.ForbiddenEnvVars)
            Assert.Contains(name, error);
        Assert.Contains("safeguard-mcp login", error);
    }

    [Fact]
    public void ValidateEnvironment_WithBlankForbiddenVar_IsTreatedAsUnset()
    {
        // Empty strings are common when an env var is "cleared" rather
        // than unset. They must not trip the lockdown.
        var error = HttpModeStartup.ValidateEnvironment(Env(
            ("SAFEGUARD_HOST", "appliance.example.test"),
            ("SAFEGUARD_PROVIDER", ""),
            ("SAFEGUARD_USER", "   "),
            ("SAFEGUARD_PASSWORD", null)));

        Assert.Null(error);
    }

    [Fact]
    public void ValidateEnvironment_HostMissingTakesPrecedenceOverForbiddenVars()
    {
        // Reporting "missing host" first keeps the failure path
        // deterministic and avoids leaking the names of credential vars
        // before the operator has even completed the host configuration.
        var error = HttpModeStartup.ValidateEnvironment(Env(
            ("SAFEGUARD_PROVIDER", "p"),
            ("SAFEGUARD_USER", "u"),
            ("SAFEGUARD_PASSWORD", "x")));

        Assert.NotNull(error);
        Assert.Contains("SAFEGUARD_HOST is required", error);
    }
}
