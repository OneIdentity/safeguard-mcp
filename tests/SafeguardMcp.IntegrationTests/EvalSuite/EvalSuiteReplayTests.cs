using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SafeguardMcp.IntegrationTests.EvalSuite;

/// <summary>
/// Regression replay of the collected error traces. Each case is one row in
/// <see cref="EvalCases.All"/>; the test either runs the reproducer against a
/// live appliance and asserts the response matches the expected guidance, or
/// records a pending placeholder (for cases whose owning capability area
/// has not shipped yet).
///
/// Like every test in this project, all rows skip when SPP_HOST is unset.
/// The eval suite is run end-of-phase against a live appliance; per-item
/// validation lives in the unit-test project.
/// </summary>
[Collection("AgentSimulation")]
public class EvalSuiteReplayTests
{
    private readonly AgentSimulationFixture _fixture;
    private readonly ITestOutputHelper _output;

    public EvalSuiteReplayTests(AgentSimulationFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public static IEnumerable<object[]> CaseIds =>
        EvalCases.All.Select(c => new object[] { c.Id });

    [RequiresApplianceTheory]
    [MemberData(nameof(CaseIds))]
    public async Task Replay(string caseId)
    {
        _fixture.RequireAvailable();

        var c = EvalCases.All.FirstOrDefault(x => x.Id == caseId)
            ?? throw new InvalidOperationException($"Unknown eval case id: {caseId}");

        _output.WriteLine($"[{c.Id}] owner={c.OwningArea} mode={c.Mode}");
        _output.WriteLine($"        {c.Description}");

        switch (c.Mode)
        {
            case ReproMode.Placeholder:
                _output.WriteLine($"        PENDING: {c.PlaceholderNote}");
                // Placeholder passes by design — the case is recorded so the
                // suite has the full 38 rows, and the assertion gets tightened
                // when the owning capability area ships.
                return;

            case ReproMode.ExpectSuccess:
            {
                var response = await ExecuteCaptureAsync(c);
                Assert.False(
                    LooksLikeError(response),
                    $"[{c.Id}] expected success, got error-shaped response: {Truncate(response)}");
                return;
            }

            case ReproMode.ExpectGuidance:
            {
                var response = await ExecuteCaptureAsync(c);
                var missing = c.ExpectedGuidance
                    .Where(s => response.IndexOf(s, StringComparison.OrdinalIgnoreCase) < 0)
                    .ToList();
                Assert.True(
                    missing.Count == 0,
                    $"[{c.Id}] response missing expected guidance fragments: " +
                    $"[{string.Join(", ", missing)}]. Response: {Truncate(response)}");
                return;
            }

            default:
                throw new InvalidOperationException($"Unhandled mode {c.Mode} for {c.Id}");
        }
    }

    private async Task<string> ExecuteCaptureAsync(EvalCase c)
    {
        if (string.IsNullOrEmpty(c.Method) || string.IsNullOrEmpty(c.Path))
        {
            throw new InvalidOperationException(
                $"[{c.Id}] Mode={c.Mode} requires a reproducer (Method+Path).");
        }

        try
        {
            return await _fixture.ExecuteAsync(
                c.Method, c.Path, query: c.Query, body: c.Body, format: c.Format);
        }
        catch (Exception ex)
        {
            // Capture exception text so guidance assertions can match against
            // the hint that the MCP layer surfaces on Safeguard 4xx/5xx errors.
            return ex.Message;
        }
    }

    private static bool LooksLikeError(string body)
        => body.IndexOf("HTTP 4", StringComparison.OrdinalIgnoreCase) >= 0
        || body.IndexOf("HTTP 5", StringComparison.OrdinalIgnoreCase) >= 0
        || body.IndexOf("\"Code\":", StringComparison.Ordinal) >= 0
        || body.IndexOf("Safeguard API error", StringComparison.OrdinalIgnoreCase) >= 0;

    private static string Truncate(string s)
        => s.Length <= 500 ? s : s[..500] + "…";
}

/// <summary>
/// Sanity coverage that runs even without an appliance: the eval suite must
/// contain the full set of recorded traces (38 cases) and every case must be
/// well-formed (id, owning area, description, and either a reproducer or a
/// placeholder note). Lives in the integration-tests project so the eval
/// data has a single home, but does not touch the network.
/// </summary>
public class EvalSuiteRegistryTests
{
    [Fact]
    public void All_cases_are_present()
    {
        // 38 collected error traces.
        Assert.Equal(38, EvalCases.All.Count);
    }

    [Fact]
    public void All_case_ids_are_unique()
    {
        var dupes = EvalCases.All
            .GroupBy(c => c.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        Assert.True(dupes.Count == 0, $"Duplicate case ids: {string.Join(", ", dupes)}");
    }

    [Fact]
    public void Every_case_is_well_formed()
    {
        foreach (var c in EvalCases.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Id), "Case id is required.");
            Assert.False(string.IsNullOrWhiteSpace(c.Description), $"[{c.Id}] description required.");
            Assert.False(string.IsNullOrWhiteSpace(c.OwningArea), $"[{c.Id}] owning area required.");

            if (c.Mode == ReproMode.Placeholder)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(c.PlaceholderNote),
                    $"[{c.Id}] placeholder cases must explain what they're waiting on.");
            }
            else
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(c.Method) || string.IsNullOrWhiteSpace(c.Path),
                    $"[{c.Id}] active cases must supply Method and Path.");

                if (c.Mode == ReproMode.ExpectGuidance)
                {
                    Assert.True(
                        c.ExpectedGuidance.Count > 0,
                        $"[{c.Id}] ExpectGuidance cases must list at least one expected substring.");
                }
            }
        }
    }
}
