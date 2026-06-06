namespace SafeguardMcp;

/// <summary>
/// HTTP-mode startup lockdowns from HTTP-AUTH-RELAY-PLAN §1.8.
///
/// <list type="bullet">
///   <item><b>SAFEGUARD_HOST is required.</b> Pure-relay HTTP mode has a
///   single explicit trust boundary — the operator-configured appliance.
///   Eliciting or inferring it per request is not safe.</item>
///   <item><b>SAFEGUARD_PROVIDER / SAFEGUARD_USER / SAFEGUARD_PASSWORD
///   are forbidden.</b> Setting environment-PKCE credentials in a
///   multi-tenant HTTP server would impersonate one identity for every
///   caller (the original bug Phase 1 fixes). Fail fast at startup
///   rather than silently elevate privileges.</item>
/// </list>
///
/// The validator is pure (takes an environment lookup delegate) so it
/// can be exercised by unit tests without mutating process-wide state.
/// </summary>
internal static class HttpModeStartup
{
    internal static readonly string[] ForbiddenEnvVars =
    {
        "SAFEGUARD_PROVIDER",
        "SAFEGUARD_USER",
        "SAFEGUARD_PASSWORD",
    };

    /// <summary>
    /// Returns <c>null</c> when the environment is valid for HTTP mode,
    /// or a human-readable error message that the entry point should
    /// write to stderr before exiting with a non-zero status.
    /// </summary>
    public static string ValidateEnvironment(Func<string, string> getEnv)
    {
        if (getEnv == null) throw new ArgumentNullException(nameof(getEnv));

        if (string.IsNullOrWhiteSpace(getEnv("SAFEGUARD_HOST")))
        {
            return "SAFEGUARD_HOST is required in HTTP mode. "
                + "Set SAFEGUARD_HOST to the Safeguard appliance DNS name or IP "
                + "before starting `safeguard-mcp --http`.";
        }

        var present = new List<string>();
        foreach (var name in ForbiddenEnvVars)
        {
            if (!string.IsNullOrWhiteSpace(getEnv(name)))
                present.Add(name);
        }

        if (present.Count > 0)
        {
            return "HTTP mode forbids environment-PKCE credentials because they would "
                + "impersonate one identity for every HTTP caller. Unset the following "
                + "environment variable" + (present.Count == 1 ? "" : "s")
                + " before starting `safeguard-mcp --http`: "
                + string.Join(", ", present)
                + ". Acquire per-caller Safeguard user tokens via `safeguard-mcp login` "
                + "or your MCP client's OAuth flow and send them as `Authorization: Bearer`.";
        }

        return null;
    }

    public static string ValidateEnvironment()
        => ValidateEnvironment(Environment.GetEnvironmentVariable);
}
