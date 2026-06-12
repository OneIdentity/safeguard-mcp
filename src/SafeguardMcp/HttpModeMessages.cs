namespace SafeguardMcp;

/// <summary>
/// Canonical user-facing wording for HTTP-mode Safeguard auth-failure
/// paths. Centralized so the documented authentication hierarchy from
/// <c>README.md</c> ("MCP client OAuth flow primary; <c>safeguard-mcp
/// login</c> fallback for clients without OAuth discovery and for
/// scripts/CI") cannot drift out of sync at individual throw sites.
///
/// All five HTTP-mode auth-failure messages must lead with the MCP
/// client's OAuth flow and reference <c>safeguard-mcp login</c> only as
/// a fallback. Tests pin both the primary phrase and the ordering.
/// </summary>
internal static class HttpModeMessages
{
    public const string NotAuthenticated =
        "Not authenticated against Safeguard. Acquire a Safeguard user token "
        + "via your MCP client's OAuth flow (the client performs OAuth discovery "
        + "against this server's resource metadata and drives device-code/PKCE "
        + "itself), or — for clients that don't implement OAuth discovery and "
        + "for scripts/CI — run `safeguard-mcp login` and forward the token as "
        + "`Authorization: Bearer <token>`.";

    public const string TokenExpired =
        "Safeguard token has expired or been revoked. Re-acquire it via your "
        + "MCP client's OAuth flow (preferred) or `safeguard-mcp login` "
        + "(fallback for clients without OAuth discovery), then retry.";

    public const string EnvForbiddenSuffix =
        "In HTTP mode each caller supplies their own bearer — the MCP client "
        + "drives OAuth discovery itself, or scripts/CI can use "
        + "`safeguard-mcp login` as a fallback.";
}
