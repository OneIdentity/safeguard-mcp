using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace SafeguardMcp.OAuth;

/// <summary>
/// AOT-safe helpers for OAuth PKCE (RFC 7636) and the opaque random
/// identifiers the bridge mints for its in-memory state maps. Uses
/// <see cref="RandomNumberGenerator"/> + <see cref="Base64Url"/>
/// (System.Buffers.Text) so no reflection or runtime codegen is
/// needed.
/// </summary>
internal static class PkceUtilities
{
    /// <summary>
    /// 32 bytes → 43 base64url characters. RFC 7636 §4.1 requires
    /// 43–128 characters of unreserved-alphabet entropy; this hits
    /// the minimum length while remaining a comfortably opaque
    /// identifier for the bridge's own session/auth-code maps.
    /// </summary>
    public const int OpaqueIdByteLength = 32;

    /// <summary>
    /// Returns a fresh base64url-encoded random token of the
    /// requested raw-byte length. The default produces a 43-char
    /// string suitable as both a PKCE <c>code_verifier</c> and as
    /// the bridge's opaque <c>bridge_session_id</c> /
    /// <c>bridge_auth_code</c> identifier.
    /// </summary>
    public static string GenerateOpaqueToken(int byteLength = OpaqueIdByteLength)
    {
        if (byteLength <= 0) throw new ArgumentOutOfRangeException(nameof(byteLength));
        Span<byte> bytes = stackalloc byte[byteLength];
        RandomNumberGenerator.Fill(bytes);
        return Base64Url.EncodeToString(bytes);
    }

    /// <summary>
    /// Computes the PKCE <c>S256</c> challenge for the given
    /// verifier: <c>BASE64URL(SHA256(ASCII(code_verifier)))</c>
    /// (RFC 7636 §4.2). The verifier alphabet is a strict subset of
    /// ASCII so encoding via <see cref="Encoding.ASCII"/> matches
    /// RFC 7636's specification.
    /// </summary>
    public static string ComputeS256Challenge(string codeVerifier)
    {
        if (codeVerifier == null) throw new ArgumentNullException(nameof(codeVerifier));
        var ascii = Encoding.ASCII.GetBytes(codeVerifier);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(ascii, hash);
        return Base64Url.EncodeToString(hash);
    }

    /// <summary>
    /// Generates a fresh verifier + S256 challenge pair for the
    /// bridge↔rSTS leg of the auth-code flow.
    /// </summary>
    public static (string Verifier, string Challenge) GenerateS256Pair()
    {
        var verifier = GenerateOpaqueToken();
        var challenge = ComputeS256Challenge(verifier);
        return (verifier, challenge);
    }
}
