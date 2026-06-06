using Microsoft.Extensions.Logging;

namespace SafeguardMcp;

/// <summary>
/// A minimal file-based logger provider for diagnosing MCP server issues.
/// Writes all log messages to a file, flushing after each entry.
/// </summary>
public sealed class FileLoggerProvider(string path) : ILoggerProvider
{
    private readonly StreamWriter _writer = new(path, append: true) { AutoFlush = true };
    private readonly Lock _lock = new();

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _writer, _lock);

    public void Dispose() => _writer.Dispose();
}

file sealed class FileLogger(string category, StreamWriter writer, Lock @lock) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => SafeguardMcp.Logging.NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = $"{DateTime.UtcNow:HH:mm:ss.fff} [{logLevel}] {category}: {formatter(state, exception)}";

        if (exception != null)
            message += Environment.NewLine + exception;

        lock (@lock)
        {
            writer.WriteLine(message);
        }
    }
}