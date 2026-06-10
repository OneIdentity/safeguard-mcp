using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Tests for Safeguard_Discover's compact / verbose listing modes and the
/// producer-side size guard. Default mode emits one line per endpoint at
/// cap=200; verbose=true emits per-parameter details at cap=20 with a
/// narrow-first short-circuit when matches exceed the cap; an ~18 KB size
/// guard truncates and prepends a notice naming the available narrowers.
/// </summary>
public class DiscoverVerboseTests
{
    private static ApiEndpoint AuditEndpoint(string path, string summary, ParamInfo[] paramInfos)
        => new ApiEndpoint("Core", "GET", path, summary, "", false, paramInfos);

    private static ApiEndpoint AuditEndpoint(string path, string summary)
        => new ApiEndpoint("Core", "GET", path, summary, "", false);

    private static ParamInfo[] FatParams() => new[]
    {
        new ParamInfo("startDate", "query", "string",
            "Log time range start. Default 1 day before endDate. (Preferred over 'filter').",
            false, true),
        new ParamInfo("endDate", "query", "string",
            "Log time range end. Default now. (Preferred over 'filter').",
            false, true),
        new ParamInfo("filter", "query", "string", "Filter expression.", false, false),
        new ParamInfo("page", "query", "integer", "0-indexed page.", false, false),
        new ParamInfo("limit", "query", "integer", "Page size.", false, false),
    };

    /// <summary>Synthesizes ~30 GET audit-log endpoints so default-mode output is wide
    /// enough to exercise cap=200 and the shape-hint threshold without recipe noise.</summary>
    private static ApiEndpoint[] BuildAuditFixture(int count = 30)
    {
        var list = new List<ApiEndpoint>();
        var p = FatParams();
        for (int i = 0; i < count; i++)
        {
            list.Add(AuditEndpoint($"/v4/AuditLog/Logins/{i}", $"Audit log login entry {i}", p));
            list.Add(AuditEndpoint($"/v4/AuditLog/ObjectChanges/{i}", $"Audit log object change {i}", p));
        }
        return list.ToArray();
    }

    [Fact]
    public void Discover_DefaultMode_OneLinePerEndpoint_NoParameterDetails()
    {
        var results = BuildAuditFixture(5);

        var output = SafeguardApiTool.FormatDiscovery(results, null, "audit log", "GET", verbose: false);

        Assert.DoesNotContain("Preferred params:", output);
        Assert.DoesNotContain("Other params:", output);
        Assert.DoesNotContain("Defaults:", output);
        Assert.Contains("/v4/AuditLog/Logins/0", output);
    }

    [Fact]
    public void Discover_DefaultMode_AuditLogGet_FitsUnderSizeGuard()
    {
        var results = BuildAuditFixture(30);

        var output = SafeguardApiTool.FormatDiscovery(results, null, "audit log", "GET", verbose: false);

        // ~18 KB producer budget; default mode must keep "audit log" GET well under it.
        Assert.True(output.Length < 18 * 1024,
            $"Default-mode audit log GET should fit under 18 KB; was {output.Length}");
        Assert.DoesNotContain("Output truncated", output);
    }

    [Fact]
    public void Discover_DefaultMode_RaisesCapTo200()
    {
        // 220 endpoints: only 200 should be shown; the rest report as "... and N more".
        var list = new List<ApiEndpoint>();
        for (int i = 0; i < 220; i++)
            list.Add(new ApiEndpoint("Core", "GET", $"/v4/Things/{i}", "thing", "", false));

        var output = SafeguardApiTool.FormatDiscovery(list.ToArray(), null, null, null, verbose: false);

        Assert.Contains("/v4/Things/199", output);
        Assert.DoesNotContain("/v4/Things/200", output);
        Assert.Contains("... and 20 more", output);
    }

    [Fact]
    public void Discover_VerboseMode_NarrowResults_EmitsParameterDetails()
    {
        var results = new[] { AuditEndpoint("/v4/AuditLog/Logins", "Logins", FatParams()) };

        var output = SafeguardApiTool.FormatDiscovery(results, null, "audit log", "GET", verbose: true);

        Assert.Contains("Preferred params:", output);
        Assert.Contains("startDate", output);
        Assert.Contains("Defaults:", output);
    }

    [Fact]
    public void Discover_VerboseMode_TooManyMatches_ShortCircuitsToPathsOnly()
    {
        // 30 matches, well over the verbose cap of 20.
        var results = BuildAuditFixture(15);

        var output = SafeguardApiTool.FormatDiscovery(results, null, "audit log", "GET", verbose: true);

        Assert.Contains("Too many matches", output);
        Assert.Contains("narrow further", output);
        Assert.DoesNotContain("Preferred params:", output);
        Assert.DoesNotContain("Defaults:", output);
        // Paths still listed.
        Assert.Contains("/v4/AuditLog/Logins/0", output);
    }

    [Fact]
    public void Discover_SizeGuard_PrependsNoticeNamingNarrowers()
    {
        // Verbose with one massive endpoint whose param-details push past 18 KB.
        var bloatedDescription = new string('x', 25 * 1024);
        var fatParam = new ParamInfo("startDate", "query", "string",
            $"Default 1 day before endDate. {bloatedDescription}",
            false, true);
        var endpoint = new ApiEndpoint("Core", "GET", "/v4/AuditLog/Bloat", "bloat", "", false,
            new[] { fatParam });

        var output = SafeguardApiTool.FormatDiscovery(new[] { endpoint }, null, "audit", "GET", verbose: true);

        Assert.StartsWith("Output truncated", output);
        Assert.Contains("search=", output);
        Assert.Contains("method=", output);
        Assert.Contains("service=", output);
        Assert.True(output.Length <= 18 * 1024 + 512,
            $"Size-guarded output must stay near the 18 KB cap; was {output.Length}");
    }
}
