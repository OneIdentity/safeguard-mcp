using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
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
    public void EnumerateImmediateProperties_OmitReadOnlyFalseKeepsReadOnly()
    {
        const string json = """
        {
          "type": "object",
          "properties": {
            "Visible": {"type":"string"},
            "DroppedReadOnly": {"type":"string","readOnly":true}
          }
        }
        """;
        using var doc = JsonDocument.Parse(json);
        var names = CatalogLoader.EnumerateImmediateProperties(doc.RootElement, omitReadOnly: false);
        Assert.Equal(new[] { "Visible", "DroppedReadOnly" }, names);
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

    private static Dictionary<string, ApiSchema> ParseSwaggerForTests(string swaggerJson, string service = "Core")
    {
        var loader = new CatalogLoader(NullLogger<CatalogLoader>.Instance);
        var schemas = new Dictionary<string, ApiSchema>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(swaggerJson);
        loader.ParseSwaggerSchemas(doc.RootElement, service, schemas);
        return schemas;
    }

    [Fact]
    public void ParseSwagger_PostWriteEndpoint_RequestSchemaOmitsReadOnly_ResponseSchemaIncludesIt()
    {
        // POST /v4/AccessRequests-style write endpoint:
        // - request body has a couple of writable fields plus a readOnly Id (must be filtered)
        // - response body returns the same DTO with the readOnly Id present (must be visible)
        const string swagger = """
        {
          "components": {
            "schemas": {
              "NewAccessRequest": {
                "type": "object",
                "required": ["AccountId", "AccessRequestType"],
                "properties": {
                  "Id": {"type":"integer","readOnly":true},
                  "AccountId": {"type":"integer"},
                  "AccessRequestType": {"type":"string"},
                  "RequestedDurationDays": {"type":"integer"}
                }
              }
            }
          },
          "paths": {
            "/v4/AccessRequests": {
              "post": {
                "requestBody": {"content":{"application/json":{"schema":{"$ref":"#/components/schemas/NewAccessRequest"}}}},
                "responses": {"201": {"content":{"application/json":{"schema":{"$ref":"#/components/schemas/NewAccessRequest"}}}}}
              }
            }
          }
        }
        """;
        var schemas = ParseSwaggerForTests(swagger);

        Assert.True(schemas.ContainsKey("POST Core /v4/AccessRequests"), "request body schema missing");
        Assert.True(schemas.ContainsKey("RESPONSE POST Core /v4/AccessRequests"), "POST response schema missing");

        var request = schemas["POST Core /v4/AccessRequests"];
        Assert.True(request.OmitReadOnly);
        Assert.DoesNotContain(request.Properties, p => p.Name == "Id");
        Assert.Contains(request.Properties, p => p.Name == "AccountId" && p.Required);
        Assert.Contains(request.Properties, p => p.Name == "AccessRequestType" && p.Required);

        var response = schemas["RESPONSE POST Core /v4/AccessRequests"];
        Assert.False(response.OmitReadOnly);
        Assert.Contains(response.Properties, p => p.Name == "Id");
        Assert.Contains(response.Properties, p => p.Name == "AccountId");
    }

    [Fact]
    public void ParseSwagger_NestedRefProducesRecursiveNestedProperties()
    {
        // Verifies depth-aware parsing: a $ref child carries full SchemaProperty[] children,
        // not just immediate names. This is what the depth knob on Safeguard_Schema renders.
        const string swagger = """
        {
          "components": {
            "schemas": {
              "Outer": {
                "type": "object",
                "properties": {
                  "Inner": {"$ref": "#/components/schemas/Inner"}
                }
              },
              "Inner": {
                "type": "object",
                "properties": {
                  "Leaf": {"type": "string"},
                  "Count": {"type": "integer"}
                }
              }
            }
          },
          "paths": {
            "/v4/Outer": {
              "get": {
                "responses": {"200":{"content":{"application/json":{"schema":{"$ref":"#/components/schemas/Outer"}}}}}
              }
            }
          }
        }
        """;
        var schemas = ParseSwaggerForTests(swagger);
        var outer = schemas["RESPONSE GET Core /v4/Outer"];
        var inner = Assert.Single(outer.Properties, p => p.Name == "Inner");
        Assert.Equal("object<Inner>", inner.Type);
        Assert.Equal(new[] { "Leaf", "Count" }, inner.NestedFields);
        Assert.Equal(2, inner.NestedProperties.Length);
        Assert.Equal("Leaf", inner.NestedProperties[0].Name);
        Assert.Equal("string", inner.NestedProperties[0].Type);
        Assert.Equal("Count", inner.NestedProperties[1].Name);
        Assert.Equal("integer", inner.NestedProperties[1].Type);
    }

    [Fact]
    public void ExtractEnums_PullsAllEnumComponentsInDeclarationOrder()
    {
        const string swagger = """
        {
          "components": {
            "schemas": {
              "AccessRequestType": {
                "type": "string",
                "enum": ["Password","RemoteDesktop","Ssh","Telnet","SshKey","RemoteDesktopApplication","ApiKey","File"]
              },
              "ScheduleType": {
                "type": "string",
                "enum": ["Never","Hourly","Daily","Weekly","Monthly"]
              },
              "PlainObject": {
                "type": "object",
                "properties": {"Foo":{"type":"string"}}
              }
            }
          }
        }
        """;
        using var doc = JsonDocument.Parse(swagger);
        var enums = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        CatalogLoader.ExtractEnums(doc.RootElement, enums);

        Assert.True(enums.ContainsKey("accessrequesttype"), "case-insensitive lookup must work");
        Assert.Equal(
            new[] { "Password", "RemoteDesktop", "Ssh", "Telnet", "SshKey", "RemoteDesktopApplication", "ApiKey", "File" },
            enums["AccessRequestType"]);
        Assert.Equal(new[] { "Never", "Hourly", "Daily", "Weekly", "Monthly" }, enums["ScheduleType"]);
        Assert.False(enums.ContainsKey("PlainObject"));
    }

    [Fact]
    public void DescribePropertyType_EnumRefRendersAsEnumLabel()
    {
        // E035: a property typed `$ref:AccessRequestType` should be rendered as enum<...>,
        // not object<...>; nested fields must be empty.
        const string swagger = """
        {
          "components": {
            "schemas": {
              "AccessRequestType": {"type":"string","enum":["Password","Ssh","RemoteDesktop"]},
              "NewAccessRequest": {
                "type": "object",
                "required": ["AccessRequestType"],
                "properties": {
                  "AccessRequestType": {"$ref":"#/components/schemas/AccessRequestType"}
                }
              }
            }
          },
          "paths": {
            "/v4/AccessRequests": {
              "post": {
                "requestBody": {"content":{"application/json":{"schema":{"$ref":"#/components/schemas/NewAccessRequest"}}}},
                "responses": {"201":{"content":{"application/json":{"schema":{"$ref":"#/components/schemas/NewAccessRequest"}}}}}
              }
            }
          }
        }
        """;
        var schemas = ParseSwaggerForTests(swagger);
        var req = schemas["POST Core /v4/AccessRequests"];
        var prop = Assert.Single(req.Properties, p => p.Name == "AccessRequestType");
        Assert.Equal("enum<AccessRequestType>", prop.Type);
        Assert.Equal("AccessRequestType", prop.EnumName);
        Assert.Empty(prop.NestedFields);
    }

    [Fact]
    public void ParseSwagger_PathsIncludeNestedAndSyntheticCount()
    {
        // Verifies the property-path closure on the response schema:
        // - top-level Id, AccountId, AccessRequestType (enum)
        // - nested Account.Id, Account.Name, Account.Asset.Id, Account.Asset.Name
        // - collection ScopeItems (array<object<ScopeItem>>) emits a synthetic ScopeItems.Count
        const string swagger = """
        {
          "components": {
            "schemas": {
              "AccessRequestType": {"type":"string","enum":["Password","Ssh"]},
              "Asset": {
                "type": "object",
                "properties": {
                  "Id": {"type":"integer"},
                  "Name": {"type":"string"}
                }
              },
              "Account": {
                "type": "object",
                "properties": {
                  "Id": {"type":"integer"},
                  "Name": {"type":"string"},
                  "Asset": {"$ref": "#/components/schemas/Asset"}
                }
              },
              "ScopeItem": {
                "type": "object",
                "properties": {"Id":{"type":"integer"}}
              },
              "AccessRequest": {
                "type": "object",
                "properties": {
                  "Id": {"type":"integer"},
                  "AccountId": {"type":"integer"},
                  "AccessRequestType": {"$ref":"#/components/schemas/AccessRequestType"},
                  "Account": {"$ref":"#/components/schemas/Account"},
                  "ScopeItems": {"type":"array","items":{"$ref":"#/components/schemas/ScopeItem"}}
                }
              }
            }
          },
          "paths": {
            "/v4/AccessRequests": {
              "get": {
                "responses": {"200":{"content":{"application/json":{"schema":{"$ref":"#/components/schemas/AccessRequest"}}}}}
              }
            }
          }
        }
        """;
        var schemas = ParseSwaggerForTests(swagger);
        var resp = schemas["RESPONSE GET Core /v4/AccessRequests"];

        var pathLookup = resp.Paths.ToDictionary(p => p.Path, p => p, StringComparer.Ordinal);

        Assert.Contains("Id", pathLookup.Keys);
        Assert.Contains("AccountId", pathLookup.Keys);

        // Nested path through a $ref child (full-graph form, what `fields=` walks).
        Assert.Contains("Account.Id", pathLookup.Keys);
        Assert.Contains("Account.Name", pathLookup.Keys);
        Assert.Contains("Account.Asset.Id", pathLookup.Keys);
        Assert.Contains("Account.Asset.Name", pathLookup.Keys);

        // Enum leaf carries EnumName.
        var typePath = pathLookup["AccessRequestType"];
        Assert.Equal("enum<AccessRequestType>", typePath.Type);
        Assert.Equal("AccessRequestType", typePath.EnumName);

        // Collection emits IsCollection + a synthetic .Count entry.
        var collection = pathLookup["ScopeItems"];
        Assert.True(collection.IsCollection);
        Assert.False(collection.IsSynthetic);
        var count = pathLookup["ScopeItems.Count"];
        Assert.True(count.IsSynthetic);
        Assert.Equal("integer", count.Type);

        // Nested members of the collection are also reachable via the swagger graph.
        Assert.Contains("ScopeItems.Id", pathLookup.Keys);
    }

    [Fact]
    public void ParseSwagger_PathsCycleGuardStopsRunawayRecursion()
    {
        // Self-referencing schema; cycle guard caps recursion at 5 levels of the same type.
        const string swagger = """
        {
          "components": {
            "schemas": {
              "Node": {
                "type": "object",
                "properties": {
                  "Name": {"type":"string"},
                  "Child": {"$ref":"#/components/schemas/Node"}
                }
              }
            }
          },
          "paths": {
            "/v4/Tree": {
              "get": {
                "responses": {"200":{"content":{"application/json":{"schema":{"$ref":"#/components/schemas/Node"}}}}}
              }
            }
          }
        }
        """;
        var schemas = ParseSwaggerForTests(swagger);
        var paths = schemas["RESPONSE GET Core /v4/Tree"].Paths
            .Where(p => p.Path.EndsWith(".Name", StringComparison.Ordinal) || p.Path == "Name")
            .Select(p => p.Path)
            .ToArray();
        // Expect Name plus Child(.Child)*.Name capped at the 5-deep type-stack rule.
        Assert.True(paths.Length <= 6, $"Cycle guard failed: {paths.Length} Name paths emitted");
        Assert.Contains("Name", paths);
        Assert.Contains("Child.Name", paths);
    }
}
