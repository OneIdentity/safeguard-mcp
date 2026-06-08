namespace SafeguardMcp.Logging;

/// <summary>
/// Shared no-op <see cref="IDisposable"/> for <c>ILogger.BeginScope</c>
/// returns. The MEL contract requires a non-null disposable so that
/// callers can wrap scopes in <c>using</c>; returning <c>null</c>
/// NREs at <c>Dispose</c>.
/// </summary>
internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();
    private NullScope() { }
    public void Dispose() { }
}
