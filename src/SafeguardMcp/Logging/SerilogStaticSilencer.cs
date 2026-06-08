using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SafeguardMcp.Logging;

/// <summary>
/// Defense-in-depth wiring for Serilog's static logger.
///
/// <para>
/// SafeguardDotNet logs via the global <see cref="Serilog.Log"/> static
/// logger. We assign a sinkless logger at startup so its output is
/// silently dropped even if a transitive dependency later configures
/// Serilog. A no-op sink — not <c>SilentLogger</c> — is used because
/// <see cref="Serilog.Core.Logger"/> still attempts to format messages
/// before forwarding them to sinks; an empty pipeline guarantees
/// zero downstream writers without code paths that could resurrect.
/// </para>
///
/// <para>
/// When the opt-in env var <c>SAFEGUARD_MCP_DEBUG_SDK_LOGGING=true</c>
/// is set, Serilog is instead pointed at a sink that forwards every
/// event through the supplied <see cref="ILoggerFactory"/> — so SDK
/// debug output goes through the same
/// <see cref="RedactingLoggerProvider"/> as every other log line.
/// </para>
/// </summary>
internal static class SerilogStaticSilencer
{
    private const string DebugEnvVar = "SAFEGUARD_MCP_DEBUG_SDK_LOGGING";

    /// <summary>
    /// Silences Serilog with no logger factory available yet.
    /// Safe to call before <see cref="IServiceProvider"/> exists.
    /// </summary>
    public static void Silence()
        => Log.Logger = new LoggerConfiguration().CreateLogger();

    /// <summary>
    /// Silences Serilog, or — when the opt-in env var is set —
    /// bridges it into the supplied factory's pipeline so SDK
    /// Debug output flows through <see cref="RedactingLoggerProvider"/>.
    /// </summary>
    public static void ConfigureWithFactory(ILoggerFactory factory, Func<string, string> getEnv)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        if (getEnv == null) throw new ArgumentNullException(nameof(getEnv));

        var optIn = getEnv(DebugEnvVar);
        if (string.Equals(optIn, "true", StringComparison.OrdinalIgnoreCase))
        {
            var bridge = factory.CreateLogger("OneIdentity.SafeguardDotNet");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(new ForwardingSink(bridge))
                .CreateLogger();
        }
        else
        {
            Silence();
        }
    }
}

file sealed class ForwardingSink : ILogEventSink
{
    private readonly Microsoft.Extensions.Logging.ILogger _target;

    public ForwardingSink(Microsoft.Extensions.Logging.ILogger target) { _target = target; }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null) return;

        var level = logEvent.Level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.Debug,
        };

        // RenderMessage materializes the event without depending on
        // Serilog formatter plug-ins. The redacting pipeline downstream
        // is responsible for scrubbing — we deliberately do not scrub
        // here so all redaction has a single chokepoint.
        var rendered = logEvent.RenderMessage();
        _target.Log(level, logEvent.Exception, "{Message}", rendered);
    }
}
