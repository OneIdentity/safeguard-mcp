using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace SafeguardMcp.Login;

/// <summary>
/// Writes a Safeguard user token to disk with an OS-appropriate
/// restrictive ACL so the file is readable only by the user running
/// <c>safeguard-mcp login --output &lt;path&gt;</c>.
///
/// <list type="bullet">
///   <item><b>Linux / macOS:</b> file is created via
///   <see cref="FileStreamOptions.UnixCreateMode"/> = <c>0600</c>
///   (UserRead | UserWrite), then <see cref="File.SetUnixFileMode"/>
///   is re-applied as a defence-in-depth step in case the underlying
///   filesystem ignored the create-mode hint.</item>
///   <item><b>Windows:</b> file is created via
///   <c>FileSystemAclExtensions.Create</c> with a
///   <see cref="FileSecurity"/> that disables inheritance and grants
///   <c>FullControl</c> only to the current user SID. No reflection,
///   no dynamic codegen — AOT-safe.</item>
/// </list>
///
/// Any pre-existing file at the destination is deleted before
/// (re-)creation so the new ACL replaces whatever the previous
/// file's permissions were.
/// </summary>
internal static class SecureTokenFile
{
    public static void Write(string path, string token)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Output path is required.", nameof(path));
        if (token == null)
            throw new ArgumentNullException(nameof(token));

        var fullPath = Path.GetFullPath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        if (OperatingSystem.IsWindows())
            WriteWindows(fullPath, token);
        else
            WriteUnix(fullPath, token);
    }

    [SupportedOSPlatform("windows")]
    private static void WriteWindows(string path, string token)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var sid = identity.User
            ?? throw new InvalidOperationException(
                "Could not resolve the current Windows user SID; "
                + "cannot create a user-only ACL for the token file.");

        var security = new FileSecurity();
        security.SetOwner(sid);
        // Disable inheritance and discard any inherited rules so the file
        // is locked to the current user even when written into a
        // directory with permissive inherited ACEs.
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(
            sid,
            FileSystemRights.FullControl,
            InheritanceFlags.None,
            PropagationFlags.None,
            AccessControlType.Allow));

        var info = new FileInfo(path);
        using var stream = FileSystemAclExtensions.Create(
            info,
            FileMode.CreateNew,
            FileSystemRights.WriteData | FileSystemRights.ReadData,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.None,
            security);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(token);
    }

    [UnsupportedOSPlatform("windows")]
    private static void WriteUnix(string path, string token)
    {
        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = mode,
        };

        using (var stream = new FileStream(path, options))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            writer.Write(token);
        }

        // Defence in depth: if the create-mode hint was ignored by the
        // underlying filesystem, force 0600 explicitly.
        File.SetUnixFileMode(path, mode);
    }
}
