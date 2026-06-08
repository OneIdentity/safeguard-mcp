using System.Net;
using System.Text;
using OneIdentity.SafeguardDotNet;
using SafeguardMcp.Tools;
using DeviceCodeInfo = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeInfo;
using DeviceCodeLogin = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLogin;
using DeviceCodeLoginParameters = OneIdentity.SafeguardDotNet.DeviceCodeLogin.DeviceCodeLoginParameters;

namespace SafeguardMcp.Login;

/// <summary>
/// Implements the <c>safeguard-mcp login</c> subcommand parsed by
/// <see cref="Program.Main"/>. Drives the SafeguardDotNet
/// <c>DeviceCodeLogin</c> infrastructure (Stage 1 rSTS + Stage 2
/// LoginResponse performed inside the SDK), then surfaces the
/// resulting Stage 2 Safeguard user token according to the requested
/// output mode:
///
/// <list type="bullet">
///   <item><b>Default</b> — human-readable summary (principal, IdP,
///   JWT-decoded expiry) plus the token and copy-paste guidance for
///   Claude Desktop, VS Code, and Cursor — to <c>stdout</c>.</item>
///   <item><b><c>--print-bearer-only</c></b> — token to <c>stdout</c>
///   only; everything else goes to <c>stderr</c>.</item>
///   <item><b><c>--output &lt;path&gt;</c></b> — token written to
///   <paramref>path</paramref> with an OS-appropriate restrictive ACL
///   via <see cref="SecureTokenFile"/>. A summary is also written to
///   <c>stdout</c> unless combined with <c>--print-bearer-only</c>.</item>
/// </list>
///
/// The Safeguard user token is held only as a stack local and a
/// <see cref="System.Security.SecureString"/> owned by the SDK
/// connection that is disposed before this method returns; nothing
/// persists in process memory after exit. Tokens are never logged.
/// </summary>
internal static class LoginCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (HasFlag(args, "-h", "--help", "-?"))
        {
            Console.Out.WriteLine(HelpText);
            return 0;
        }

        var host = GetArgValue(args, "--host")
            ?? Environment.GetEnvironmentVariable("SAFEGUARD_HOST")?.Trim();
        var printBearerOnly = HasFlag(args, "--print-bearer-only");
        var outputPath = GetArgValue(args, "--output");
        var ignoreSsl = HasFlag(args, "--ignore-ssl") || ResolveIgnoreSslEnv();

        if (string.IsNullOrWhiteSpace(host))
        {
            Console.Error.WriteLine(
                "Error: Safeguard appliance host is required. "
                + "Pass --host <appliance> or set the SAFEGUARD_HOST environment variable.");
            Console.Error.WriteLine();
            Console.Error.WriteLine(HelpText);
            return 2;
        }

        var parameters = new DeviceCodeLoginParameters
        {
            DisplayCallback = info => WriteDeviceCodePrompt(host, info),
        };

        ISafeguardConnection connection = null;
        try
        {
            try
            {
                connection = await DeviceCodeLogin.ConnectAsync(
                    host, parameters, ignoreSsl: ignoreSsl, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Login cancelled.");
                return 1;
            }
            catch (Exception ex) when (ex is HttpRequestException or System.Net.Sockets.SocketException)
            {
                Console.Error.WriteLine(
                    $"Cannot reach Safeguard appliance '{host}': {ex.Message}. "
                    + "Verify the hostname/IP and that the appliance is reachable.");
                return 1;
            }
            catch (SafeguardDotNetException ex)
            {
                Console.Error.WriteLine(
                    $"Authentication against '{host}' failed: {ex.Message}. "
                    + "If your administrator has not enabled the Device Authorization Grant, "
                    + "acquire a token using your MCP client's OAuth flow instead.");
                return 1;
            }

            string token;
            using (var secureToken = connection.GetAccessToken())
            {
                if (secureToken == null || secureToken.Length == 0)
                {
                    Console.Error.WriteLine(
                        "Login completed but the SDK returned an empty Safeguard user token. "
                        + "This usually indicates a Stage 2 LoginResponse failure; re-run with --ignore-ssl=false "
                        + "and verify the appliance is healthy.");
                    return 1;
                }
                token = new NetworkCredential(string.Empty, secureToken).Password;
            }

            PrincipalInfo principal = null;
            try
            {
                principal = await SessionHelpers.GetPrincipalInfoFromConnectionAsync(connection, host, cancellationToken);
            }
            catch
            {
                // /Me is best-effort; we already have a valid token. The
                // summary just degrades to "principal unknown".
            }

            if (!string.IsNullOrEmpty(outputPath))
            {
                try
                {
                    SecureTokenFile.Write(outputPath, token);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to write token to '{outputPath}': {ex.Message}");
                    return 1;
                }

                Console.Error.WriteLine(
                    OperatingSystem.IsWindows()
                        ? $"Wrote Safeguard user token to '{outputPath}' (Windows ACL: current user only, inheritance disabled)."
                        : $"Wrote Safeguard user token to '{outputPath}' (mode 0600).");

                if (!printBearerOnly)
                    WriteSummary(Console.Out, host, principal, token, outputPath);
                return 0;
            }

            if (printBearerOnly)
            {
                Console.Out.WriteLine(token);
                return 0;
            }

            WriteSummary(Console.Out, host, principal, token, null);
            return 0;
        }
        finally
        {
            connection?.Dispose();
        }
    }

    private static void WriteDeviceCodePrompt(string host, DeviceCodeInfo info)
    {
        var url = string.IsNullOrWhiteSpace(info.VerificationUriComplete)
            ? info.VerificationUri
            : info.VerificationUriComplete;

        // Always to stderr — keeps stdout clean for --print-bearer-only.
        Console.Error.WriteLine();
        Console.Error.WriteLine($"To finish signing in to '{host}':");
        Console.Error.WriteLine($"  1. Open: {url}");
        Console.Error.WriteLine($"  2. Enter code: {info.UserCode}");
        Console.Error.WriteLine($"  (Code expires in {info.ExpiresIn} seconds.)");
        Console.Error.WriteLine();
    }

    private static void WriteSummary(TextWriter writer, string host, PrincipalInfo principal, string token, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Authenticated to Safeguard appliance at {host}.");
        if (principal != null)
        {
            if (!string.IsNullOrWhiteSpace(principal.DisplayName) || !string.IsNullOrWhiteSpace(principal.Name))
            {
                sb.Append("  Principal: ").Append(principal.DisplayName ?? principal.Name);
                if (!string.IsNullOrWhiteSpace(principal.IdentityProvider))
                    sb.Append(" (").Append(principal.IdentityProvider).Append(')');
                sb.AppendLine();
            }
            if (principal.TokenLifetimeMinutes > 0)
                sb.AppendLine($"  Token lifetime: {principal.TokenLifetimeMinutes} minutes remaining");
            if (principal.TokenExpiresAt.HasValue)
                sb.AppendLine($"  Token expires:  {principal.TokenExpiresAt.Value:u}");
        }
        sb.AppendLine();

        if (outputPath != null)
        {
            sb.AppendLine($"Token written to: {outputPath}");
        }
        else
        {
            sb.AppendLine("Safeguard user token (treat as a credential — do not share):");
            sb.AppendLine(token);
        }

        sb.AppendLine();
        sb.AppendLine("PRIMARY PATH (recommended): point your MCP client at https://<your-mcp-host>/mcp");
        sb.AppendLine("and let it discover OAuth via /.well-known/oauth-authorization-server. The");
        sb.AppendLine("client will run a browser-based PKCE login against the appliance's rSTS and");
        sb.AppendLine("manage the bearer for you — no manual paste required.");
        sb.AppendLine();
        sb.AppendLine("MANUAL FALLBACK (for scripting or MCP clients that don't speak OAuth discovery):");
        sb.AppendLine("configure your MCP client to send");
        sb.AppendLine("  Authorization: Bearer <token>");
        sb.AppendLine("on every MCP request. Example client config snippets:");
        sb.AppendLine();
        sb.AppendLine("  Claude Desktop (claude_desktop_config.json):");
        sb.AppendLine("    \"mcpServers\": {");
        sb.AppendLine("      \"safeguard\": {");
        sb.AppendLine("        \"url\": \"https://<your-mcp-host>/mcp\",");
        sb.AppendLine("        \"headers\": { \"Authorization\": \"Bearer <token>\" }");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("  VS Code (.vscode/mcp.json):");
        sb.AppendLine("    \"servers\": {");
        sb.AppendLine("      \"safeguard\": {");
        sb.AppendLine("        \"type\": \"http\",");
        sb.AppendLine("        \"url\": \"https://<your-mcp-host>/mcp\",");
        sb.AppendLine("        \"headers\": { \"Authorization\": \"Bearer <token>\" }");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("  Cursor (~/.cursor/mcp.json):");
        sb.AppendLine("    \"mcpServers\": {");
        sb.AppendLine("      \"safeguard\": {");
        sb.AppendLine("        \"url\": \"https://<your-mcp-host>/mcp\",");
        sb.AppendLine("        \"headers\": { \"Authorization\": \"Bearer <token>\" }");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("The Safeguard appliance is the authority on token validity; re-run `safeguard-mcp login`");
        sb.AppendLine("when the token expires or is revoked.");

        writer.Write(sb.ToString());
    }

    private static bool ResolveIgnoreSslEnv()
        => bool.TryParse(Environment.GetEnvironmentVariable("SAFEGUARD_IGNORE_SSL"), out var v) && v;

    private static bool HasFlag(string[] args, params string[] flags)
    {
        foreach (var arg in args)
            foreach (var flag in flags)
                if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                    return true;
        return false;
    }

    private static string GetArgValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                    return args[i + 1];
                return null;
            }
            var prefix = name + "=";
            if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return args[i].Substring(prefix.Length);
        }
        return null;
    }

    private const string HelpText = @"safeguard-mcp login — acquire a Safeguard user token via device-code login.

USAGE:
  safeguard-mcp login [--host <appliance>] [--ignore-ssl]
                      [--print-bearer-only | --output <path>]

OPTIONS:
  --host <appliance>      Safeguard appliance DNS name or IP.
                          Defaults to the SAFEGUARD_HOST environment variable.
  --ignore-ssl            Skip TLS certificate validation on the appliance
                          connection (lab/test only). Also honoured via
                          SAFEGUARD_IGNORE_SSL=true.
  --print-bearer-only     Write the Safeguard user token to stdout only.
                          Prompts and diagnostics go to stderr.
  --output <path>         Write the token to <path> with a restrictive ACL
                          (Unix: 0600; Windows: explicit user-only ACE,
                          inheritance disabled).

The token printed is the Stage 2 Safeguard user token, suitable for use as
the `Authorization: Bearer <token>` header against an HTTP-mode
safeguard-mcp server. Tokens are never logged.

When you're done with the token, revoke it server-side before its natural
TTL with `safeguard-mcp logout --host <appliance> --input <path>` (or pipe
the token to `logout --input -`).";
}
