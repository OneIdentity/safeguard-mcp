#nullable disable

using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Verifies the minimal lookup behavior of <see cref="ClientRegistry"/>
/// required by plan §2.2.c (the /authorize endpoint must reject
/// client_ids that did not complete /register, and must enforce
/// exact-match redirect_uri validation per RFC 6749 §3.1.2.3).
/// The DCR write path is task 2.2.f.
/// </summary>
public class ClientRegistryTests
{
    [Fact]
    public void IsValidRedirectUri_UnknownClient_ReturnsFalse()
    {
        var reg = new ClientRegistry();
        Assert.False(reg.IsValidRedirectUri("nope", "http://127.0.0.1/cb", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsValidRedirectUri_RegisteredClient_MatchingUri_ReturnsTrue()
    {
        var reg = new ClientRegistry();
        Assert.True(reg.TryAdd("c1", new[] { "http://127.0.0.1:8765/cb" }, DateTimeOffset.UtcNow.AddDays(30)));
        Assert.True(reg.IsValidRedirectUri("c1", "http://127.0.0.1:8765/cb", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsValidRedirectUri_RegisteredClient_NonMatchingUri_ReturnsFalse()
    {
        var reg = new ClientRegistry();
        reg.TryAdd("c1", new[] { "http://127.0.0.1:8765/cb" }, DateTimeOffset.UtcNow.AddDays(30));
        // Even a trailing slash differs — exact match per RFC 6749.
        Assert.False(reg.IsValidRedirectUri("c1", "http://127.0.0.1:8765/cb/", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsValidRedirectUri_ExpiredEntry_ReturnsFalseAndEvicts()
    {
        var reg = new ClientRegistry();
        var expiry = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        reg.TryAdd("c1", new[] { "http://127.0.0.1/cb" }, expiry);
        Assert.False(reg.IsValidRedirectUri("c1", "http://127.0.0.1/cb", expiry.AddSeconds(1)));
        Assert.Equal(0, reg.Count);
    }

    [Fact]
    public void TryAdd_Duplicate_ReturnsFalse()
    {
        var reg = new ClientRegistry();
        Assert.True(reg.TryAdd("c1", new[] { "http://127.0.0.1/cb" }, DateTimeOffset.UtcNow.AddDays(30)));
        Assert.False(reg.TryAdd("c1", new[] { "http://127.0.0.1/cb" }, DateTimeOffset.UtcNow.AddDays(30)));
    }
}
