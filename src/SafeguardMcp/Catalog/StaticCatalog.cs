using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tools;

/// <summary>
/// Provides a pre-compiled endpoint index for discovery before connecting to an appliance.
/// Loaded once from an embedded text resource. Schemas, params, and body info come from
/// the live swagger after connection.
/// </summary>
public static class SafeguardCatalog
{
    private static readonly Lazy<ApiEndpoint[]> _endpoints = new(LoadFromResource);

    public static ApiEndpoint[] Endpoints => _endpoints.Value;

    private static ApiEndpoint[] LoadFromResource()
    {
        using var stream = typeof(SafeguardCatalog).Assembly
            .GetManifestResourceStream("SafeguardMcp.Catalog.StaticCatalog.txt");

        if (stream is null)
            return Array.Empty<ApiEndpoint>();

        using var reader = new StreamReader(stream);
        var endpoints = new List<ApiEndpoint>(1100);

        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split('|', 4);
            if (parts.Length == 4)
            {
                endpoints.Add(new ApiEndpoint(
                    Service: parts[0],
                    Method: parts[1],
                    Path: parts[2],
                    Summary: parts[3],
                    Params: "",
                    HasBody: false,
                    ParamInfos: Array.Empty<ParamInfo>()));
            }
        }

        return endpoints.ToArray();
    }
}
