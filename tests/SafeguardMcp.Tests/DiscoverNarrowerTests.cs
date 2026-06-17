using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Tests the narrower-required contract, the query→search alias, and the
/// appliance-diagnostic synonym group.
/// </summary>
public class DiscoverNarrowerTests
{
    private static ApiEndpoint Ep(string service, string method, string path, string summary)
        => new ApiEndpoint(service, method, path, summary, "", false);

    private static ApiEndpoint[] ApplianceFixture() => new[]
    {
        Ep("Appliance", "GET", "/v4/SystemTime", "Get the current system time."),
        Ep("Appliance", "GET", "/v4/Version", "Get the appliance software version."),
        Ep("Appliance", "GET", "/v4/ApplianceStatus", "Get appliance status."),
        Ep("Appliance", "GET", "/v4/ApplianceStatus/Health", "Get appliance health checks."),
        Ep("Core", "GET", "/v4/Users", "Get a list of users."),
        Ep("Core", "GET", "/v4/Assets", "Get a list of assets."),
    };

    // --- No-narrower directive ---------------------------------------------

    [Fact]
    public void Discover_NoNarrowers_ReturnsDirective_NotCatalogDump()
    {
        var output = SafeguardApiTool.FormatDiscovery(ApplianceFixture(), null, null, null, verbose: false);

        Assert.Contains("requires at least one narrower", output);
        Assert.Contains("service=\"Appliance\"", output);
        Assert.DoesNotContain("/v4/Users", output);
        Assert.DoesNotContain("/v4/SystemTime", output);
    }

    [Fact]
    public void Discover_NoNarrowers_VerboseTrue_StillReturnsDirective()
    {
        var output = SafeguardApiTool.FormatDiscovery(ApplianceFixture(), null, null, null, verbose: true);

        Assert.Contains("requires at least one narrower", output);
    }

    [Fact]
    public void Discover_WhitespaceOnlyNarrowers_TreatedAsAbsent()
    {
        var output = SafeguardApiTool.FormatDiscovery(ApplianceFixture(), "  ", "\t", "", verbose: false);

        Assert.Contains("requires at least one narrower", output);
    }

    [Fact]
    public void Discover_ServiceOnlyNarrower_DoesNotTriggerDirective()
    {
        var output = SafeguardApiTool.FormatDiscovery(ApplianceFixture(), "Appliance", null, null, verbose: false);

        Assert.DoesNotContain("requires at least one narrower", output);
        Assert.Contains("/v4/SystemTime", output);
    }

    [Fact]
    public void Discover_MethodOnlyNarrower_DoesNotTriggerDirective()
    {
        var output = SafeguardApiTool.FormatDiscovery(ApplianceFixture(), null, null, "GET", verbose: false);

        Assert.DoesNotContain("requires at least one narrower", output);
    }

    // --- query alias --------------------------------------------------------

    [Fact]
    public void ResolveDiscoverSearch_QueryAloneUsedAsSearch()
    {
        var (search, notice) = SafeguardApiTool.ResolveDiscoverSearch(search: null, query: "uptime");

        Assert.Equal("uptime", search);
        Assert.Null(notice);
    }

    [Fact]
    public void ResolveDiscoverSearch_SearchAlonePassesThrough()
    {
        var (search, notice) = SafeguardApiTool.ResolveDiscoverSearch(search: "uptime", query: null);

        Assert.Equal("uptime", search);
        Assert.Null(notice);
    }

    [Fact]
    public void ResolveDiscoverSearch_BothMatchSameValue_NoNotice()
    {
        var (search, notice) = SafeguardApiTool.ResolveDiscoverSearch(search: "uptime", query: "uptime");

        Assert.Equal("uptime", search);
        Assert.Null(notice);
    }

    [Fact]
    public void ResolveDiscoverSearch_BothMatchCaseInsensitive_NoNotice()
    {
        var (search, notice) = SafeguardApiTool.ResolveDiscoverSearch(search: "Uptime", query: "uptime");

        Assert.Equal("Uptime", search);
        Assert.Null(notice);
    }

    [Fact]
    public void ResolveDiscoverSearch_BothDiffer_PrefersSearchAndWarns()
    {
        var (search, notice) = SafeguardApiTool.ResolveDiscoverSearch(search: "users", query: "uptime");

        Assert.Equal("users", search);
        Assert.NotNull(notice);
        Assert.Contains("using search=", notice);
    }

    [Fact]
    public void ResolveDiscoverSearch_BothNullOrWhitespace_ReturnsNullSearch()
    {
        var (search, notice) = SafeguardApiTool.ResolveDiscoverSearch(search: "  ", query: null);

        Assert.Null(search);
        Assert.Null(notice);
    }

    [Fact]
    public void ResolveDiscoverSearch_TrimsBothInputs()
    {
        var (search, _) = SafeguardApiTool.ResolveDiscoverSearch(search: null, query: "  uptime  ");

        Assert.Equal("uptime", search);
    }

    // --- Appliance diagnostic synonyms (the agent's recovery path) ---------

    [Fact]
    public void TerminologyMap_Uptime_ExpandsToApplianceStatusFamily()
    {
        var expanded = TerminologyMap.ExpandSearchTerms("uptime");

        Assert.Contains(expanded, t => t.Equals("ApplianceStatus", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(expanded, t => t.Equals("SystemTime", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(expanded, t => t.Equals("Health", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TerminologyMap_SystemTime_ExpandsToApplianceFamily()
    {
        var expanded = TerminologyMap.ExpandSearchTerms("system time");

        Assert.Contains(expanded, t => t.Equals("ApplianceStatus", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Discover_SearchUptime_FindsApplianceTimeAndStatusEndpoints()
    {
        var output = SafeguardApiTool.FormatDiscovery(ApplianceFixture(), null, "uptime", null, verbose: false);

        Assert.DoesNotContain("requires at least one narrower", output);
        // At minimum SystemTime and ApplianceStatus must surface — these are
        // the endpoints that actually answer "how long has it been up?".
        Assert.Contains("/v4/SystemTime", output);
        Assert.Contains("/v4/ApplianceStatus", output);
    }

    [Fact]
    public void Discover_ServiceApplianceSearchUptime_DoesNotReturnEmpty()
    {
        var output = SafeguardApiTool.FormatDiscovery(ApplianceFixture(), "Appliance", "uptime", null, verbose: false);

        Assert.DoesNotContain("No endpoints matched", output);
        Assert.Contains("/v4/SystemTime", output);
    }
}
