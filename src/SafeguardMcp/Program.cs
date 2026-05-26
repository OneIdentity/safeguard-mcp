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
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
            builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

            // Also write to a log file for diagnosing MCP server issues.
            builder.Logging.AddProvider(new FileLoggerProvider(
                Path.Combine(AppContext.BaseDirectory, "safeguard-mcp.log")));

            builder.Services.AddSingleton<CatalogLoader>();
            builder.Services.AddSingleton<CatalogProvider>();
            builder.Services.AddSingleton<SafeguardConnectionManager>();

            builder.Services.AddMcpServer()
                .WithStdioServerTransport()
                .WithTools<SafeguardApiTool>()
                .WithTools<RandomPasswordTool>()
                .WithResourcesFromAssembly();

            var app = builder.Build();

            await app.RunAsync();
        }
    }
}