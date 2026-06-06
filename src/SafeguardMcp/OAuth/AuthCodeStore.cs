using System.Collections.Concurrent;

namespace SafeguardMcp.OAuth;

/// <summary>
/// In-memory map of issued <c>bridge_auth_code</c> values awaiting
/// redemption at <c>POST /token</c>. Populated by the
/// <c>/authorize/callback</c> handler (plan §2.2.d) and consumed by
/// the <c>/token</c> handler (plan §2.2.e, task 2.2.e).
///
/// <para>
/// Each entry holds the upstream rSTS authorization code (the
/// single-use credential the bridge will hand to
/// <c>Safeguard.PostAuthorizationCodeFlowAsync</c>), the
/// bridge↔rSTS PKCE verifier needed for that exchange, and the
/// client↔bridge PKCE challenge plus redirect_uri that the
/// client's <c>code_verifier</c> must match.
/// </para>
///
/// <para>
/// TTL is <see cref="BridgeOptions.AuthCodeTtlSeconds"/> (≤60s,
/// derived from <c>BRIDGE_AUTH_CODE_TTL_SECONDS</c>, bounded by
/// rSTS's own 5-minute upstream limit). The entry must be deleted
/// from this store before any upstream call (plan §2.2 step 2) so
/// a slow rSTS exchange cannot be replayed.
/// </para>
///
/// <para>
/// Holds zero Safeguard token material. The rSTS authorization code
/// is a credential, not a token; it expires server-side at rSTS in
/// ≤5 minutes regardless of how long it lingers here.
/// </para>
/// </summary>
internal sealed class AuthCodeStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly TimeProvider _time;

    public AuthCodeStore() : this(TimeProvider.System) { }
    public AuthCodeStore(TimeProvider time)
    {
        if (time == null) throw new ArgumentNullException(nameof(time));
        _time = time;
    }

    /// <summary>
    /// Inserts a new auth-code entry. <paramref name="bridgeAuthCode"/>
    /// must be a freshly minted opaque token — collisions throw to
    /// surface a generator bug rather than silently overwrite.
    /// </summary>
    public void Add(string bridgeAuthCode, Entry entry)
    {
        if (string.IsNullOrEmpty(bridgeAuthCode)) throw new ArgumentException(nameof(bridgeAuthCode));
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (!_entries.TryAdd(bridgeAuthCode, entry))
            throw new InvalidOperationException("bridge_auth_code collision.");
    }

    /// <summary>
    /// Single-use take. Returns and removes the entry if it exists
    /// and has not expired. Expired entries are dropped on read so a
    /// later replay cannot succeed even before the periodic sweep.
    /// </summary>
    public bool TryConsume(string bridgeAuthCode, out Entry entry)
    {
        entry = null;
        if (string.IsNullOrEmpty(bridgeAuthCode)) return false;
        if (!_entries.TryRemove(bridgeAuthCode, out var found)) return false;
        if (found.ExpiresAt <= _time.GetUtcNow())
            return false;
        entry = found;
        return true;
    }

    /// <summary>Total live entries; intended for tests and diagnostics.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Immutable record minted at the bridge's <c>/authorize/callback</c>
    /// handler and consumed by <c>/token</c>.
    /// </summary>
    internal sealed class Entry
    {
        public string RstsAuthCode { get; }
        public string BridgeToRstsPkceVerifier { get; }
        public string ClientPkceChallenge { get; }
        public string ClientRedirectUri { get; }
        public string ClientId { get; }
        public DateTimeOffset ExpiresAt { get; }

        public Entry(
            string rstsAuthCode,
            string bridgeToRstsPkceVerifier,
            string clientPkceChallenge,
            string clientRedirectUri,
            string clientId,
            DateTimeOffset expiresAt)
        {
            RstsAuthCode = rstsAuthCode;
            BridgeToRstsPkceVerifier = bridgeToRstsPkceVerifier;
            ClientPkceChallenge = clientPkceChallenge;
            ClientRedirectUri = clientRedirectUri;
            ClientId = clientId;
            ExpiresAt = expiresAt;
        }
    }
}
