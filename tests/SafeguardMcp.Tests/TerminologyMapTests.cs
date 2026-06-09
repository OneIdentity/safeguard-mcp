using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tests;

public class TerminologyMapTests
{
    [Theory]
    [InlineData("entitlement", new[] { "entitlement", "role", "roles" })]
    [InlineData("Entitlement", new[] { "entitlement", "role", "roles" })]
    [InlineData("password profile", new[] { "password profile", "change profile", "passwordprofiles" })]
    [InlineData("privileged access", new[] { "privileged access", "access request", "accessrequests" })]
    [InlineData("pam", new[] { "pam", "access request", "accessrequests" })]
    public void ExpandSearchTerms_ExpandsKnownAliases(string input, string[] expectedContains)
    {
        var expanded = TerminologyMap.ExpandSearchTerms(input);

        foreach (var term in expectedContains)
            Assert.Contains(term, expanded, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExpandSearchTerms_UnknownTerm_ReturnsOriginal()
    {
        var expanded = TerminologyMap.ExpandSearchTerms("xyzzy_not_a_term");
        Assert.Single(expanded);
        Assert.Equal("xyzzy_not_a_term", expanded[0]);
    }

    [Theory]
    [InlineData("entitlement")]
    [InlineData("role")]
    [InlineData("managed system")]
    [InlineData("asset")]
    [InlineData("partition")]
    public void MatchesAny_FindsKnownTerms(string term)
    {
        var expanded = TerminologyMap.ExpandSearchTerms(term);
        // When expanded terms are searched against a text containing the API endpoint name,
        // at least one should match
        var sampleText = "/v4/Roles /v4/Assets /v4/AssetPartitions /v4/AssetAccounts";
        Assert.True(TerminologyMap.MatchesAny(expanded, sampleText));
    }

    [Fact]
    public void MatchesAny_UnknownTerm_ReturnsFalse()
    {
        var expanded = TerminologyMap.ExpandSearchTerms("completely_unknown_term_xyz");
        var sampleText = "/v4/Users /v4/Assets";
        Assert.False(TerminologyMap.MatchesAny(expanded, sampleText));
    }
}
