using System.Text.Json;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tests;

public class SchemaParsingTests
{
    private static (JsonElement PropSchema, JsonElement Components, bool HasComponents) Parse(string fullJson)
    {
        // The test JSON shape is: { "components": { "schemas": {...} }, "prop": {... the property schema ...} }
        var doc = JsonDocument.Parse(fullJson);
        var components = default(JsonElement);
        var hasComponents = doc.RootElement.TryGetProperty("components", out var comps)
            && comps.TryGetProperty("schemas", out components);
        var prop = doc.RootElement.GetProperty("prop");
        return (prop, components, hasComponents);
    }

    [Fact]
    public void DescribePropertyType_PrimitiveString()
    {
        var (prop, components, has) = Parse("""{"prop":{"type":"string"}}""");
        var (type, nested) = CatalogLoader.DescribePropertyType(prop, components, has);
        Assert.Equal("string", type);
        Assert.Empty(nested);
    }

    [Fact]
    public void DescribePropertyType_PrimitiveInteger()
    {
        var (prop, components, has) = Parse("""{"prop":{"type":"integer"}}""");
        var (type, nested) = CatalogLoader.DescribePropertyType(prop, components, has);
        Assert.Equal("integer", type);
        Assert.Empty(nested);
    }

    [Fact]
    public void DescribePropertyType_DirectRefExpandsChildNames()
    {
        const string json = """
        {
          "components": {
            "schemas": {
              "TaskProperties": {
                "type": "object",
                "properties": {
                  "HasAccountTaskFailure": {"type": "boolean"},
                  "LastPasswordCheckDate": {"type": "string"}
                }
              }
            }
          },
          "prop": {"$ref": "#/components/schemas/TaskProperties"}
        }
        """;
        var (prop, components, has) = Parse(json);
        var (type, nested) = CatalogLoader.DescribePropertyType(prop, components, has);
        Assert.Equal("object<TaskProperties>", type);
        Assert.Equal(new[] { "HasAccountTaskFailure", "LastPasswordCheckDate" }, nested);
    }

    [Fact]
    public void DescribePropertyType_ArrayOfRefExpandsChildNames()
    {
        const string json = """
        {
          "components": {
            "schemas": {
              "Tag": {
                "type": "object",
                "properties": {
                  "Name": {"type": "string"},
                  "Description": {"type": "string"}
                }
              }
            }
          },
          "prop": {"type":"array","items":{"$ref":"#/components/schemas/Tag"}}
        }
        """;
        var (prop, components, has) = Parse(json);
        var (type, nested) = CatalogLoader.DescribePropertyType(prop, components, has);
        Assert.Equal("array<Tag>", type);
        Assert.Equal(new[] { "Name", "Description" }, nested);
    }

    [Fact]
    public void DescribePropertyType_InlineObjectExpandsChildNames()
    {
        const string json = """
        {
          "prop": {
            "type": "object",
            "properties": {
              "Foo": {"type":"string"},
              "Bar": {"type":"integer"}
            }
          }
        }
        """;
        var (prop, components, has) = Parse(json);
        var (type, nested) = CatalogLoader.DescribePropertyType(prop, components, has);
        Assert.Equal("object", type);
        Assert.Equal(new[] { "Foo", "Bar" }, nested);
    }

    [Fact]
    public void DescribePropertyType_AllOfWithRefIsResolved()
    {
        const string json = """
        {
          "components": {
            "schemas": {
              "Base": {
                "type": "object",
                "properties": {"Id":{"type":"integer"},"Name":{"type":"string"}}
              }
            }
          },
          "prop": {"allOf":[{"$ref":"#/components/schemas/Base"}]}
        }
        """;
        var (prop, components, has) = Parse(json);
        var (type, nested) = CatalogLoader.DescribePropertyType(prop, components, has);
        Assert.Equal("object<Base>", type);
        Assert.Equal(new[] { "Id", "Name" }, nested);
    }

    [Fact]
    public void DescribePropertyType_OneOfReturnsObjectWithoutNested()
    {
        const string json = """
        {
          "prop": {"oneOf":[{"type":"string"},{"type":"integer"}]}
        }
        """;
        var (prop, components, has) = Parse(json);
        var (_, nested) = CatalogLoader.DescribePropertyType(prop, components, has);
        Assert.Empty(nested);
    }

    [Fact]
    public void DescribePropertyType_UnresolvableRefFallsBackToObjectWithoutNested()
    {
        const string json = """
        {
          "components": {"schemas": {}},
          "prop": {"$ref": "#/components/schemas/Missing"}
        }
        """;
        var (prop, components, has) = Parse(json);
        var (type, nested) = CatalogLoader.DescribePropertyType(prop, components, has);
        Assert.Equal("object<Missing>", type);
        Assert.Empty(nested);
    }

    [Fact]
    public void EnumerateImmediateProperties_SkipsReadOnlyUnlessRequired()
    {
        const string json = """
        {
          "type": "object",
          "required": ["KeepReadOnly"],
          "properties": {
            "Visible": {"type":"string"},
            "DroppedReadOnly": {"type":"string","readOnly":true},
            "KeepReadOnly": {"type":"string","readOnly":true}
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);
        var names = CatalogLoader.EnumerateImmediateProperties(doc.RootElement);
        Assert.Equal(new[] { "Visible", "KeepReadOnly" }, names);
    }

    [Fact]
    public void EnumerateImmediateProperties_CapsAt25WithOverflowMarker()
    {
        // Build an object schema with 30 string properties.
        var sb = new System.Text.StringBuilder();
        sb.Append("""{"type":"object","properties":{""");
        for (var i = 0; i < 30; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append("F").Append(i).Append("\":{\"type\":\"string\"}");
        }
        sb.Append("}}");
        using var doc = JsonDocument.Parse(sb.ToString());
        var names = CatalogLoader.EnumerateImmediateProperties(doc.RootElement);
        Assert.Equal(26, names.Length);
        Assert.Equal("F0", names[0]);
        Assert.Equal("F24", names[24]);
        Assert.StartsWith("... (+5 more)", names[25]);
    }

    [Fact]
    public void EnumerateImmediateProperties_EmptyForSchemaWithoutProperties()
    {
        using var doc = JsonDocument.Parse("""{"type":"object"}""");
        Assert.Empty(CatalogLoader.EnumerateImmediateProperties(doc.RootElement));
    }
}
