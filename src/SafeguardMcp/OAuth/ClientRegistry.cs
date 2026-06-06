using System.Collections.Concurrent;

namespace SafeguardMcp.OAuth;

/// <summary>
/// Per-process registry of MCP clients that completed Dynamic Client
/// Registration (RFC 7591) against <c>POST /register</c>. The full
/// registration handler is task 2.2.f; this type ships now so
/// <see cref="AuthorizeEndpoints"/> can enforce the "client_id matches
/// a /register record" check in plan §2.2.c.
///
/// <para>
/// Storage is in-memory only. Bridge restart loses the registry;
/// clients that see a 401 from <c>/authorize</c> are expected to
/// re-register transparently (plan §2.2). The registry holds no
/// secrets — DCR clients are public PKCE-only clients per
/// plan §2.2.f.
/// </para>
/// </summary>
internal sealed class ClientRegistry
{
    private readonly ConcurrentDictionary<string, RegisteredClient> _clients =
        new(StringComparer.Ordinal);

    /// <summary>Records a registered client. Returns false if the id already exists.</summary>
    public bool TryAdd(string clientId, IReadOnlyList<string> redirectUris, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrEmpty(clientId)) throw new ArgumentException("clientId required", nameof(clientId));
        if (redirectUris == null) throw new ArgumentNullException(nameof(redirectUris));
        return _clients.TryAdd(clientId, new RegisteredClient(redirectUris, expiresAt));
    }

    /// <summary>
    /// Looks up a non-expired client by id and confirms the supplied
    /// <paramref name="redirectUri"/> matches one of its registered
    /// redirect URIs by exact (case-sensitive) string comparison —
    /// the same rule RFC 6749 §3.1.2.3 requires.
    /// </summary>
    public bool IsValidRedirectUri(string clientId, string redirectUri, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            return false;
        if (!_clients.TryGetValue(clientId, out var entry))
            return false;
        if (entry.ExpiresAt <= now)
        {
            _clients.TryRemove(clientId, out _);
            return false;
        }
        foreach (var registered in entry.RedirectUris)
        {
            if (string.Equals(registered, redirectUri, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <summary>Total live entries; intended for tests and diagnostics.</summary>
    public int Count => _clients.Count;

    private sealed class RegisteredClient
    {
        public IReadOnlyList<string> RedirectUris { get; }
        public DateTimeOffset ExpiresAt { get; }

        public RegisteredClient(IReadOnlyList<string> redirectUris, DateTimeOffset expiresAt)
        {
            RedirectUris = redirectUris;
            ExpiresAt = expiresAt;
        }
    }
}
