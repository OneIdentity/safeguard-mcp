#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SafeguardMcp.Logging;

namespace SafeguardMcp.Tests;

/// <summary>
/// Phase 1 task 1.C — bearer-redaction log-capture tests.
///
/// <para>
/// Captures every <see cref="ILoggerProvider"/> configured by
/// <see cref="RedactingLoggerProvider"/> across success and exception
/// paths and asserts that the rendered text never contains JWT-shaped
/// substrings, <c>Bearer </c> headers, OAuth <c>code=</c>/
/// <c>code_verifier=</c> parameters, or their JSON-property equivalents.
/// </para>
///
/// <para>
/// The tests pass an arbitrary inner <see cref="ILoggerProvider"/>
/// (a <see cref="CapturingProvider"/>) into
/// <see cref="RedactingLoggerProvider"/>, exercise both
/// message-with-state and exception-only code paths, and inspect the
/// captured records. Real providers (stderr, file) are not used here
/// because the security property under test is "the wrapper scrubs
/// every log line"; the choice of sink is orthogonal.
/// </para>
/// </summary>
public class BearerRedactionTests
{
    private const string SampleJwt =
        "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9" +
        ".eyJzdWIiOiJhbGljZSIsImV4cCI6OTk5OTk5OTk5OX0" +
        ".sigsig-_AAA";

    public static IEnumerable<object[]> SecretLines()
    {
        // Each row exercises a different pattern from
        // HTTP-AUTH-RELAY-PLAN §1.10.
        yield return new object[] { $"Authorization: Bearer {SampleJwt}" };
        yield return new object[] { $"raw JWT in the middle of text: {SampleJwt} (do not log)" };
        yield return new object[] { "callback hit with code=abcDEF123-_." };
        yield return new object[] { "PKCE verifier code_verifier=verifierValue-Foo_Bar." };
        yield return new object[] { "rSTS POST body {\"code\":\"abcDEF123\",\"grant_type\":\"authorization_code\"}" };
        yield return new object[] { "rSTS POST body {\"code_verifier\":\"verifierValue-Foo_Bar\"}" };
        yield return new object[] { $"Authorization: bearer {SampleJwt} (lowercase scheme)" };
    }

    [Theory]
    [MemberData(nameof(SecretLines))]
    public void Scrub_RemovesEveryDocumentedSecretShape(string input)
    {
        var scrubbed = InvokeScrub(input);
        AssertNoSecrets(scrubbed, input);
    }

    [Fact]
    public void RedactingLoggerProvider_SuccessPath_ScrubsRenderedMessage()
    {
        var inner = new CapturingProvider();
        using var provider = new RedactingLoggerProvider(inner);
        var logger = provider.CreateLogger("Test");

        foreach (var row in SecretLines())
        {
            var line = (string)row[0];
            logger.LogInformation("event: {Line}", line);
        }

        Assert.NotEmpty(inner.Records);
        foreach (var record in inner.Records)
            AssertNoSecrets(record.Message, record.Message);
    }

    [Fact]
    public void RedactingLoggerProvider_ExceptionPath_ScrubsExceptionMessageAndToString()
    {
        var inner = new CapturingProvider();
        using var provider = new RedactingLoggerProvider(inner);
        var logger = provider.CreateLogger("Test");

        var ex = new InvalidOperationException(
            $"appliance rejected Bearer {SampleJwt} (token expired)");

        logger.LogError(ex, "callback failed for code=abcDEF123");

        Assert.NotEmpty(inner.Records);
        foreach (var record in inner.Records)
        {
            AssertNoSecrets(record.Message, record.Message);
            if (record.Exception != null)
            {
                AssertNoSecrets(record.Exception.Message, record.Exception.Message);
                AssertNoSecrets(record.Exception.ToString(), record.Exception.ToString());
            }
        }
    }

    [Fact]
    public void RedactingLoggerProvider_BelowMinimumLevel_DoesNotForward()
    {
        var inner = new CapturingProvider(minimumLevel: LogLevel.Warning);
        using var provider = new RedactingLoggerProvider(inner);
        var logger = provider.CreateLogger("Test");

        logger.LogDebug("Bearer {Token}", SampleJwt);

        Assert.Empty(inner.Records);
    }

