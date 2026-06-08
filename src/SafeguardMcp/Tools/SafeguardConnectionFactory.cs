using System.Security;
using OneIdentity.SafeguardDotNet;
using DeviceCodeLogin = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLogin;
using DeviceCodeLoginParameters = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLoginParameters;
using PkceLogin = OneIdentity.SafeguardDotNet.PkceNoninteractiveLogin.PkceNoninteractiveLogin;

namespace SafeguardMcp.Tools;

/// <summary>
/// Indirection over the static SafeguardDotNet login entry points so
/// the device-code, PKCE, and access-token-attach paths in the session
/// implementations can be unit-tested without contacting a live
/// appliance.
/// </summary>
public interface ISafeguardConnectionFactory
{
    Task<ISafeguardConnection> ConnectDeviceCodeAsync(
        string host,
        DeviceCodeLoginParameters parameters,
        bool ignoreSsl,
        CancellationToken ct);

    ISafeguardConnection ConnectPkce(
        string host,
        string provider,
        string user,
        SecureString password,
        bool ignoreSsl);

    /// <summary>
    /// Wraps the SDK's
    /// <c>Safeguard.Connect(host, accessToken, apiVersion, ignoreSsl)</c>
    /// overload. Used by <c>HttpRelaySafeguardSession</c> to attach the
    /// caller-supplied Safeguard user token (from the request bearer)
    /// to a transient connection. The SDK does no I/O on construction
    /// and only holds the access token as a <see cref="SecureString"/>
    /// inside the returned connection; the SDK has no refresh path for
    /// this overload — the caller must re-acquire on 401.
    /// </summary>
    ISafeguardConnection ConnectWithAccessToken(
        string host,
        SecureString accessToken,
        int apiVersion,
        bool ignoreSsl);
}

internal sealed class SafeguardConnectionFactory : ISafeguardConnectionFactory
{
    public Task<ISafeguardConnection> ConnectDeviceCodeAsync(
            string host,
            DeviceCodeLoginParameters parameters,
            bool ignoreSsl,
            CancellationToken ct)
        => DeviceCodeLogin.ConnectAsync(host, parameters, ignoreSsl: ignoreSsl, cancellationToken: ct);

    public ISafeguardConnection ConnectPkce(
            string host,
            string provider,
            string user,
            SecureString password,
            bool ignoreSsl)
        => PkceLogin.Connect(host, provider, user, password, ignoreSsl: ignoreSsl);

    public ISafeguardConnection ConnectWithAccessToken(
            string host,
            SecureString accessToken,
            int apiVersion,
            bool ignoreSsl)
        => Safeguard.Connect(host, accessToken, apiVersion, ignoreSsl);
}
