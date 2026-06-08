#nullable disable

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using SafeguardMcp.Login;

namespace SafeguardMcp.Tests.Login;

/// <summary>
/// Verifies the <c>safeguard-mcp logout</c> subcommand:
///
/// <list type="bullet">
///   <item>Posts to <c>POST https://&lt;host&gt;/service/core/v4/Token/Logout</c>
///         with the bearer it read from <c>--input &lt;path&gt;</c> or stdin.</item>
///   <item>200 → exit 0 with <c>Token revoked.</c> on stdout.</item>
///   <item>401 → exit 0 with <c>Token already invalid; nothing to revoke.</c>
///         on stdout (idempotent success).</item>
///   <item>Other 4xx/5xx → exit 1.</item>
///   <item>Network error (<see cref="HttpRequestException"/>) → exit 1.</item>
///   <item>Missing <c>--host</c> → exit 2.</item>
///   <item>Missing/empty input → exit 2; no HTTP call made.</item>
///   <item>The <c>--input</c> file is NOT deleted on success.</item>
/// </list>
/// </summary>
public class LogoutCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TextWriter _origOut;
    private readonly TextWriter _origErr;
    private readonly Func<bool, HttpMessageHandler> _origHandlerFactory;
    private readonly Func<TextReader> _origStdinFactory;

    public LogoutCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sg-mcp-logout-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _origOut = Console.Out;
        _origErr = Console.Error;
        _origHandlerFactory = LogoutCommand.HandlerFactory;
        _origStdinFactory = LogoutCommand.StdinFactory;
    }

    public void Dispose()
    {
        Console.SetOut(_origOut);
        Console.SetError(_origErr);
        LogoutCommand.HandlerFactory = _origHandlerFactory;
        LogoutCommand.StdinFactory = _origStdinFactory;
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task Success_200_FromInputFile_ReturnsZero()
    {
        var tokenPath = Path.Combine(_tempDir, "token");
        File.WriteAllText(tokenPath, "abc.def.ghi\n");

        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        LogoutCommand.HandlerFactory = _ => handler;

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com", "--input", tokenPath },
                CancellationToken.None));

        Assert.Equal(0, result.exitCode);
        Assert.Contains("Token revoked.", result.outText);
        Assert.Single(handler.Requests);
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("https://appliance.example.com/service/core/v4/Token/Logout", req.RequestUri!.ToString());
        Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
        Assert.Equal("abc.def.ghi", req.Headers.Authorization.Parameter);
        Assert.True(File.Exists(tokenPath));
    }

    [Fact]
    public async Task Unauthorized_401_IsTreatedAsIdempotentSuccess()
    {
        var tokenPath = Path.Combine(_tempDir, "token");
        File.WriteAllText(tokenPath, "stale-token");

        LogoutCommand.HandlerFactory = _ => new CapturingHandler(
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com", "--input", tokenPath },
                CancellationToken.None));

        Assert.Equal(0, result.exitCode);
        Assert.Contains("already invalid", result.outText);
    }

    [Fact]
    public async Task ServerError_500_ReturnsOne()
    {
        var tokenPath = Path.Combine(_tempDir, "token");
        File.WriteAllText(tokenPath, "abc");

        LogoutCommand.HandlerFactory = _ => new CapturingHandler(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = "Internal Server Error" });

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com", "--input", tokenPath },
                CancellationToken.None));

        Assert.Equal(1, result.exitCode);
        Assert.Contains("HTTP 500", result.errText);
    }

    [Fact]
    public async Task NetworkError_ReturnsOne()
    {
        var tokenPath = Path.Combine(_tempDir, "token");
        File.WriteAllText(tokenPath, "abc");

        LogoutCommand.HandlerFactory = _ => new CapturingHandler(
            _ => throw new HttpRequestException("connection refused"));

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com", "--input", tokenPath },
                CancellationToken.None));

        Assert.Equal(1, result.exitCode);
        Assert.Contains("Cannot reach Safeguard appliance 'appliance.example.com'", result.errText);
    }

    [Fact]
    public async Task MissingHost_ReturnsTwo_NoHttpCall()
    {
        var tokenPath = Path.Combine(_tempDir, "token");
        File.WriteAllText(tokenPath, "abc");

        var called = false;
        LogoutCommand.HandlerFactory = _ =>
        {
            called = true;
            return new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        };
        var prev = Environment.GetEnvironmentVariable("SAFEGUARD_HOST");
        Environment.SetEnvironmentVariable("SAFEGUARD_HOST", null);
        try
        {
            var result = await CaptureAsync(() =>
                LogoutCommand.RunAsync(
                    new[] { "--input", tokenPath },
                    CancellationToken.None));

            Assert.Equal(2, result.exitCode);
            Assert.Contains("appliance host is required", result.errText);
            Assert.False(called);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SAFEGUARD_HOST", prev);
        }
    }

    [Fact]
    public async Task EmptyInputFile_ReturnsTwo_NoHttpCall()
    {
        var tokenPath = Path.Combine(_tempDir, "empty");
        File.WriteAllText(tokenPath, "   \n\n");

        var called = false;
        LogoutCommand.HandlerFactory = _ =>
        {
            called = true;
            return new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        };

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com", "--input", tokenPath },
                CancellationToken.None));

        Assert.Equal(2, result.exitCode);
        Assert.Contains("is empty", result.errText);
        Assert.False(called);
    }

    [Fact]
    public async Task MissingInputFile_ReturnsTwo_NoHttpCall()
    {
        var tokenPath = Path.Combine(_tempDir, "does-not-exist");

        var called = false;
        LogoutCommand.HandlerFactory = _ =>
        {
            called = true;
            return new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        };

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com", "--input", tokenPath },
                CancellationToken.None));

        Assert.Equal(2, result.exitCode);
        Assert.Contains("not found", result.errText);
        Assert.False(called);
    }

    [Fact]
    public async Task MissingInputDirectory_ReturnsTwo_NoHttpCall()
    {
        // Parent directory missing surfaces as DirectoryNotFoundException
        // rather than FileNotFoundException; logout must treat both as
        // the same "input not found" usage error.
        var tokenPath = Path.Combine(_tempDir, "does-not-exist-dir", "token");

        var called = false;
        LogoutCommand.HandlerFactory = _ =>
        {
            called = true;
            return new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        };

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com", "--input", tokenPath },
                CancellationToken.None));

        Assert.Equal(2, result.exitCode);
        Assert.Contains("not found", result.errText);
        Assert.DoesNotContain("IO_PathNotFound", result.errText);
        Assert.False(called);
    }

    [Fact]
    public async Task StdinInput_DashFlag_ReadsTokenFromStdin()
    {
        LogoutCommand.StdinFactory = () => new StringReader("piped-token\n");
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        LogoutCommand.HandlerFactory = _ => handler;

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com", "--input", "-" },
                CancellationToken.None));

        Assert.Equal(0, result.exitCode);
        Assert.Single(handler.Requests);
        Assert.Equal("piped-token", handler.Requests[0].Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task NoInputFlag_DefaultsToStdin()
    {
        LogoutCommand.StdinFactory = () => new StringReader("default-stdin-token");
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        LogoutCommand.HandlerFactory = _ => handler;

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com" },
                CancellationToken.None));

        Assert.Equal(0, result.exitCode);
        Assert.Single(handler.Requests);
        Assert.Equal("default-stdin-token", handler.Requests[0].Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task EmptyStdin_ReturnsTwo_PointsAtPipe()
    {
        LogoutCommand.StdinFactory = () => new StringReader("");
        var called = false;
        LogoutCommand.HandlerFactory = _ =>
        {
            called = true;
            return new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        };

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(
                new[] { "--host", "appliance.example.com" },
                CancellationToken.None));

        Assert.Equal(2, result.exitCode);
        Assert.Contains("Pipe the token", result.errText);
        Assert.False(called);
    }

    [Fact]
    public async Task HelpFlag_ReturnsZero_NoHttpCall()
    {
        var called = false;
        LogoutCommand.HandlerFactory = _ =>
        {
            called = true;
            return new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        };

        var result = await CaptureAsync(() =>
            LogoutCommand.RunAsync(new[] { "--help" }, CancellationToken.None));

        Assert.Equal(0, result.exitCode);
        Assert.Contains("safeguard-mcp logout", result.outText);
        Assert.False(called);
    }

    private static async Task<(int exitCode, string outText, string errText)> CaptureAsync(
        Func<Task<int>> action)
    {
        var outW = new StringWriter();
        var errW = new StringWriter();
        Console.SetOut(outW);
        Console.SetError(errW);
        var code = await action();
        return (code, outW.ToString(), errW.ToString());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public List<HttpRequestMessage> Requests { get; } = new();

        public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            try
            {
                return Task.FromResult(_responder(request));
            }
            catch (Exception ex)
            {
                return Task.FromException<HttpResponseMessage>(ex);
            }
        }
    }
}
