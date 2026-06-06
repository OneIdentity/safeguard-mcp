#nullable disable

using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

internal sealed class FakeTime : TimeProvider
{
    private DateTimeOffset _now;
    public FakeTime() : this(new DateTimeOffset(2026, 6, 5, 0, 0, 0, TimeSpan.Zero)) { }
    public FakeTime(DateTimeOffset start) { _now = start; }
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}

/// <summary>
/// Verifies the in-memory state stores backing the bridge's OAuth
/// flow:
/// <list type="bullet">
///   <item>Entries are single-use — a second consume returns false.</item>
///   <item>Expired entries are dropped on read so a slow caller
///   cannot redeem stale state.</item>
///   <item>Duplicate inserts throw — surfaces a generator bug
///   instead of silently overwriting a live entry.</item>
/// </list>
/// </summary>
public class FlowAndAuthCodeStoreTests
{
    [Fact]
    public void AuthorizeFlowStore_AddThenConsume_Succeeds()
    {
        var time = new FakeTime();
        var store = new AuthorizeFlowStore(time);
        var entry = NewFlow(time.GetUtcNow().AddSeconds(60));

        store.Add("sid-1", entry);
        Assert.Equal(1, store.Count);

        Assert.True(store.TryConsume("sid-1", out var got));
        Assert.Same(entry, got);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void AuthorizeFlowStore_DoubleConsume_FailsSecondTime()
    {
        var time = new FakeTime();
        var store = new AuthorizeFlowStore(time);
        store.Add("sid", NewFlow(time.GetUtcNow().AddSeconds(60)));

        Assert.True(store.TryConsume("sid", out _));
        Assert.False(store.TryConsume("sid", out _));
    }

    [Fact]
    public void AuthorizeFlowStore_ExpiredEntry_ConsumeReturnsFalse()
    {
        var time = new FakeTime();
        var store = new AuthorizeFlowStore(time);
        store.Add("sid", NewFlow(time.GetUtcNow().AddSeconds(60)));

        time.Advance(TimeSpan.FromSeconds(120));
        Assert.False(store.TryConsume("sid", out _));
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void AuthorizeFlowStore_DuplicateAdd_Throws()
    {
        var time = new FakeTime();
        var store = new AuthorizeFlowStore(time);
        store.Add("sid", NewFlow(time.GetUtcNow().AddSeconds(60)));
        Assert.Throws<InvalidOperationException>(() =>
            store.Add("sid", NewFlow(time.GetUtcNow().AddSeconds(60))));
    }

    [Fact]
    public void AuthCodeStore_AddThenConsume_Succeeds()
    {
        var time = new FakeTime();
        var store = new AuthCodeStore(time);
        var entry = NewAuthCode(time.GetUtcNow().AddSeconds(60));

        store.Add("code-1", entry);
        Assert.True(store.TryConsume("code-1", out var got));
        Assert.Same(entry, got);
    }

    [Fact]
    public void AuthCodeStore_Replay_FailsSecondTime()
    {
        // The entry is deleted before any upstream call; a later
        // replay of the same bridge_auth_code must return false.
        var time = new FakeTime();
        var store = new AuthCodeStore(time);
        store.Add("code-1", NewAuthCode(time.GetUtcNow().AddSeconds(60)));

        Assert.True(store.TryConsume("code-1", out _));
        Assert.False(store.TryConsume("code-1", out _));
    }

    [Fact]
    public void AuthCodeStore_ExpiredEntry_ConsumeReturnsFalse()
    {
        var time = new FakeTime();
        var store = new AuthCodeStore(time);
        store.Add("code-1", NewAuthCode(time.GetUtcNow().AddSeconds(30)));

        time.Advance(TimeSpan.FromSeconds(60));
        Assert.False(store.TryConsume("code-1", out _));
    }

    private static AuthorizeFlowStore.Entry NewFlow(DateTimeOffset exp) =>
        new AuthorizeFlowStore.Entry(
            clientId: "mcp-client-1",
            clientRedirectUri: "http://127.0.0.1:8765/cb",
            clientState: "client-state",
            clientPkceChallenge: "challenge",
            bridgeToRstsPkceVerifier: "verifier",
            expiresAt: exp);

    private static AuthCodeStore.Entry NewAuthCode(DateTimeOffset exp) =>
        new AuthCodeStore.Entry(
            rstsAuthCode: "rsts-auth-code",
            bridgeToRstsPkceVerifier: "verifier",
            clientPkceChallenge: "challenge",
            clientRedirectUri: "http://127.0.0.1:8765/cb",
            clientId: "mcp-client-1",
            expiresAt: exp);
}
