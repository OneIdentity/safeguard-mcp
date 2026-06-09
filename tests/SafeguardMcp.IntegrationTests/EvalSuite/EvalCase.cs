using System.Collections.Generic;

namespace SafeguardMcp.IntegrationTests.EvalSuite;

/// <summary>
/// One case in the regression-replay eval suite. Built from the collected
/// error traces; each case names the capability area that owns the fix
/// and (where possible) the substrings the response is expected to
/// contain once that fix ships.
/// </summary>
/// <remarks>
/// Cases come in three flavors:
///   * <see cref="ReproMode.ExpectSuccess"/> — running the reproducer should
///     succeed (returned body must not look like an error envelope).
///   * <see cref="ReproMode.ExpectGuidance"/> — the response is allowed to be
///     an error, but it must mention every fragment in
///     <see cref="ExpectedGuidance"/>.
///   * <see cref="ReproMode.Placeholder"/> — no live reproducer yet (either
///     the underlying behavior is diagnostic-only, or the assertion can't be
///     tightened until the owning capability area ships). The test records
///     the pending status and passes.
/// </remarks>
internal sealed class EvalCase
{
    public string Id { get; init; }
    public string Description { get; init; }
    public string OwningArea { get; init; }
    public ReproMode Mode { get; init; }

    // Reproducer (populated when Mode != Placeholder).
    public string Method { get; init; }
    public string Path { get; init; }
    public string Query { get; init; }
    public string Body { get; init; }
    public string Format { get; init; } = "json";

    // Substrings that the response (or thrown exception message) must contain
    // when Mode == ExpectGuidance. Case-insensitive.
    public IReadOnlyList<string> ExpectedGuidance { get; init; } = System.Array.Empty<string>();

    // Free-form note shown when Mode == Placeholder. Explains what the case
    // is waiting on (typically the owning capability area) and what the
    // upgraded assertion should look like.
    public string PlaceholderNote { get; init; }

    public override string ToString() => Id;
}

internal enum ReproMode
{
    Placeholder,
    ExpectSuccess,
    ExpectGuidance,
}