    [Fact]
    public void Scrub_LeavesNonSecretTextUntouched()
    {
        const string benign = "loaded 42 endpoints from catalog at 12:34:56";
        Assert.Equal(benign, InvokeScrub(benign));
    }

    private static void AssertNoSecrets(string actual, string context)
    {
        Assert.DoesNotContain(SampleJwt, actual);
        Assert.DoesNotContain("abcDEF123", actual);
        Assert.DoesNotContain("verifierValue", actual);
        Assert.False(
            ContainsBearerWithToken(actual),
            $"output retained a 'Bearer <token>' header: '{actual}' (context: '{context}')");
        Assert.False(
            ContainsJwtShape(actual),
            $"output retained a JWT-shaped substring: '{actual}' (context: '{context}')");
        Assert.False(
            ContainsCodeParam(actual),
            $"output retained a code= parameter: '{actual}' (context: '{context}')");
        Assert.False(
            ContainsCodeVerifierParam(actual),
            $"output retained a code_verifier= parameter: '{actual}' (context: '{context}')");
        Assert.False(
            ContainsCodeJsonProperty(actual),
            $"output retained a JSON \"code\":\"...\" property: '{actual}' (context: '{context}')");
        Assert.False(
            ContainsCodeVerifierJsonProperty(actual),
            $"output retained a JSON \"code_verifier\":\"...\" property: '{actual}' (context: '{context}')");
    }

    // The matchers below assert the redacted output, so they look for
    // a real value after the prefix — the placeholder strings like
    // "[REDACTED]" are explicitly allowed.

    private static bool ContainsBearerWithToken(string s)
        => System.Text.RegularExpressions.Regex.IsMatch(
            s, @"Bearer\s+(?!\[REDACTED\])[A-Za-z0-9\-_.]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static bool ContainsJwtShape(string s)
        => System.Text.RegularExpressions.Regex.IsMatch(
            s, @"eyJ[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+");

    private static bool ContainsCodeParam(string s)
        => System.Text.RegularExpressions.Regex.IsMatch(
            s, @"(?<![A-Za-z_])code=(?!\[REDACTED\])[^&\s""]+");

    private static bool ContainsCodeVerifierParam(string s)
        => System.Text.RegularExpressions.Regex.IsMatch(
            s, @"code_verifier=(?!\[REDACTED\])[^&\s""]+");

    private static bool ContainsCodeJsonProperty(string s)
        => System.Text.RegularExpressions.Regex.IsMatch(
            s, @"""code""\s*:\s*""(?!\[REDACTED\])[^""]+""");

    private static bool ContainsCodeVerifierJsonProperty(string s)
        => System.Text.RegularExpressions.Regex.IsMatch(
            s, @"""code_verifier""\s*:\s*""(?!\[REDACTED\])[^""]+""");

    private static string InvokeScrub(string input)
    {
        // SecretRedactor is internal — the tests live in the
        // InternalsVisibleTo-targeted assembly so we can call it
        // directly without reflection.
        return SecretRedactor.Scrub(input);
    }

    private sealed record CapturedRecord(LogLevel Level, string Category, string Message, Exception Exception);

    private sealed class CapturingProvider : ILoggerProvider
    {
        private readonly LogLevel _minimumLevel;
        public ConcurrentQueue<CapturedRecord> Records { get; } = new();

        public CapturingProvider(LogLevel minimumLevel = LogLevel.Trace)
        {
            _minimumLevel = minimumLevel;
        }

        public ILogger CreateLogger(string categoryName)
            => new CapturingLogger(categoryName, _minimumLevel, Records);

        public void Dispose() { }
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly string _category;
        private readonly LogLevel _minimumLevel;
        private readonly ConcurrentQueue<CapturedRecord> _records;

        public CapturingLogger(string category, LogLevel minimumLevel, ConcurrentQueue<CapturedRecord> records)
        {
            _category = category;
            _minimumLevel = minimumLevel;
            _records = records;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            _records.Enqueue(new CapturedRecord(logLevel, _category, formatter(state, exception), exception));
        }
    }
}
