using System.Net;
using System.Security;
using System.Text;
using System.Text.Json;
using ModelContextProtocol;
using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.Tools;

/// <summary>
/// Internal helpers shared by the two <see cref="ISafeguardSession"/>
/// implementations. Kept small and stateless to keep the per-request
/// HTTP session footprint minimal.
/// </summary>
internal static class SessionHelpers
{
    /// <summary>
    /// Copies the characters of <paramref name="str"/> into a fresh
    /// read-only <see cref="SecureString"/>. The caller owns the
    /// returned instance and must dispose it.
    /// </summary>
    public static SecureString ToSecureString(string str)
    {
        var secure = new SecureString();
        if (str != null)
        {
            foreach (var c in str)
                secure.AppendChar(c);
        }
        secure.MakeReadOnly();
        return secure;
    }

    /// <summary>
    /// Implements <see cref="ISafeguardSession.GetPrincipalInfoAsync"/>
    /// by calling <c>GET /service/core/v4/Me</c> through the session
    /// and decoding (without verification) the JWT body for expiry
    /// display. Returns a populated <see cref="PrincipalInfo"/>.
    /// </summary>
    public static Task<PrincipalInfo> GetPrincipalInfoAsync(
        ISafeguardSession session,
        string applianceHost,
        CancellationToken cancellationToken)
    {
        return session.ExecuteWithConnectionAsync(
            connection => GetPrincipalInfoFromConnectionAsync(connection, applianceHost, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Connection-direct overload of
    /// <see cref="GetPrincipalInfoAsync(ISafeguardSession, string, CancellationToken)"/>
    /// used by the <c>safeguard-mcp login</c> subcommand, which has an
    /// <see cref="ISafeguardConnection"/> in hand rather than a session.
    /// </summary>
    public static async Task<PrincipalInfo> GetPrincipalInfoFromConnectionAsync(
        ISafeguardConnection connection,
        string applianceHost,
        CancellationToken cancellationToken)
    {
        var response = await Task.Run(() =>
            connection.InvokeMethodFull(Service.Core, Method.Get, "Me", null, null, null, null),
            cancellationToken);
        var info = ParseMeResponse(response.Body) ?? new PrincipalInfo();
        info.ApplianceHost = applianceHost;
        PopulateTokenExpiry(connection, info);
        return info;
    }

    private static PrincipalInfo ParseMeResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var info = new PrincipalInfo();
            if (doc.RootElement.TryGetProperty("DisplayName", out var displayElem) && displayElem.ValueKind == JsonValueKind.String)
                info.DisplayName = displayElem.GetString();
            if (doc.RootElement.TryGetProperty("Name", out var nameElem) && nameElem.ValueKind == JsonValueKind.String)
                info.Name = nameElem.GetString();
            if (doc.RootElement.TryGetProperty("IdentityProviderName", out var idpElem) && idpElem.ValueKind == JsonValueKind.String)
                info.IdentityProvider = idpElem.GetString();
            else if (doc.RootElement.TryGetProperty("PrimaryAuthenticationProviderName", out var authElem) && authElem.ValueKind == JsonValueKind.String)
                info.IdentityProvider = authElem.GetString();
            return info;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void PopulateTokenExpiry(ISafeguardConnection connection, PrincipalInfo info)
    {
        try
        {
            info.TokenLifetimeMinutes = connection.GetAccessTokenLifetimeRemaining();
        }
        catch
        {
            info.TokenLifetimeMinutes = -1;
        }

        try
        {
            // NOTE: ISafeguardConnection.GetAccessToken() returns the
            // SDK's internal bearer SecureString BY REFERENCE — disposing
            // it zeroes the SDK's own copy and the next SDK call throws
            // ObjectDisposedException. Read it without taking ownership.
            var token = connection.GetAccessToken();
            var raw = SecureStringExtensions.ReadInsecure(token);
            if (string.IsNullOrEmpty(raw))
                return;
            var exp = TryReadJwtExp(raw);
            if (exp.HasValue)
                info.TokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(exp.Value);
        }
        catch
        {
            // Display-only; never let JWT decoding fail an API call.
        }
    }

    /// <summary>
    /// Decodes the second segment of a JWT and returns the
    /// <c>exp</c> claim if present. No signature verification is
    /// performed — display use only.
    /// </summary>
    public static long? TryReadJwtExp(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return null;

        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            if (!doc.RootElement.TryGetProperty("exp", out var expElem))
                return null;
            return expElem.ValueKind switch
            {
                JsonValueKind.Number when expElem.TryGetInt64(out var n) => n,
                JsonValueKind.String when long.TryParse(expElem.GetString(), out var s) => s,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
