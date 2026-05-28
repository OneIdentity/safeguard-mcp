using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;
using Xunit;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Shared fixture for agent simulation tests. Extends ApplianceFixture by:
/// - Creating a test admin user with all admin roles
/// - Re-authenticating as that user
/// - Providing Discover/Schema/Execute/QueryHelp/Workflows helper methods
/// - Managing LIFO cleanup of test objects
/// - Pre-cleaning stale objects from prior failed runs
/// </summary>
public class AgentSimulationFixture : IAsyncLifetime
{
    private const string TestPrefix = "McpTest_";
    private static readonly string[] AllAdminRoles =
    [
        "GlobalAdmin", "Auditor", "ApplicationAuditor", "SystemAuditor",
        "AssetAdmin", "ApplianceAdmin", "PolicyAdmin", "UserAdmin",
        "HelpdeskAdmin", "OperationsAdmin"
    ];

    private readonly Stack<(string Method, string Path)> _cleanupStack = new();

    public SafeguardConnectionManager ConnectionManager { get; private set; }
    public CatalogProvider CatalogProvider { get; private set; }
    public SafeguardApiTool ApiTool { get; private set; }
    public SafeguardWorkflows Workflows { get; private set; }
    public string Host { get; private set; }
    public bool Available { get; private set; }
    public string UnavailableReason { get; private set; }
    public int TestAdminUserId { get; private set; }

