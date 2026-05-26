using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Loads the API catalog dynamically from a Safeguard appliance's swagger endpoints.
/// Falls back to the compiled static catalog if swagger is unavailable.
/// </summary>
public class CatalogLoader
{
    private static readonly HttpClientHandler IgnoreSslHandler = new()
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    private static readonly HttpClient HttpClient = new(IgnoreSslHandler)
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly ILogger<CatalogLoader> _logger;

    public CatalogLoader(ILogger<CatalogLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads the API catalog from a live appliance's swagger endpoints.
    /// Returns endpoints and schemas, or null if swagger is unavailable.
    /// </summary>
    public async Task<DynamicCatalog> LoadFromApplianceAsync(string host, CancellationToken ct = default)
    {
        var endpoints = new List<ApiEndpoint>();
        var schemas = new Dictionary<string, ApiSchema>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in new[] { "Core", "Appliance", "Notification" })
        {
            try
            {
                var url = $"https://{host}/service/{service}/swagger/v4/swagger.json";
                _logger.LogInformation("Loading swagger from {Url}...", url);

                var json = await HttpClient.GetStringAsync(url, ct);
                using var doc = JsonDocument.Parse(json);

                ParseSwaggerPaths(doc, service, endpoints);
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
            Schemas = schemas
        };
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

                var paramNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                AddQueryParameterNames(pathItem, paramNames);
                AddQueryParameterNames(operation, paramNames);

                endpoints.Add(new ApiEndpoint(
                    service,
                    method,
                    path,
                    summary,
                    string.Join(", ", paramNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)),
                    hasBody));
            }
        }
    }

    private void ParseSwaggerSchemas(JsonDocument doc, string service, Dictionary<string, ApiSchema> schemas)
    {
        if (!doc.RootElement.TryGetProperty("paths", out var paths))
            return;

        JsonElement components = default;
        var hasComponents = doc.RootElement.TryGetProperty("components", out var componentsRoot)
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

    private static void AddQueryParameterNames(JsonElement container, HashSet<string> paramNames)
    {
        if (!container.TryGetProperty("parameters", out var parameters) || parameters.ValueKind != JsonValueKind.Array)
            return;

        foreach (var param in parameters.EnumerateArray())
        {
            if (param.TryGetProperty("name", out var name)
                && param.TryGetProperty("in", out var location)
                && string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase))
            {
                var paramName = name.GetString();
                if (!string.IsNullOrWhiteSpace(paramName))
                    paramNames.Add(paramName);
            }
        }
    }

    private ApiSchema? ExtractSchemaFromRequestBody(JsonElement requestBody, JsonElement components, bool hasComponents)
    {
        if (!requestBody.TryGetProperty("content", out var content)
            || !TryGetSchemaFromContent(content, out var schema))
        {
            return null;
        }

        return ResolveSchema(schema, components, hasComponents);
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

        return ResolveSchema(schema, components, hasComponents);
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

    private ApiSchema? ResolveSchema(JsonElement schema, JsonElement components, bool hasComponents)
    {
        if (schema.TryGetProperty("$ref", out var refProp))
        {
            var refPath = refProp.GetString();
            if (hasComponents && refPath != null && refPath.StartsWith("#/components/schemas/", StringComparison.Ordinal))
            {
                var schemaName = refPath["#/components/schemas/".Length..];
                if (components.TryGetProperty(schemaName, out var resolved))
                    return ParseSchemaObject(resolved, components, hasComponents, schemaName);
            }

            return null;
        }

        if (schema.TryGetProperty("type", out var typeProp)
            && string.Equals(typeProp.GetString(), "array", StringComparison.OrdinalIgnoreCase))
        {
            if (schema.TryGetProperty("items", out var items))
                return ResolveSchema(items, components, hasComponents);

            return null;
        }

        return ParseSchemaObject(schema, components, hasComponents, null);
    }

    private ApiSchema? ParseSchemaObject(JsonElement schema, JsonElement components, bool hasComponents, string schemaName)
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

            var propType = "string";
            if (propSchema.TryGetProperty("type", out var propertyType))
                propType = propertyType.GetString() ?? "string";
            else if (propSchema.TryGetProperty("$ref", out _))
                propType = "object";

            var description = propSchema.TryGetProperty("description", out var desc)
                ? desc.GetString() ?? string.Empty
                : string.Empty;

            var isReadOnly = propSchema.TryGetProperty("readOnly", out var readOnly)
                && readOnly.ValueKind == JsonValueKind.True;
            if (isReadOnly)
                continue;

            props.Add(new SchemaProperty(
                propName,
                propType,
                description,
                requiredSet.Contains(propName)));
        }

        return new ApiSchema(
            schemaName ?? "inline",
            props.ToArray(),
            requiredSet.ToArray());
    }
}
