namespace SafeguardMcp.OAuth;

/// <summary>
/// Parsed OAuth metadata-bridge configuration. Pure value object —
/// no I/O.
///
/// <para>
/// In HTTP mode the bridge is on by default. The only opt-out is
/// <c>BRIDGE_DISABLED=true</c>, intended for operators who front
/// safeguard-mcp with a separate OAuth gateway and want the relay
/// only.
/// </para>
///
/// <para>
/// <see cref="OverridePublicUrl"/> and <see cref="OverrideClientId"/>
/// are <em>optional</em> pinning overrides. When unset (the common
/// case), the bridge infers its public URL from each incoming
/// request's <c>Scheme</c> + <c>Host</c> + <c>PathBase</c> via
/// <see cref="BridgeUrlResolver"/>, and reuses that inferred URL as
/// the rSTS <c>RelyingPartyApplication.Realm</c> (i.e. the
/// <c>client_id</c> sent on the bridge↔rSTS hop). Operators only need
/// to pin these values when the rSTS Realm was pre-registered under a
/// different name from the user-facing hostname.
/// </para>
/// </summary>
internal sealed class BridgeOptions
{
    public const string McpPublicUrlEnvVar = "MCP_PUBLIC_URL";
    public const string RstsClientIdEnvVar = "RSTS_CLIENT_ID";
    public const string AuthCodeTtlEnvVar = "BRIDGE_AUTH_CODE_TTL_SECONDS";
    public const string SafeguardHostEnvVar = "SAFEGUARD_HOST";
    public const string SafeguardIgnoreSslEnvVar = "SAFEGUARD_IGNORE_SSL";
    public const string BridgeDisabledEnvVar = "BRIDGE_DISABLED";

    public const int DefaultAuthCodeTtlSeconds = 60;
    // rSTS's own auth-code TTL is 5 minutes; do not exceed it.
    public const int MaxAuthCodeTtlSeconds = 300;

    /// <summary>
    /// Optional pinned public URL for the bridge (no trailing slash).
    /// When null/empty, the bridge infers its public URL per-request
    /// via <see cref="BridgeUrlResolver"/>.
    /// </summary>
    public string OverridePublicUrl { get; }

    /// <summary>
    /// Optional pinned <c>RelyingPartyApplication.Realm</c> for the
    /// bridge↔rSTS hop. When null/empty, the resolver defaults the
    /// <c>client_id</c> to whatever public URL the request resolves to.
    /// </summary>
    public string OverrideClientId { get; }

    /// <summary>Authorization-code TTL, in seconds.</summary>
    public int AuthCodeTtlSeconds { get; }

    /// <summary>The Safeguard appliance host the bridge brokers tokens for.</summary>
    public string SafeguardHost { get; }

    /// <summary>Whether outbound TLS verification is suppressed (deployment-wide).</summary>
    public bool IgnoreSsl { get; }

    private BridgeOptions(string overridePublicUrl, string overrideClientId, int authCodeTtlSeconds, string safeguardHost, bool ignoreSsl)
    {
        OverridePublicUrl = overridePublicUrl;
        OverrideClientId = overrideClientId;
        AuthCodeTtlSeconds = authCodeTtlSeconds;
        SafeguardHost = safeguardHost;
        IgnoreSsl = ignoreSsl;
    }

    /// <summary>
    /// Returns a populated <see cref="BridgeOptions"/>, a structured
    /// "bridge is inactive" result (only when
    /// <c>BRIDGE_DISABLED=true</c>), or a fail-fast error string when
    /// any provided override is malformed. Caller (Program.cs) writes
    /// the error to stderr and exits with a non-zero status before
    /// <see cref="Microsoft.AspNetCore.Builder.WebApplication.RunAsync"/>.
    /// </summary>
    public static BridgeParseResult Parse(Func<string, string> getEnv)
    {
        if (getEnv == null) throw new ArgumentNullException(nameof(getEnv));

        var disabledRaw = Trimmed(getEnv(BridgeDisabledEnvVar));
        if (disabledRaw != null
            && bool.TryParse(disabledRaw, out var disabled)
            && disabled)
        {
            return BridgeParseResult.Inactive();
        }

        var publicUrl = Trimmed(getEnv(McpPublicUrlEnvVar));
        var clientId = Trimmed(getEnv(RstsClientIdEnvVar));
        var ttlRaw = Trimmed(getEnv(AuthCodeTtlEnvVar));

        if (publicUrl != null)
        {
            if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var publicUri)
                || (publicUri.Scheme != Uri.UriSchemeHttp && publicUri.Scheme != Uri.UriSchemeHttps))
            {
                return BridgeParseResult.ForError(
                    $"{McpPublicUrlEnvVar} must be an absolute http(s) URI when set. "
                    + "Leave it unset to let the bridge infer its public URL from each incoming request, "
                    + "or set it to the externally-resolvable URL of the MCP server, e.g. https://mcp.example.com.");
            }
            // Normalize trailing slash off the override so derived
            // URLs never produce "//authorize".
            publicUrl = publicUrl.TrimEnd('/');
        }

        if (clientId != null)
        {
            if (!Uri.TryCreate(clientId, UriKind.Absolute, out _))
            {
                return BridgeParseResult.ForError(
                    $"{RstsClientIdEnvVar} must be an absolute URI when set — rSTS asserts "
                    + "RelyingPartyApplication.Realm is an absolute URI in its constructor.");
            }
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
                $"{SafeguardHostEnvVar} must be set before bridge options are parsed "
                + "(HTTP-mode startup lockdown should have caught this).");
        }

        var ignoreSsl = bool.TryParse(getEnv(SafeguardIgnoreSslEnvVar), out var v) && v;

        return BridgeParseResult.Success(new BridgeOptions(publicUrl, clientId, ttl, safeguardHost, ignoreSsl));
    }

    private static string Trimmed(string s)
    {
        if (s == null) return null;
        var t = s.Trim();
        return t.Length == 0 ? null : t;
    }
}

/// <summary>
/// Discriminated return shape: bridge inactive (BRIDGE_DISABLED=true),
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
