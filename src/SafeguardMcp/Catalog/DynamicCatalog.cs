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

    /// <summary>
    /// Enum vocabularies extracted from the swagger components. Keyed by enum schema name
    /// (case-insensitive). Values are kept in swagger declaration order so the agent sees
    /// them in the same order as the appliance's source.
    /// </summary>
    public Dictionary<string, string[]> Enums { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents the schema for an API request or response body.
/// </summary>
/// <remarks>
/// <para><see cref="OmitReadOnly"/> indicates whether <c>readOnly</c>-marked properties were
/// filtered out at parse time. Request-body schemas set this to <c>true</c> (the agent cannot
/// send <c>Id</c>, <c>CreatedDate</c>, etc.); response-body schemas set this to <c>false</c>
/// so the agent can see what the appliance returns.</para>
/// </remarks>
public readonly record struct ApiSchema(
    string TypeName,
    SchemaProperty[] Properties,
    string[] RequiredFields,
    bool OmitReadOnly)
{
    public ApiSchema(string typeName, SchemaProperty[] properties, string[] requiredFields)
        : this(typeName, properties, requiredFields, true)
    {
    }
}

/// <summary>
/// Represents a single property in a schema.
/// </summary>
/// <remarks>
/// <para><see cref="NestedFields"/> contains the immediate child property names of a complex
/// object/array property whose schema was resolved from a <c>$ref</c>. Empty for primitive
/// properties or when the referenced schema could not be resolved.</para>
/// <para><see cref="NestedProperties"/> carries the same children with full type info, parsed
/// recursively up to a depth cap. This is what feeds the <c>depth</c> knob on
/// <c>Safeguard_Schema</c>; <see cref="NestedFields"/> is preserved as the cheap immediate-name
/// view for callers that only need the names.</para>
/// <para><see cref="EnumName"/> is set when the property is typed by a <c>$ref</c> to an enum
/// schema (i.e. one whose definition has a non-empty <c>.enum</c> array). The renderer uses
/// this to inline allowed values; the dynamic catalog's <c>Enums</c> dictionary holds the
/// values themselves.</para>
/// </remarks>
public readonly record struct SchemaProperty(
    string Name,
    string Type,
    string Description,
    bool Required,
    string[] NestedFields,
    SchemaProperty[] NestedProperties,
    string EnumName)
{
    public SchemaProperty(string name, string type, string description, bool required)
        : this(name, type, description, required, Array.Empty<string>(), Array.Empty<SchemaProperty>(), null)
    {
    }

    public SchemaProperty(string name, string type, string description, bool required, string[] nestedFields)
        : this(name, type, description, required, nestedFields, Array.Empty<SchemaProperty>(), null)
    {
    }
}
