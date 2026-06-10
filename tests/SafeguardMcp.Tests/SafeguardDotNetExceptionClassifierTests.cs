#nullable disable

using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Login;

namespace SafeguardMcp.Tests;

/// <summary>
/// Pins the SafeguardDotNetExceptionClassifier's behavior so that the
/// rSTS "OAuth2 device code grant type is not allowed." literal
/// (defined verbatim in rSTS HttpService/I18nUser.resx and thrown by
/// OAuthTokenService.cs at two sites when the appliance's
/// AllowedOAuth2GrantTypes cluster setting omits DeviceCode) is
/// surfaced as an actionable, configuration-aware diagnostic rather
/// than as the generic "Authentication failed -> set SAFEGUARD_*
/// env vars" wording.
/// </summary>
public class SafeguardDotNetExceptionClassifierTests
{
    private const string Host = "safeguard.example.test";

    [Fact]
    public void TryGetSpecificMessage_RstsDeviceCodeDisabledLiteral_ReturnsSpecificMessage()
    {
        var ex = new SafeguardDotNetException(
            "Error returned from Safeguard rSTS, Error: invalid_request "
            + "{\"error\":\"invalid_request\",\"error_description\":\"OAuth2 device code grant type is not allowed.\"}");

        var matched = SafeguardDotNetExceptionClassifier.TryGetSpecificMessage(ex, Host, out var message);

        Assert.True(matched);
        Assert.Contains("Device-code login is disabled", message);
        Assert.Contains(Host, message);
        Assert.Contains("Allowed OAuth2 Grant Types", message);
        Assert.Contains(
            "Appliance Management -> Safeguard Access -> Local Login Control",
            message);
        Assert.Contains(
            "PUT /service/appliance/v3/ClusterConfig/AllowedOAuth2GrantTypes",
            message);
        Assert.Contains("AuthorizationCode,DeviceCode", message);
    }

    [Fact]
    public void TryGetSpecificMessage_CaseInsensitiveMatch_StillReturnsSpecificMessage()
    {
        var ex = new SafeguardDotNetException(
            "OAUTH2 DEVICE CODE GRANT TYPE IS NOT ALLOWED.");

        var matched = SafeguardDotNetExceptionClassifier.TryGetSpecificMessage(ex, Host, out var message);

        Assert.True(matched);
        Assert.Contains("Allowed OAuth2 Grant Types", message);
    }

    [Fact]
    public void TryGetSpecificMessage_PkceFallbackMentionedAsSecondary_NotPrimary()
    {
        var ex = new SafeguardDotNetException("OAuth2 device code grant type is not allowed.");

        Assert.True(SafeguardDotNetExceptionClassifier.TryGetSpecificMessage(ex, Host, out var message));

        var pkceIndex = message.IndexOf("PKCE", StringComparison.Ordinal);
        var settingIndex = message.IndexOf("Allowed OAuth2 Grant Types", StringComparison.Ordinal);
        Assert.True(settingIndex >= 0, "appliance setting must be named");
        Assert.True(pkceIndex >= 0, "PKCE workaround must still be mentioned");
        Assert.True(
            settingIndex < pkceIndex,
            "appliance setting must be presented before the PKCE workaround");
        Assert.Contains("workaround", message);
    }

    [Fact]
    public void TryGetSpecificMessage_GenericSafeguardDotNetException_ReturnsFalse()
    {
        var ex = new SafeguardDotNetException("Some other unrelated SDK failure");

        var matched = SafeguardDotNetExceptionClassifier.TryGetSpecificMessage(ex, Host, out var message);

        Assert.False(matched);
        Assert.Null(message);
    }

    [Fact]
    public void TryGetSpecificMessage_NullException_ReturnsFalse()
    {
        var matched = SafeguardDotNetExceptionClassifier.TryGetSpecificMessage(null, Host, out var message);

        Assert.False(matched);
        Assert.Null(message);
    }

    [Fact]
    public void TryGetSpecificMessage_NoHost_StillProducesUsableMessage()
    {
        var ex = new SafeguardDotNetException("OAuth2 device code grant type is not allowed.");

        Assert.True(SafeguardDotNetExceptionClassifier.TryGetSpecificMessage(ex, host: null, out var message));
        Assert.Contains("the appliance", message);
    }
}
