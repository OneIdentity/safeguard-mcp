using System.Net;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SafeguardMcp.Catalog;
using SafeguardMcp.Logging;
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
            // Defense-in-depth: silence SafeguardDotNet's static Serilog
            // logger before any code path can trigger SDK logging. The
            // factory-aware wiring happens after the host is built.
            SerilogStaticSilencer.Silence();

            if (args.Length > 0 && string.Equals(args[0], "login", StringComparison.OrdinalIgnoreCase))
            {
                // Subcommand owns its own --help / --version flags below the
                // top-level dispatch so `safeguard-mcp login --help` reaches it.
                var subArgs = args.Length == 1 ? Array.Empty<string>() : args[1..];
                return await LoginCommand.RunAsync(subArgs, CancellationToken.None);
            }

            if (args.Length > 0 && string.Equals(args[0], "logout", StringComparison.OrdinalIgnoreCase))
            {
                var subArgs = args.Length == 1 ? Array.Empty<string>() : args[1..];
                return await LogoutCommand.RunAsync(subArgs, CancellationToken.None);
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

                // Parse OAuth metadata bridge configuration. The bridge
                // is on by default in HTTP mode; set BRIDGE_DISABLED=true
                // to opt out. MCP_PUBLIC_URL and RSTS_CLIENT_ID are
                // optional pinning overrides — when absent, the bridge
                // infers its public URL from each incoming request.
                var bridgeParse = OAuth.BridgeOptions.Parse(Environment.GetEnvironmentVariable);
                if (bridgeParse.Error != null)
                {
                    Console.Error.WriteLine("safeguard-mcp: " + bridgeParse.Error);
                    return 2;
                }
                foreach (var warning in bridgeParse.Warnings)
                    Console.Error.WriteLine("safeguard-mcp: warning: " + warning);

                await RunHttpAsync(args, bridgeParse.Options);
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
                               OAuth metadata bridge is active by default;
                               set BRIDGE_DISABLED=true to opt out (relay only).
  safeguard-mcp login [opts]   Acquire a Safeguard user token via device-code
                               login and print it (or write it to a file with
                               a restrictive ACL). See `safeguard-mcp login --help`.
  safeguard-mcp logout [opts]  Revoke a token previously issued by `login`.
                               See `safeguard-mcp logout --help`.
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

        /// <summary>
        /// Replaces host-installed logging providers with stderr + file,
        /// each wrapped in <see cref="RedactingLoggerProvider"/>.
        /// Single chokepoint so both transports get identical scrubbing.
        /// </summary>
        private static void ConfigureRedactingLogging(ILoggingBuilder logging)
        {
            // stdio mode must keep stdout reserved for the MCP protocol —
            // stderr is the only safe console destination, and the file
            // sink mirrors what HTTP mode also gets.
            logging.ClearProviders();
            logging.AddProvider(new RedactingLoggerProvider(new StderrLoggerProvider()));
            logging.AddProvider(new RedactingLoggerProvider(new FileLoggerProvider(
                Path.Combine(AppContext.BaseDirectory, "safeguard-mcp.log"))));
        }

        private static async Task RunStdioAsync(string[] args)
        {
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            ConfigureRedactingLogging(builder.Logging);

            RegisterServices(builder.Services);
            builder.Services.AddSingleton<ISafeguardSession, StdioSafeguardSession>();

            AddSafeguardMcpComponents(builder.Services.AddMcpServer().WithStdioServerTransport());

            var app = builder.Build();
            SerilogStaticSilencer.ConfigureWithFactory(
                app.Services.GetRequiredService<ILoggerFactory>(),
                Environment.GetEnvironmentVariable);
            LogStartupAuthMode(app.Services.GetRequiredService<ILogger<Program>>());
            await app.RunAsync();
        }

        private static async Task RunHttpAsync(string[] args, OAuth.BridgeOptions bridgeOptions)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            ConfigureRedactingLogging(builder.Logging);

            // Defense-in-depth HttpLogging defaults: even though we do
            // not currently call UseHttpLogging, if a future engineer
            // enables it the Authorization / Cookie / Set-Cookie headers
            // and request/response bodies must never be logged.
            builder.Services.AddHttpLogging(o =>
            {
                o.LoggingFields = HttpLoggingFields.RequestMethod
                    | HttpLoggingFields.RequestPath
                    | HttpLoggingFields.RequestProtocol
                    | HttpLoggingFields.RequestScheme
                    | HttpLoggingFields.ResponseStatusCode;
                o.RequestHeaders.Clear();
                o.ResponseHeaders.Clear();
                o.MediaTypeOptions.Clear();
            });

            RegisterServices(builder.Services);
            builder.Services.AddHttpContextAccessor();
            // Scoped: each MCP HTTP request gets a fresh session bound to its bearer.
            builder.Services.AddScoped<ISafeguardSession, HttpRelaySafeguardSession>();
            builder.Services.AddHealthChecks();

            if (bridgeOptions != null)
            {
                builder.Services.AddSingleton(bridgeOptions);
                builder.Services.AddSingleton<OAuth.BridgeUrlResolver>();
                builder.Services.AddSingleton<OAuth.ClientRegistry>();
                builder.Services.AddSingleton<OAuth.AuthorizeFlowStore>();
                builder.Services.AddSingleton<OAuth.AuthCodeStore>();
                builder.Services.AddSingleton<OAuth.IRstsTokenExchanger, OAuth.SdkRstsTokenExchanger>();

                // ForwardedHeaders trust list: loopback by default plus
                // RFC1918 ranges (the common case is cluster-internal
                // ingress). Operators extend the list via the
                // BRIDGE_TRUSTED_PROXIES env var; BridgeOptions.Parse
                // already validated those CIDRs.
                builder.Services.Configure<ForwardedHeadersOptions>(opts =>
                {
                    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                          | ForwardedHeaders.XForwardedProto
                                          | ForwardedHeaders.XForwardedHost;
                    opts.ForwardLimit = 2;
                    opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
                    opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
                    opts.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));

                    foreach (var net in bridgeOptions.TrustedProxies)
                        opts.KnownIPNetworks.Add(net);
                });
            }

            AddSafeguardMcpComponents(builder.Services.AddMcpServer().WithHttpTransport());

            var app = builder.Build();
            SerilogStaticSilencer.ConfigureWithFactory(
                app.Services.GetRequiredService<ILoggerFactory>(),
                Environment.GetEnvironmentVariable);
            LogStartupAuthMode(app.Services.GetRequiredService<ILogger<Program>>());
            // HTTP mode requires SAFEGUARD_HOST at startup; warm the dynamic
            // catalog opportunistically (best-effort, anonymous load via
            // /service/Notification/v4/Status) so the first tool call has
            // schemas/endpoints ready.
            WarmCatalog(app.Services);
            if (bridgeOptions != null)
            {
                // Trust X-Forwarded-* from configured upstreams so
                // BridgeUrlResolver sees the externally-visible scheme
                // and host when the bridge is fronted by an ingress /
                // reverse proxy.
                app.UseForwardedHeaders();
            }
            app.MapHealthChecks("/healthz");
            app.MapMcp("/mcp");
            if (bridgeOptions != null)
            {
                OAuth.WellKnownEndpoints.Map(app);
                OAuth.AuthorizeEndpoints.Map(app, bridgeOptions);
                OAuth.TokenEndpoint.Map(app, bridgeOptions);
                OAuth.RegistrationEndpoint.Map(app, bridgeOptions);

                var pinning = !string.IsNullOrEmpty(bridgeOptions.OverridePublicUrl)
                    ? $"pinned to {bridgeOptions.OverridePublicUrl}"
                    : "inferred per-request from forwarded Host";
                app.Services.GetRequiredService<ILogger<Program>>().LogInformation(
                    "OAuth metadata bridge active ({Mode}); well-known metadata exposed for MCP clients.",
                    pinning);
            }
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