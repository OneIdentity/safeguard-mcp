using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.Login;

/// <summary>
/// Classifies <see cref="SafeguardDotNetException"/> instances thrown
/// during the device-code authentication dance so that recognizable
/// upstream conditions can be surfaced as actionable guidance instead
/// of opaque wrappers around appliance prose.
///
/// <para>Currently detects the rSTS "OAuth2 device code grant type is
/// not allowed." condition. The literal originates in
/// <c>Rsts.I18nUser.OAuth2DeviceCodeNotAllowed</c> (rSTS
/// <c>HttpService/I18nUser.resx</c>) and is thrown by
/// <c>OAuthTokenService.cs</c> (two sites) as an
/// <c>InvalidRequestException</c> when the appliance's
/// <c>AllowedOAuth2GrantTypes</c> cluster setting omits
/// <c>DeviceCode</c>. Matching is a case-insensitive substring search
/// on the SDK-wrapped message so it tolerates future SDK reformatting
/// and any localized prefix.</para>
/// </summary>
internal static class SafeguardDotNetExceptionClassifier
{
    private const string DeviceCodeDisabledNeedle = "device code grant type is not allowed";

    /// <summary>
    /// If the exception indicates that the appliance has the
    /// Device Authorization Grant disabled at the rSTS layer, returns
    /// a user-facing message that names the upstream setting, the
    /// Web UI path, and the admin API for re-enabling it, and
    /// mentions the in-session PKCE workaround as a secondary option.
    /// Returns <c>false</c> for any other SafeguardDotNet condition so
    /// callers fall back to their existing generic wording.
    /// </summary>
    public static bool TryGetSpecificMessage(
        SafeguardDotNetException ex,
        string host,
        out string message)
    {
        if (ex != null && IsDeviceCodeDisabled(ex.Message))
        {
            message = BuildDeviceCodeDisabledMessage(host);
            return true;
        }

        message = null;
        return false;
    }

    public static bool IsDeviceCodeDisabled(string exceptionMessage)
    {
        return !string.IsNullOrEmpty(exceptionMessage)
            && exceptionMessage.IndexOf(
                DeviceCodeDisabledNeedle,
                StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string BuildDeviceCodeDisabledMessage(string host)
    {
        var target = string.IsNullOrWhiteSpace(host) ? "the appliance" : $"'{host}'";
        return
            $"Device-code login is disabled on {target}: the appliance's "
            + "\"Allowed OAuth2 Grant Types\" setting does not include "
            + "DeviceCode, so rSTS refused the request (\"OAuth2 device "
            + "code grant type is not allowed.\"). "
            + "Ask a Safeguard administrator to enable DeviceCode under "
            + "Appliance Management -> Safeguard Access -> Local Login "
            + "Control in the Web UI, or via the admin API: "
            + "PUT /service/appliance/v3/ClusterConfig/AllowedOAuth2GrantTypes "
            + "with a bare JSON string body listing the desired grants "
            + "(for example \"AuthorizationCode,DeviceCode\"). "
            + "As an in-session workaround you may set SAFEGUARD_PROVIDER, "
            + "SAFEGUARD_USER, and SAFEGUARD_PASSWORD to use the PKCE "
            + "fallback, but the appliance setting is the proper fix.";
    }
}
