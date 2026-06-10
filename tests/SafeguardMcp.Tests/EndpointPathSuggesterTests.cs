using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

public class EndpointPathSuggesterTests
{
    private static ApiEndpoint Ep(string method, string path)
        => new(service: "Core", method: method, path: path, summary: string.Empty,
               @params: string.Empty, hasBody: false);

    private static readonly ApiEndpoint[] DefaultCatalog = new[]
    {
        Ep("GET",  "/v4/Assets"),
        Ep("POST", "/v4/Assets"),
        Ep("GET",  "/v4/Assets/{id}"),
        Ep("GET",  "/v4/Accounts"),
        Ep("GET",  "/v4/AssetAccounts"),
        Ep("GET",  "/v4/AssetPartitions/Tags"),
        Ep("GET",  "/v4/AssetPartitions/{id}/Tags"),
        Ep("POST", "/v4/AssetPartitions/{id}/Tags"),
        Ep("GET",  "/v4/Users")
    };

    [Fact]
    public void Suggest_LeafExactMatch_RanksAssetPartitionsTagsFirst()
    {
        var suggestions = EndpointPathSuggester.Suggest("GET", "/v4/Tags", DefaultCatalog);
        Assert.NotEmpty(suggestions);
        Assert.Equal("/v4/AssetPartitions/Tags", suggestions[0]);
        // The {id}-scoped sibling shows up too since it shares the leaf.
        Assert.Contains("/v4/AssetPartitions/{id}/Tags", suggestions);
    }

    [Fact]
    public void Suggest_PluralizationSlip_FindsAccounts()
    {
        var suggestions = EndpointPathSuggester.Suggest("GET", "/v4/Account", DefaultCatalog);
        Assert.NotEmpty(suggestions);
        Assert.Equal("/v4/Accounts", suggestions[0]);
    }

    [Fact]
    public void Suggest_NoCandidate_ReturnsEmpty()
    {
        var suggestions = EndpointPathSuggester.Suggest("GET", "/v4/CompletelyMadeUp", DefaultCatalog);
        Assert.Empty(suggestions);
    }

    [Fact]
    public void Suggest_MethodAware_PostTagsSkipsGetOnlyPaths()
    {
        var catalog = new[]
        {
            Ep("GET", "/v4/AssetPartitions/Tags"),       // GET-only — must be skipped
            Ep("POST", "/v4/AssetPartitions/{id}/Tags")  // POST exists — may suggest
        };
        var suggestions = EndpointPathSuggester.Suggest("POST", "/v4/Tags", catalog);
        Assert.DoesNotContain("/v4/AssetPartitions/Tags", suggestions);
    }

    [Fact]
    public void Suggest_CapsAtThree()
    {
        var catalog = new[]
        {
            Ep("GET", "/v4/A/Tags"),
            Ep("GET", "/v4/B/Tags"),
            Ep("GET", "/v4/C/Tags"),
            Ep("GET", "/v4/D/Tags"),
            Ep("GET", "/v4/E/Tags")
        };
        var suggestions = EndpointPathSuggester.Suggest("GET", "/v4/Tags", catalog);
        Assert.Equal(3, suggestions.Length);
    }

    [Fact]
    public void Suggest_DedupesIdenticalPaths()
    {
        var catalog = new[]
        {
            Ep("GET", "/v4/AssetPartitions/Tags"),
            Ep("GET", "/v4/AssetPartitions/Tags") // duplicate (rare but defensive)
        };
        var suggestions = EndpointPathSuggester.Suggest("GET", "/v4/Tags", catalog);
        Assert.Single(suggestions);
    }

    [Fact]
    public void Suggest_EmptyCatalog_ReturnsEmpty()
    {
        var suggestions = EndpointPathSuggester.Suggest("GET", "/v4/Tags", Array.Empty<ApiEndpoint>());
        Assert.Empty(suggestions);
    }

    // --- ApiToolHelpers.GetErrorHint integration: 404 split & 405 branch ---

    [Fact]
    public void GetErrorHint_404_PathDoesNotExist_SurfacesSuggestionsInline()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/Tags");
        var hint = ApiToolHelpers.GetErrorHint(
            statusCode: 404,
            apiMessage: null,
            hasModelState: false,
            ctx,
            paths: Array.Empty<ApiSchemaPropertyPath>(),
            requestPath: "/v4/Tags",
            templateMatched: false,
            pathSuggestions: new[] { "/v4/AssetPartitions/Tags" },
            supportedMethods: null);

        Assert.Contains("No endpoint at /v4/Tags", hint);
        Assert.Contains("/v4/AssetPartitions/Tags", hint);
        Assert.Contains("Did you mean", hint);
    }

    [Fact]
    public void GetErrorHint_404_PathDoesNotExist_NoSuggestions_FallsBackToDiscover()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/CompletelyMadeUp");
        var hint = ApiToolHelpers.GetErrorHint(
            statusCode: 404,
            apiMessage: null,
            hasModelState: false,
            ctx,
            paths: Array.Empty<ApiSchemaPropertyPath>(),
            requestPath: "/v4/CompletelyMadeUp",
            templateMatched: false,
            pathSuggestions: Array.Empty<string>(),
            supportedMethods: null);

        Assert.Contains("No endpoint at /v4/CompletelyMadeUp", hint);
        Assert.Contains("Safeguard_Discover", hint);
        Assert.DoesNotContain("Did you mean", hint);
    }

    [Fact]
    public void GetErrorHint_404_IdNotFound_KeepsExistingWording()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/Assets/{id}");
        var hint = ApiToolHelpers.GetErrorHint(
            statusCode: 404,
            apiMessage: null,
            hasModelState: false,
            ctx,
            paths: Array.Empty<ApiSchemaPropertyPath>(),
            requestPath: "/v4/Assets/99999",
            templateMatched: true,
            pathSuggestions: null,
            supportedMethods: null);

        // The catalog matched the template, so we keep the original
        // "Verify the ID exists" hint rather than nudging at the path.
        Assert.Equal("Resource not found. Verify the ID exists using a GET call.", hint);
    }

    [Fact]
    public void GetErrorHint_405_ListsSupportedMethodsFromCatalog()
    {
        var ctx = new ErrorContext("Core", "DELETE", "/v4/Assets");
        var hint = ApiToolHelpers.GetErrorHint(
            statusCode: 405,
            apiMessage: null,
            hasModelState: false,
            ctx,
            paths: Array.Empty<ApiSchemaPropertyPath>(),
            requestPath: "/v4/Assets",
            templateMatched: true,
            pathSuggestions: null,
            supportedMethods: new[] { "GET", "POST" });

        Assert.Contains("Method DELETE not supported at /v4/Assets", hint);
        Assert.Contains("Supported: GET, POST", hint);
    }

    [Fact]
    public void GetErrorHint_405_NoCatalogMatch_FallsBackToDiscoverNudge()
    {
        var ctx = new ErrorContext("Core", "DELETE", "/v4/Unknown");
        var hint = ApiToolHelpers.GetErrorHint(
            statusCode: 405,
            apiMessage: null,
            hasModelState: false,
            ctx,
            paths: Array.Empty<ApiSchemaPropertyPath>(),
            requestPath: "/v4/Unknown",
            templateMatched: false,
            pathSuggestions: null,
            supportedMethods: Array.Empty<string>());

        Assert.Contains("Method DELETE not supported", hint);
        Assert.Contains("Safeguard_Discover", hint);
    }
}
