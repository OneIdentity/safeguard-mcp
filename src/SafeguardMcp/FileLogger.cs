using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SafeguardMcp;

/// <summary>
/// A minimal file-based logger provider for diagnosing MCP server issues.
///
/// <para>
/// The log path is shared across every <c>safeguard-mcp</c> process started
/// from the same install (a single file under <see cref="System.AppContext.BaseDirectory"/>),
/// so the file is opened per-write with <see cref="FileShare.ReadWrite"/> +
/// <see cref="FileShare.Delete"/> and <see cref="FileMode.Append"/>. That
/// gives correct shared-append behavior (seek-to-EOF at every open) at the
/// cost of one open/close per log line — fine for a low-volume diagnostic
/// sink, and necessary because <see cref="StreamWriter"/> + <c>AutoFlush</c>
/// only flushes the writer's char buffer, not the underlying
/// <see cref="FileStream"/> byte buffer, so a held-open handle would lose
/// inter-process append semantics.
/// </para>
///
/// <para>
/// If the file cannot be opened at all (read-only filesystem, ACL denial,
/// out of space, etc.) the provider transparently disables itself —
/// logging is observability, not correctness, and a logging hiccup must
/// never prevent the MCP server from coming up or stop it from serving
/// requests.
/// </para>
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly bool _enabled;
    private readonly Lock _lock = new();

    public FileLoggerProvider(string path)
    {
        _path = path;
        _enabled = TryProbeWritable(path);
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _path, _enabled, _lock);

    public void Dispose() { }

    private static bool TryProbeWritable(string path)
    {
        try
        {
            using var fs = new FileStream(
                path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

file sealed class FileLogger(string category, string path, bool enabled, Lock @lock) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => SafeguardMcp.Logging.NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => enabled && logLevel >= LogLevel.Debug;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = $"{DateTime.UtcNow:HH:mm:ss.fff} [{logLevel}] {category}: {formatter(state, exception)}";
        if (exception != null)
            message += Environment.NewLine + exception;

        var bytes = Encoding.UTF8.GetBytes(message + Environment.NewLine);

        lock (@lock)
        {
            try
            {
                // Per-write open so FileMode.Append's seek-to-EOF runs
                // on each call. bufferSize:1 disables internal buffering
                // so the byte payload reaches the OS write call directly.
                using var fs = new FileStream(
                    path,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 1);
                fs.Write(bytes, 0, bytes.Length);
            }
            catch (IOException)
            {
                // Disk full, file locked exclusively by an external
                // tool, etc. Logging never blocks the MCP transport.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}