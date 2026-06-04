using System.Text.RegularExpressions;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Assertion helpers for validating <c>Safeguard_Discover</c> output.
/// </summary>
public static class DiscoverAssertions
{
    private static readonly Regex EndpointLineRegex = new(
        @"^\s*(?<method>GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)\s+(?:(?<service>Appliance|Core|Notification)\s+)?(?<path>/\S+)(?<rest>.*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Asserts that the discovery output contains an endpoint whose method matches
    /// <paramref name="method"/> and whose path contains <paramref name="pathContains"/>.
    /// </summary>
    public static void AssertFindsEndpoint(string discoverOutput, string method, string pathContains)
    {
        var endpoints = ParseEndpoints(discoverOutput);
        var match = endpoints.FirstOrDefault(endpoint =>
            endpoint.Method.Equals(method, StringComparison.OrdinalIgnoreCase)
            && endpoint.Path.Contains(pathContains, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            Fail(
                $"Expected discover output to contain a {method} endpoint with path containing '{pathContains}', but none was found."
                + Environment.NewLine
                + Environment.NewLine
                + "Discover output:"
                + Environment.NewLine
                + FormatOutput(discoverOutput));
        }
    }

    /// <summary>
    /// Asserts that the discovery output line for <paramref name="path"/> contains the
    /// <c>[body]</c> marker.
    /// </summary>
    public static void AssertHasBodyIndicator(string discoverOutput, string path)
    {
        var endpoint = FindEndpointByPath(discoverOutput, path);
        if (!endpoint.OriginalLine.Contains("[body]", StringComparison.OrdinalIgnoreCase))
        {
            Fail(
                $"Expected endpoint '{path}' to include the [body] marker, but it did not."
                + Environment.NewLine
                + $"Matched line: {endpoint.OriginalLine}");
        }
    }

    /// <summary>
    /// Asserts that the discovery output line for <paramref name="path"/> shows query
    /// parameters.
    /// </summary>
    public static void AssertHasParams(string discoverOutput, string path)
    {
        var endpoint = FindEndpointByPath(discoverOutput, path);
        if (!endpoint.OriginalLine.Contains("?")
            && !endpoint.OriginalLine.Contains("params:", StringComparison.OrdinalIgnoreCase))
        {
            Fail(
                $"Expected endpoint '{path}' to show query parameters, but it did not."
                + Environment.NewLine
                + $"Matched line: {endpoint.OriginalLine}");
        }
    }

    /// <summary>
    /// Executes a live discovery search and asserts that the expected path appears in the results.
    /// </summary>
    public static void AssertTerminologyWorks(AgentSimulationFixture fixture, string searchTerm, string expectedPath)
    {
        if (fixture is null)
        {
            Fail("AgentSimulationFixture must not be null.");
        }

        var discoverOutput = fixture.Discover(search: searchTerm);
        var endpoints = ParseEndpoints(discoverOutput);
        var match = endpoints.FirstOrDefault(endpoint =>
            endpoint.Path.Contains(expectedPath, StringComparison.OrdinalIgnoreCase));

        if (match is null && !(discoverOutput?.Contains(expectedPath, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            Fail(
                $"Expected discovery search '{searchTerm}' to find a path containing '{expectedPath}', but it did not."
                + Environment.NewLine
                + Environment.NewLine
                + "Discover output:"
                + Environment.NewLine
                + FormatOutput(discoverOutput));
        }
    }

    private static EndpointLine FindEndpointByPath(string discoverOutput, string path)
    {
        var endpoints = ParseEndpoints(discoverOutput);
        var match = endpoints.FirstOrDefault(endpoint =>
            endpoint.Path.Contains(path, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            Fail(
                $"Expected discover output to contain an endpoint with path containing '{path}', but none was found."
                + Environment.NewLine
                + Environment.NewLine
                + "Discover output:"
                + Environment.NewLine
                + FormatOutput(discoverOutput));
        }

        return match!;
    }

    private static List<EndpointLine> ParseEndpoints(string discoverOutput)
    {
        var endpoints = new List<EndpointLine>();
        if (string.IsNullOrWhiteSpace(discoverOutput))
        {
            return endpoints;
        }

        foreach (var rawLine in discoverOutput.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = EndpointLineRegex.Match(rawLine);
            if (!match.Success)
            {
                continue;
            }

            endpoints.Add(new EndpointLine(
                match.Groups["method"].Value,
                match.Groups["path"].Value,
                rawLine.TrimEnd()));
        }

        return endpoints;
    }

    private static string FormatOutput(string discoverOutput)
        => string.IsNullOrWhiteSpace(discoverOutput) ? "<empty>" : discoverOutput.Trim();

    private static void Fail(string message) => Assert.Fail(message);

    private sealed record EndpointLine(string Method, string Path, string OriginalLine);
}
