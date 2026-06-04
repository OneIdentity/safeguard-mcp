namespace SafeguardMcp.Catalog;

/// <summary>
/// Represents an API endpoint discovered from Safeguard swagger or the static fallback catalog.
/// </summary>
public readonly record struct ApiEndpoint(
    string Service,
    string Method,
    string Path,
    string Summary,
    string Params,
    bool HasBody);

/// <summary>
/// Holds dynamically-loaded catalog data from a live appliance's swagger.
/// </summary>
public class DynamicCatalog
{
    public ApiEndpoint[] Endpoints { get; init; } = Array.Empty<ApiEndpoint>();
    public Dictionary<string, ApiSchema> Schemas { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents the schema for an API request or response body.
/// </summary>
public readonly record struct ApiSchema(
    string TypeName,
    SchemaProperty[] Properties,
    string[] RequiredFields);

/// <summary>
/// Represents a single property in a schema.
/// </summary>
public readonly record struct SchemaProperty(
    string Name,
    string Type,
    string Description,
    bool Required);
