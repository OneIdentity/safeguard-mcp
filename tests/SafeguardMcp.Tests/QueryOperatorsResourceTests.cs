using System.Linq;
using System.Text.RegularExpressions;
using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tests;

public class QueryOperatorsResourceTests
{
    // Authoritative binary-operator regex copied verbatim from PangaeaAppliance
    // src/Data/Middleware/Query/QueryFilterUtils.cs:14. If the appliance adds or
    // removes a binary operator, update both this constant AND
    // Catalog/Resources/query-operators.json. The tests below will fail until
    // they agree, which is the entire point of the drift detector.
    private const string ApplianceBinaryRegex =
        @" (ieq|eq|ne|gt|ge|lt|le|contains|icontains|in|iew|ew|isw|sw) ";

    private static readonly string[] ApplianceBinaryOperators =
    {
        "eq", "ne", "gt", "ge", "lt", "le",
        "contains", "icontains", "ieq",
        "sw", "isw", "ew", "iew",
        "in"
    };

    [Fact]
    public void Json_LoadsFromEmbeddedResource()
    {
        var json = QueryOperatorsResource.GetJson();
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("\"binary\"", json);
        Assert.Contains("\"unary\"", json);
        Assert.Contains("\"literals\"", json);
    }

    [Fact]
    public void Json_DoesNotMentionNotInOperator()
    {
        var json = QueryOperatorsResource.GetJson();
        // The JSON may reference `not_in` only in the `unsupported` list. Confirm
        // it never appears as a usable operator entry.
        using var doc = QueryOperatorsResource.Parse();
        var binary = doc.RootElement.GetProperty("binary");
        foreach (var entry in binary.EnumerateArray())
        {
            var op = entry.GetProperty("op").GetString();
            Assert.NotEqual("not_in", op);
            Assert.NotEqual("not in", op);
        }
        var unary = doc.RootElement.GetProperty("unary");
        foreach (var entry in unary.EnumerateArray())
        {
            Assert.NotEqual("not_in", entry.GetProperty("op").GetString());
        }
    }

    [Fact]
    public void Json_BinaryOperators_MatchAppliance()
    {
        using var doc = QueryOperatorsResource.Parse();
        var binary = doc.RootElement.GetProperty("binary");
        var ops = binary.EnumerateArray()
            .Select(e => e.GetProperty("op").GetString()!)
            .ToArray();

        Assert.Equal(
            ApplianceBinaryOperators.OrderBy(s => s),
            ops.OrderBy(s => s));
    }

    [Fact]
    public void Json_BinaryOperators_AreSubsetOfApplianceRegex()
    {
        // Drift detector: every operator in the JSON must be matchable by the
        // appliance regex. Catches typos like "starts_with" before they ship.
        var regex = new Regex(ApplianceBinaryRegex, RegexOptions.IgnoreCase);
        using var doc = QueryOperatorsResource.Parse();
        var binary = doc.RootElement.GetProperty("binary");
        foreach (var entry in binary.EnumerateArray())
        {
            var op = entry.GetProperty("op").GetString()!;
            // The appliance regex requires a leading and trailing space.
            Assert.True(regex.IsMatch($" {op} "),
                $"Binary operator '{op}' from query-operators.json is not matched " +
                $"by the appliance regex {ApplianceBinaryRegex}.");
        }
    }

    [Fact]
    public void Json_EveryBinaryOperator_HasExample()
    {
        using var doc = QueryOperatorsResource.Parse();
        var binary = doc.RootElement.GetProperty("binary");
        foreach (var entry in binary.EnumerateArray())
        {
            var op = entry.GetProperty("op").GetString()!;
            var example = entry.GetProperty("example").GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(example),
                $"Operator '{op}' is missing an example.");
            Assert.Contains($" {op} ", $" {example} ".ToLowerInvariant());
        }
    }

    [Fact]
    public void Json_UnaryIncludesNot()
    {
        using var doc = QueryOperatorsResource.Parse();
        var unary = doc.RootElement.GetProperty("unary");
        var ops = unary.EnumerateArray()
            .Select(e => e.GetProperty("op").GetString()!)
            .ToArray();
        Assert.Contains("not", ops);
    }

    [Fact]
    public void Json_LogicalIncludesAndOr()
    {
        using var doc = QueryOperatorsResource.Parse();
        var logical = doc.RootElement.GetProperty("logical");
        var ops = logical.EnumerateArray()
            .Select(e => e.GetProperty("op").GetString()!)
            .ToArray();
        Assert.Contains("and", ops);
        Assert.Contains("or", ops);
    }

    [Fact]
    public void Json_LiteralsIncludeNullAndList()
    {
        using var doc = QueryOperatorsResource.Parse();
        var literals = doc.RootElement.GetProperty("literals");
        var kinds = literals.EnumerateArray()
            .Select(e => e.GetProperty("kind").GetString()!)
            .ToArray();
        Assert.Contains("null", kinds);
        Assert.Contains("list", kinds);
        Assert.Contains("string", kinds);
    }

    [Fact]
    public void Json_RulesFlagOperatorCaseInsensitivity()
    {
        using var doc = QueryOperatorsResource.Parse();
        var rules = doc.RootElement.GetProperty("rules");
        Assert.True(rules.GetProperty("operators_case_insensitive").GetBoolean());
        Assert.True(rules.GetProperty("string_literals_case_sensitive").GetBoolean());
    }

    [Fact]
    public void Json_UnsupportedListCallsOutNotIn()
    {
        using var doc = QueryOperatorsResource.Parse();
        var unsupported = doc.RootElement.GetProperty("rules").GetProperty("unsupported");
        var tokens = unsupported.EnumerateArray()
            .Select(e => e.GetProperty("token").GetString()!)
            .ToArray();
        Assert.Contains("not_in", tokens);
        Assert.Contains("not in", tokens);
        Assert.Contains("like", tokens);
    }

    [Fact]
    public void Parse_ReturnsFreshDocumentEachCall()
    {
        // Disposing one parsed document must not affect a subsequent caller.
        using (var first = QueryOperatorsResource.Parse())
        {
            _ = first.RootElement.GetProperty("binary");
        }
        using var second = QueryOperatorsResource.Parse();
        Assert.True(second.RootElement.TryGetProperty("binary", out _));
    }
}
