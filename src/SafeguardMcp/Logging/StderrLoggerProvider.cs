using Microsoft.Extensions.Logging;

namespace SafeguardMcp.Logging;

/// <summary>
/// Minimal stderr <see cref="ILoggerProvider"/> that mirrors
/// <see cref="FileLoggerProvider"/>'s format. Replaces the default
/// console provider so we don't need to wrap an opaque provider type
/// just to scrub its output — every line is routed through
/// <see cref="RedactingLoggerProvider"/> at startup regardless.
/// Logs are written to <see cref="Console.Error"/> so stdio mode keeps
/// stdout reserved for the MCP protocol.
/// </summary>
public sealed class StderrLoggerProvider : ILoggerProvider
{
    private readonly Lock _lock = new();

    public ILogger CreateLogger(string categoryName) => new StderrLogger(categoryName, _lock);

    public void Dispose() { }
}

file sealed class StderrLogger : ILogger
{
    private readonly string _category;
    private readonly Lock _lock;

    public StderrLogger(string category, Lock @lock)
    {
        _category = category;
        _lock = @lock;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = $"{DateTime.UtcNow:HH:mm:ss.fff} [{logLevel}] {_category}: {formatter(state, exception)}";
        if (exception != null)
            message += Environment.NewLine + exception;

        lock (_lock)
        {
            Console.Error.WriteLine(message);
        }
    }
}
