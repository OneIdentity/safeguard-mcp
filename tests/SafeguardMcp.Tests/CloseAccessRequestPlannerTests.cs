using System.Text.Json;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Unit tests for the pure-logic planner that backs
/// Safeguard_CloseAccessRequest. Covers every State row in the
/// requester path, the PolicyAdmin-on-someone-else branch, the
/// permission-fail branch, comment truncation, and the
/// unknown-state diagnostic.
/// </summary>
public class CloseAccessRequestPlannerTests
{
    private const string ReqId = "abc-123";

    [Fact]
    public void ValidateArgs_MissingRequestId_Errors()
    {
        var errors = CloseAccessRequestPlanner.ValidateArgs(new CloseAccessRequestInputs());
        Assert.Contains(errors, e => e.Contains("requestId is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateArgs_HappyPath_NoErrors()
    {
        var errors = CloseAccessRequestPlanner.ValidateArgs(new CloseAccessRequestInputs
        {
            RequestId = ReqId,
        });
        Assert.Empty(errors);
    }

    [Fact]
    public void TruncateComment_ShortInput_Returned_Unchanged()
    {
        var notices = new List<Notice>();
        var result = CloseAccessRequestPlanner.TruncateComment("ok", notices);
        Assert.Equal("ok", result);
        Assert.Empty(notices);
    }

    [Fact]
    public void TruncateComment_LongInput_TruncatedWithNotice()
    {
        var notices = new List<Notice>();
        var input = new string('x', CloseAccessRequestPlanner.MaxCommentLength + 50);
        var result = CloseAccessRequestPlanner.TruncateComment(input, notices);
        Assert.Equal(CloseAccessRequestPlanner.MaxCommentLength, result.Length);
        Assert.Single(notices);
        Assert.Equal("comment_truncated", notices[0].Kind);
        Assert.Contains("255", notices[0].Message);
    }

    [Theory]
    [InlineData("New", "Cancel")]
    [InlineData("PendingApproval", "Cancel")]
    [InlineData("Approved", "Cancel")]
    [InlineData("PendingTimeRequested", "Cancel")]
    [InlineData("RequestAvailable", "Cancel")]
    [InlineData("PendingAccountRestored", "Cancel")]
    [InlineData("PendingPasswordReset", "Cancel")]
    [InlineData("PasswordCheckedOut", "CheckIn")]
    [InlineData("SshKeyCheckedOut", "CheckIn")]
    [InlineData("SessionInitialized", "CheckIn")]
    [InlineData("Expired", "Acknowledge")]
    [InlineData("PendingAcknowledgment", "Acknowledge")]
    [InlineData("Closed", "None")]
    [InlineData("Complete", "None")]
    [InlineData("Reclaimed", "None")]
    public void Plan_RequesterPath_DispatchesPerStateMap(string state, string expectedAction)
    {
        var plan = CloseAccessRequestPlanner.Plan(
            state, callerIsRequester: true, callerIsPolicyAdmin: false,
            truncatedComment: "ok", requestId: ReqId);
        Assert.Null(plan.Refusal);
        Assert.Equal(expectedAction, plan.Action);
    }

    [Fact]
    public void Plan_CheckIn_DropsCommentBody_AndEmitsIgnoredNotice()
    {
        var plan = CloseAccessRequestPlanner.Plan(
            "PasswordCheckedOut", callerIsRequester: true, callerIsPolicyAdmin: false,
            truncatedComment: "ignored", requestId: ReqId);
        Assert.Equal("CheckIn", plan.Action);
        Assert.Null(plan.Body);
        Assert.Contains(plan.Notices, n => n.Kind == "comment_ignored");
    }

    [Fact]
    public void Plan_Cancel_EchoesCommentAsJsonString()
    {
        var plan = CloseAccessRequestPlanner.Plan(
            "New", callerIsRequester: true, callerIsPolicyAdmin: false,
            truncatedComment: "abort", requestId: ReqId);
        Assert.Equal("Cancel", plan.Action);
        Assert.Equal("\"abort\"", plan.Body);
    }

    [Theory]
    [InlineData("RequestCheckedIn")]
    [InlineData("Terminated")]
    [InlineData("PendingReview")]
    [InlineData("PendingAccountSuspended")]
    [InlineData("PendingAccountDemoted")]
    public void Plan_RequesterPath_DivergenceRow_RefusesWithStateNamed(string state)
    {
        var plan = CloseAccessRequestPlanner.Plan(
            state, callerIsRequester: true, callerIsPolicyAdmin: false,
            truncatedComment: null, requestId: ReqId);
        Assert.Null(plan.Action);
        Assert.NotNull(plan.Refusal);
        Assert.Contains(state, plan.Refusal);
        Assert.Contains("PolicyAdmin", plan.Refusal);
    }

    [Fact]
    public void Plan_PolicyAdminOnOwnRequest_PendingReview_DispatchesClose()
    {
        var plan = CloseAccessRequestPlanner.Plan(
            "PendingReview", callerIsRequester: true, callerIsPolicyAdmin: true,
            truncatedComment: null, requestId: ReqId);
        Assert.Equal("Close", plan.Action);
    }

    [Theory]
    [InlineData("RequestCheckedIn")]
    [InlineData("Terminated")]
    [InlineData("PendingAccountSuspended")]
    [InlineData("PendingAccountDemoted")]
    public void Plan_PolicyAdminOnOwnRequest_NeedsAdminRow_DispatchesClose(string state)
    {
        var plan = CloseAccessRequestPlanner.Plan(
            state, callerIsRequester: true, callerIsPolicyAdmin: true,
            truncatedComment: "admin-close", requestId: ReqId);
        Assert.Null(plan.Refusal);
        Assert.Equal("Close", plan.Action);
        Assert.Equal("\"admin-close\"", plan.Body);
    }

    [Theory]
    [InlineData("PendingReview")]
    [InlineData("PendingPasswordReset")]
    [InlineData("PendingAccountDemoted")]
    [InlineData("PendingAccountSuspended")]
    [InlineData("PendingAcknowledgment")]
    [InlineData("RequestCheckedIn")]
    [InlineData("Terminated")]
    public void Plan_PolicyAdminOnOtherRequest_NonTerminalState_DispatchesClose(string state)
    {
        var plan = CloseAccessRequestPlanner.Plan(
            state, callerIsRequester: false, callerIsPolicyAdmin: true,
            truncatedComment: "review-close", requestId: ReqId);
        Assert.Null(plan.Refusal);
        Assert.Equal("Close", plan.Action);
        Assert.Equal("\"review-close\"", plan.Body);
    }

    [Theory]
    [InlineData("New")]
    [InlineData("PasswordCheckedOut")]
    [InlineData("RequestAvailable")]
    [InlineData("Expired")]
    public void Plan_PolicyAdminOnOtherRequest_NonAdminRow_AlsoDispatchesClose(string state)
    {
        var plan = CloseAccessRequestPlanner.Plan(
            state, callerIsRequester: false, callerIsPolicyAdmin: true,
            truncatedComment: null, requestId: ReqId);
        Assert.Null(plan.Refusal);
        Assert.Equal("Close", plan.Action);
    }

    [Theory]
    [InlineData("Closed")]
    [InlineData("Complete")]
    [InlineData("Reclaimed")]
    public void Plan_PolicyAdminOnOtherRequest_TerminalState_NoOps(string state)
    {
        var plan = CloseAccessRequestPlanner.Plan(
            state, callerIsRequester: false, callerIsPolicyAdmin: true,
            truncatedComment: null, requestId: ReqId);
        Assert.Null(plan.Refusal);
        Assert.Equal("None", plan.Action);
    }

    [Fact]
    public void Plan_PolicyAdminOnOtherRequest_UnknownState_RefusesWithDiagnostic()
    {
        var plan = CloseAccessRequestPlanner.Plan(
            "Bogus", callerIsRequester: false, callerIsPolicyAdmin: true,
            truncatedComment: null, requestId: ReqId);
        Assert.Null(plan.Action);
        Assert.NotNull(plan.Refusal);
        Assert.Contains("Bogus", plan.Refusal);
    }

    [Fact]
    public void Plan_NeitherRequesterNorAdmin_RefusesWithSafeguardPsMessage()
    {
        var plan = CloseAccessRequestPlanner.Plan(
            "PendingReview", callerIsRequester: false, callerIsPolicyAdmin: false,
            truncatedComment: null, requestId: ReqId);
        Assert.Null(plan.Action);
        Assert.NotNull(plan.Refusal);
        Assert.Contains($"didn't request '{ReqId}'", plan.Refusal);
        Assert.Contains("policy admin", plan.Refusal);
    }

    [Fact]
    public void Plan_UnknownState_DiagnosticNamesStateNoFallback()
    {
        var plan = CloseAccessRequestPlanner.Plan(
            "Bogus", callerIsRequester: true, callerIsPolicyAdmin: false,
            truncatedComment: null, requestId: ReqId);
        Assert.Null(plan.Action);
        Assert.NotNull(plan.Refusal);
        Assert.Contains("Bogus", plan.Refusal);
        Assert.Contains("does not pick a fallback", plan.Refusal);
    }

    [Fact]
    public void Plan_InferredRow_EmitsNotVerifiedNotice()
    {
        var plan = CloseAccessRequestPlanner.Plan(
            "SshKeyCheckedOut", callerIsRequester: true, callerIsPolicyAdmin: false,
            truncatedComment: null, requestId: ReqId);
        Assert.Equal("CheckIn", plan.Action);
        Assert.Contains(plan.Notices, n => n.Kind == "inferred-not-verified-on-this-appliance");
    }

    [Fact]
    public void Plan_VerifiedRow_DoesNotEmitInferredNotice()
    {
        var plan = CloseAccessRequestPlanner.Plan(
            "PasswordCheckedOut", callerIsRequester: true, callerIsPolicyAdmin: false,
            truncatedComment: null, requestId: ReqId);
        Assert.Equal("CheckIn", plan.Action);
        Assert.DoesNotContain(plan.Notices, n => n.Kind == "inferred-not-verified-on-this-appliance");
    }

    [Fact]
    public void ProjectAccessRequest_AllFieldsFalse_ProjectsToSgFields()
    {
        var input = "{\"Id\":\"abc\",\"State\":\"New\",\"AccountName\":\"svc\",\"ExtraField\":\"dropped\"}";
        var projected = CloseAccessRequestPlanner.ProjectAccessRequest(input, allFields: false);
        using var doc = JsonDocument.Parse(projected);
        Assert.True(doc.RootElement.TryGetProperty("Id", out _));
        Assert.True(doc.RootElement.TryGetProperty("State", out _));
        Assert.True(doc.RootElement.TryGetProperty("AccountName", out _));
        Assert.False(doc.RootElement.TryGetProperty("ExtraField", out _));
    }

    [Fact]
    public void ProjectAccessRequest_AllFieldsTrue_ReturnsVerbatim()
    {
        var input = "{\"Id\":\"abc\",\"ExtraField\":\"kept\"}";
        var projected = CloseAccessRequestPlanner.ProjectAccessRequest(input, allFields: true);
        Assert.Equal(input, projected);
    }

    [Fact]
    public void ExtractGate_ParsesIdStateRequesterId()
    {
        var input = "{\"Id\":\"abc\",\"State\":\"New\",\"RequesterId\":42}";
        var (state, requesterId, id) = CloseAccessRequestPlanner.ExtractGate(input);
        Assert.Equal("New", state);
        Assert.Equal(42, requesterId);
        Assert.Equal("abc", id);
    }

    [Fact]
    public void ExtractMe_DetectsPolicyAdmin()
    {
        var input = "{\"Id\":7,\"AdminRoles\":[\"PolicyAdmin\",\"GlobalAdmin\"]}";
        var (id, isPolicyAdmin) = CloseAccessRequestPlanner.ExtractMe(input);
        Assert.Equal(7, id);
        Assert.True(isPolicyAdmin);
    }

    [Fact]
    public void ExtractMe_NonPolicyAdmin()
    {
        var input = "{\"Id\":7,\"AdminRoles\":[\"AssetAdmin\"]}";
        var (_, isPolicyAdmin) = CloseAccessRequestPlanner.ExtractMe(input);
        Assert.False(isPolicyAdmin);
    }

    [Fact]
    public void BuildSuccessEnvelope_ShapesOkActionRequestNotices()
    {
        var envelope = CloseAccessRequestPlanner.BuildSuccessEnvelope(
            action: "Cancel",
            requestJson: "{\"Id\":\"abc\",\"State\":\"Closed\"}",
            notices: new List<Notice> { new("k", "m", "s") });
        using var doc = JsonDocument.Parse(envelope);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("Cancel", doc.RootElement.GetProperty("action").GetString());
        Assert.Equal(JsonValueKind.Object, doc.RootElement.GetProperty("request").ValueKind);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("notices").ValueKind);
        Assert.Equal(1, doc.RootElement.GetProperty("notices").GetArrayLength());
    }

    [Fact]
    public void BuildRefusalEnvelope_ShapesOkFalseError()
    {
        var envelope = CloseAccessRequestPlanner.BuildRefusalEnvelope(
            error: "nope",
            notices: null);
        using var doc = JsonDocument.Parse(envelope);
        Assert.False(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("nope", doc.RootElement.GetProperty("error").GetString());
        Assert.Equal(0, doc.RootElement.GetProperty("notices").GetArrayLength());
    }
}
