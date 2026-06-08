using System.Buffers;
using System.Text;
using System.Text.Json;

namespace SafeguardMcp.OAuth;

/// <summary>
/// AOT-safe builders for the bridge's RFC 9728 (protected-resource)
/// and RFC 8414 (authorization-server) metadata documents. Uses
/// <see cref="Utf8JsonWriter"/> directly — no
/// <c>JsonSerializer.Serialize</c>, no <c>JsonNode</c>, per the
/// project's JSON-handling constraints.
/// </summary>
internal static class WellKnownMetadata
{
    public const string ProtectedResourcePath = "/.well-known/oauth-protected-resource";
    public const string AuthorizationServerPath = "/.well-known/oauth-authorization-server";

    private static readonly JsonWriterOptions WriterOptions = new() { Indented = true };

    /// <summary>
    /// RFC 9728 protected-resource metadata. Names the bridge's
    /// public URL as the protected resource and the bridge itself
    /// as its authorization server.
    /// </summary>
    public static string BuildProtectedResourceJson(BridgeRequestUrls urls)
    {
        if (urls == null) throw new ArgumentNullException(nameof(urls));

        return WriteJson(w =>
        {
            w.WriteStartObject();
            w.WriteString("resource", urls.PublicUrl);

            w.WriteStartArray("authorization_servers");
            w.WriteStringValue(urls.PublicUrl);
            w.WriteEndArray();

            w.WriteStartArray("bearer_methods_supported");
            w.WriteStringValue("header");
            w.WriteEndArray();

            // RFC 9728 §2: resource servers MAY publish the JWT alg
            // values they accept. Safeguard's appliance accepts RS256
            // (rSTS-shared cert) per PangaeaAppliance
            // TokenAuthenticationProvider; we advertise the same so
            // sophisticated clients can validate before relay.
            w.WriteStartArray("resource_signing_alg_values_supported");
            w.WriteStringValue("RS256");
            w.WriteEndArray();

            w.WriteEndObject();
        });
    }

    /// <summary>
    /// RFC 8414 authorization-server metadata for the bridge.
    /// </summary>
    public static string BuildAuthorizationServerJson(BridgeRequestUrls urls)
    {
        if (urls == null) throw new ArgumentNullException(nameof(urls));

        return WriteJson(w =>
        {
            w.WriteStartObject();
            w.WriteString("issuer", urls.PublicUrl);
            w.WriteString("authorization_endpoint", urls.AuthorizeEndpoint);
            w.WriteString("token_endpoint", urls.TokenEndpoint);
            w.WriteString("registration_endpoint", urls.RegistrationEndpoint);

            w.WriteStartArray("response_types_supported");
            w.WriteStringValue("code");
            w.WriteEndArray();

            w.WriteStartArray("grant_types_supported");
            w.WriteStringValue("authorization_code");
            w.WriteEndArray();

            w.WriteStartArray("code_challenge_methods_supported");
            w.WriteStringValue("S256");
            w.WriteEndArray();

            // RFC 8414 §2: "none" advertises a public client model —
            // PKCE on the client↔bridge leg is the binding, no
            // client_secret is ever issued.
            w.WriteStartArray("token_endpoint_auth_methods_supported");
            w.WriteStringValue("none");
            w.WriteEndArray();

            w.WriteEndObject();
        });
    }

    private static string WriteJson(Action<Utf8JsonWriter> body)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer, WriterOptions))
            body(w);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
