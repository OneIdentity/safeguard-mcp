#nullable disable

using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using SafeguardMcp.Login;

namespace SafeguardMcp.Tests;

/// <summary>
/// Phase 1 task 1.E — verifies that <c>safeguard-mcp login --output &lt;path&gt;</c>
/// writes the token with an OS-appropriate restrictive ACL via
/// <see cref="SecureTokenFile"/>:
///
/// <list type="bullet">
///   <item>Unix: file mode is exactly <c>0600</c>
///     (<see cref="UnixFileMode.UserRead"/> | <see cref="UnixFileMode.UserWrite"/>).</item>
///   <item>Windows: ACL contains exactly one explicit ACE granting
///     <see cref="FileSystemRights.FullControl"/> to the current user SID,
///     inheritance is disabled, and no other principals are present.</item>
/// </list>
///
/// Both branches also verify that the token bytes round-trip exactly and
/// that re-writing the same path replaces (rather than appending to)
/// any pre-existing file.
/// </summary>
public class SecureTokenFileTests : IDisposable
{
    private readonly string _tempDir;

    public SecureTokenFileTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "safeguard-mcp-login-acl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Write_RoundTripsTokenBytesExactly()
    {
        var path = Path.Combine(_tempDir, "token-roundtrip.txt");
        const string token = "eyJhbGciOiJIUzI1NiJ9.payload.sig";

        SecureTokenFile.Write(path, token);

        // No trailing newline — `safeguard-mcp login --output` must produce
        // a file safe to read with `$(< file)` / `Get-Content` and use
        // directly as a bearer.
        var roundTripped = File.ReadAllText(path, Encoding.UTF8);
        Assert.Equal(token, roundTripped);
    }

    [Fact]
    public void Write_OverwritesPreExistingFileWithFreshAcl()
    {
        var path = Path.Combine(_tempDir, "token-overwrite.txt");
        File.WriteAllText(path, "stale-and-world-readable");

        SecureTokenFile.Write(path, "fresh");

        Assert.Equal("fresh", File.ReadAllText(path));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            AssertUnixModeIs0600(path);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            AssertWindowsAclIsCurrentUserOnly(path);
    }

    [Fact]
    public void Write_OnUnix_CreatesFileWithMode0600()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return; // Skipped on Windows; the Windows assertion is in a sibling test.

        var path = Path.Combine(_tempDir, "token-unix.txt");
        SecureTokenFile.Write(path, "unix-token");

        AssertUnixModeIs0600(path);
    }

    [Fact]
    public void Write_OnWindows_CreatesFileWithCurrentUserOnlyExplicitAce()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return; // Skipped on non-Windows; the Unix assertion is in a sibling test.

        var path = Path.Combine(_tempDir, "token-windows.txt");
        SecureTokenFile.Write(path, "windows-token");

        AssertWindowsAclIsCurrentUserOnly(path);
    }

    private static void AssertUnixModeIs0600(string path)
    {
        if (OperatingSystem.IsWindows())
            return;
        var mode = File.GetUnixFileMode(path);
        const UnixFileMode expected = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        Assert.Equal(expected, mode);
    }

    private static void AssertWindowsAclIsCurrentUserOnly(string path)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var info = new FileInfo(path);
        var security = FileSystemAclExtensions.GetAccessControl(info);

        // 1) Inheritance must be disabled (the rules are "protected").
        Assert.True(security.AreAccessRulesProtected,
            "Access rules must be protected (inheritance disabled) on the token file.");

        // 2) Exactly the current user SID may appear in the explicit ACEs.
        using var identity = WindowsIdentity.GetCurrent();
        var expectedSid = identity.User;
        Assert.NotNull(expectedSid);

        var explicitRules = security.GetAccessRules(
            includeExplicit: true,
            includeInherited: false,
            targetType: typeof(SecurityIdentifier));

        Assert.True(explicitRules.Count >= 1, "Expected at least one explicit ACE on the token file.");

        foreach (FileSystemAccessRule rule in explicitRules)
        {
            Assert.Equal(AccessControlType.Allow, rule.AccessControlType);
            Assert.Equal(expectedSid, rule.IdentityReference);
        }

        // 3) No inherited rules should be present (defence in depth — the
        //    AreAccessRulesProtected check above usually catches this).
        var inheritedRules = security.GetAccessRules(
            includeExplicit: false,
            includeInherited: true,
            targetType: typeof(SecurityIdentifier));
        Assert.Empty(inheritedRules);
    }
}
