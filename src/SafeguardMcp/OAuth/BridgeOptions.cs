namespace SafeguardMcp.OAuth;

/// <summary>
/// Parsed OAuth metadata-bridge configuration. Pure value object —
/// no I/O.
///
/// <para>
/// Activation signal is <c>MCP_PUBLIC_URL</c>: when absent, the bridge
/// is inactive and the well-known endpoints are not mapped. When
/// present, <c>RSTS_CLIENT_ID</c> is also required (the bridge cannot
/// drive an rSTS auth-code flow without a pre-registered
/// <c>RelyingPartyApplication.Realm</c>).
/// </para>
/// </summary>
internal sealed class BridgeOptions
{
    public const string McpPublicUrlEnvVar = "MCP_PUBLIC_URL";
    public const string RstsClientIdEnvVar = "RSTS_CLIENT_ID";
    public const string AuthCodeTtlEnvVar = "BRIDGE_AUTH_CODE_TTL_SECONDS";
    public const string SafeguardHostEnvVar = "SAFEGUARD_HOST";
    public const string SafeguardIgnoreSslEnvVar = "SAFEGUARD_IGNORE_SSL";

    public const int DefaultAuthCodeTtlSeconds = 60;
    // rSTS's own auth-code TTL is 5 minutes; do not exceed it.
    public const int MaxAuthCodeTtlSeconds = 300;

    /// <summary>Externally-resolvable URL of the MCP server, no trailing slash.</summary>
    public string McpPublicUrl { get; }
    /// <summary>The <c>RelyingPartyApplication.Realm</c> registered in rSTS for this bridge.</summary>
    public string RstsClientId { get; }
    /// <summary>Authorization-code TTL, in seconds.</summary>
    public int AuthCodeTtlSeconds { get; }
    /// <summary>The Safeguard appliance host the bridge brokers tokens for.</summary>
    public string SafeguardHost { get; }
    /// <summary>Whether outbound TLS verification is suppressed (deployment-wide).</summary>
    public bool IgnoreSsl { get; }

    private BridgeOptions(string mcpPublicUrl, string rstsClientId, int authCodeTtlSeconds, string safeguardHost, bool ignoreSsl)
    {
        McpPublicUrl = mcpPublicUrl;
        RstsClientId = rstsClientId;
        AuthCodeTtlSeconds = authCodeTtlSeconds;
        SafeguardHost = safeguardHost;
        IgnoreSsl = ignoreSsl;
    }

    /// <summary>
    /// Returns a populated <see cref="BridgeOptions"/>, a structured
    /// "bridge is inactive" result, or a fail-fast error string.
    /// Caller (Program.cs) writes the error to stderr and exits with a
    /// non-zero status before <see cref="Microsoft.AspNetCore.Builder.WebApplication.RunAsync"/>.
    /// </summary>
    public static BridgeParseResult Parse(Func<string, string> getEnv)
    {
        if (getEnv == null) throw new ArgumentNullException(nameof(getEnv));

        var publicUrl = Trimmed(getEnv(McpPublicUrlEnvVar));
        var clientId = Trimmed(getEnv(RstsClientIdEnvVar));
        var ttlRaw = Trimmed(getEnv(AuthCodeTtlEnvVar));

        // No MCP_PUBLIC_URL → bridge inactive. Tolerate stray related
        // vars rather than fail-fast: an operator running pure-relay
        // shouldn't be blocked by leftover Phase-2 configuration.
        if (publicUrl == null)
            return BridgeParseResult.Inactive();

        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var publicUri)
            || (publicUri.Scheme != Uri.UriSchemeHttp && publicUri.Scheme != Uri.UriSchemeHttps))
        {
            return BridgeParseResult.ForError(
                $"{McpPublicUrlEnvVar} must be an absolute http(s) URI. "
                + "Set it to the externally-resolvable URL of the MCP server, e.g. https://mcp.example.com.");
        }

        if (clientId == null)
        {
            return BridgeParseResult.ForError(
                $"{RstsClientIdEnvVar} is required when {McpPublicUrlEnvVar} is set. "
                + "Set it to the Realm of the RelyingPartyApplication pre-registered in rSTS for this bridge.");
        }

        if (!Uri.TryCreate(clientId, UriKind.Absolute, out _))
        {
            return BridgeParseResult.ForError(
                $"{RstsClientIdEnvVar} must be an absolute URI — rSTS asserts "
                + "RelyingPartyApplication.Realm is an absolute URI in its constructor.");
        }

        int ttl = DefaultAuthCodeTtlSeconds;
        if (ttlRaw != null)
        {
            if (!int.TryParse(ttlRaw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out ttl)
                || ttl < 1 || ttl > MaxAuthCodeTtlSeconds)
            {
                return BridgeParseResult.ForError(
                    $"{AuthCodeTtlEnvVar} must be an integer between 1 and {MaxAuthCodeTtlSeconds}; "
                    + $"got '{ttlRaw}'. rSTS's own auth-code TTL is 5 minutes — the bridge must not exceed it.");
            }
        }

        var safeguardHost = Trimmed(getEnv(SafeguardHostEnvVar));
        // SAFEGUARD_HOST is already required by HttpModeStartup; in
        // bridge construction we treat its absence as a programming
        // error since BridgeOptions is parsed only after HTTP-mode
        // lockdowns pass. Defensive check still surfaces a useful
        // message if call order ever changes.
        if (safeguardHost == null)
        {
            return BridgeParseResult.ForError(
                $"{SafeguardHostEnvVar} must be set before {McpPublicUrlEnvVar} is parsed "
                + "(HTTP-mode startup lockdown should have caught this).");
        }

        var ignoreSsl = bool.TryParse(getEnv(SafeguardIgnoreSslEnvVar), out var v) && v;

        // Normalize trailing slash off MCP_PUBLIC_URL so derived URLs
        // never produce "//authorize".
        var normalized = publicUrl.TrimEnd('/');

        return BridgeParseResult.Success(new BridgeOptions(normalized, clientId, ttl, safeguardHost, ignoreSsl));
    }

    private static string Trimmed(string s)
    {
        if (s == null) return null;
        var t = s.Trim();
        return t.Length == 0 ? null : t;
    }

    public string AuthorizeEndpoint => McpPublicUrl + "/authorize";
    public string AuthorizeCallbackEndpoint => McpPublicUrl + "/authorize/callback";
    public string TokenEndpoint => McpPublicUrl + "/token";
    public string RegistrationEndpoint => McpPublicUrl + "/register";
    public string ProtectedResourceMetadataUrl => McpPublicUrl + "/.well-known/oauth-protected-resource";
    public string AuthorizationServerMetadataUrl => McpPublicUrl + "/.well-known/oauth-authorization-server";
}

/// <summary>
/// Discriminated return shape: bridge inactive (no env vars set),
/// bridge misconfigured (return error to caller), or bridge active
/// (return populated <see cref="BridgeOptions"/>).
/// </summary>
internal readonly struct BridgeParseResult
{
    public bool IsActive { get; }
    public BridgeOptions Options { get; }
    public string Error { get; }

    private BridgeParseResult(bool isActive, BridgeOptions options, string error)
    {
        IsActive = isActive;
        Options = options;
        Error = error;
    }

    public static BridgeParseResult Inactive() => new BridgeParseResult(false, null, null);
    public static BridgeParseResult ForError(string message) => new BridgeParseResult(false, null, message);
    public static BridgeParseResult Success(BridgeOptions options) => new BridgeParseResult(true, options, null);
}
