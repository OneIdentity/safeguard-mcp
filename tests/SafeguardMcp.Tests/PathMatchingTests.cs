using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class PathMatchingTests
{
    [Theory]
    [InlineData("/v4/Users", "/v4/Users", true)]
    [InlineData("/v4/Users", "/v4/users", true)]
    [InlineData("/v4/Users/{id}", "/v4/Users/123", true)]
    [InlineData("/v4/Users/{id}", "/v4/Users/abc-def-ghi", true)]
    [InlineData("/v4/Roles/{roleId}/Members", "/v4/Roles/42/Members", true)]
    [InlineData("/v4/AssetPartitions/{id}/Profiles/{profileId}", "/v4/AssetPartitions/1/Profiles/5", true)]
    [InlineData("/v4/Users", "/v4/Assets", false)]
    [InlineData("/v4/Users/{id}", "/v4/Users", false)]
    [InlineData("/v4/Users", "/v4/Users/123", false)]
    [InlineData("/v4/Roles/{id}/Members", "/v4/Roles/1/Members/Add", false)]
    public void PathsMatch_VariousCases(string catalogPath, string actualPath, bool expected)
    {
        Assert.Equal(expected, ApiToolHelpers.PathsMatch(catalogPath, actualPath));
    }

    [Theory]
    [InlineData("/v4/Users?filter=Name eq 'test'", "/v4/Users")]
    [InlineData("v4/Users", "/v4/Users")]
    [InlineData("  /v4/Users  ", "/v4/Users")]
    [InlineData("/v4/Assets?fields=Id,Name&limit=50", "/v4/Assets")]
    public void NormalizePath_StripsQueryAndAddsLeadingSlash(string input, string expected)
    {
        Assert.Equal(expected, ApiToolHelpers.NormalizePath(input));
    }

    [Theory]
    [InlineData("/v4/Users/{id}", true)]
    [InlineData("/v4/Roles/{roleId}/Members/{memberId}", true)]
    [InlineData("/v4/Users", false)]
    [InlineData("/v4/Roles/{roleId}/Members", false)]
    public void EndsWithPlaceholder_DetectsCorrectly(string path, bool expected)
    {
        Assert.Equal(expected, ApiToolHelpers.EndsWithPlaceholder(path));
    }

    [Theory]
    [InlineData("{id}", true)]
    [InlineData("{roleId}", true)]
    [InlineData("Users", false)]
    [InlineData("{", false)]
    [InlineData("}", false)]
    [InlineData("id}", false)]
    public void IsPlaceholder_DetectsCorrectly(string segment, bool expected)
    {
        Assert.Equal(expected, ApiToolHelpers.IsPlaceholder(segment));
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("999999999", true)]
    [InlineData("a1b2c3d4-e5f6-7890-abcd-ef1234567890", true)]
    [InlineData("{id}", true)]
    [InlineData("", true)]
    [InlineData("Users", false)]
    [InlineData("Members", false)]
    [InlineData("abc", false)]
    public void LooksLikeId_IdentifiesIds(string segment, bool expected)
    {
        Assert.Equal(expected, ApiToolHelpers.LooksLikeId(segment));
    }
}
