using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

            builder.Services.AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly()
                .WithResourcesFromAssembly();

            var app = builder.Build();
            await app.RunAsync();
        }

        private static async Task RunHttpAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            builder.Logging.AddProvider(new FileLoggerProvider(
                Path.Combine(AppContext.BaseDirectory, "safeguard-mcp.log")));

            RegisterServices(builder.Services);

            builder.Services.AddMcpServer()
                .WithHttpTransport()
                .WithToolsFromAssembly()
                .WithResourcesFromAssembly();

            var app = builder.Build();
            app.MapMcp();
            await app.RunAsync();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<CatalogLoader>();
            services.AddSingleton<CatalogProvider>();
            services.AddSingleton<SafeguardConnectionManager>();
        }
    }
}