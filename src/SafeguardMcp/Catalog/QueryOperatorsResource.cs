using System.Text.Json;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Authoritative Safeguard query operator vocabulary, materialised from the embedded
/// <c>query-operators.json</c> resource. The JSON itself mirrors the appliance's
/// <c>ApiDoc.xml</c> filter-operator list and the binary-operator regex in
/// <c>Data/Middleware/Query/QueryFilterUtils.cs</c>; this class is the in-process
/// loader that other catalog/tool code uses without re-parsing prose.
/// </summary>
internal static class QueryOperatorsResource
{
    private static readonly string RawJson = EmbeddedResources.Load("query-operators.json");

    public static string GetJson() => RawJson;

    public static JsonDocument Parse() => JsonDocument.Parse(RawJson);
}
