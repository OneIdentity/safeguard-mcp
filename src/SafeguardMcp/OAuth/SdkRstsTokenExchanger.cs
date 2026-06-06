using System.Security;
using System.Threading;
using System.Threading.Tasks;
using OneIdentity.SafeguardDotNet;

namespace SafeguardMcp.OAuth;

/// <summary>
/// Production <see cref="IRstsTokenExchanger"/> backed by the
/// SafeguardDotNet SDK statics
/// (<c>Safeguard.AgentBasedLoginUtils.PostAuthorizationCodeFlowAsync</c>
/// and <c>PostLoginResponseAsync</c>) — the two helpers FACTS
/// §SafeguardDotNet SDK identifies as the supported AOT-safe seam
/// for the bridge's <c>/token</c> Stage-1 + Stage-2 exchange.
///
/// <para>
/// No state, no fields holding token material — both helpers return
/// their results to stack locals in the caller (<see cref="TokenEndpoint"/>),
/// preserving the plan §2.3 "no Safeguard access tokens stored, on
/// any code path, at any time" invariant.
/// </para>
/// </summary>
internal sealed class SdkRstsTokenExchanger : IRstsTokenExchanger
{
    private const int LoginResponseApiVersion = 4;

    public Task<SecureString> ExchangeAuthorizationCodeAsync(
        string appliance,
        string rstsAuthorizationCode,
        string pkceVerifier,
        string redirectUri,
        bool ignoreSsl,
        CancellationToken cancellationToken)
    {
        return Safeguard.AgentBasedLoginUtils.PostAuthorizationCodeFlowAsync(
            appliance,
            rstsAuthorizationCode,
            pkceVerifier,
            redirectUri,
            ignoreSsl,
            cancellationToken);
    }

    public Task<string> ExchangeRstsTokenForLoginResponseAsync(
        string appliance,
        SecureString rstsAccessToken,
        bool ignoreSsl,
        CancellationToken cancellationToken)
    {
        return Safeguard.AgentBasedLoginUtils.PostLoginResponseAsync(
            appliance,
            rstsAccessToken,
            LoginResponseApiVersion,
            ignoreSsl,
            cancellationToken);
    }
}
