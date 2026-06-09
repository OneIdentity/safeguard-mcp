using Microsoft.Extensions.Logging;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Provides access to the API catalog and schemas. Single-target:
/// at most one dynamic catalog (loaded from the configured Safeguard
/// appliance) is cached at any time. Returns an empty endpoint span
/// until a dynamic catalog has been successfully loaded.
/// </summary>
public class CatalogProvider
{
    private readonly CatalogLoader _loader;
    private readonly ILogger<CatalogProvider> _logger;
    private DynamicCatalog _dynamic;

    public CatalogProvider(CatalogLoader loader, ILogger<CatalogProvider> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    /// <summary>
    /// Triggers dynamic catalog loading from the appliance. Safe to
    /// call multiple times — the latest successful load wins.
    /// </summary>
    public async Task LoadCatalogAsync(string host, bool ignoreSsl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(host))
            return;

        try
        {
            var catalog = await _loader.LoadFromApplianceAsync(host, ignoreSsl, ct);
            if (catalog != null)
            {
                _dynamic = catalog;
                _logger.LogInformation(
                    "Dynamic catalog loaded from '{Host}': {Endpoints} endpoints, {Schemas} schemas.",
                    host,
                    catalog.Endpoints.Length,
                    catalog.Schemas.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load dynamic catalog from '{Host}'.", host);
        }
    }

    /// <summary>
    /// Gets the endpoint catalog. Returns the dynamic catalog when
    /// loaded; otherwise an empty span.
    /// </summary>
    public ReadOnlySpan<ApiEndpoint> GetEndpoints()
        => _dynamic != null ? _dynamic.Endpoints.AsSpan() : ReadOnlySpan<ApiEndpoint>.Empty;

    /// <summary>
    /// Gets the request schema for a specific endpoint key
    /// (e.g. <c>"POST Core /v4/AssetAccounts"</c>).
    /// </summary>
    public ApiSchema? GetSchema(string method, string service, string path)
    {
        var key = $"{method.ToUpperInvariant()} {service} {path}";
        if (_dynamic != null && _dynamic.Schemas.TryGetValue(key, out var schema))
            return schema;
        return null;
    }

    /// <summary>Gets the response schema for a specific endpoint.</summary>
    public ApiSchema? GetResponseSchema(string method, string service, string path)
    {
        var key = $"RESPONSE {method.ToUpperInvariant()} {service} {path}";
        if (_dynamic != null && _dynamic.Schemas.TryGetValue(key, out var schema))
            return schema;
        return null;
    }

    /// <summary>Whether a dynamic catalog has been loaded.</summary>
    public bool HasDynamicCatalog => _dynamic != null;

    /// <summary>
    /// Looks up enum values by schema name (case-insensitive). Returns null if no enum with
    /// that name was discovered in the loaded swagger.
    /// </summary>
    public string[] GetEnum(string name)
    {
        if (_dynamic == null || string.IsNullOrWhiteSpace(name))
            return null;
        return _dynamic.Enums.TryGetValue(name, out var values) ? values : null;
    }

    /// <summary>
    /// Returns the names of all enums in the dynamic catalog, sorted alphabetically.
    /// Empty when no dynamic catalog has been loaded.
    /// </summary>
    public IReadOnlyList<string> GetEnumNames()
    {
        if (_dynamic == null)
            return Array.Empty<string>();
        return _dynamic.Enums.Keys
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Drops any cached dynamic catalog (e.g. on disconnect).</summary>
    public void ClearCatalog() => _dynamic = null;
}
