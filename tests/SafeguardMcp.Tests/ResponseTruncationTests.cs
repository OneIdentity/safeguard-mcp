using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class ResponseTruncationTests
{
    [Fact]
    public void TryTruncateJsonArray_ArrayWithinLimit_ReturnsFalse()
    {
        var json = "[{\"Id\":1},{\"Id\":2},{\"Id\":3}]";
        var result = ApiToolHelpers.TryTruncateJsonArray(json, 5, out var truncated, out var total);

        Assert.False(result);
        Assert.Null(truncated);
        Assert.Equal(3, total);
    }

    [Fact]
    public void TryTruncateJsonArray_ArrayExceedsLimit_TruncatesAndReportsTotal()
    {
        var items = Enumerable.Range(1, 150).Select(i => $"{{\"Id\":{i}}}");
        var json = "[" + string.Join(",", items) + "]";

        var result = ApiToolHelpers.TryTruncateJsonArray(json, 100, out var truncated, out var total);

        Assert.True(result);
        Assert.Equal(150, total);
        Assert.NotNull(truncated);

        // Verify truncated output has exactly 100 items
        using var doc = System.Text.Json.JsonDocument.Parse(truncated);
        Assert.Equal(100, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void TryTruncateJsonArray_NotAnArray_ReturnsFalse()
    {
        var json = "{\"Name\":\"test\",\"Id\":1}";
        var result = ApiToolHelpers.TryTruncateJsonArray(json, 10, out _, out _);
        Assert.False(result);
    }

    [Fact]
    public void TryTruncateJsonArray_EmptyString_ReturnsFalse()
    {
        Assert.False(ApiToolHelpers.TryTruncateJsonArray("", 10, out _, out _));
        Assert.False(ApiToolHelpers.TryTruncateJsonArray(null, 10, out _, out _));
    }

    [Fact]
    public void TryTruncateJsonArray_InvalidJson_ReturnsFalse()
    {
        var result = ApiToolHelpers.TryTruncateJsonArray("not json at all", 10, out _, out _);
        Assert.False(result);
    }

    [Fact]
    public void TryTruncateJsonArray_EmptyArray_ReturnsFalse()
    {
        var result = ApiToolHelpers.TryTruncateJsonArray("[]", 10, out _, out var total);
        Assert.False(result);
        Assert.Equal(0, total);
    }

    [Fact]
    public void TryTruncateJsonArray_ExactlyAtLimit_ReturnsFalse()
    {
        var items = Enumerable.Range(1, 10).Select(i => $"{{\"Id\":{i}}}");
        var json = "[" + string.Join(",", items) + "]";

        var result = ApiToolHelpers.TryTruncateJsonArray(json, 10, out _, out var total);
        Assert.False(result);
        Assert.Equal(10, total);
    }
}
