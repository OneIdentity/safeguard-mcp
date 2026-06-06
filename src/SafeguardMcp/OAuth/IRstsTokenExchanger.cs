using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace SafeguardMcp.OAuth;

/// <summary>
/// Thin seam around the two SafeguardDotNet static helpers the
/// bridge's <c>POST /token</c> handler calls. Exists so tests can
/// substitute a fake exchanger and exercise the handler without
/// reaching rSTS or the Safeguard appliance — there is intentionally
/// no other behavior on this interface.
///
/// <para>
/// Implementations <strong>must</strong> use the SDK helpers
/// (<c>Safeguard.AgentBasedLoginUtils.PostAuthorizationCodeFlowAsync</c>
/// and <c>PostLoginResponseAsync</c>) and <strong>must not</strong>
/// hand-roll the rSTS or LoginResponse wire formats — see
/// HTTP-AUTH-RELAY-PLAN-FACTS §SafeguardDotNet SDK and
/// HTTP-AUTH-RELAY-PROMPT §3 "Use SDK helpers, not hand-rolled HTTP."
/// </para>
/// </summary>
internal interface IRstsTokenExchanger
{
    /// <summary>
    /// Exchanges the rSTS authorization code for an rSTS access token.
    /// The returned <see cref="SecureString"/> is owned by the caller
    /// and must be disposed as soon as the LoginResponse exchange
    /// finishes (plan §2.3 — token material is a stack local in the
    /// <c>/token</c> handler).
    /// </summary>
    Task<SecureString> ExchangeAuthorizationCodeAsync(
        string appliance,
        string rstsAuthorizationCode,
        string pkceVerifier,
        string redirectUri,
        bool ignoreSsl,
        CancellationToken cancellationToken);

    /// <summary>
    /// Posts the rSTS access token to
    /// <c>/service/core/v4/Token/LoginResponse</c> and returns the
    /// raw JSON body (the SDK helper returns <c>string</c>). The
    /// caller parses with <see cref="System.Text.Json.JsonDocument"/>
    /// per the AOT-only JSON rule.
    /// </summary>
    Task<string> ExchangeRstsTokenForLoginResponseAsync(
        string appliance,
        SecureString rstsAccessToken,
        bool ignoreSsl,
        CancellationToken cancellationToken);
}
