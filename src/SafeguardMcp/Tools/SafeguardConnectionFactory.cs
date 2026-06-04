using System.Security;
using OneIdentity.SafeguardDotNet;
using DeviceCodeLogin = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLogin;
using DeviceCodeLoginParameters = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLoginParameters;
using PkceLogin = OneIdentity.SafeguardDotNet.PkceNoninteractiveLogin.PkceNoninteractiveLogin;

namespace SafeguardMcp.Tools;

/// <summary>
/// Indirection over the static SafeguardDotNet login entry points so the
/// device-code and PKCE branches in <see cref="SafeguardConnectionManager"/>
/// can be unit-tested without contacting a live appliance.
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
}
