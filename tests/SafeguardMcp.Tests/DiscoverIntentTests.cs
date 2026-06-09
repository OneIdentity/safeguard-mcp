using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Tests for Safeguard_Discover's verb-intent synonyms and recipe ranking.
/// Exercises FormatDiscovery directly with a small ApiEndpoint fixture so we
/// don't need the full DI graph; recipe data is loaded from the real embedded
/// resources via RecipeIndex.
/// </summary>
public class DiscoverIntentTests
{
    private static ApiEndpoint[] BuildFixture()
    {
        // Representative endpoints for ranking tests. Keep this small —
        // the goal is to exercise ranking, not duplicate the live catalog.
        return
        [
            new ApiEndpoint("Core", "GET",    "/v4/Users",                              "Gets a list of users", "", false),
            new ApiEndpoint("Core", "GET",    "/v4/Users/{id}",                         "Gets a single user", "", false),
            new ApiEndpoint("Core", "GET",    "/v4/Roles",                              "Gets a list of entitlements", "", false),
            new ApiEndpoint("Core", "GET",    "/v4/Assets",                             "Gets a list of assets", "", false),
            new ApiEndpoint("Core", "PUT",    "/v4/Assets/{id}",                        "Updates an asset", "", true),
            new ApiEndpoint("Core", "GET",    "/v4/AssetPartitions",                    "Gets a list of asset partitions", "", false),
            new ApiEndpoint("Core", "GET",    "/v4/AssetAccounts",                      "Gets a list of accounts across all partitions", "", false),
            new ApiEndpoint("Core", "PUT",    "/v4/AssetAccounts/{id}",                 "Updates an existing asset account", "", true),
            new ApiEndpoint("Core", "GET",    "/v4/Deleted",                            "Gets a list of delete types", "", false),
            new ApiEndpoint("Core", "GET",    "/v4/Deleted/AssetAccounts",              "Gets a list of deleted asset accounts", "", false),
            new ApiEndpoint("Core", "GET",    "/v4/Deleted/Assets",                     "Gets a list of deleted assets", "", false),
            new ApiEndpoint("Core", "POST",   "/v4/Deleted/AssetAccounts/{id}/Restore", "Restore a single deleted asset account entity", "", false),
            new ApiEndpoint("Core", "GET",    "/v4/AuditLog/Logins",                    "Gets a set of audit log login entries", "", false),
            new ApiEndpoint("Core", "GET",    "/v4/AuditLog/ObjectChanges",             "Gets a set of ObjectChangeLog entries", "", false),
            new ApiEndpoint("Core", "GET",    "/v4/AccessRequests",                     "Gets a list of access requests", "", false),
        ];
    }

    [Fact]
    public void Discover_MoveSearch_RanksPartitionReassignmentRecipeFirst()
    {
        var result = SafeguardApiTool.FormatDiscovery(BuildFixture(), null, "move", null);

        Assert.Contains("Recipes that match", result);

        // The partition-reassignment recipe id must appear before any endpoint listing.
        var recipeIdx = result.IndexOf("partition-reassignment", StringComparison.Ordinal);
        Assert.True(recipeIdx >= 0, "partition-reassignment recipe should be listed");

        // Strong callout fires because "move" hits the recipe's tag list directly.
        Assert.Contains("Strong recipe match: partition-reassignment", result);
    }

    [Fact]
    public void Discover_DeletedSearch_ShowsDeletedEndpointsAndAuditRecipeFirst()
    {
        var result = SafeguardApiTool.FormatDiscovery(BuildFixture(), null, "deleted", null);

        // Recipe (audit-history style) is listed.
        Assert.Contains("deleted-objects-recovery", result);

        // The recipe block is rendered above the endpoint table.
        var recipeIdx = result.IndexOf("deleted-objects-recovery", StringComparison.Ordinal);
        var endpointIdx = result.IndexOf("/v4/Deleted/AssetAccounts", StringComparison.Ordinal);
        Assert.True(recipeIdx >= 0 && endpointIdx >= 0);
        Assert.True(recipeIdx < endpointIdx,
            "Recipe block must appear before raw Deleted/* endpoints");

        // Deleted/* endpoints are still in the output (raw endpoints not suppressed).
        Assert.Contains("/v4/Deleted/AssetAccounts", result);
        Assert.Contains("/v4/Deleted/Assets", result);
    }

    [Fact]
    public void Discover_RecentActivitySearch_RanksUserAuditRecipeAboveRawEndpoints()
    {
        var result = SafeguardApiTool.FormatDiscovery(BuildFixture(), null, "recent activity", null);

        // user-audit recipe surfaces because it carries "recent" and "activity" in its tags.
        Assert.Contains("user-audit", result);

        // /v4/AuditLog/Logins is in the endpoint listing.
        Assert.Contains("/v4/AuditLog/Logins", result);

        // Recipe block precedes the audit-log endpoint.
        var recipeIdx = result.IndexOf("user-audit", StringComparison.Ordinal);
        var loginsIdx = result.IndexOf("/v4/AuditLog/Logins", StringComparison.Ordinal);
        Assert.True(recipeIdx < loginsIdx,
            "user-audit recipe must rank above the raw AuditLog/Logins endpoint");
    }

    [Fact]
    public void Discover_PlainEndpointSearch_FallsBackToKeywordMatchWithoutRecipeBlock()
    {
        // "users" matches the Users endpoint but no recipe tag — recipe block
        // should not be prepended (keeps the no-noise contract on common searches).
        var result = SafeguardApiTool.FormatDiscovery(BuildFixture(), null, "users", null);

        Assert.Contains("/v4/Users", result);
        Assert.DoesNotContain("Recipes that match", result);
        Assert.DoesNotContain("Strong recipe match", result);
    }

    [Fact]
    public void Discover_SessionSearch_RecipeRankedAboveRawSessionsEndpoints()
    {
        // Both an endpoint and the session-access-request recipe match "session";
        // the recipe should appear first thanks to the recipe block ordering.
        var endpoints = new[]
        {
            new ApiEndpoint("Core", "GET", "/v4/SessionRecordings", "Gets a list of session recordings", "", false),
        };
        var result = SafeguardApiTool.FormatDiscovery(endpoints, null, "session", null);

        var recipeIdx = result.IndexOf("session-access-request", StringComparison.Ordinal);
        var endpointIdx = result.IndexOf("/v4/SessionRecordings", StringComparison.Ordinal);
        Assert.True(recipeIdx >= 0, "session-access-request recipe should be listed");
        Assert.True(endpointIdx >= 0);
        Assert.True(recipeIdx < endpointIdx,
            "Recipe must rank above raw matching endpoint when both match the search term");
    }

    [Fact]
    public void Discover_NoMatchingRecipeOrEndpoint_ReturnsExistingHint()
    {
        var result = SafeguardApiTool.FormatDiscovery(BuildFixture(), null, "xyzzy_no_such_term", null);

        Assert.Contains("No endpoints matched the search criteria", result);
        Assert.DoesNotContain("Recipes that match", result);
    }

    [Fact]
    public void Discover_EmptySearch_DoesNotEmitRecipeBlock()
    {
        var result = SafeguardApiTool.FormatDiscovery(BuildFixture(), null, null, null);

        Assert.DoesNotContain("Recipes that match", result);
        Assert.Contains("/v4/Users", result);
    }
}
