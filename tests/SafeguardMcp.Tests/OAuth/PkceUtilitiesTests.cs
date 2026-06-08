#nullable disable

using System.Text;
using SafeguardMcp.OAuth;

namespace SafeguardMcp.Tests.OAuth;

/// <summary>
/// Verifies <see cref="PkceUtilities"/> against RFC 7636 test
/// vectors and basic invariants. The exact verifier/challenge pair
/// is the example from RFC 7636 §4.6 (Appendix B); a green test
/// here proves the bridge will produce challenges rSTS accepts.
/// </summary>
public class PkceUtilitiesTests
{
    [Fact]
    public void ComputeS256Challenge_MatchesRfc7636AppendixB()
    {
        // RFC 7636 §4.6: code_verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
        //                code_challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        const string expected = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        Assert.Equal(expected, PkceUtilities.ComputeS256Challenge(verifier));
    }

    [Fact]
    public void GenerateS256Pair_ProducesSelfConsistentChallenge()
    {
        var (v, c) = PkceUtilities.GenerateS256Pair();
        Assert.Equal(c, PkceUtilities.ComputeS256Challenge(v));
    }

    [Fact]
    public void GenerateOpaqueToken_DefaultLength_Is43Base64UrlChars()
    {
        // 32 bytes encoded base64url without padding = 43 chars; this
        // is the RFC 7636 minimum legal verifier length and the
        // entropy budget the bridge uses for its own opaque ids.
        var t = PkceUtilities.GenerateOpaqueToken();
        Assert.Equal(43, t.Length);
        foreach (var ch in t)
        {
            var ok = (ch >= 'A' && ch <= 'Z')
                || (ch >= 'a' && ch <= 'z')
                || (ch >= '0' && ch <= '9')
                || ch == '-' || ch == '_';
            Assert.True(ok, $"non-base64url char '{ch}' in token '{t}'");
        }
    }

    [Fact]
    public void GenerateOpaqueToken_IsUnique()
    {
        var a = PkceUtilities.GenerateOpaqueToken();
        var b = PkceUtilities.GenerateOpaqueToken();
        Assert.NotEqual(a, b);
    }
}
