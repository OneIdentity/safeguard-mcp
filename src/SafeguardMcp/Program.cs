using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp
{
    // See:
    // https://github.com/dotnet/extensions/tree/main/src/ProjectTemplates/Microsoft.McpServer.ProjectTemplates/templates/McpServer-CSharp/local
    // and
    // https://modelcontextprotocol.io/docs/develop/build-server
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Contains("--http", StringComparer.OrdinalIgnoreCase))
                await RunHttpAsync(args);
            else
                await RunStdioAsync(args);
        }

        private static async Task RunStdioAsync(string[] args)
        {
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
            builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
            builder.Logging.AddProvider(new FileLoggerProvider(
                Path.Combine(AppContext.BaseDirectory, "safeguard-mcp.log")));

            RegisterServices(builder.Services);

            AddSafeguardMcpComponents(builder.Services.AddMcpServer().WithStdioServerTransport());

            var app = builder.Build();
            await app.RunAsync();
        }

        private static async Task RunHttpAsync(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            builder.Logging.AddProvider(new FileLoggerProvider(
                Path.Combine(AppContext.BaseDirectory, "safeguard-mcp.log")));

            RegisterServices(builder.Services);

            AddSafeguardMcpComponents(builder.Services.AddMcpServer().WithHttpTransport());

            var app = builder.Build();
            app.MapMcp();
            await app.RunAsync();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<CatalogLoader>();
            services.AddSingleton<CatalogProvider>();
            services.AddSingleton<ISafeguardConnectionFactory, SafeguardConnectionFactory>();
            services.AddSingleton<SafeguardConnectionManager>();
        }

        // Explicit generic registration preserves required members for trimming/AOT
        // and avoids the reflection-based assembly scan that WithToolsFromAssembly /
        // WithResourcesFromAssembly perform. Centralized here so stdio and HTTP
        // transports stay in lockstep when new tools or resources are added.
        private static IMcpServerBuilder AddSafeguardMcpComponents(IMcpServerBuilder builder) => builder
            .WithTools<SafeguardApiTool>()
            .WithTools<RandomPasswordTool>()
            .WithTools<SafeguardWorkflows>()
            .WithResources<ApiOverviewResource>()
            .WithResources<CommonPatternsResource>()
            .WithResources<QuerySyntaxResource>()
            .WithResources<TerminologyResource>();
    }
}