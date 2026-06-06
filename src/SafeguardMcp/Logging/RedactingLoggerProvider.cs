using Microsoft.Extensions.Logging;

namespace SafeguardMcp.Logging;

/// <summary>
/// Wraps an inner <see cref="ILoggerProvider"/> and scrubs every
/// rendered log message through <see cref="SecretRedactor.Scrub(string)"/>
/// before forwarding it on. Per HTTP-AUTH-RELAY-PLAN §1.10, every
/// configured provider (console, file, etc.) is wrapped at startup so
/// no JWT, Bearer token, OAuth <c>code</c>, or PKCE <c>code_verifier</c>
/// can leak through any logging sink.
///
/// Scope state (anything passed to <see cref="ILogger.BeginScope{TState}(TState)"/>)
/// is forwarded as-is — application code does not place secrets in scopes
/// and intercepting them would require reflection that is hostile to AOT.
/// </summary>
public sealed class RedactingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ILoggerProvider _inner;

    public RedactingLoggerProvider(ILoggerProvider inner)
    {
        if (inner == null) throw new ArgumentNullException(nameof(inner));
        _inner = inner;
    }

    public ILogger CreateLogger(string categoryName)
        => new RedactingLogger(_inner.CreateLogger(categoryName));

    public void Dispose() => _inner.Dispose();

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        if (_inner is ISupportExternalScope s)
            s.SetScopeProvider(scopeProvider);
    }
}

internal sealed class RedactingLogger : ILogger
{
    private readonly ILogger _inner;

    public RedactingLogger(ILogger inner) { _inner = inner; }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => _inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));
        if (!_inner.IsEnabled(logLevel))
            return;

        // Render once here, scrub, then hand a constant formatter to the
        // inner logger. The inner logger can no longer access the raw
        // TState so no path exists for secrets to bypass redaction.
        var rendered = formatter(state, exception);
        var scrubbed = SecretRedactor.Scrub(rendered);
        var scrubbedException = exception == null ? null : new RedactedException(exception);

        _inner.Log(
            logLevel,
            eventId,
            scrubbed,
            scrubbedException,
            (s, _) => s);
    }
}

/// <summary>
/// Wraps an exception so its <see cref="Exception.Message"/>,
/// <see cref="Exception.ToString"/>, <see cref="Exception.StackTrace"/>,
/// and chained <see cref="Exception.InnerException"/> are all scrubbed
/// before any logger can format them. Recursive wrapping closes the
/// chain-walking footgun: a sink that does
/// <c>ex.InnerException.Message</c> still sees redacted text rather
/// than reaching the original instance directly.
/// </summary>
internal sealed class RedactedException : Exception
{
    private readonly Exception _original;

    public RedactedException(Exception original)
        : base(
            SecretRedactor.Scrub(original?.Message ?? string.Empty),
            original?.InnerException == null ? null : new RedactedException(original.InnerException))
    {
        _original = original;
    }

    public override string StackTrace => SecretRedactor.Scrub(_original?.StackTrace ?? string.Empty);

    public override string ToString() => SecretRedactor.Scrub(_original?.ToString() ?? string.Empty);
}
