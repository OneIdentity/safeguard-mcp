using System.Linq;
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
/// - Providing Discover/Schema/Execute/Reference helper methods
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

    internal TestConnectionManager ConnectionManager { get; private set; }
    public CatalogProvider CatalogProvider { get; private set; }
    internal SafeguardApiTool ApiTool { get; private set; }
    internal SafeguardRetrieveCredentialTool RetrieveCredentialTool { get; private set; }
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
        ConnectionManager = new TestConnectionManager(
            new NullLogger<StdioSafeguardSession>(), config, CatalogProvider);

        // If SPP_HOST is configured, connection failures must propagate — not silently pass.
        Environment.SetEnvironmentVariable("SAFEGUARD_PROVIDER", provider);
        Environment.SetEnvironmentVariable("SAFEGUARD_USER", "admin");
        Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", password);
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", Host);
        Environment.SetEnvironmentVariable("SAFEGUARD_IGNORE_SSL", ignoreSsl.ToString());

        await ConnectionManager.EnsureAuthenticatedAsync(null, Host, CancellationToken.None);
        // Production fires catalog loading fire-and-forget after
        // auth; tests need it loaded before they run. Await
        // deterministically instead of racing a fixed Task.Delay.
        await CatalogProvider.LoadCatalogAsync(Host, ignoreSsl);

        // Sweep stale test users first, as the bootstrap admin: this must
        // happen before CreateTestAdminAsync so a leftover McpTest_Admin from a
        // crashed run doesn't collide on the unique-name constraint.
        await PreCleanStaleUsersAsync();
        await CreateTestAdminAsync(password);

        ConnectionManager.Dispose();
        CatalogProvider = new CatalogProvider(catalogLoader, new NullLogger<CatalogProvider>());
        ConnectionManager = new TestConnectionManager(
            new NullLogger<StdioSafeguardSession>(), config, CatalogProvider);

        Environment.SetEnvironmentVariable("SAFEGUARD_USER", $"{TestPrefix}Admin");
        Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", password);

        await ConnectionManager.EnsureAuthenticatedAsync(null, Host, CancellationToken.None);
        await CatalogProvider.LoadCatalogAsync(Host, ignoreSsl);

        // Sweep stale config objects (assets, accounts, roles, policies) as the
        // freshly-created McpTest_Admin, which holds every admin role. The
        // bootstrap admin often lacks AssetAdmin/PolicyAdmin and gets 403 on
        // these collections, which is why they previously accumulated.
        await PreCleanStaleConfigAsync();

        ApiTool = new SafeguardApiTool(ConnectionManager, CatalogProvider, config);
        RetrieveCredentialTool = new SafeguardRetrieveCredentialTool(
            ConnectionManager, new NullLogger<SafeguardRetrieveCredentialTool>());
        Available = true;
    }

    public async Task DisposeAsync()
    {
        if (ConnectionManager != null && !string.IsNullOrWhiteSpace(Host))
        {
            while (_cleanupStack.Count > 0)
            {
                var (method, path) = _cleanupStack.Pop();
                await DeleteReleasingBlockingRequestAsync(method, path, logTag: "Cleanup");
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
                using var cleanupMgr = new TestConnectionManager(
                    new NullLogger<StdioSafeguardSession>(), config, catalogProvider);
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
        => ApiTool.Safeguard_Schema(path: path, method: method);

    /// <summary>Calls Safeguard_Execute — executes an API call.</summary>
    /// <summary>
    /// Calls <c>Safeguard_Execute</c> and returns the raw API body (peeled out of the
    /// response envelope when one is present). Tests that need to assert against the
    /// envelope itself should call <c>ApiTool.Safeguard_Execute</c> directly.
    /// </summary>
    public async Task<string> ExecuteAsync(string method, string path, string query = null, string body = null, string format = "json")
    {
        var raw = await ApiTool.Safeguard_Execute(null, method: method, path: path, query: query, body: body, format: format);
        return EnvelopeTestHelpers.UnwrapData(raw);
    }

    /// <summary>
    /// Retrieves an access-request password via Safeguard_RetrieveCredential
    /// (the only path that performs the password checkout — Safeguard_Execute
    /// refuses CheckOutPassword as a sensitive endpoint). Returns the
    /// user-audience plaintext block text.
    /// </summary>
    public async Task<string> RetrieveAccessRequestPasswordAsync(string accessRequestId)
    {
        var blocks = await RetrieveCredentialTool.Safeguard_RetrieveCredential(
            null, kind: "access-request-password", accessRequestId: accessRequestId);
        var userBlock = blocks.OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .LastOrDefault();
        return userBlock?.Text;
    }

    /// <summary>Calls Safeguard_Reference topic=query-syntax — gets query syntax help.</summary>
    public string QueryHelp(string path = null)
        => ApiTool.Safeguard_Reference(topic: "query-syntax", path: path);

    /// <summary>Calls Safeguard_Reference topic=workflows — gets workflow recipes.</summary>
    public string GetWorkflows(string search = null, string id = null)
        => ApiTool.Safeguard_Reference(topic: "workflows", search: search, id: id);

    /// <summary>
    /// Registers a cleanup action to be executed in LIFO order during teardown.
    /// Call this after creating any test object.
    /// </summary>
    public void RegisterCleanup(string method, string path)
        => _cleanupStack.Push((method, path));

    /// <summary>
    /// Config collections swept for stale <see cref="TestPrefix"/> objects, in
    /// dependency-safe deletion order: policies reference roles and accounts;
    /// accounts reference assets; so the referencing objects are removed before
    /// the objects they point at. Swept as the privileged McpTest_Admin so an
    /// interrupted run never leaves residue behind for the next one. Users are
    /// swept separately (see <see cref="PreCleanStaleUsersAsync"/>) because that
    /// has to run earlier, before the test admin is created.
    /// </summary>
    private static readonly string[] ConfigSweepResources =
    [
        "AccessPolicies", "Roles", "AssetAccounts", "Assets"
    ];

    /// <summary>Removes stale test users (run as bootstrap admin, before the test admin exists).</summary>
    private Task PreCleanStaleUsersAsync() => SweepStaleAsync("Users");

    /// <summary>Removes stale test config objects (run as the all-roles McpTest_Admin).</summary>
    private async Task PreCleanStaleConfigAsync()
    {
        foreach (var resource in ConfigSweepResources)
            await SweepStaleAsync(resource);
    }

    /// <summary>Deletes every object in <paramref name="resource"/> whose Name starts with the test prefix.</summary>
    private async Task SweepStaleAsync(string resource)
    {
        try
        {
            var response = await ConnectionManager.InvokeAsync(
                Host,
                Service.Core,
                "GET",
                resource,
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
                Console.WriteLine($"[PreClean] Removing stale {resource}: {name} (Id={id})");
                await DeleteReleasingBlockingRequestAsync("DELETE", $"{resource}/{id}", logTag: "PreClean");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PreClean] Warning sweeping {resource}: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes the object at <paramref name="path"/>. Assets and asset accounts can be
    /// pinned by a lingering access request (e.g. left in PendingPasswordReset after a
    /// check-in); the appliance reports this as error 50104 and names the blocking
    /// request. When that happens we close the request (McpTest_Admin holds PolicyAdmin)
    /// and retry, so the underlying object is reclaimable instead of leaking indefinitely.
    /// </summary>
    private async Task DeleteReleasingBlockingRequestAsync(string method, string path, string logTag)
    {
        try
        {
            await ConnectionManager.InvokeAsync(Host, Service.Core, method, path);
        }
        catch (Exception ex)
        {
            var requestId = ExtractBlockingAccessRequestId(ex.Message);
            if (requestId == null)
            {
                Console.WriteLine($"[{logTag}] Failed {method} {path}: {ex.Message}");
                return;
            }

            await CloseAccessRequestAsync(requestId, logTag);
            try
            {
                await ConnectionManager.InvokeAsync(Host, Service.Core, method, path);
            }
            catch (Exception retryEx)
            {
                Console.WriteLine($"[{logTag}] Failed {method} {path} after closing request {requestId}: {retryEx.Message}");
            }
        }
    }

    /// <summary>Closes a blocking access request so its referenced asset/account can be deleted.</summary>
    private async Task CloseAccessRequestAsync(string requestId, string logTag)
    {
        Console.WriteLine($"[{logTag}] Closing blocking access request {requestId}");
        try
        {
            await ConnectionManager.InvokeAsync(
                Host, Service.Core, "POST", $"AccessRequests/{requestId}/Close",
                body: "\"integration test cleanup\"");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{logTag}] Failed to close access request {requestId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Pulls the access-request id out of error 50104, e.g.
    /// "This object is referenced by AccessRequest 1-1-1-... (PendingPasswordReset).",
    /// returning null when the message is not that referenced-by-request error.
    /// </summary>
    private static string ExtractBlockingAccessRequestId(string message)
    {
        const string marker = "AccessRequest ";
        if (string.IsNullOrEmpty(message))
            return null;
        var idx = message.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
            return null;
        var start = idx + marker.Length;
        var end = start;
        while (end < message.Length && !char.IsWhiteSpace(message[end]))
            end++;
        var id = message[start..end].Trim();
        return string.IsNullOrWhiteSpace(id) ? null : id;
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