    public async Task InitializeAsync()
    {
        Host = Environment.GetEnvironmentVariable("SPP_HOST");
        if (string.IsNullOrWhiteSpace(Host))
        {
            Available = false;
            UnavailableReason = "SPP_HOST environment variable is not set. Set it to run integration tests.";
            return;
        }

        var password = Environment.GetEnvironmentVariable("SPP_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            Available = false;
            UnavailableReason = "SPP_PASSWORD environment variable is not set. Set it to run integration tests.";
            return;
        }

        var provider = Environment.GetEnvironmentVariable("SPP_PROVIDER") ?? "local";
        var verifyEnv = Environment.GetEnvironmentVariable("SPP_VERIFY");
        var ignoreSsl = string.Equals(verifyEnv, "false", StringComparison.OrdinalIgnoreCase);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Safeguard:IgnoreSsl"] = ignoreSsl.ToString(),
                ["Safeguard:MaxResultsBeforeTruncation"] = "200",
                ["Safeguard:MaxResponseChars"] = "60000",
                ["Safeguard:DefaultLimit"] = "50",
                ["Safeguard:AutoInjectLimit"] = "true",
            })
            .Build();

        var catalogLoader = new CatalogLoader(new NullLogger<CatalogLoader>());
        CatalogProvider = new CatalogProvider(catalogLoader, new NullLogger<CatalogProvider>());
        ConnectionManager = new SafeguardConnectionManager(
            new NullLogger<SafeguardConnectionManager>(), config, CatalogProvider);

        // If SPP_HOST is configured, connection failures must propagate — not silently pass.
        Environment.SetEnvironmentVariable("SAFEGUARD_PROVIDER", provider);
        Environment.SetEnvironmentVariable("SAFEGUARD_USER", "admin");
        Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", password);
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", Host);
        Environment.SetEnvironmentVariable("SAFEGUARD_IGNORE_SSL", ignoreSsl.ToString());

        await ConnectionManager.EnsureAuthenticatedAsync(null, Host, CancellationToken.None);
        await Task.Delay(4000);

        await PreCleanStaleObjectsAsync();
        await CreateTestAdminAsync(password);

        ConnectionManager.Dispose();
        CatalogProvider = new CatalogProvider(catalogLoader, new NullLogger<CatalogProvider>());
        ConnectionManager = new SafeguardConnectionManager(
            new NullLogger<SafeguardConnectionManager>(), config, CatalogProvider);

        Environment.SetEnvironmentVariable("SAFEGUARD_USER", $"{TestPrefix}Admin");
        Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", password);

        await ConnectionManager.EnsureAuthenticatedAsync(null, Host, CancellationToken.None);
        await Task.Delay(3000);

        ApiTool = new SafeguardApiTool(ConnectionManager, CatalogProvider, config);
        Workflows = new SafeguardWorkflows();
        Available = true;
    }

    public async Task DisposeAsync()
    {
        if (ConnectionManager != null && !string.IsNullOrWhiteSpace(Host))
        {
            while (_cleanupStack.Count > 0)
            {
                var (method, path) = _cleanupStack.Pop();
                try
                {
                    await ConnectionManager.InvokeAsync(Host, Service.Core, method, path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Cleanup] Failed {method} {path}: {ex.Message}");
                }
            }
        }

        if (TestAdminUserId > 0 && !string.IsNullOrWhiteSpace(Host))
        {
            try
            {
                Environment.SetEnvironmentVariable("SAFEGUARD_USER", "admin");
                var password = Environment.GetEnvironmentVariable("SPP_PASSWORD");
                Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", password);

                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Safeguard:IgnoreSsl"] = Environment.GetEnvironmentVariable("SAFEGUARD_IGNORE_SSL") ?? "false",
                    })
                    .Build();
                var catalogLoader = new CatalogLoader(new NullLogger<CatalogLoader>());
                var catalogProvider = new CatalogProvider(catalogLoader, new NullLogger<CatalogProvider>());
                using var cleanupMgr = new SafeguardConnectionManager(
                    new NullLogger<SafeguardConnectionManager>(), config, catalogProvider);
                await cleanupMgr.EnsureAuthenticatedAsync(null, Host, CancellationToken.None);
                await cleanupMgr.InvokeAsync(Host, Service.Core, "DELETE", $"Users/{TestAdminUserId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Failed to delete test admin: {ex.Message}");
            }
        }

        ConnectionManager?.Dispose();
    }

    /// <summary>
    /// Call at the top of every test that requires a live appliance connection.
    /// Throws Assert.Fail with a clear message if the fixture is unavailable,
    /// ensuring the test FAILS rather than silently passing.
    /// </summary>
    public void RequireAvailable()
    {
        if (!Available)
            Assert.Fail(UnavailableReason ?? "AgentSimulationFixture is not available (unknown reason).");
    }

    /// <summary>Calls Safeguard_Discover — searches for endpoints by keyword/method/service.</summary>
    public string Discover(string search = null, string method = null, string service = null)
        => ApiTool.Safeguard_Discover(service: service, search: search, method: method);

    /// <summary>Calls Safeguard_Schema — gets request/response schema for an endpoint.</summary>
    public string Schema(string path, string method = "POST")
        => ApiTool.Safeguard_Schema(path: path, method: method, host: Host);

    /// <summary>Calls Safeguard_Execute — executes an API call.</summary>
    public async Task<string> ExecuteAsync(string method, string path, string query = null, string body = null, string format = "json")
        => await ApiTool.Safeguard_Execute(null, method: method, path: path, query: query, body: body, format: format, host: Host);

    /// <summary>Calls Safeguard_QueryHelp — gets query syntax help.</summary>
    public string QueryHelp(string path = null)
        => ApiTool.Safeguard_QueryHelp(path: path);

    /// <summary>Calls Safeguard_Workflows — gets workflow recipes.</summary>
    public string GetWorkflows(string search = null, string id = null)
        => Workflows.Safeguard_Workflows(search: search, id: id);

    /// <summary>
    /// Registers a cleanup action to be executed in LIFO order during teardown.
    /// Call this after creating any test object.
    /// </summary>
    public void RegisterCleanup(string method, string path)
        => _cleanupStack.Push((method, path));

    private async Task PreCleanStaleObjectsAsync()
    {
        try
        {
            var response = await ConnectionManager.InvokeAsync(
                Host,
                Service.Core,
                "GET",
                "Users",
                parameters: new Dictionary<string, string>
                {
                    ["filter"] = $"Name isw '{TestPrefix}'",
                    ["fields"] = "Id,Name"
                });

            using var doc = JsonDocument.Parse(response.Body);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var id = element.GetProperty("Id").GetInt32();
                var name = element.GetProperty("Name").GetString();
                Console.WriteLine($"[PreClean] Removing stale test user: {name} (Id={id})");
                try
                {
                    await ConnectionManager.InvokeAsync(Host, Service.Core, "DELETE", $"Users/{id}");
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PreClean] Warning: {ex.Message}");
        }

        try
        {
            var response = await ConnectionManager.InvokeAsync(
                Host,
                Service.Core,
                "GET",
                "Assets",
                parameters: new Dictionary<string, string>
                {
                    ["filter"] = $"Name isw '{TestPrefix}'",
                    ["fields"] = "Id,Name"
                });

            using var doc = JsonDocument.Parse(response.Body);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var id = element.GetProperty("Id").GetInt32();
                var name = element.GetProperty("Name").GetString();
                Console.WriteLine($"[PreClean] Removing stale test asset: {name} (Id={id})");
                try
                {
                    await ConnectionManager.InvokeAsync(Host, Service.Core, "DELETE", $"Assets/{id}");
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PreClean] Warning: {ex.Message}");
        }
    }

    private async Task CreateTestAdminAsync(string password)
    {
        var body = JsonSerializer.Serialize(new
        {
            Name = $"{TestPrefix}Admin",
            AdminRoles = AllAdminRoles,
            PrimaryAuthenticationProvider = new { Id = -1 },
        });

        var response = await ConnectionManager.InvokeAsync(
            Host,
            Service.Core,
            "POST",
            "Users",
            body: body);

        using var doc = JsonDocument.Parse(response.Body);
        TestAdminUserId = doc.RootElement.GetProperty("Id").GetInt32();
        Console.WriteLine($"[AgentSimulationFixture] Created test admin: {TestPrefix}Admin (Id={TestAdminUserId})");

        // Password must be set separately via PUT — POST body Password field does not set local auth credentials.
        var passwordBody = JsonSerializer.Serialize(password);
        await ConnectionManager.InvokeAsync(
            Host,
            Service.Core,
            "PUT",
            $"Users/{TestAdminUserId}/Password",
            body: passwordBody);
        Console.WriteLine($"[AgentSimulationFixture] Set password for test admin (Id={TestAdminUserId})");
    }
}

[CollectionDefinition("AgentSimulation")]
public class AgentSimulationCollection : ICollectionFixture<AgentSimulationFixture> { }
