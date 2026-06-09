using System.Linq;
using System.Text.Json;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class WholeRecordTruncationTests
{
    [Fact]
    public void WithinBudget_ReturnsArrayUnchanged()
    {
        var body = "[{\"Id\":1},{\"Id\":2}]";
        var outcome = ApiToolHelpers.TryTruncateJsonArrayWithBudget(
            body, maxItems: 100, maxBytes: 1000, out var result, out var total, out var kept);

        Assert.Equal(ApiToolHelpers.TruncationOutcome.WithinBudget, outcome);
        Assert.Equal(2, total);
        Assert.Equal(2, kept);
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void NonArrayBody_ReturnsNotArray()
    {
        var body = "{\"Id\":1}";
        var outcome = ApiToolHelpers.TryTruncateJsonArrayWithBudget(
            body, maxItems: 100, maxBytes: 1000, out var result, out _, out _);
        Assert.Equal(ApiToolHelpers.TruncationOutcome.NotArray, outcome);
        Assert.Null(result);
    }

    [Fact]
    public void ExceedsItemCap_DropsTrailingRecords_OutputIsValidJsonArray()
    {
        var items = Enumerable.Range(1, 150).Select(i => $"{{\"Id\":{i}}}");
        var body = "[" + string.Join(",", items) + "]";

        var outcome = ApiToolHelpers.TryTruncateJsonArrayWithBudget(
            body, maxItems: 100, maxBytes: 1_000_000, out var result, out var total, out var kept);

        Assert.Equal(ApiToolHelpers.TruncationOutcome.RecordsDropped, outcome);
        Assert.Equal(150, total);
        Assert.Equal(100, kept);
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(100, doc.RootElement.GetArrayLength());
        Assert.Equal(1, doc.RootElement[0].GetProperty("Id").GetInt32());
        Assert.Equal(100, doc.RootElement[99].GetProperty("Id").GetInt32());
    }

    [Fact]
    public void ExceedsByteBudget_DropsRecordsFromTail_NeverCutsMidObject()
    {
        // 50 records, each ~200 chars of bulk; the byte budget will force record drops.
        var bulk = new string('x', 180);
        var items = Enumerable.Range(1, 50).Select(i => $"{{\"Id\":{i},\"Bulk\":\"{bulk}\"}}");
        var body = "[" + string.Join(",", items) + "]";

        var outcome = ApiToolHelpers.TryTruncateJsonArrayWithBudget(
            body, maxItems: 1000, maxBytes: 2000, out var result, out var total, out var kept);

        Assert.Equal(ApiToolHelpers.TruncationOutcome.RecordsDropped, outcome);
        Assert.Equal(50, total);
        Assert.True(kept < 50);
        Assert.True(kept >= 1);

        // Result is valid JSON and a proper array — no mid-object cut.
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(kept, doc.RootElement.GetArrayLength());
        Assert.True(result.Length <= 2000 || kept == 1);
    }

    [Fact]
    public void SingleRecordExceedsBudget_KeptIntact_ReportsRecordTooLarge()
    {
        var hugeBulk = new string('x', 5000);
        var body = $"[{{\"Id\":1,\"OldValue\":\"{hugeBulk}\"}}]";

        var outcome = ApiToolHelpers.TryTruncateJsonArrayWithBudget(
            body, maxItems: 100, maxBytes: 2000, out var result, out var total, out var kept);

        Assert.Equal(ApiToolHelpers.TruncationOutcome.RecordTooLarge, outcome);
        Assert.Equal(1, total);
        Assert.Equal(1, kept);
        // The record is preserved intact; JSON remains parseable.
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
        Assert.Equal(hugeBulk, doc.RootElement[0].GetProperty("OldValue").GetString());
    }

    [Fact]
    public void EmptyArray_ReturnsWithinBudget()
    {
        var outcome = ApiToolHelpers.TryTruncateJsonArrayWithBudget(
            "[]", maxItems: 100, maxBytes: 1000, out var result, out var total, out var kept);
        Assert.Equal(ApiToolHelpers.TruncationOutcome.WithinBudget, outcome);
        Assert.Equal(0, total);
        Assert.Equal(0, kept);
        Assert.Equal("[]", result);
    }

    [Fact]
    public void InvalidJson_ReturnsNotArray()
    {
        var outcome = ApiToolHelpers.TryTruncateJsonArrayWithBudget(
            "not json", maxItems: 100, maxBytes: 1000, out var result, out _, out _);
        Assert.Equal(ApiToolHelpers.TruncationOutcome.NotArray, outcome);
        Assert.Null(result);
    }
}
