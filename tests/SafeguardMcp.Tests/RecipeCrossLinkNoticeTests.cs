using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Pins the <c>session_token_issued_offer_to_launch</c> notice that
/// <see cref="SafeguardApiTool.BuildSessionLaunchOfferNotice"/> emits on
/// successful POST .../InitializeSession responses, alongside the existing
/// recipe-pointer notice. The notice carries the OFFER convention verbatim
/// ("Want me to launch it for you?") and the request id so the agent can
/// wire a later Safeguard_CloseAccessRequest call.
///
/// The notice intentionally does NOT fire on the four credential-checkout
/// leaves (CheckOutPassword / CheckOutSshKey / CheckOutApiKeys / CheckOutFile);
/// those flow through Safeguard_RetrieveCredential, which routes plaintext to
/// a user-audience block — the agent never has the credential in context and
/// therefore cannot "launch" with it, so the offer pattern would be wrong.
/// </summary>
public class RecipeCrossLinkNoticeTests
{
    [Fact]
    public void OfferNotice_FiresOnInitializeSession()
    {
        var notice = SafeguardApiTool.BuildSessionLaunchOfferNotice(
            "POST", "/v4/AccessRequests/123/InitializeSession");

        Assert.NotNull(notice);
        Assert.Equal(NoticeKinds.SessionTokenIssuedOfferToLaunch, notice.Kind);
    }

    [Fact]
    public void OfferNotice_PinsExactOfferPhrase()
    {
        var notice = SafeguardApiTool.BuildSessionLaunchOfferNotice(
            "POST", "/v4/AccessRequests/123/InitializeSession");

        Assert.NotNull(notice);
        // Pin the exact agent-facing phrase. Hosts and agents key off this
        // wording when rendering the launch offer; do not paraphrase.
        Assert.Contains("Want me to launch it for you?", notice.Suggestion);
    }

    [Fact]
    public void OfferNotice_CarriesRequestIdForLaterCheckIn()
    {
        var notice = SafeguardApiTool.BuildSessionLaunchOfferNotice(
            "POST", "/v4/AccessRequests/42-abc/InitializeSession");

        Assert.NotNull(notice);
        Assert.Contains("42-abc", notice.Message);
        Assert.Contains("Safeguard_CloseAccessRequest", notice.Message);
        Assert.Contains("42-abc", notice.Suggestion);
    }

    [Fact]
    public void OfferNotice_NamesPerOsLaunchHints()
    {
        var notice = SafeguardApiTool.BuildSessionLaunchOfferNotice(
            "POST", "/v4/AccessRequests/123/InitializeSession");

        Assert.NotNull(notice);
        Assert.Contains("Start-Process", notice.Suggestion);
        Assert.Contains("mstsc", notice.Suggestion);
        Assert.Contains("ssh", notice.Suggestion);
        Assert.Contains("RdpConnectionFile", notice.Suggestion);
    }

    [Fact]
    public void OfferNotice_AndRecipeNotice_BothFireOnInitializeSession()
    {
        const string path = "/v4/AccessRequests/123/InitializeSession";

        var recipe = SafeguardApiTool.BuildRecipeCrossLinkNotice("POST", path);
        var offer = SafeguardApiTool.BuildSessionLaunchOfferNotice("POST", path);

        // Recipe pointer still fires (existing behavior preserved); the offer
        // notice fires alongside it — two notices, not one-or-the-other.
        Assert.NotNull(recipe);
        Assert.Equal(NoticeKinds.WorkflowRecipeSuggested, recipe.Kind);
        Assert.NotNull(offer);
        Assert.Equal(NoticeKinds.SessionTokenIssuedOfferToLaunch, offer.Kind);
    }

    [Theory]
    [InlineData("/v4/AccessRequests/123/CheckOutPassword")]
    [InlineData("/v4/AccessRequests/123/CheckOutSshKey")]
    [InlineData("/v4/AccessRequests/123/CheckOutApiKeys")]
    [InlineData("/v4/AccessRequests/123/CheckOutFile")]
    public void OfferNotice_DoesNotFireOnCredentialCheckoutLeaves(string path)
    {
        var notice = SafeguardApiTool.BuildSessionLaunchOfferNotice("POST", path);

        // Those leaves go through Safeguard_RetrieveCredential, not Initialize-
        // Session — the agent never has plaintext to "launch" with, so the
        // offer pattern would be wrong.
        Assert.Null(notice);
    }

    [Fact]
    public void OfferNotice_DoesNotFireOnPostCreate()
    {
        var notice = SafeguardApiTool.BuildSessionLaunchOfferNotice("POST", "/v4/AccessRequests");
        Assert.Null(notice);
    }

    [Fact]
    public void OfferNotice_DoesNotFireOnCloseCompositeLeaves()
    {
        Assert.Null(SafeguardApiTool.BuildSessionLaunchOfferNotice("POST", "/v4/AccessRequests/123/CheckIn"));
        Assert.Null(SafeguardApiTool.BuildSessionLaunchOfferNotice("POST", "/v4/AccessRequests/123/Cancel"));
        Assert.Null(SafeguardApiTool.BuildSessionLaunchOfferNotice("POST", "/v4/AccessRequests/123/Close"));
        Assert.Null(SafeguardApiTool.BuildSessionLaunchOfferNotice("POST", "/v4/AccessRequests/123/Acknowledge"));
    }

    [Fact]
    public void OfferNotice_DoesNotFireOnGet()
    {
        var notice = SafeguardApiTool.BuildSessionLaunchOfferNotice(
            "GET", "/v4/AccessRequests/123/InitializeSession");
        Assert.Null(notice);
    }

    [Fact]
    public void OfferNotice_DoesNotFireOnUnrelatedPath()
    {
        var notice = SafeguardApiTool.BuildSessionLaunchOfferNotice(
            "POST", "/v4/Users/123/InitializeSession");
        Assert.Null(notice);
    }
}
