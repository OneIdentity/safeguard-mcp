using System.Collections.Concurrent;

namespace SafeguardMcp.OAuth;

/// <summary>
/// In-memory map of in-flight bridge↔rSTS authorization-code flows,
/// keyed by an opaque <c>bridge_session_id</c> the bridge mints at
/// <c>/authorize</c> and reads back at <c>/authorize/callback</c>.
///
/// <para>
/// Each entry caches:
/// <list type="bullet">
///   <item>The MCP client's PKCE challenge and redirect_uri/state so
///   <c>/token</c> can later verify the client's
///   <c>code_verifier</c> and the callback can hand control back to
///   the client's user-agent.</item>
///   <item>The bridge↔rSTS PKCE verifier the bridge generated for
///   the front-channel hop; required when <c>/token</c> later
///   exchanges the rSTS authorization code via
///   <c>Safeguard.PostAuthorizationCodeFlowAsync</c>.</item>
/// </list>
/// </para>
///
/// <para>
/// Entries auto-expire at <c>ExpiresAt</c>. The default lifetime is
/// rSTS's own 5-minute authorization-code TTL
/// (<see cref="BridgeOptions.MaxAuthCodeTtlSeconds"/>) — there is no
/// point holding flow state longer than the upstream code is valid.
/// </para>
///
/// <para>
/// Holds zero token material. Entries hold short-lived OAuth
/// machinery only; rSTS access tokens and Safeguard user tokens
/// never enter this store — the bridge's state-residency invariant.
/// </para>
/// </summary>
internal sealed class AuthorizeFlowStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly TimeProvider _time;

    public AuthorizeFlowStore() : this(TimeProvider.System) { }
    public AuthorizeFlowStore(TimeProvider time)
    {
        if (time == null) throw new ArgumentNullException(nameof(time));
        _time = time;
    }

    /// <summary>
    /// Inserts a new flow entry. <paramref name="bridgeSessionId"/>
    /// must be a freshly minted opaque token — collisions throw to
    /// surface a generator bug rather than silently overwrite.
    /// </summary>
    public void Add(string bridgeSessionId, Entry entry)
    {
        if (string.IsNullOrEmpty(bridgeSessionId)) throw new ArgumentException(nameof(bridgeSessionId));
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (!_entries.TryAdd(bridgeSessionId, entry))
            throw new InvalidOperationException("bridge_session_id collision.");
    }

    /// <summary>
    /// Single-use take. Returns and removes the entry if it exists
    /// and has not expired. Expired entries are dropped on read.
    /// </summary>
    public bool TryConsume(string bridgeSessionId, out Entry entry)
    {
        entry = null;
        if (string.IsNullOrEmpty(bridgeSessionId)) return false;
        if (!_entries.TryRemove(bridgeSessionId, out var found)) return false;
        if (found.ExpiresAt <= _time.GetUtcNow())
            return false;
        entry = found;
        return true;
    }

    /// <summary>Total live entries; intended for tests and diagnostics.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Immutable record of one in-flight authorization request. Constructed by
    /// <see cref="AuthorizeEndpoints"/>; consumed once by the callback handler.
    /// </summary>
    internal sealed class Entry
    {
        public string ClientId { get; }
        public string ClientRedirectUri { get; }
        public string ClientState { get; }
        public string ClientPkceChallenge { get; }
        public string BridgeToRstsPkceVerifier { get; }
        public DateTimeOffset ExpiresAt { get; }

        public Entry(
            string clientId,
            string clientRedirectUri,
            string clientState,
            string clientPkceChallenge,
            string bridgeToRstsPkceVerifier,
            DateTimeOffset expiresAt)
        {
            ClientId = clientId;
            ClientRedirectUri = clientRedirectUri;
            ClientState = clientState;
            ClientPkceChallenge = clientPkceChallenge;
            BridgeToRstsPkceVerifier = bridgeToRstsPkceVerifier;
            ExpiresAt = expiresAt;
        }
    }
}
