using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Locks in the CSV output hygiene contract:
/// when a consumer persists the body returned by <see cref="ResponseEnvelopeBuilder.BuildCsvWithMeta"/>
/// to a file, the file must:
///   * begin with the header row's first character (no UTF-8 BOM, no blank line, no preamble);
///   * preserve every input row whole (no truncated rows);
///   * be parseable by row-oriented CSV consumers (PowerShell Import-Csv, CsvHelper, etc.) on the first try.
///
/// Source-of-truth note: PangaeaAppliance's CsvOutputFormatter registers
/// <c>SupportedEncodings.Add(Encoding.UTF8)</c> (not <c>new UTF8Encoding(true)</c>),
/// and ASP.NET Core's default response StreamWriter does not emit a BOM. So MCP
/// must NOT add one either; if these tests ever fail because the result starts
/// with 0xEF 0xBB 0xBF, something has regressed and Import-Csv on Windows will
/// silently corrupt the first column header.
/// </summary>
public class CsvOutputHygieneTests
{
    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

    private const string HeaderRow = "LogTime,EventName,UserName";
    private const string SampleCsv =
        "LogTime,EventName,UserName\n"
        + "2026-01-01T00:00:00Z,Login,alice\n"
        + "2026-01-02T00:00:00Z,Logout,bob\n"
        + "2026-01-03T00:00:00Z,PasswordChange,carol\n";

    [Fact]
    public void SavedCsv_NoNotices_StartsWithHeaderByte_NoBom()
    {
        var result = ResponseEnvelopeBuilder.BuildCsvWithMeta(
            SampleCsv, new List<Notice>(), paging: null, truncation: null);

        var bytes = Encoding.UTF8.GetBytes(result);

        Assert.False(StartsWithBom(bytes), "CSV body must not begin with a UTF-8 BOM.");
        Assert.Equal((byte)'L', bytes[0]);
        Assert.StartsWith(HeaderRow + "\n", result);
    }

    [Fact]
    public void SavedCsv_WithNotices_StartsWithHeaderByte_NoBom_NoPreamble()
    {
        var notices = new List<Notice>
        {
            new Notice(NoticeKinds.AutoLimitApplied, "Auto-applied limit=50."),
        };
        var paging = new PagingInfo
        {
            Page = 0,
            Limit = 50,
            Returned = -1,
            LimitSource = "auto",
            More = false,
            Next = null,
        };

        var result = ResponseEnvelopeBuilder.BuildCsvWithMeta(
            SampleCsv, notices, paging, truncation: null);

        var bytes = Encoding.UTF8.GetBytes(result);

        Assert.False(StartsWithBom(bytes), "CSV body must not begin with a UTF-8 BOM, even when meta is appended.");
        Assert.Equal((byte)'L', bytes[0]);
        Assert.StartsWith(HeaderRow + "\n", result);
        Assert.DoesNotContain("\n\n" + HeaderRow, result);
    }

    [Fact]
    public void SavedCsv_RowsAreWhole_AllInputRecordsPreservedAndImportable()
    {
        var notices = new List<Notice>
        {
            new Notice(NoticeKinds.AutoLimitApplied, "Auto-applied limit=50."),
        };

        var result = ResponseEnvelopeBuilder.BuildCsvWithMeta(
            SampleCsv, notices, paging: null, truncation: null);

        Assert.StartsWith(SampleCsv, result);

        // Simulate the row-oriented parse a consumer would do (e.g. PowerShell Import-Csv).
        // Comment lines starting with '#' are filtered the way Import-Csv -Header (or any
        // sensible CSV reader configured to skip comments) would handle the trailing meta.
        var dataLines = result
            .Split('\n')
            .Where(line => line.Length > 0 && !line.StartsWith("#"))
            .ToList();

        Assert.Equal(4, dataLines.Count); // 1 header + 3 records
        Assert.Equal(HeaderRow, dataLines[0]);

        var headerFieldCount = HeaderRow.Split(',').Length;
        for (var i = 1; i < dataLines.Count; i++)
        {
            var fields = dataLines[i].Split(',');
            Assert.Equal(headerFieldCount, fields.Length);
            Assert.False(string.IsNullOrWhiteSpace(fields[0]),
                $"Row {i} appears truncated (empty first field): '{dataLines[i]}'");
        }
    }

    [Fact]
    public void SavedCsv_RoundTripsThroughDiskWithoutBomOrBlankLine()
    {
        var result = ResponseEnvelopeBuilder.BuildCsvWithMeta(
            SampleCsv, new List<Notice>(), paging: null, truncation: null);

        var path = Path.Combine(Path.GetTempPath(), $"safeguard-mcp-csv-hygiene-{System.Guid.NewGuid():N}.csv");
        try
        {
            // Mirror what a consumer (or the future CsvSaved code path) is expected to do:
            // write the body as UTF-8 with no BOM. The encoding here is the default UTF-8
            // (no preamble) — matching the appliance's BOM-less emission.
            File.WriteAllText(path, result, new UTF8Encoding(false));

            var raw = File.ReadAllBytes(path);
            Assert.False(StartsWithBom(raw), "Saved CSV file must not contain a UTF-8 BOM.");
            Assert.Equal((byte)'L', raw[0]);

            // Read first line back (defensive against \r\n on Windows even though we wrote \n).
            using var reader = new StreamReader(path, new UTF8Encoding(false));
            var firstLine = reader.ReadLine();
            Assert.Equal(HeaderRow, firstLine);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void SavedCsv_AppliancePreservedQuotedFieldWithEmbeddedNewline_RemainsWhole()
    {
        // Matches PangaeaAppliance CsvSerializer behavior: values containing , \n \r or "
        // are wrapped in double quotes and embedded quotes are doubled. The MCP envelope
        // must not split such a record across lines or otherwise corrupt it.
        var quotedCsv =
            "Id,Reason\n"
            + "1,\"line one\nline two\"\n"
            + "2,\"He said \"\"hi\"\"\"\n";

        var notices = new List<Notice>
        {
            new Notice(NoticeKinds.AutoLimitApplied, "Auto-applied limit=50."),
        };

        var result = ResponseEnvelopeBuilder.BuildCsvWithMeta(
            quotedCsv, notices, paging: null, truncation: null);

        Assert.StartsWith(quotedCsv, result);
        // The trailing meta line must be on its own line AFTER the CSV body, not merged into it.
        var metaIndex = result.IndexOf("# Safeguard meta:", System.StringComparison.Ordinal);
        Assert.True(metaIndex > quotedCsv.Length - 1,
            "Meta comment must appear after the full CSV body, not inside it.");
        Assert.Equal('\n', result[metaIndex - 1]);
    }

    private static bool StartsWithBom(byte[] bytes)
    {
        if (bytes.Length < Utf8Bom.Length) return false;
        for (var i = 0; i < Utf8Bom.Length; i++)
        {
            if (bytes[i] != Utf8Bom[i]) return false;
        }
        return true;
    }
}
