using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Loads the API catalog dynamically from a Safeguard appliance's swagger endpoints.
/// Falls back to the compiled static catalog if swagger is unavailable.
/// </summary>
public class CatalogLoader
{
    // AutomaticDecompression is required: the Safeguard appliance serves large swagger
    // documents (notably Core, ~3.5 MB) very slowly when the client does not negotiate
    // gzip — uncompressed chunked transfers can take 40+ seconds, while gzip completes
    // in under a second. See integration-test fixture timing investigation.
    private static readonly HttpClient IgnoreSslClient = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        AutomaticDecompression = DecompressionMethods.All
    }) { Timeout = TimeSpan.FromSeconds(30) };

    private static readonly HttpClient StrictSslClient = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All
    }) { Timeout = TimeSpan.FromSeconds(30) };

    private readonly ILogger<CatalogLoader> _logger;

    public CatalogLoader(ILogger<CatalogLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads the API catalog from a live appliance's swagger endpoints.
    /// Returns endpoints and schemas, or null if swagger is unavailable.
    /// </summary>
    public async Task<DynamicCatalog> LoadFromApplianceAsync(string host, bool ignoreSsl, CancellationToken ct = default)
    {
        var endpoints = new List<ApiEndpoint>();
        var schemas = new Dictionary<string, ApiSchema>(StringComparer.OrdinalIgnoreCase);
        var enums = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var client = ignoreSsl ? IgnoreSslClient : StrictSslClient;

        foreach (var service in new[] { "Core", "Appliance", "Notification" })
        {
            try
            {
                var url = $"https://{host}/service/{service}/swagger/v4/swagger.json";
                _logger.LogInformation("Loading swagger from {Url}...", url);

                var json = await client.GetStringAsync(url, ct);
                using var doc = JsonDocument.Parse(json);

                ParseSwaggerPaths(doc, service, endpoints);
                ExtractEnums(doc.RootElement, enums);
                ParseSwaggerSchemas(doc, service, schemas);

                _logger.LogInformation(
                    "Loaded {Count} endpoints from {Service} swagger.",
                    endpoints.Count(e => e.Service.Equals(service, StringComparison.OrdinalIgnoreCase)),
                    service);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to load swagger for {Service} service. Will use static catalog for this service.",
                    service);
            }
        }

        if (endpoints.Count == 0)
        {
            _logger.LogWarning("No swagger endpoints loaded. Using static catalog entirely.");
            return null;
        }

        return new DynamicCatalog
        {
            Endpoints = endpoints.ToArray(),
            Schemas = schemas,
            Enums = enums
        };
    }

    // Pulls every components.schemas.X with a non-empty `.enum` array into the dictionary,
    // preserving swagger declaration order. Last wins on cross-service collisions (the same
    // enum name typically appears in every service's swagger with identical values).
    internal static void ExtractEnums(JsonElement root, Dictionary<string, string[]> enums)
    {
        if (!root.TryGetProperty("components", out var components)
            || !components.TryGetProperty("schemas", out var schemas))
            return;

        foreach (var entry in schemas.EnumerateObject())
        {
            if (!entry.Value.TryGetProperty("enum", out var enumValues)
                || enumValues.ValueKind != JsonValueKind.Array)
                continue;

            var values = new List<string>();
            foreach (var v in enumValues.EnumerateArray())
            {
                if (v.ValueKind == JsonValueKind.String)
                {
                    var s = v.GetString();
                    if (!string.IsNullOrEmpty(s))
                        values.Add(s);
                }
                else if (v.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                {
                    values.Add(v.GetRawText());
                }
            }

            if (values.Count > 0)
                enums[entry.Name] = values.ToArray();
        }
    }

    // Inspects a $ref target and returns its declared `.enum` values if present, else null.
    private static string[] TryGetEnumValues(JsonElement components, bool hasComponents, string refName)
    {
        if (!hasComponents || string.IsNullOrEmpty(refName))
            return null;
        if (!components.TryGetProperty(refName, out var resolved))
            return null;
        if (!resolved.TryGetProperty("enum", out var enumValues) || enumValues.ValueKind != JsonValueKind.Array)
            return null;
        var values = new List<string>();
        foreach (var v in enumValues.EnumerateArray())
        {
            if (v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString();
                if (!string.IsNullOrEmpty(s))
                    values.Add(s);
            }
            else if (v.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                values.Add(v.GetRawText());
            }
        }
        return values.Count == 0 ? null : values.ToArray();
    }

    private void ParseSwaggerPaths(JsonDocument doc, string service, List<ApiEndpoint> endpoints)
    {
        if (!doc.RootElement.TryGetProperty("paths", out var paths))
            return;

        foreach (var pathEntry in paths.EnumerateObject())
        {
            var path = pathEntry.Name;
            var pathItem = pathEntry.Value;

            foreach (var methodEntry in pathItem.EnumerateObject())
            {
                var method = methodEntry.Name.ToUpperInvariant();
                if (!IsOperationMethod(method))
                    continue;

                var operation = methodEntry.Value;
                var summary = operation.TryGetProperty("summary", out var summaryProp)
                    ? summaryProp.GetString() ?? string.Empty
                    : string.Empty;
                var hasBody = operation.TryGetProperty("requestBody", out _);

                var paramInfos = new List<ParamInfo>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Operation-level parameters take precedence over path-level when names collide.
                CollectParameters(operation, paramInfos, seen);
                CollectParameters(pathItem, paramInfos, seen);

                var queryParamNames = paramInfos
                    .Where(p => string.Equals(p.In, "query", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Name)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

                endpoints.Add(new ApiEndpoint(
                    service,
                    method,
                    path,
                    summary,
                    string.Join(", ", queryParamNames),
                    hasBody,
                    paramInfos.ToArray()));
            }
        }
    }

    private void ParseSwaggerSchemas(JsonDocument doc, string service, Dictionary<string, ApiSchema> schemas)
    {
        ParseSwaggerSchemas(doc.RootElement, service, schemas);
    }

    internal void ParseSwaggerSchemas(JsonElement root, string service, Dictionary<string, ApiSchema> schemas)
    {
        if (!root.TryGetProperty("paths", out var paths))
            return;

        JsonElement components = default;
        var hasComponents = root.TryGetProperty("components", out var componentsRoot)
            && componentsRoot.TryGetProperty("schemas", out components);

        foreach (var pathEntry in paths.EnumerateObject())
        {
            var path = pathEntry.Name;
            foreach (var methodEntry in pathEntry.Value.EnumerateObject())
            {
                var method = methodEntry.Name.ToUpperInvariant();
                if (!IsOperationMethod(method))
                    continue;

                var operation = methodEntry.Value;

                if (operation.TryGetProperty("requestBody", out var requestBody))
                {
                    var schema = ExtractSchemaFromRequestBody(requestBody, components, hasComponents);
                    if (schema != null)
                        schemas[$"{method} {service} {path}"] = schema.Value;
                }

                if (operation.TryGetProperty("responses", out var responses))
                {
                    var responseSchema = ExtractSchemaFromResponse(responses, components, hasComponents);
                    if (responseSchema != null)
                        schemas[$"RESPONSE {method} {service} {path}"] = responseSchema.Value;
                }
            }
        }
    }

    private static bool IsOperationMethod(string method) => method is "GET" or "POST" or "PUT" or "PATCH" or "DELETE";

    // Matches the controller-XML-doc preference marker as it appears in swagger descriptions.
    // Both quoted forms — `(Preferred over 'filter')` (single-quoted, observed on startDate/endDate)
    // and `(Preferred over filter)` (unquoted, observed on userId) — are accepted. Verified on
    // /service/core/swagger/v4/swagger.json from a live appliance.
    private static readonly Regex PreferredOverFilterRegex = new(
        @"\(\s*Preferred\s+over\s+['""]?filter['""]?\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static bool IsPreferredOverFilter(string description)
        => !string.IsNullOrEmpty(description) && PreferredOverFilterRegex.IsMatch(description);

    internal static void CollectParameters(JsonElement container, List<ParamInfo> paramInfos, HashSet<string> seen)
    {
        if (!container.TryGetProperty("parameters", out var parameters) || parameters.ValueKind != JsonValueKind.Array)
            return;

        foreach (var param in parameters.EnumerateArray())
        {
            if (!param.TryGetProperty("name", out var nameProp))
                continue;
            var name = nameProp.GetString();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var location = param.TryGetProperty("in", out var inProp) ? inProp.GetString() ?? string.Empty : string.Empty;
            if (!string.Equals(location, "query", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(location, "path", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Operation-level params are added first; skip the same name on the path-level pass.
            if (!seen.Add(name))
                continue;

            var description = param.TryGetProperty("description", out var descProp)
                ? descProp.GetString() ?? string.Empty
                : string.Empty;

            var required = param.TryGetProperty("required", out var reqProp)
                && reqProp.ValueKind == JsonValueKind.True;

            var type = string.Empty;
            if (param.TryGetProperty("schema", out var schemaProp))
            {
                if (schemaProp.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
                {
                    type = typeProp.GetString() ?? string.Empty;
                    if (string.Equals(type, "array", StringComparison.OrdinalIgnoreCase)
                        && schemaProp.TryGetProperty("items", out var itemsProp)
                        && itemsProp.TryGetProperty("type", out var itemsTypeProp)
                        && itemsTypeProp.ValueKind == JsonValueKind.String)
                    {
                        type = $"array<{itemsTypeProp.GetString()}>";
                    }
                }
                else if (schemaProp.TryGetProperty("$ref", out var refProp))
                {
                    var refPath = refProp.GetString();
                    const string prefix = "#/components/schemas/";
                    if (refPath != null && refPath.StartsWith(prefix, StringComparison.Ordinal))
                        type = refPath[prefix.Length..];
                }
            }

            paramInfos.Add(new ParamInfo(
                name,
                location.ToLowerInvariant(),
                type,
                description,
                required,
                IsPreferredOverFilter(description)));
        }
    }

    private ApiSchema? ExtractSchemaFromRequestBody(JsonElement requestBody, JsonElement components, bool hasComponents)
    {
        if (!requestBody.TryGetProperty("content", out var content)
            || !TryGetSchemaFromContent(content, out var schema))
        {
            return null;
        }

        return ResolveSchema(schema, components, hasComponents, omitReadOnly: true);
    }

    private ApiSchema? ExtractSchemaFromResponse(JsonElement responses, JsonElement components, bool hasComponents)
    {
        JsonElement response = default;
        if (!responses.TryGetProperty("200", out response)
            && !responses.TryGetProperty("201", out response))
        {
            return null;
        }

        if (!response.TryGetProperty("content", out var content)
            || !TryGetSchemaFromContent(content, out var schema))
        {
            return null;
        }

        return ResolveSchema(schema, components, hasComponents, omitReadOnly: false);
    }

    private static bool TryGetSchemaFromContent(JsonElement content, out JsonElement schema)
    {
        foreach (var contentType in new[] { "application/json", "text/json", "*/*" })
        {
            if (content.TryGetProperty(contentType, out var jsonContent)
                && jsonContent.TryGetProperty("schema", out schema))
            {
                return true;
            }
        }

        foreach (var entry in content.EnumerateObject())
        {
            if (entry.Value.TryGetProperty("schema", out schema))
                return true;
        }

        schema = default;
        return false;
    }

    private ApiSchema? ResolveSchema(JsonElement schema, JsonElement components, bool hasComponents, bool omitReadOnly)
    {
        if (schema.TryGetProperty("$ref", out var refProp))
        {
            var refPath = refProp.GetString();
            if (hasComponents && refPath != null && refPath.StartsWith("#/components/schemas/", StringComparison.Ordinal))
            {
                var schemaName = refPath["#/components/schemas/".Length..];
                if (components.TryGetProperty(schemaName, out var resolved))
                    return ParseSchemaObject(resolved, components, hasComponents, schemaName, omitReadOnly);
            }

            return null;
        }

        if (schema.TryGetProperty("type", out var typeProp)
            && string.Equals(typeProp.GetString(), "array", StringComparison.OrdinalIgnoreCase))
        {
            if (schema.TryGetProperty("items", out var items))
                return ResolveSchema(items, components, hasComponents, omitReadOnly);

            return null;
        }

        return ParseSchemaObject(schema, components, hasComponents, null, omitReadOnly);
    }

    private const int MaxNestedFields = 25;

    // Maximum nesting depth for parsed-time recursive child SchemaProperty[] expansion.
    // The renderer's `depth` knob caps at this value; the agent can still chase deeper paths
    // via the flat property-path closure on ApiSchema (added by the property-paths feature).
    private const int MaxParsedNestedDepth = 3;

    private ApiSchema? ParseSchemaObject(JsonElement schema, JsonElement components, bool hasComponents, string schemaName, bool omitReadOnly)
    {
        if (!schema.TryGetProperty("properties", out var properties))
            return null;

        var requiredSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var req in required.EnumerateArray())
            {
                var requiredName = req.GetString();
                if (!string.IsNullOrWhiteSpace(requiredName))
                    requiredSet.Add(requiredName);
            }
        }

        var props = new List<SchemaProperty>();
        foreach (var prop in properties.EnumerateObject())
        {
            var propName = prop.Name;
            var propSchema = prop.Value;

            var (propType, nestedFields, enumName) = DescribePropertyTypeWithEnum(propSchema, components, hasComponents, omitReadOnly);
            var nestedProps = BuildNestedProperties(propSchema, components, hasComponents, omitReadOnly, depth: 1, parentChain: schemaName);

            var description = propSchema.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? string.Empty
                : string.Empty;

            var isReadOnly = propSchema.TryGetProperty("readOnly", out var readOnly)
                && readOnly.ValueKind == JsonValueKind.True;
            // Keep readOnly properties if they are in the required array — the swagger may mark
            // navigation properties as readOnly for GET responses while they are still needed in
            // POST/PUT request bodies (common in Safeguard API swagger documentation).
            if (omitReadOnly && isReadOnly && !requiredSet.Contains(propName))
                continue;

            props.Add(new SchemaProperty(
                propName,
                propType,
                description,
                requiredSet.Contains(propName),
                nestedFields,
                nestedProps,
                enumName));
        }

        return new ApiSchema(
            schemaName ?? "inline",
            props.ToArray(),
            requiredSet.ToArray(),
            omitReadOnly);
    }

    /// <summary>
    /// Computes the human-readable type label and the immediate child property names for a
    /// property's schema. For complex types referenced via <c>$ref</c> the type is decorated with
    /// the referenced schema name (e.g. <c>object&lt;TaskProperties&gt;</c>) and the child names
    /// are returned. For enum-typed <c>$ref</c>s the label is <c>enum&lt;Name&gt;</c> and no
    /// nested fields are emitted (callers can resolve allowed values via the catalog's
    /// <c>Enums</c> dictionary or the <c>EnumName</c> field on <see cref="SchemaProperty"/>).
    /// </summary>
    internal static (string Type, string[] NestedFields) DescribePropertyType(
        JsonElement propSchema,
        JsonElement components,
        bool hasComponents)
    {
        var (type, nested, _) = DescribePropertyTypeWithEnum(propSchema, components, hasComponents, omitReadOnly: true);
        return (type, nested);
    }

    internal static (string Type, string[] NestedFields) DescribePropertyType(
        JsonElement propSchema,
        JsonElement components,
        bool hasComponents,
        bool omitReadOnly)
    {
        var (type, nested, _) = DescribePropertyTypeWithEnum(propSchema, components, hasComponents, omitReadOnly);
        return (type, nested);
    }

    internal static (string Type, string[] NestedFields, string EnumName) DescribePropertyTypeWithEnum(
        JsonElement propSchema,
        JsonElement components,
        bool hasComponents,
        bool omitReadOnly)
    {
        // Direct $ref to a complex type — enum or object.
        if (propSchema.TryGetProperty("$ref", out var refProp))
        {
            var refName = ResolveRefName(refProp);
            if (refName != null && TryGetEnumValues(components, hasComponents, refName) != null)
                return ($"enum<{refName}>", Array.Empty<string>(), refName);

            var nested = TryEnumerateRefProperties(refProp, components, hasComponents, omitReadOnly);
            return (refName != null ? $"object<{refName}>" : "object", nested, null);
        }

        // type: array of complex items.
        if (propSchema.TryGetProperty("type", out var typeProp)
            && string.Equals(typeProp.GetString(), "array", StringComparison.OrdinalIgnoreCase))
        {
            if (propSchema.TryGetProperty("items", out var items))
            {
                if (items.TryGetProperty("$ref", out var itemsRef))
                {
                    var refName = ResolveRefName(itemsRef);
                    if (refName != null && TryGetEnumValues(components, hasComponents, refName) != null)
                        return ($"array<enum<{refName}>>", Array.Empty<string>(), refName);

                    var nested = TryEnumerateRefProperties(itemsRef, components, hasComponents, omitReadOnly);
                    return (refName != null ? $"array<{refName}>" : "array", nested, null);
                }

                if (items.TryGetProperty("properties", out _))
                    return ("array<object>", EnumerateImmediateProperties(items, omitReadOnly), null);
            }

            return ("array", Array.Empty<string>(), null);
        }

        // Inline object with properties.
        if (typeProp.ValueKind == JsonValueKind.String
            && string.Equals(typeProp.GetString(), "object", StringComparison.OrdinalIgnoreCase)
            && propSchema.TryGetProperty("properties", out _))
        {
            return ("object", EnumerateImmediateProperties(propSchema, omitReadOnly), null);
        }

        // allOf containing a $ref or inline object — pick the first resolvable part.
        if (propSchema.TryGetProperty("allOf", out var allOf) && allOf.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in allOf.EnumerateArray())
            {
                if (part.TryGetProperty("$ref", out var partRef))
                {
                    var refName = ResolveRefName(partRef);
                    if (refName != null && TryGetEnumValues(components, hasComponents, refName) != null)
                        return ($"enum<{refName}>", Array.Empty<string>(), refName);

                    var nested = TryEnumerateRefProperties(partRef, components, hasComponents, omitReadOnly);
                    if (nested.Length > 0)
                        return (refName != null ? $"object<{refName}>" : "object", nested, null);
                }
                else if (part.TryGetProperty("properties", out _))
                {
                    return ("object", EnumerateImmediateProperties(part, omitReadOnly), null);
                }
            }
        }

        // Primitive or free-form object — preserve original behavior.
        var propType = "string";
        if (typeProp.ValueKind == JsonValueKind.String)
            propType = typeProp.GetString() ?? "string";
        else if (propSchema.TryGetProperty("$ref", out _))
            propType = "object";

        return (propType, Array.Empty<string>(), null);
    }

    // Recursive child SchemaProperty[] expansion done at parse time. Returns an array of
    // SchemaProperty matching the children of this property's resolved object schema, walked up
    // to MaxParsedNestedDepth levels deep. Cycle-guarded by parent-type chain.
    private static SchemaProperty[] BuildNestedProperties(
        JsonElement propSchema,
        JsonElement components,
        bool hasComponents,
        bool omitReadOnly,
        int depth,
        string parentChain)
    {
        if (depth >= MaxParsedNestedDepth)
            return Array.Empty<SchemaProperty>();

        // Resolve to an object schema (direct $ref, allOf-of-$ref, array-of-$ref, or inline).
        if (!TryResolveToObjectSchema(propSchema, components, hasComponents, parentChain, out var resolved, out var nextChain))
            return Array.Empty<SchemaProperty>();

        if (!resolved.TryGetProperty("properties", out var properties))
            return Array.Empty<SchemaProperty>();

        var requiredSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (resolved.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var req in required.EnumerateArray())
            {
                var requiredName = req.GetString();
                if (!string.IsNullOrWhiteSpace(requiredName))
                    requiredSet.Add(requiredName);
            }
        }

        var list = new List<SchemaProperty>();
        foreach (var prop in properties.EnumerateObject())
        {
            var isReadOnly = prop.Value.TryGetProperty("readOnly", out var readOnly)
                && readOnly.ValueKind == JsonValueKind.True;
            if (omitReadOnly && isReadOnly && !requiredSet.Contains(prop.Name))
                continue;

            var (childType, childNestedFields, childEnumName) = DescribePropertyTypeWithEnum(prop.Value, components, hasComponents, omitReadOnly);
            var description = prop.Value.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? string.Empty
                : string.Empty;
            var children = BuildNestedProperties(prop.Value, components, hasComponents, omitReadOnly, depth + 1, nextChain);
            list.Add(new SchemaProperty(
                prop.Name,
                childType,
                description,
                requiredSet.Contains(prop.Name),
                childNestedFields,
                children,
                childEnumName));
        }

        return list.ToArray();
    }

    // Walks single-step indirections (direct $ref, allOf-of-$ref, array-items-$ref/object) to
    // find an object schema with a `properties` block. Returns the resolved element and the
    // updated parent-chain used for cycle-guarding (5-deep matches the appliance's serializer).
    private static bool TryResolveToObjectSchema(
        JsonElement propSchema,
        JsonElement components,
        bool hasComponents,
        string parentChain,
        out JsonElement resolved,
        out string nextChain)
    {
        nextChain = parentChain ?? string.Empty;

        if (propSchema.TryGetProperty("$ref", out var refProp))
        {
            var name = ResolveRefName(refProp);
            if (!CycleGuardOk(parentChain, name) || !hasComponents || name == null
                || !components.TryGetProperty(name, out var refTarget))
            {
                resolved = default;
                return false;
            }
            nextChain = AppendChain(parentChain, name);
            resolved = refTarget;

            // Single allOf-of-$ref hop for inheritance shapes.
            if (!resolved.TryGetProperty("properties", out _)
                && resolved.TryGetProperty("allOf", out var allOfInner)
                && allOfInner.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in allOfInner.EnumerateArray())
                {
                    if (part.TryGetProperty("properties", out _))
                    {
                        resolved = part;
                        break;
                    }
                }
            }
            return true;
        }

        if (propSchema.TryGetProperty("type", out var typeProp)
            && string.Equals(typeProp.GetString(), "array", StringComparison.OrdinalIgnoreCase)
            && propSchema.TryGetProperty("items", out var items))
        {
            return TryResolveToObjectSchema(items, components, hasComponents, parentChain, out resolved, out nextChain);
        }

        if (propSchema.TryGetProperty("allOf", out var allOf) && allOf.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in allOf.EnumerateArray())
            {
                if (TryResolveToObjectSchema(part, components, hasComponents, parentChain, out resolved, out nextChain))
                    return true;
            }
        }

        if (propSchema.TryGetProperty("properties", out _))
        {
            resolved = propSchema;
            return true;
        }

        resolved = default;
        return false;
    }

    private static bool CycleGuardOk(string parentChain, string nextName)
    {
        if (string.IsNullOrEmpty(parentChain) || string.IsNullOrEmpty(nextName))
            return true;
        // Same type may appear at most 5 levels deep (matches the appliance serializer).
        var count = 0;
        var idx = 0;
        while (true)
        {
            var match = parentChain.IndexOf("/" + nextName + "/", idx, StringComparison.OrdinalIgnoreCase);
            if (match < 0)
                break;
            count++;
            idx = match + 1;
        }
        return count < 5;
    }

    private static string AppendChain(string parentChain, string name)
    {
        if (string.IsNullOrEmpty(parentChain))
            return "/" + name + "/";
        return parentChain + name + "/";
    }

    private static string ResolveRefName(JsonElement refProp)
    {
        var refPath = refProp.GetString();
        const string prefix = "#/components/schemas/";
        if (refPath != null && refPath.StartsWith(prefix, StringComparison.Ordinal))
            return refPath[prefix.Length..];
        return null;
    }

    private static string[] TryEnumerateRefProperties(JsonElement refProp, JsonElement components, bool hasComponents)
        => TryEnumerateRefProperties(refProp, components, hasComponents, omitReadOnly: true);

    private static string[] TryEnumerateRefProperties(JsonElement refProp, JsonElement components, bool hasComponents, bool omitReadOnly)
    {
        if (!hasComponents)
            return Array.Empty<string>();

        var refName = ResolveRefName(refProp);
        if (refName == null || !components.TryGetProperty(refName, out var resolved))
            return Array.Empty<string>();

        // Follow a single allOf-of-$ref level if needed — e.g. inheritance shapes.
        if (!resolved.TryGetProperty("properties", out _)
            && resolved.TryGetProperty("allOf", out var allOf)
            && allOf.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in allOf.EnumerateArray())
            {
                if (part.TryGetProperty("properties", out _))
                {
                    resolved = part;
                    break;
                }
            }
        }

        return EnumerateImmediateProperties(resolved, omitReadOnly);
    }

    internal static string[] EnumerateImmediateProperties(JsonElement schema)
        => EnumerateImmediateProperties(schema, omitReadOnly: true);

    internal static string[] EnumerateImmediateProperties(JsonElement schema, bool omitReadOnly)
    {
        if (!schema.TryGetProperty("properties", out var properties))
            return Array.Empty<string>();

        var requiredSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var req in required.EnumerateArray())
            {
                var requiredName = req.GetString();
                if (!string.IsNullOrWhiteSpace(requiredName))
                    requiredSet.Add(requiredName);
            }
        }

        var kept = new List<string>();
        var totalKept = 0;
        foreach (var prop in properties.EnumerateObject())
        {
            var isReadOnly = prop.Value.TryGetProperty("readOnly", out var readOnly)
                && readOnly.ValueKind == JsonValueKind.True;
            if (omitReadOnly && isReadOnly && !requiredSet.Contains(prop.Name))
                continue;

            totalKept++;
            if (kept.Count < MaxNestedFields)
                kept.Add(prop.Name);
        }

        if (totalKept == 0)
            return Array.Empty<string>();

        if (totalKept > MaxNestedFields)
        {
            var result = new string[MaxNestedFields + 1];
            for (var i = 0; i < MaxNestedFields; i++)
                result[i] = kept[i];
            result[MaxNestedFields] = $"... (+{totalKept - MaxNestedFields} more)";
            return result;
        }

        return kept.ToArray();
    }
}
