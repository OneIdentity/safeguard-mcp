using System.Text.Json;

namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Shared helpers for integration tests that consume <c>Safeguard_Execute</c> responses.
///
/// As of the response-envelope cutover, <c>Safeguard_Execute</c> returns a structured
/// JSON envelope of the form
/// <code>{ "data": &lt;raw body&gt;, "meta": { "notices": [...], "paging": {...}?, "truncation": {...}? } }</code>
/// for JSON requests. Tests that want to assert on the API body itself need to peel
/// <c>data</c> off the envelope first.
/// </summary>
internal static class EnvelopeTestHelpers
{
    /// <summary>
    /// Returns the raw JSON of <c>data</c> from a Safeguard_Execute envelope. If the
    /// response is not an envelope (e.g. a plain CSV body or an older non-envelope
    /// shape), the input is returned unchanged so callers don't need to special-case
    /// transitional fixtures.
    /// </summary>
    internal static string UnwrapData(string response)
    {
        if (string.IsNullOrEmpty(response)) return response;

        try
        {
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("data", out var data)
                && doc.RootElement.TryGetProperty("meta", out _))
            {
                return data.ValueKind switch
                {
                    JsonValueKind.String => data.GetString() ?? string.Empty,
                    JsonValueKind.Null => "null",
                    _ => data.GetRawText(),
                };
            }
        }
        catch (JsonException)
        {
            // Fall through — non-JSON responses (e.g. CSV) are returned unchanged.
        }

        return response;
    }
}
