#nullable disable

using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using OneIdentity.SafeguardDotNet;
using OneIdentity.SafeguardDotNet.Event;
using OneIdentity.SafeguardDotNet.Sps;
using SafeguardMcp.Tools;
using Xunit;

namespace SafeguardMcp.Tests;

/// <summary>
/// Regression tests pinning the contract that
/// <see cref="ISafeguardConnection.GetAccessToken"/> returns a borrowed
/// SecureString — call sites must NOT dispose it. Disposing the
/// borrowed instance zeros the SDK's internal bearer and triggers a
/// re-prompt on the next call (the symptom that originally motivated
/// these tests).
/// </summary>
public class BorrowedAccessTokenTests
{
    [Fact]
    public void PopulateTokenExpiry_DoesNotDisposeBorrowedSecureString()
    {
        var conn = new BorrowedTokenConnection(BuildJwt(expSecondsFromNow: 600));

        var info = InvokePopulateTokenExpiry(conn);

        Assert.False(conn.SecureToken.IsDisposed());
        Assert.Equal(1, conn.GetAccessTokenCallCount);
        Assert.NotNull(info.TokenExpiresAt);
    }

    [Fact]
    public void GetBearerToken_DoesNotDisposeBorrowedSecureString()
    {
        var conn = new BorrowedTokenConnection("not-a-jwt-but-still-a-bearer");

        var bearer = InvokeGetBearerToken(conn);

        Assert.Equal("not-a-jwt-but-still-a-bearer", bearer);
        Assert.False(conn.SecureToken.IsDisposed());
    }

    [Fact]
    public void RepeatedCalls_StillReturnSameLiveSecureString()
    {
        // Mirrors the real failure mode: Connect fires PopulateTokenExpiry,
        // then the next Execute fires GetBearerToken. If either disposes,
        // the second readback throws ObjectDisposedException.
        var conn = new BorrowedTokenConnection(BuildJwt(expSecondsFromNow: 600));

        InvokePopulateTokenExpiry(conn);
        var bearer = InvokeGetBearerToken(conn);

        Assert.False(conn.SecureToken.IsDisposed());
        Assert.False(string.IsNullOrEmpty(bearer));
    }

    [Fact]
    public void ReadInsecure_DoesNotDisposeSource()
    {
        var secure = ToSecureString("hello-borrowed");

        var read = SecureStringExtensions.ReadInsecure(secure);

        Assert.Equal("hello-borrowed", read);
        // Source SecureString must still be readable (not disposed).
        var roundTrip = new NetworkCredential(string.Empty, secure).Password;
        Assert.Equal("hello-borrowed", roundTrip);
    }

    [Fact]
    public void ReadInsecure_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(SecureStringExtensions.ReadInsecure(null));
        Assert.Null(SecureStringExtensions.ReadInsecure(new SecureString()));
    }

    private static PrincipalInfo InvokePopulateTokenExpiry(ISafeguardConnection conn)
    {
        var method = typeof(SessionHelpers).GetMethod(
            "PopulateTokenExpiry",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var info = new PrincipalInfo();
        method.Invoke(null, new object[] { conn, info });
        return info;
    }

    private static string InvokeGetBearerToken(ISafeguardConnection conn)
    {
        var method = typeof(SafeguardInvoker).GetMethod(
            "GetBearerToken",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, new object[] { conn });
    }

    private static string BuildJwt(long expSecondsFromNow)
    {
        var exp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expSecondsFromNow;
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncode("{\"exp\":" + exp + "}");
        return header + "." + payload + ".";
    }

    private static string Base64UrlEncode(string s)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        return System.Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static SecureString ToSecureString(string s)
    {
        var ss = new SecureString();
        foreach (var c in s) ss.AppendChar(c);
        ss.MakeReadOnly();
        return ss;
    }

    /// <summary>
    /// Models the SDK contract: GetAccessToken hands back the
    /// authenticator's owned SecureString by reference. The fake
    /// holds it for the lifetime of the connection and exposes
    /// IsDisposed() so the test can assert the borrowed instance
    /// outlived the call site.
    /// </summary>
    private sealed class BorrowedTokenConnection : ISafeguardConnection
    {
        public OwnedSecureString SecureToken { get; }
        public int GetAccessTokenCallCount;

        public BorrowedTokenConnection(string token)
        {
            var ss = new SecureString();
            foreach (var c in token) ss.AppendChar(c);
            ss.MakeReadOnly();
            SecureToken = new OwnedSecureString(ss);
        }

        public SecureString GetAccessToken()
        {
            GetAccessTokenCallCount++;
            return SecureToken.Inner;
        }

        public int GetAccessTokenLifetimeRemaining() => 60;
        public void RefreshAccessToken() { }
        public void LogOut() { }
        public void Dispose() => SecureToken.Dispose();

        public IStreamingRequest Streaming => throw new System.NotSupportedException();
        public string InvokeMethod(Service service, Method method, string relativeUrl, string body = null,
            IDictionary<string, string> parameters = null, IDictionary<string, string> additionalHeaders = null,
            System.TimeSpan? timeout = null) => throw new System.NotSupportedException();
        public FullResponse InvokeMethodFull(Service service, Method method, string relativeUrl, string body = null,
            IDictionary<string, string> parameters = null, IDictionary<string, string> additionalHeaders = null,
            System.TimeSpan? timeout = null) => throw new System.NotSupportedException();
        public string InvokeMethodCsv(Service service, Method method, string relativeUrl, string body = null,
            IDictionary<string, string> parameters = null, IDictionary<string, string> additionalHeaders = null,
            System.TimeSpan? timeout = null) => throw new System.NotSupportedException();
        public FullResponse JoinSps(ISafeguardSessionsConnection spsConnection, string certificateChain, string sppAddress)
            => throw new System.NotSupportedException();
        public ISafeguardEventListener GetEventListener() => throw new System.NotSupportedException();
        public ISafeguardEventListener GetPersistentEventListener() => throw new System.NotSupportedException();
        public ISafeguardConnection GetManagementServiceConnection(string networkAddress) => throw new System.NotSupportedException();
    }

    /// <summary>
    /// Wraps a SecureString so we can detect disposal: a disposed
    /// SecureString throws ObjectDisposedException from its public
    /// surface (e.g. <c>Length</c> / <c>Copy</c>). We probe with
    /// NetworkCredential.Password which internally
    /// <c>SecureStringToGlobalAllocUnicode</c>s and rejects a
    /// disposed source.
    /// </summary>
    private sealed class OwnedSecureString
    {
        public SecureString Inner { get; }
        public OwnedSecureString(SecureString inner) { Inner = inner; }
        public bool IsDisposed()
        {
            try
            {
                _ = new NetworkCredential(string.Empty, Inner).Password;
                return false;
            }
            catch (System.ObjectDisposedException)
            {
                return true;
            }
        }
        public void Dispose() => Inner.Dispose();
    }
}
