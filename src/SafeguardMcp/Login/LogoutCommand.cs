using System.Net;
using System.Net.Http.Headers;

namespace SafeguardMcp.Login;

/// <summary>
/// Implements the <c>safeguard-mcp logout</c> subcommand: the symmetric
/// counterpart to <c>safeguard-mcp login</c>. Reads a previously-issued
/// Safeguard user token (from <c>--input &lt;path&gt;</c> or from
/// <c>stdin</c>) and revokes it server-side by posting to the appliance's
/// <c>/service/core/v4/Token/Logout</c> endpoint.
///
/// <list type="bullet">
///   <item>HTTP 200 — token revoked. Exit 0.</item>
///   <item>HTTP 401 — token was already invalid (expired or previously
///   revoked). Treated as idempotent success. Exit 0.</item>
///   <item>Network/TLS error — exit 1.</item>
///   <item>Usage error (missing host, missing/empty input) — exit 2.</item>
/// </list>
///
/// The command never deletes the <c>--input</c> file; managing that file
/// is the caller's responsibility (mirrors how <c>login --output</c>
/// writes it). Token material is held only on the stack and inside a
/// disposed <see cref="HttpRequestMessage"/>; nothing persists in
/// process memory after exit. Tokens are never logged.
/// </summary>
internal static class LogoutCommand
{
    /// <summary>
    /// Test seam — overridable factory for the <see cref="HttpMessageHandler"/>
    /// used to call the appliance. Production code path uses
    /// <see cref="DefaultHandlerFactory"/>, which honours
    /// <c>--ignore-ssl</c> by disabling server certificate validation.
    /// </summary>
    internal static Func<bool, HttpMessageHandler> HandlerFactory = DefaultHandlerFactory;

    /// <summary>
    /// Test seam — overridable stdin reader. Defaults to
    /// <see cref="Console.In"/>.
    /// </summary>
    internal static Func<TextReader> StdinFactory = () => Console.In;

    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (HasFlag(args, "-h", "--help", "-?"))
        {
            Console.Out.WriteLine(HelpText);
            return 0;
        }

        var host = GetArgValue(args, "--host")
            ?? Environment.GetEnvironmentVariable("SAFEGUARD_HOST")?.Trim();
        var inputPath = GetArgValue(args, "--input");
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

        string token;
        try
        {
            token = ReadToken(inputPath);
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: token input file not found: {ex.FileName ?? inputPath}.");
            return 2;
        }
        catch (DirectoryNotFoundException)
        {
            Console.Error.WriteLine($"Error: token input file not found: {inputPath}.");
            return 2;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Error: failed to read token from '{inputPath}': {ex.Message}");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            Console.Error.WriteLine(
                inputPath == null || inputPath == "-"
                    ? "Error: no token received on stdin. Pipe the token from `login --print-bearer-only`, "
                      + "or pass --input <path> to read it from a file."
                    : $"Error: token input file '{inputPath}' is empty.");
            return 2;
        }

        var url = $"https://{host}/service/core/v4/Token/Logout";

        using var handler = HandlerFactory(ignoreSsl);
        using var http = new HttpClient(handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Token/Logout takes no body, but the appliance requires a non-null
        // request entity on POST. An empty content with explicit length 0
        // keeps proxies and the appliance happy.
        request.Content = new ByteArrayContent(Array.Empty<byte>());

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Logout cancelled.");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine(
                $"Cannot reach Safeguard appliance '{host}': {ex.Message}. "
                + "Verify the hostname/IP and that the appliance is reachable.");
            return 1;
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                Console.Out.WriteLine("Token revoked.");
                return 0;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // 401 means the appliance no longer accepts this token —
                // it is already invalid. Logout's job is to ensure the
                // token cannot be used; if it can't be, we're done.
                Console.Out.WriteLine("Token already invalid; nothing to revoke.");
                return 0;
            }

            Console.Error.WriteLine(
                $"Logout against '{host}' failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
            return 1;
        }
    }

    private static string ReadToken(string inputPath)
    {
        if (string.IsNullOrEmpty(inputPath) || inputPath == "-")
            return StdinFactory().ReadToEnd().Trim();

        // File.ReadAllText surfaces FileNotFoundException / IOException
        // which RunAsync translates into exit-code-2 user-facing messages.
        return File.ReadAllText(inputPath).Trim();
    }

    private static HttpMessageHandler DefaultHandlerFactory(bool ignoreSsl)
    {
        var handler = new HttpClientHandler();
        if (ignoreSsl)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        return handler;
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

    private const string HelpText = @"safeguard-mcp logout — revoke a Safeguard user token previously issued by `login`.

USAGE:
  safeguard-mcp logout [--host <appliance>] [--ignore-ssl]
                       [--input <path> | --input -]

OPTIONS:
  --host <appliance>      Safeguard appliance DNS name or IP.
                          Defaults to the SAFEGUARD_HOST environment variable.
  --ignore-ssl            Skip TLS certificate validation on the appliance
                          connection (lab/test only). Also honoured via
                          SAFEGUARD_IGNORE_SSL=true.
  --input <path>          Read the token to revoke from <path> (the file
                          previously written by `login --output <path>`).
  --input -               Read the token from stdin. This is also the
                          default when --input is omitted, so you can pipe:
                            cat token.txt | safeguard-mcp logout --host …

EXIT CODES:
  0  Token revoked, or the appliance reports it is already invalid
     (HTTP 401 — treated as idempotent success).
  1  Network/TLS error reaching the appliance, or the appliance returned
     an unexpected status.
  2  Usage error (missing --host, missing/empty token input).

This command does not delete the --input file. If you want the token
file gone after revocation, remove it with your shell:
    safeguard-mcp logout --host … --input ~/.safeguard-mcp/token && rm ~/.safeguard-mcp/token";
}
