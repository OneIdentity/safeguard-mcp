using SafeguardMcp.Catalog;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// B.1 coverage: the property-path suggester must pull "did you mean"
/// candidates only from the catalog's path graph and the operator
/// vocabulary -- never from a hard-coded table or made-up flattening.
/// These tests pin the priority ordering (exact -> flat-FK -> substring
/// -> fuzzy) and the wording emitted by the context-aware error hint
/// for 70001 / 70002 / 70009.
/// </summary>
public class PropertyPathSuggesterTests
{
    private static ApiSchemaPropertyPath P(string path, bool isSynthetic = false, bool isCollection = false)
        => new(path, "string", null, isCollection, isSynthetic);

    [Fact]
    public void Suggest_FlatFkPattern_AssetIdMatchesAssetDotId()
    {
        var paths = new[]
        {
            P("Id"), P("Name"), P("Asset.Id"), P("Asset.Name"),
        };
        var result = PropertyPathSuggester.Suggest("AssetId", paths, QueryParamKind.Filter);
        Assert.Equal("Asset.Id", result[0]);
    }

    [Fact]
    public void Suggest_FlatFkPattern_UserNameFallsToSubstringMatchOnName()
    {
        // No User.Name on this entity -- the FK rule misses, substring
        // rule catches `Name` as a substring of `UserName`.
        var paths = new[] { P("Id"), P("Name"), P("Description") };
        var result = PropertyPathSuggester.Suggest("UserName", paths, QueryParamKind.Filter);
        Assert.Contains("Name", result);
    }

    [Fact]
    public void Suggest_CaseInsensitiveLeafMatch()
    {
        var paths = new[] { P("Id"), P("Name") };
        var result = PropertyPathSuggester.Suggest("name", paths, QueryParamKind.Fields);
        Assert.Equal("Name", result[0]);
    }

    [Fact]
    public void Suggest_FlatteningMiss_AccountAssetNameSuggestsAccountDotAssetDotName()
    {
        // Mirrors the E020/E024 trace family: the agent guessed the
        // flattened `Account.AssetName`, the graph has the nested form.
        var paths = new[]
        {
            P("Id"), P("Name"), P("Account.Id"), P("Account.Asset.Id"), P("Account.Asset.Name"),
        };
        var result = PropertyPathSuggester.Suggest("Account.AssetName", paths, QueryParamKind.Filter);
        Assert.Contains("Account.Asset.Name", result);
    }

    [Fact]
    public void Suggest_FuzzyFallback_TypoOnPropertyName()
    {
        var paths = new[] { P("Id"), P("CreatedDate"), P("Description") };
        var result = PropertyPathSuggester.Suggest("Decription", paths, QueryParamKind.Filter);
        Assert.Equal("Description", result[0]);
    }

    [Fact]
    public void Suggest_NoMatch_ReturnsEmpty()
    {
        var paths = new[] { P("Id"), P("Name") };
        var result = PropertyPathSuggester.Suggest("Qwertyuiopas", paths, QueryParamKind.Filter);
        Assert.Empty(result);
    }

    [Fact]
    public void Suggest_FieldsKind_ExcludesSyntheticCountPaths()
    {
        // Synthetic `<Collection>.Count` is valid for filter/orderby but
        // never for fields=, so it must not appear in a `fields` pool.
        var paths = new[]
        {
            P("Members", isCollection: true),
            P("Members.Count", isSynthetic: true),
        };
        var asField = PropertyPathSuggester.Suggest("Members.Count", paths, QueryParamKind.Fields);
        Assert.DoesNotContain("Members.Count", asField);

        var asFilter = PropertyPathSuggester.Suggest("Members.Count", paths, QueryParamKind.Filter);
        Assert.Contains("Members.Count", asFilter);
    }

    [Fact]
    public void TryStripFkSuffix_HandlesIdAndName()
    {
        Assert.True(PropertyPathSuggester.TryStripFkSuffix("AssetId", out var p, out var s));
        Assert.Equal("Asset", p);
        Assert.Equal("Id", s);

        Assert.True(PropertyPathSuggester.TryStripFkSuffix("UserName", out p, out s));
        Assert.Equal("User", p);
        Assert.Equal("Name", s);

        Assert.False(PropertyPathSuggester.TryStripFkSuffix("Id", out _, out _));
        Assert.False(PropertyPathSuggester.TryStripFkSuffix("Name", out _, out _));
        Assert.False(PropertyPathSuggester.TryStripFkSuffix(string.Empty, out _, out _));
    }
}

/// <summary>
/// B.1 coverage for the context-aware 70001 / 70002 / 70009 hint
/// formatter: confirms the rejected token is named, the suggestion is
/// pulled from the supplied path graph, the filter/orderby vs fields
/// divergence is mentioned in one line, and the "use Safeguard_Schema"
/// fall-through fires when no good candidate exists.
/// </summary>
public class ContextAwareErrorHintTests
{
    private static ApiSchemaPropertyPath P(string path, bool isSynthetic = false, bool isCollection = false)
        => new(path, "string", null, isCollection, isSynthetic);

    private static readonly ApiSchemaPropertyPath[] AssetAccountPaths = new[]
    {
        P("Id"), P("Name"), P("Description"),
        P("Asset.Id"), P("Asset.Name"),
        P("Profiles", isCollection: true),
        P("Profiles.Count", isSynthetic: true),
    };

