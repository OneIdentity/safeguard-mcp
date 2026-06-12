using System.Net;
using System.Security;

namespace SafeguardMcp.Tools;

/// <summary>
/// Helpers for reading <see cref="SecureString"/> values that are
/// borrowed from another component (most importantly, the
/// SafeguardDotNet SDK). The SDK's
/// <c>ISafeguardConnection.GetAccessToken()</c> returns its internal
/// bearer <see cref="SecureString"/> by reference — it is NOT a copy.
/// Disposing that instance zeroes the SDK's own bearer and the next
/// call into the SDK throws <see cref="ObjectDisposedException"/>.
///
/// The helpers below are intentionally narrow: they read the cleartext
/// into a transient, zero an unmanaged copy, and never touch the
/// source <see cref="SecureString"/>.
/// </summary>
internal static class SecureStringExtensions
{
    /// <summary>
    /// Reads <paramref name="borrowed"/> into a managed <see cref="string"/>
    /// without disposing or otherwise mutating the source. Returns
    /// <c>null</c> when the source is <c>null</c> or empty.
    /// </summary>
    /// <remarks>
    /// The intermediate unmanaged buffer allocated by the BCL is zeroed
    /// before being freed. The returned managed string is, by the
    /// nature of <see cref="string"/>, not pinnable or zeroable; the
    /// caller is expected to drop the reference promptly.
    /// </remarks>
    public static string ReadInsecure(SecureString borrowed)
    {
        if (borrowed == null || borrowed.Length == 0)
            return null;

        // NetworkCredential.Password copies the SecureString into a
        // managed string without disposing it — which is exactly the
        // borrowed-source contract we need. Wrapping the call this
        // way keeps every call site honest about not assigning the
        // SecureString to a `using` variable.
        return new NetworkCredential(string.Empty, borrowed).Password;
    }
}
