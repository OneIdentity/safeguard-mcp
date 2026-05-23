// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

[McpServerToolType]
public partial class CoreTools(SafeguardAuth auth)
{
    private const string Service = "service/Core";

    private async Task<string> GetAsync(McpServer server, string path)
    {
        var host = await auth.EnsureAuthenticatedAsync(server, null, CancellationToken.None);
        return await auth.RequestAsync(host, HttpMethod.Get, auth.BuildUrl(host, Service, path));
    }

    private async Task<string> PostAsync(McpServer server, string path, string body = null)
    {
        var host = await auth.EnsureAuthenticatedAsync(server, null, CancellationToken.None);
        return await auth.RequestAsync(host, HttpMethod.Post, auth.BuildUrl(host, Service, path), body);
    }

    private async Task<string> PutAsync(McpServer server, string path, string body)
    {
        var host = await auth.EnsureAuthenticatedAsync(server, null, CancellationToken.None);
        return await auth.RequestAsync(host, HttpMethod.Put, auth.BuildUrl(host, Service, path), body);
    }

    private async Task<string> DeleteAsync(McpServer server, string path)
    {
        var host = await auth.EnsureAuthenticatedAsync(server, null, CancellationToken.None);
        return await auth.RequestAsync(host, HttpMethod.Delete, auth.BuildUrl(host, Service, path));
    }

    /// <summary>Builds a query string from name/value pairs, omitting null or empty values.</summary>
    private static string Q(params (string name, string value)[] ps)
    {
        var parts = new List<string>();
        foreach (var (name, value) in ps)
        {
            if (!string.IsNullOrEmpty(value))
                parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
        }
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }
}
