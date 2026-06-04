using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Provides access to the API catalog and schemas, preferring dynamic (per-host) data
/// with fallback to the compiled static catalog.
/// </summary>
public class CatalogProvider
{
    private readonly ConcurrentDictionary<string, DynamicCatalog> _perHostCatalogs = new(StringComparer.OrdinalIgnoreCase);
    private readonly CatalogLoader _loader;
    private readonly ILogger<CatalogProvider> _logger;

    public CatalogProvider(CatalogLoader loader, ILogger<CatalogProvider> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    /// <summary>
    /// Triggers dynamic catalog loading for a host. Called after successful connection.
    /// Runs in the background and does not block connection.
    /// </summary>
    public async Task LoadCatalogForHostAsync(string host, bool ignoreSsl, CancellationToken ct = default)
    {
        try
        {
            var catalog = await _loader.LoadFromApplianceAsync(host, ignoreSsl, ct);
            if (catalog != null)
            {
                _perHostCatalogs[host] = catalog;
                _logger.LogInformation(
                    "Dynamic catalog loaded for '{Host}': {Endpoints} endpoints, {Schemas} schemas.",
                    host,
                    catalog.Endpoints.Length,
                    catalog.Schemas.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load dynamic catalog for '{Host}'. Using static catalog.", host);
        }
    }

    /// <summary>
    /// Gets the endpoint catalog for a given host, preferring dynamic, falling back to static.
    /// </summary>
    public ReadOnlySpan<ApiEndpoint> GetEndpoints(string host = null)
    {
        if (!string.IsNullOrWhiteSpace(host) && _perHostCatalogs.TryGetValue(host, out var dynamicCatalog))
            return dynamicCatalog.Endpoints.AsSpan();

        return SafeguardCatalog.Endpoints.AsSpan();
    }

    /// <summary>
    /// Gets the schema for a specific endpoint key (e.g. "POST Core /v4/AssetAccounts").
    /// </summary>
    public ApiSchema? GetSchema(string method, string service, string path, string host = null)
    {
        var key = $"{method.ToUpperInvariant()} {service} {path}";

        if (!string.IsNullOrWhiteSpace(host)
            && _perHostCatalogs.TryGetValue(host, out var dynamicCatalog)
            && dynamicCatalog.Schemas.TryGetValue(key, out var schema))
        {
            return schema;
        }

        return null;
    }

    /// <summary>
    /// Gets the response schema for a specific endpoint.
    /// </summary>
    public ApiSchema? GetResponseSchema(string method, string service, string path, string host = null)
    {
        var key = $"RESPONSE {method.ToUpperInvariant()} {service} {path}";

        if (!string.IsNullOrWhiteSpace(host)
            && _perHostCatalogs.TryGetValue(host, out var dynamicCatalog)
            && dynamicCatalog.Schemas.TryGetValue(key, out var schema))
        {
            return schema;
        }

        return null;
    }

    /// <summary>Returns whether a dynamic catalog is loaded for the given host.</summary>
    public bool HasDynamicCatalog(string host) => _perHostCatalogs.ContainsKey(host);

    /// <summary>Removes cached catalog for a host (e.g. on disconnect).</summary>
    public void RemoveCatalog(string host)
    {
        if (!string.IsNullOrWhiteSpace(host))
            _perHostCatalogs.TryRemove(host, out _);
    }
}
