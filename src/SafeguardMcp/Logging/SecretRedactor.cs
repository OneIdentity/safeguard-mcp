using System.Text.RegularExpressions;

namespace SafeguardMcp.Logging;

/// <summary>
/// Centralized scrubber for log-shaped secrets — every redaction
/// rule lives here so callers cannot accidentally route around it.
/// Patterns:
/// <list type="bullet">
///   <item><c>Bearer &lt;token&gt;</c></item>
///   <item>Standalone JWT (three base64url segments separated by dots,
///   first beginning with <c>eyJ</c>)</item>
///   <item>OAuth <c>code=</c> and <c>code_verifier=</c> query/body params</item>
///   <item>JSON-shaped <c>"code"</c> and <c>"code_verifier"</c> properties</item>
/// </list>
/// All regexes are source-generated for native-AOT safety.
/// </summary>
internal static partial class SecretRedactor
{
    private const string BearerReplacement = "Bearer [REDACTED]";
    private const string JwtReplacement = "[REDACTED-JWT]";
    private const string CodeParamReplacement = "code=[REDACTED]";
    private const string CodeVerifierParamReplacement = "code_verifier=[REDACTED]";
    private const string CodeJsonReplacement = "\"code\":\"[REDACTED]\"";
    private const string CodeVerifierJsonReplacement = "\"code_verifier\":\"[REDACTED]\"";

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-_.]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"eyJ[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+")]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"code=[^&\s""]+")]
    private static partial Regex CodeParamRegex();

    [GeneratedRegex(@"code_verifier=[^&\s""]+")]
    private static partial Regex CodeVerifierParamRegex();

    [GeneratedRegex(@"""code""\s*:\s*""[^""]+""")]
    private static partial Regex CodeJsonRegex();

    [GeneratedRegex(@"""code_verifier""\s*:\s*""[^""]+""")]
    private static partial Regex CodeVerifierJsonRegex();

    /// <summary>
    /// Returns <paramref name="input"/> with every matched secret shape
    /// replaced by a stable placeholder. Returns the original string
    /// when no patterns match (zero-allocation fast path).
    /// </summary>
    public static string Scrub(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // code_verifier patterns must run before code= patterns so that
        // "code_verifier=" is not partially clobbered by the broader
        // "code=" regex.
        var s = input;
        s = CodeVerifierJsonRegex().Replace(s, CodeVerifierJsonReplacement);
        s = CodeJsonRegex().Replace(s, CodeJsonReplacement);
        s = CodeVerifierParamRegex().Replace(s, CodeVerifierParamReplacement);
        s = CodeParamRegex().Replace(s, CodeParamReplacement);
        s = BearerRegex().Replace(s, BearerReplacement);
        s = JwtRegex().Replace(s, JwtReplacement);
        return s;
    }
}
