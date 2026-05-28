using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Simulates how an AI agent would construct a JSON request body from schema output.
/// Deliberately naive — uses only the information available in schema text and AGENT HINTS.
/// If a required field cannot be resolved, throws <see cref="SchemaGapException"/> to
/// surface the discoverability problem.
/// </summary>
public class SchemaBodyBuilder
{
    private static readonly Random Rng = new();
    private const string TestPrefix = "McpTest_";

    private readonly AgentSimulationFixture _fixture;
    private readonly Dictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);

    public SchemaBodyBuilder(AgentSimulationFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Manually override a field value. Use this for fields that require setup-provided
    /// values (e.g., an AssetId from a pre-created asset).
    /// </summary>
    public SchemaBodyBuilder WithOverride(string propertyName, JsonNode value)
    {
        _overrides[propertyName] = value.ToJsonString();
        return this;
    }

    /// <summary>
    /// Manually override a field with a raw JSON string value.
    /// </summary>
    public SchemaBodyBuilder WithOverride(string propertyName, string rawJson)
    {
        _overrides[propertyName] = rawJson;
        return this;
    }

    /// <summary>
    /// Builds a JSON body from the schema output text. Parses the required fields,
    /// applies hints, and generates appropriate test values.
    /// </summary>
    /// <param name="schemaOutput">The full text output from Safeguard_Schema</param>
    /// <returns>A JSON string ready for use in Safeguard_Execute</returns>
    /// <exception cref="SchemaGapException">Thrown when a required field cannot be resolved</exception>
    public async Task<string> BuildAsync(string schemaOutput)
    {
        var requiredFields = ParseRequiredFields(schemaOutput);
        var hints = ParseHints(schemaOutput);
        var obj = new JsonObject();

        foreach (var field in requiredFields)
        {
            if (_overrides.TryGetValue(field.Name, out var overrideValue))
            {
                obj[field.Name] = JsonNode.Parse(overrideValue);
                continue;
            }

            var hint = hints.GetValueOrDefault(field.Name);
            var value = await ResolveValueAsync(field.Name, field.Type, hint);
            obj[field.Name] = value;
        }

        // Also include hint-only fields marked as "(Required for creation)"
        var existingFields = new HashSet<string>(
            requiredFields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var (name, hint) in hints)
        {
            if (existingFields.Contains(name))
                continue;
            if (hint.Contains("(Required for creation)", StringComparison.OrdinalIgnoreCase))
            {
                if (_overrides.TryGetValue(name, out var overrideValue))
                {
                    obj[name] = JsonNode.Parse(overrideValue);
                }
                else
                {
                    var value = await ResolveValueAsync(name, "object", hint);
                    obj[name] = value;
                }
                existingFields.Add(name);
            }
        }

        // Always include any remaining overrides (fields the caller knows are needed
        // but that may be Optional in the swagger schema)
        foreach (var (name, jsonValue) in _overrides)
        {
            if (!existingFields.Contains(name))
            {
                obj[name] = JsonNode.Parse(jsonValue);
            }
        }

        return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Builds a JSON body including both required and specified optional fields.
    /// </summary>
    public async Task<string> BuildWithOptionalsAsync(string schemaOutput, params string[] optionalFieldNames)
    {
        var requiredFields = ParseRequiredFields(schemaOutput);
        var optionalFields = ParseOptionalFields(schemaOutput);
        var hints = ParseHints(schemaOutput);
        var obj = new JsonObject();

        foreach (var field in requiredFields)
        {
            if (_overrides.TryGetValue(field.Name, out var overrideValue))
            {
                obj[field.Name] = JsonNode.Parse(overrideValue);
                continue;
            }

            var hint = hints.GetValueOrDefault(field.Name);
            obj[field.Name] = await ResolveValueAsync(field.Name, field.Type, hint);
        }

        foreach (var name in optionalFieldNames)
        {
            if (_overrides.TryGetValue(name, out var overrideValue))
            {
                obj[name] = JsonNode.Parse(overrideValue);
                continue;
            }

            var field = optionalFields.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (field.Name != null)
            {
                var hint = hints.GetValueOrDefault(field.Name);
                obj[field.Name] = await ResolveValueAsync(field.Name, field.Type, hint);
            }
        }

        return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private async Task<JsonNode> ResolveValueAsync(string name, string type, string hint)
    {
        if (hint != null)
        {
            var jsonPattern = ExtractJsonLiteral(hint);
            if (jsonPattern != null)
            {
                return JsonNode.Parse(jsonPattern);
            }

            var sentinel = ExtractSentinelValue(hint);
            if (sentinel.HasValue)
            {
                return JsonValue.Create(sentinel.Value);
            }

            var enumValue = ExtractFirstEnumValue(hint);
            if (enumValue != null)
            {
                return JsonValue.Create(enumValue);
            }

            var discovered = await TryDiscoverValueAsync(name, hint);
            if (discovered != null)
            {
                return discovered;
            }
        }

        return type.ToLowerInvariant() switch
        {
            "string" => JsonValue.Create($"{TestPrefix}{name}_{Rng.Next(10000, 99999)}"),
            "integer" or "int32" or "int64" => throw new SchemaGapException(name, type,
                "Integer field with no hint — agent cannot guess a valid value. Add a SchemaHint."),
            "boolean" or "bool" => JsonValue.Create(false),
            "array" => new JsonArray(),
            "object" => throw new SchemaGapException(name, type,
                "Object field with no hint — agent cannot construct nested object without guidance. Add a SchemaHint."),
            _ => JsonValue.Create($"{TestPrefix}{name}_{Rng.Next(10000, 99999)}")
        };
    }

    private async Task<JsonNode> TryDiscoverValueAsync(string name, string hint)
    {
        var getMatch = Regex.Match(hint, @"GET\s+(/v4/[^\s?]+)(\?[^\s]+)?");
        if (!getMatch.Success)
        {
            return null;
        }

        var path = getMatch.Groups[1].Value;
        var query = getMatch.Groups[2].Success ? getMatch.Groups[2].Value.TrimStart('?') : "limit=1";

        if (!query.Contains("limit", StringComparison.OrdinalIgnoreCase))
        {
            query += "&limit=1";
        }

        try
        {
            var response = await _fixture.ExecuteAsync("GET", path, query: query);

            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];
                if (first.TryGetProperty("Id", out var idProp))
                {
                    return JsonValue.Create(idProp.GetInt32());
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static List<(string Name, string Type)> ParseRequiredFields(string schemaOutput)
    {
        var fields = new List<(string Name, string Type)>();
        var inRequired = false;

        foreach (var line in schemaOutput.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Required:", StringComparison.OrdinalIgnoreCase))
            {
                inRequired = true;
                continue;
            }

            if (trimmed.StartsWith("Optional:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("AGENT HINTS:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("RESPONSE BODY", StringComparison.OrdinalIgnoreCase))
            {
                inRequired = false;
                continue;
            }

            if (inRequired && trimmed != "None")
            {
                var match = Regex.Match(trimmed, @"^(\w+)\s*\((\w+)\)");
                if (match.Success)
                {
                    fields.Add((match.Groups[1].Value, match.Groups[2].Value));
                }
            }
        }

        return fields;
    }

    private static List<(string Name, string Type)> ParseOptionalFields(string schemaOutput)
    {
        var fields = new List<(string Name, string Type)>();
        var inOptional = false;

        foreach (var line in schemaOutput.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Optional:", StringComparison.OrdinalIgnoreCase))
            {
                inOptional = true;
                continue;
            }

            if (trimmed.StartsWith("AGENT HINTS:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("RESPONSE BODY", StringComparison.OrdinalIgnoreCase))
            {
                inOptional = false;
                continue;
            }

            if (inOptional && trimmed != "None")
            {
                var match = Regex.Match(trimmed, @"^(\w+)\s*\((\w+)\)");
                if (match.Success)
                {
                    fields.Add((match.Groups[1].Value, match.Groups[2].Value));
                }
            }
        }

        return fields;
    }

    private static Dictionary<string, string> ParseHints(string schemaOutput)
    {
        var hints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var inHints = false;

        foreach (var line in schemaOutput.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("AGENT HINTS:", StringComparison.OrdinalIgnoreCase))
            {
                inHints = true;
                continue;
            }

            if (inHints && (trimmed.StartsWith("RESPONSE BODY", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(trimmed) && !trimmed.Contains(':')))
            {
                if (trimmed.StartsWith("RESPONSE BODY", StringComparison.OrdinalIgnoreCase))
                {
                    inHints = false;
                    continue;
                }
            }

            if (inHints)
            {
                var hintMatch = Regex.Match(trimmed, @"^(\w+):\s+(.+)$");
                if (hintMatch.Success)
                {
                    hints[hintMatch.Groups[1].Value] = hintMatch.Groups[2].Value;
                }
            }
        }

        return hints;
    }

    private static string ExtractJsonLiteral(string hint)
    {
        var match = Regex.Match(hint, @"\{""[^""]+"":\s*-?\d+\}");
        if (match.Success)
        {
            return match.Value;
        }

        return null;
    }

    private static int? ExtractSentinelValue(string hint)
    {
        var match = Regex.Match(hint, @"[Uu]se\s+(-?\d+)\s");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
        {
            return value;
        }

        return null;
    }

    private static string ExtractFirstEnumValue(string hint)
    {
        var match = Regex.Match(hint, @"[Aa]llowed values:\s*""([^""]+)""");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = Regex.Match(hint, @"[Aa]vailable\s+\w+:\s*(\w+)");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}

/// <summary>
/// Exception thrown when SchemaBodyBuilder cannot resolve a required field value.
/// This indicates a gap in the schema hints that would prevent a real agent from succeeding.
/// </summary>
public class SchemaGapException : Exception
{
    public string FieldName { get; }
    public string FieldType { get; }

    public SchemaGapException(string fieldName, string fieldType, string message)
        : base($"Schema gap for '{fieldName}' ({fieldType}): {message}")
    {
        FieldName = fieldName;
        FieldType = fieldType;
    }
}
