#nullable disable

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using SafeguardMcp;
using Xunit;

namespace SafeguardMcp.Tests.Logging;

/// <summary>
/// Concurrency and resilience tests for <see cref="FileLoggerProvider"/>.
///
/// <para>
/// The log path is shared across every <c>safeguard-mcp</c> child process
/// started from the same install. The provider must therefore tolerate
/// two providers pointed at the same file (the multi-MCP-client scenario)
/// and must never throw out of its constructor when the file cannot be
/// opened — logging is observability, not correctness, and a logging
/// hiccup must never kill the MCP transport.
/// </para>
/// </summary>
public class FileLoggerProviderTests
{
    private static string NewTempPath()
        => Path.Combine(Path.GetTempPath(), "safeguard-mcp-test-" + Guid.NewGuid().ToString("N") + ".log");

    [Fact]
    public void TwoProvidersOnSamePath_DoNotThrow_AndBothCanLog()
    {
        var path = NewTempPath();
        try
        {
            var p1 = new FileLoggerProvider(path);
            // Second provider on the same path — this used to throw
            // IOException because StreamWriter(path, append:true) opens
            // with FileShare.Read only. Must now succeed and the second
            // provider's writes must not overwrite the first's.
            var p2 = new FileLoggerProvider(path);

            var l1 = p1.CreateLogger("p1");
            var l2 = p2.CreateLogger("p2");

            l1.LogInformation("hello-from-p1");
            l2.LogInformation("hello-from-p2");

            // No flush dance needed — the provider opens/closes the
            // file per write, so contents are on disk immediately.
            var contents = File.ReadAllText(path);
            Assert.Contains("hello-from-p1", contents);
            Assert.Contains("hello-from-p2", contents);

            p1.Dispose();
            p2.Dispose();
        }
        finally
        {
            try { File.Delete(path); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void UnopenablePath_DoesNotThrow_AndLoggingIsNoOp()
    {
        // A path whose parent directory does not exist will fail to
        // open. The provider must swallow this and fall back to a
        // no-op writer so host startup is never blocked by a logging
        // problem.
        var bogus = Path.Combine(
            Path.GetTempPath(),
            "safeguard-mcp-no-such-dir-" + Guid.NewGuid().ToString("N"),
            "child.log");

        using var p = new FileLoggerProvider(bogus);
        var logger = p.CreateLogger("nope");

        // Must not throw, must not create the file.
        logger.LogInformation("this should be swallowed");

        Assert.False(File.Exists(bogus));
    }
}