    private static readonly ApiSchemaPropertyPath[] UserPaths = new[]
    {
        P("Id"), P("Name"), P("DisplayName"), P("EmailAddress"),
    };

    [Fact]
    public void Hint_70009_FlatFkOnAssetAccounts_NamesAssetDotIdAndDivergenceNote()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/AssetAccounts");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid filter property - 'AssetId' is not a valid filter property name.",
            hasModelState: false,
            ctx,
            AssetAccountPaths);

        Assert.Contains("'AssetId'", hint);
        Assert.Contains("filter property", hint);
        Assert.Contains("/v4/AssetAccounts", hint);
        Assert.Contains("`Asset.Id`", hint);
        Assert.Contains("flattened forms", hint);
        Assert.Contains("Account.AssetName", hint);
    }

    [Fact]
    public void Hint_70001_FuzzyMatch_SuggestsClosestOrderByCandidate()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/Users");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid order by property - 'Foo' is not a valid property name.",
            hasModelState: false,
            ctx,
            UserPaths);

        // 'Foo' is short -- threshold is 1; with no fuzzy hit we expect the
        // graceful "no close match" fall-through naming Safeguard_Schema.
        Assert.Contains("'Foo'", hint);
        Assert.Contains("orderby property", hint);
        Assert.Contains("/v4/Users", hint);
        Assert.Contains("Safeguard_Schema", hint);
    }

    [Fact]
    public void Hint_70001_OrderByCloseTypo_SuggestsDescendingForm()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/Users");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid order by property - 'DisplyName' is not a valid property name.",
            hasModelState: false,
            ctx,
            UserPaths);

        Assert.Contains("'DisplyName'", hint);
        Assert.Contains("`DisplayName`", hint);
        Assert.Contains("`-DisplayName`", hint);
        Assert.Contains("flattened forms", hint);
    }

    [Fact]
    public void Hint_70002_UserName_SuggestsName()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/AssetAccounts");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid field property - 'UserName' is not a valid property name.",
            hasModelState: false,
            ctx,
            AssetAccountPaths);

        Assert.Contains("'UserName'", hint);
        Assert.Contains("field property", hint);
        Assert.Contains("`Name`", hint);
        // fields= should not get the filter/orderby divergence note;
        // it gets the child-collection guidance instead.
        Assert.DoesNotContain("flattened forms", hint);
        Assert.Contains("child collections", hint);
    }

    [Fact]
    public void Hint_70001_PropertyOnlyValidInFields_IncludesDivergenceNote()
    {
        // `Profiles.Count` is valid in filter/orderby (synthetic). When the
        // agent tries it as orderby on a graph that includes the synthetic
        // entry the suggester finds it. The point of this test is the
        // divergence line is present for orderby (and absent for fields).
        var ctx = new ErrorContext("Core", "GET", "/v4/AssetAccounts");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid order by property - 'ProfileCount' is not a valid property name.",
            hasModelState: false,
            ctx,
            AssetAccountPaths);

        Assert.Contains("orderby property", hint);
        Assert.Contains("flattened forms", hint);
        Assert.Contains("not filterable", hint);
    }

    [Fact]
    public void Hint_70009_NoGoodMatch_FallsBackToSafeguardSchemaWithoutInventing()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/AssetAccounts");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid filter property - 'Qwertyuiopas' is not a valid filter property name.",
            hasModelState: false,
            ctx,
            AssetAccountPaths);

        Assert.Contains("'Qwertyuiopas'", hint);
        Assert.Contains("No close match", hint);
        Assert.Contains("Safeguard_Schema", hint);
        Assert.Contains("path=/v4/AssetAccounts", hint);
        // Must not have invented a "Try `<foo>`" suggestion.
        Assert.DoesNotContain("Try `", hint);
    }

    [Fact]
    public void Hint_70009_BadOperator_NotIn_SuggestsCanonicalForm()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/AssetAccounts");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid filter property - 'not_in' is not a valid filter property name.",
            hasModelState: false,
            ctx,
            AssetAccountPaths);

        Assert.Contains("'not_in'", hint);
        Assert.Contains("not a Safeguard filter operator", hint);
        Assert.Contains("not (Field in [...])", hint);
    }

    [Fact]
    public void Hint_70009_BadOperator_Like_SuggestsContains()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/AssetAccounts");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid filter property - 'like' is not a valid filter property name.",
            hasModelState: false,
            ctx,
            AssetAccountPaths);

        Assert.Contains("'like'", hint);
        Assert.Contains("contains", hint);
    }

    [Fact]
    public void Hint_EmptyPathGraph_StillNamesBadTokenAndPointsToSchema()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/SomeNewEndpoint");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Invalid filter property - 'Foo' is not a valid filter property name.",
            hasModelState: false,
            ctx,
            System.Array.Empty<ApiSchemaPropertyPath>());

        Assert.Contains("'Foo'", hint);
        Assert.Contains("Safeguard_Schema", hint);
        Assert.Contains("path=/v4/SomeNewEndpoint", hint);
        Assert.DoesNotContain("Try `", hint);
    }

    [Fact]
    public void Hint_NonPropertyError_DoesNotChangeBehavior()
    {
        var ctx = new ErrorContext("Core", "GET", "/v4/AssetAccounts");
        var hint = ApiToolHelpers.GetErrorHint(
            400,
            "Some unrelated 400 message",
            hasModelState: false,
            ctx,
            AssetAccountPaths);
        Assert.Equal(ApiToolHelpers.GetErrorHint(400), hint);
    }
}
