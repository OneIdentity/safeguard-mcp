using System.Collections.Generic;
using System.Text.Json;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class ResponseEnvelopeTests
{
    private static JsonElement Meta(JsonDocument doc) => doc.RootElement.GetProperty("meta");

    [Fact]
    public void Envelope_HasDataAndMeta_OnSuccessWithNoNotices()
    {
        var notices = new List<Notice>();
        var paging = new PagingInfo
        {
            Page = 0,
            Limit = 50,
            Returned = 3,
            LimitSource = "auto",
            More = false,
            Next = null
        };

        var json = ResponseEnvelopeBuilder.BuildJsonEnvelope(
            "[{\"Id\":1},{\"Id\":2},{\"Id\":3}]",
            notices,
            paging,
            truncation: null);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("data").ValueKind);
        Assert.Equal(3, doc.RootElement.GetProperty("data").GetArrayLength());

        var meta = Meta(doc);
        Assert.Equal(JsonValueKind.Array, meta.GetProperty("notices").ValueKind);
        Assert.Equal(0, meta.GetProperty("notices").GetArrayLength());
        Assert.True(meta.TryGetProperty("paging", out var pagingElement));
        Assert.Equal(0, pagingElement.GetProperty("page").GetInt32());
        Assert.Equal(50, pagingElement.GetProperty("limit").GetInt32());
        Assert.Equal(3, pagingElement.GetProperty("returned").GetInt32());
        Assert.Equal("auto", pagingElement.GetProperty("limitSource").GetString());
        Assert.False(pagingElement.GetProperty("more").GetBoolean());
    }

    [Fact]
    public void Envelope_PreservesNonArrayDataPayload()
    {
        var json = ResponseEnvelopeBuilder.BuildJsonEnvelope(
            "{\"Id\":42,\"Name\":\"alice\"}",
            new List<Notice>(),
            paging: null,
            truncation: null);

        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.Equal(42, data.GetProperty("Id").GetInt32());
        Assert.False(Meta(doc).TryGetProperty("paging", out _));
    }

    [Fact]
    public void Envelope_FallsBackToStringForUnparseableBody()
    {
        var json = ResponseEnvelopeBuilder.BuildJsonEnvelope(
            "not json",
            new List<Notice>(),
            paging: null,
            truncation: null);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.String, doc.RootElement.GetProperty("data").ValueKind);
        Assert.Equal("not json", doc.RootElement.GetProperty("data").GetString());
    }

    [Fact]
    public void Envelope_NoticesAreSerializedWithKindMessageAndOptionalSuggestion()
    {
        var notices = new List<Notice>
        {
            new Notice(NoticeKinds.AutoLimitApplied, "Auto-applied limit=50.", "Specify 'limit' to override."),
            new Notice(NoticeKinds.AutoWindowApplied, "Default time window applied."),
        };

        var json = ResponseEnvelopeBuilder.BuildJsonEnvelope("[]", notices, paging: null, truncation: null);

        using var doc = JsonDocument.Parse(json);
        var arr = Meta(doc).GetProperty("notices");
        Assert.Equal(2, arr.GetArrayLength());

        var first = arr[0];
        Assert.Equal(NoticeKinds.AutoLimitApplied, first.GetProperty("kind").GetString());
        Assert.Equal("Auto-applied limit=50.", first.GetProperty("message").GetString());
        Assert.Equal("Specify 'limit' to override.", first.GetProperty("suggestion").GetString());

        var second = arr[1];
        Assert.Equal(NoticeKinds.AutoWindowApplied, second.GetProperty("kind").GetString());
        Assert.False(second.TryGetProperty("suggestion", out _));
    }

    [Fact]
    public void Envelope_TruncationBlockOmittedWhenNotApplied()
    {
        var json = ResponseEnvelopeBuilder.BuildJsonEnvelope(
            "[]",
            new List<Notice>(),
            paging: null,
            truncation: new TruncationInfo { Applied = false });

        using var doc = JsonDocument.Parse(json);
        Assert.False(Meta(doc).TryGetProperty("truncation", out _));
    }

    [Fact]
    public void Envelope_PagingAbsentWhenPagingIsNull()
    {
        var json = ResponseEnvelopeBuilder.BuildJsonEnvelope(
            "{\"value\":1}",
            new List<Notice>(),
            paging: null,
            truncation: null);

        using var doc = JsonDocument.Parse(json);
        Assert.False(Meta(doc).TryGetProperty("paging", out _));
    }

    [Fact]
    public void BuildNextQueryString_PreservesOtherParametersAndBumpsPage()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["filter"] = "Name eq 'admin'",
            ["fields"] = "Id,Name",
            ["limit"] = "50",
            ["page"] = "2"
        };

        var next = ResponseEnvelopeBuilder.BuildNextQueryString(parameters, currentPage: 2, limit: 50);

        Assert.NotNull(next);
        Assert.Contains("page=3", next);
        Assert.Contains("limit=50", next);
        Assert.Contains("filter=", next);
        Assert.Contains("fields=Id%2CName", next);
    }

    [Fact]
    public void BuildNextQueryString_AddsPageAndLimitWhenMissing()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["fields"] = "Id"
        };

        var next = ResponseEnvelopeBuilder.BuildNextQueryString(parameters, currentPage: 0, limit: 25);

        Assert.NotNull(next);
        Assert.Contains("page=1", next);
        Assert.Contains("limit=25", next);
        Assert.Contains("fields=Id", next);
    }

    [Fact]
    public void BuildNextQueryString_ReturnsNullWhenLimitIsUnknown()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Assert.Null(ResponseEnvelopeBuilder.BuildNextQueryString(parameters, 0, null));
    }

    [Fact]
    public void Csv_ReturnsBytesUnchangedWhenNoNotices()
    {
        var csv = "LogTime,EventName\n2026-01-01,Foo\n2026-01-02,Bar\n";
        var result = ResponseEnvelopeBuilder.BuildCsvWithMeta(csv, new List<Notice>(), paging: null, truncation: null);
        Assert.Equal(csv, result);
    }

    [Fact]
    public void Csv_AppendsMetaCommentWhenNoticesArePresent()
    {
        var csv = "LogTime,EventName\n2026-01-01,Foo\n";
        var notices = new List<Notice> { new Notice(NoticeKinds.AutoLimitApplied, "Auto-applied limit=50.") };

        var result = ResponseEnvelopeBuilder.BuildCsvWithMeta(csv, notices, paging: null, truncation: null);

        // CSV header and data are byte-identical at the start of the response.
        Assert.StartsWith(csv, result);
        // Meta is appended on its own line after the data.
        Assert.Contains("\n# Safeguard meta: {", result);
        Assert.Contains(NoticeKinds.AutoLimitApplied, result);
    }
}
