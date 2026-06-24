using System.Collections.Generic;
using System.Text.Json;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Locks the count=true envelope shape: the scalar count is surfaced in meta.count
/// (data null), with no auto-limit / paging notices. A normal collection request is
/// left untouched (the helper returns null and the caller formats as usual).
/// </summary>
public class CountOnlyEnvelopeTests
{
    private static IDictionary<string, string> CountParams() =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["count"] = "true" };

    [Fact]
    public void CountRequest_SurfacesValueInMetaCount_WithDataNull()
    {
        var envelope = SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "52", CountParams());

        Assert.NotNull(envelope);
        using var doc = JsonDocument.Parse(envelope);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("data").ValueKind);

        var meta = doc.RootElement.GetProperty("meta");
        Assert.True(meta.TryGetProperty("count", out var count));
        Assert.Equal(52, count.GetInt64());
    }

    [Fact]
    public void CountRequest_EmitsNoPagingBlockAndNoPagingNotice()
    {
        var envelope = SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "0", CountParams());

        Assert.NotNull(envelope);
        using var doc = JsonDocument.Parse(envelope);
        var meta = doc.RootElement.GetProperty("meta");

        Assert.False(meta.TryGetProperty("paging", out _));

        var notices = meta.GetProperty("notices");
        foreach (var notice in notices.EnumerateArray())
        {
            var kind = notice.GetProperty("kind").GetString();
            Assert.NotEqual(NoticeKinds.AutoLimitApplied, kind);
            Assert.NotEqual(NoticeKinds.PagingMoreAvailable, kind);
        }
    }

    [Fact]
    public void CountRequest_ZeroCountIsUnambiguous()
    {
        var envelope = SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "0", CountParams());

        Assert.NotNull(envelope);
        using var doc = JsonDocument.Parse(envelope);
        Assert.Equal(0, doc.RootElement.GetProperty("meta").GetProperty("count").GetInt64());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("data").ValueKind);
    }

    [Fact]
    public void CountRequest_AttachesCountOnlyNotice()
    {
        var envelope = SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "7", CountParams());

        Assert.NotNull(envelope);
        using var doc = JsonDocument.Parse(envelope);
        var notices = doc.RootElement.GetProperty("meta").GetProperty("notices");

        var kinds = new List<string>();
        foreach (var notice in notices.EnumerateArray())
            kinds.Add(notice.GetProperty("kind").GetString()!);

        Assert.Contains(NoticeKinds.CountOnlyResponse, kinds);
    }

    [Fact]
    public void NonCountRequest_IsLeftToNormalFormatting()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["filter"] = "Name eq 'admin'"
        };

        Assert.Null(SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "[{\"Id\":1}]", parameters));
        Assert.Null(SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "52", parameters));
        Assert.Null(SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "52", null));
    }

    [Fact]
    public void CountRequest_WithNonScalarBody_FallsThroughToNormalFormatting()
    {
        // A collection that doesn't honor count=true returns an array, not a bare integer;
        // we must not mis-map it into meta.count.
        Assert.Null(SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "[{\"Id\":1}]", CountParams()));
        Assert.Null(SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "{\"value\":1}", CountParams()));
    }

    [Fact]
    public void CountRequest_CsvFormat_IsNotTreatedAsCountEnvelope()
    {
        Assert.Null(SafeguardApiTool.TryBuildCountOnlyEnvelope("csv", "52", CountParams()));
    }

    [Fact]
    public void CountValue_IsCaseInsensitive()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["count"] = "True" };
        Assert.NotNull(SafeguardApiTool.TryBuildCountOnlyEnvelope("json", "3", parameters));
    }
}
