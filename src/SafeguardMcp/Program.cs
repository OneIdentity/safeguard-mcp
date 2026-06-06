using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SafeguardMcp.Catalog;
using SafeguardMcp.Login;
using SafeguardMcp.Tools;

namespace SafeguardMcp
{
    // See:
    // https://github.com/dotnet/extensions/tree/main/src/ProjectTemplates/Microsoft.McpServer.ProjectTemplates/templates/McpServer-CSharp/local
    // and
    // https://modelcontextprotocol.io/docs/develop/build-server
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "login", StringComparison.OrdinalIgnoreCase))
            {
                // Subcommand owns its own --help / --version flags below the
                // top-level dispatch so `safeguard-mcp login --help` reaches it.
                var subArgs = args.Length == 1 ? Array.Empty<string>() : args[1..];
                return await LoginCommand.RunAsync(subArgs, CancellationToken.None);
            }

            if (HasFlag(args, "-v", "--version"))
            {
                Console.Out.WriteLine(GetVersion());
                return 0;
            }

            if (HasFlag(args, "-h", "--help", "-?", "/?"))
            {
                Console.Out.WriteLine(GetHelpText());
                return 0;
            }

            if (args.Contains("--http", StringComparer.OrdinalIgnoreCase))
            {
                var lockdownError = HttpModeStartup.ValidateEnvironment();
                if (lockdownError != null)
                {
                    Console.Error.WriteLine("safeguard-mcp: " + lockdownError);
                    return 2;
                }
                await RunHttpAsync(args);
            }
            else
                await RunStdioAsync(args);

            return 0;
        }

        private static bool HasFlag(string[] args, params string[] flags)
        {
            foreach (var arg in args)
                foreach (var flag in flags)
                    if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                        return true;
            return false;
        }

        // BuildInfo.Version is generated from <Version> in the csproj — see GenerateBuildInfo target.
        private static string GetVersion() => BuildInfo.Version;

        private static string GetHelpText() => $@"safeguard-mcp {GetVersion()}
Model Context Protocol server for One Identity Safeguard for Privileged Passwords.

USAGE:
  safeguard-mcp                Run as MCP stdio server (default; for IDE integration).
  safeguard-mcp --http         Run as MCP HTTP server on http://localhost:8080/mcp.
  safeguard-mcp login [opts]   Acquire a Safeguard user token via device-code
                               login and print it (or write it to a file with
                               a restrictive ACL). See `safeguard-mcp login --help`.
  safeguard-mcp --version      Print version and exit.
  safeguard-mcp --help         Print this help and exit.

ENVIRONMENT:
  SAFEGUARD_HOST               DNS name or IP of the Safeguard appliance to pre-configure.
                               When set, the agent must call Safeguard_Connect to authenticate.
  SAFEGUARD_PROVIDER,          Optional non-interactive PKCE credentials. When all three
  SAFEGUARD_USER,              are set, the server uses PKCE instead of device-code auth.
  SAFEGUARD_PASSWORD
  SAFEGUARD_IGNORE_SSL=true    Skip TLS verification (lab/test only).
  ASPNETCORE_URLS              In --http mode, the URLs to bind (default http://0.0.0.0:8080).

Documentation: https://github.com/OneIdentity/safeguard-mcp";

        private static async Task RunStdioAsync(string[] args)
        {
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
            builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
            builder.Logging.AddProvider(new FileLoggerProvider(
                Path.Combine(AppContext.BaseDirectory, "safeguard-mcp.log")));

            RegisterServices(builder.Services);
            builder.Services.AddSingleton<ISafeguardSession, StdioSafeguardSession>();

            AddSafeguardMcpComponents(builder.Services.AddMcpServer().WithStdioServerTransport());

            var app = builder.Build();
            LogStartupAuthMode(app.Services.GetRequiredService<ILogger<Program>>());
            await app.RunAsync();
        }

        private static async Task RunHttpAsync(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            builder.Logging.AddProvider(new FileLoggerProvider(
                Path.Combine(AppContext.BaseDirectory, "safeguard-mcp.log")));

            RegisterServices(builder.Services);
            builder.Services.AddHttpContextAccessor();
            // Scoped: each MCP HTTP request gets a fresh session bound to its bearer.
            builder.Services.AddScoped<ISafeguardSession, HttpRelaySafeguardSession>();
            builder.Services.AddHealthChecks();

            AddSafeguardMcpComponents(builder.Services.AddMcpServer().WithHttpTransport());

            var app = builder.Build();
            LogStartupAuthMode(app.Services.GetRequiredService<ILogger<Program>>());
            // HTTP mode requires SAFEGUARD_HOST at startup; warm the dynamic
            // catalog opportunistically (best-effort, anonymous load via
            // /service/Notification/v4/Status) so the first tool call has
            // schemas/endpoints ready.
            WarmCatalog(app.Services);
            app.MapHealthChecks("/healthz");
            app.MapMcp();
            await app.RunAsync();
        }

        private static void WarmCatalog(IServiceProvider services)
        {
            var host = Environment.GetEnvironmentVariable("SAFEGUARD_HOST");
            if (string.IsNullOrWhiteSpace(host))
                return;

            var ignoreSsl = bool.TryParse(Environment.GetEnvironmentVariable("SAFEGUARD_IGNORE_SSL"), out var v) && v;
            var catalog = services.GetRequiredService<CatalogProvider>();
            _ = Task.Run(() => catalog.LoadCatalogAsync(host.Trim(), ignoreSsl));
        }

        // Device-code can't pre-auth (no MCP session yet); log the configured posture.
        private static void LogStartupAuthMode(ILogger logger)
        {
            var host = Environment.GetEnvironmentVariable("SAFEGUARD_HOST");
            var provider = Environment.GetEnvironmentVariable("SAFEGUARD_PROVIDER");
            var user = Environment.GetEnvironmentVariable("SAFEGUARD_USER");
            var hasPkce = !string.IsNullOrWhiteSpace(provider)
                && !string.IsNullOrWhiteSpace(user)
                && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SAFEGUARD_PASSWORD"));

            if (string.IsNullOrWhiteSpace(host))
                return;

            if (hasPkce)
            {
                logger.LogInformation(
                    "Safeguard host '{Host}' and PKCE credentials configured; first authentication will use non-interactive PKCE.",
                    host);
            }
            else
            {
                logger.LogInformation(
                    "Safeguard host '{Host}' pre-configured; agent must call Safeguard_Connect to authenticate via device-code.",
                    host);
            }
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<CatalogLoader>();
            services.AddSingleton<CatalogProvider>();
            services.AddSingleton<ISafeguardConnectionFactory, SafeguardConnectionFactory>();
        }

        // Explicit generic registration preserves required members for trimming/AOT
        // and avoids the reflection-based assembly scan that WithToolsFromAssembly /
        // WithResourcesFromAssembly perform. Centralized here so stdio and HTTP
        // transports stay in lockstep when new tools or resources are added.
        private static IMcpServerBuilder AddSafeguardMcpComponents(IMcpServerBuilder builder) => builder
            .WithTools<SafeguardApiTool>()
            .WithTools<SafeguardWorkflows>()
            .WithResources<ApiOverviewResource>()
            .WithResources<CommonPatternsResource>()
            .WithResources<QuerySyntaxResource>()
            .WithResources<TerminologyResource>();
    }
}