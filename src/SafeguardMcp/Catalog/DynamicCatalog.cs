namespace SafeguardMcp.Catalog;

/// <summary>
/// Represents an API endpoint discovered from Safeguard swagger or the static fallback catalog.
/// </summary>
/// <remarks>
/// <para><see cref="Params"/> is a comma-joined list of query parameter names retained for
/// back-compat with code that only needs a quick name check (e.g. limit-injection heuristics).
/// New rendering should prefer <see cref="ParamInfos"/>, which carries per-parameter
/// description, type, required-ness, location, and the "Preferred over filter" marker pulled
/// from the controller XML docs via swagger.</para>
/// </remarks>
public readonly record struct ApiEndpoint(
    string Service,
    string Method,
    string Path,
    string Summary,
    string Params,
    bool HasBody,
    ParamInfo[] ParamInfos)
{
    public ApiEndpoint(string service, string method, string path, string summary, string @params, bool hasBody)
        : this(service, method, path, summary, @params, hasBody, Array.Empty<ParamInfo>())
    {
    }
}

/// <summary>
/// Per-parameter metadata for an API endpoint, sourced from the swagger document.
/// </summary>
/// <remarks>
/// <para><see cref="PreferredOverFilter"/> is detected by regex on <see cref="Description"/>;
/// the Safeguard controllers mark first-class scoping parameters (e.g. <c>startDate</c>,
/// <c>endDate</c>, <c>userId</c>, <c>assetId</c>, <c>accountId</c>) with a
/// <c>(Preferred over 'filter')</c> sentence in their XML docs. Both quoted and unquoted forms
/// are accepted.</para>
/// </remarks>
public readonly record struct ParamInfo(
    string Name,
    string In,
    string Type,
    string Description,
    bool Required,
    bool PreferredOverFilter);

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
/// <remarks>
/// <para><see cref="NestedFields"/> contains the immediate child property names of a complex
/// object/array property whose schema was resolved from a <c>$ref</c>. Empty for primitive
/// properties or when the referenced schema could not be resolved.</para>
/// </remarks>
public readonly record struct SchemaProperty(
    string Name,
    string Type,
    string Description,
    bool Required,
    string[] NestedFields)
{
    public SchemaProperty(string name, string type, string description, bool required)
        : this(name, type, description, required, Array.Empty<string>())
    {
    }
}
