using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Shared fixture that establishes a single authenticated connection to a Safeguard
/// appliance for all tests in the collection. Reuses the connection across tests
/// to avoid re-authenticating for every test method.
///
/// Required environment variables (consistent with PySafeguard and safeguard.js):
///   SPP_HOST     — appliance hostname or IP
///   SPP_USERNAME — username (default: "Admin")
///   SPP_PASSWORD — password
///
/// Optional:
///   SPP_PROVIDER — identity provider (default: "local")
///   SPP_CA_FILE  — path to CA cert for TLS verification
///   SPP_VERIFY   — set "false" to disable TLS verification (self-signed lab certs)
/// </summary>
public class ApplianceFixture : IAsyncLifetime
{
    internal TestConnectionManager ConnectionManager { get; private set; }
    public CatalogProvider CatalogProvider { get; private set; }
    public string Host { get; private set; }
    public bool Available { get; private set; }

    public async Task InitializeAsync()
    {
        Host = Environment.GetEnvironmentVariable("SPP_HOST");
        if (string.IsNullOrWhiteSpace(Host))
        {
            Available = false;
            return;
        }

        var user = Environment.GetEnvironmentVariable("SPP_USERNAME") ?? "Admin";
        var password = Environment.GetEnvironmentVariable("SPP_PASSWORD");
        var provider = Environment.GetEnvironmentVariable("SPP_PROVIDER") ?? "local";

        if (string.IsNullOrWhiteSpace(password))
        {
            Available = false;
            return;
        }

        // Determine SSL policy: SPP_VERIFY=false disables TLS verification
        var verifyEnv = Environment.GetEnvironmentVariable("SPP_VERIFY");
        var ignoreSsl = string.Equals(verifyEnv, "false", StringComparison.OrdinalIgnoreCase);

        // Set the env vars that StdioSafeguardSession reads
        Environment.SetEnvironmentVariable("SAFEGUARD_PROVIDER", provider);
        Environment.SetEnvironmentVariable("SAFEGUARD_USER", user);
        Environment.SetEnvironmentVariable("SAFEGUARD_PASSWORD", password);
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", Host);
        Environment.SetEnvironmentVariable("SAFEGUARD_IGNORE_SSL", ignoreSsl.ToString());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Safeguard:IgnoreSsl"] = ignoreSsl.ToString(),
                ["Safeguard:MaxResultsBeforeTruncation"] = "100",
                ["Safeguard:MaxResponseChars"] = "30000",
                ["Safeguard:DefaultLimit"] = "50",
                ["Safeguard:AutoInjectLimit"] = "true",
            })
            .Build();

        var catalogLoader = new CatalogLoader(new NullLogger<CatalogLoader>());
        CatalogProvider = new CatalogProvider(catalogLoader, new NullLogger<CatalogProvider>());
        ConnectionManager = new TestConnectionManager(
            new NullLogger<StdioSafeguardSession>(), config, CatalogProvider);

        try
        {
            // Drive connection through the same path production uses
            await ConnectionManager.EnsureAuthenticatedAsync(null, Host, CancellationToken.None);
            Available = true;

            // Production fires the catalog load fire-and-forget after
            // authentication; tests need the catalog *available* before
            // they run, so await it deterministically instead of
            // racing a fixed Task.Delay.
            await CatalogProvider.LoadCatalogAsync(Host, ignoreSsl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApplianceFixture] Connection failed: {ex.Message}");
            Available = false;
        }
    }

    public Task DisposeAsync()
    {
        ConnectionManager?.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("Appliance")]
public class ApplianceCollection : ICollectionFixture<ApplianceFixture> { }
