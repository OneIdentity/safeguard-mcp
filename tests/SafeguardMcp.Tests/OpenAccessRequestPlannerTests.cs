using System.Text.Json;
using SafeguardMcp.Tools;

namespace SafeguardMcp.Tests;

/// <summary>
/// Unit tests for the pure-logic planner that backs Safeguard_OpenAccessRequest.
/// The planner is tested directly so the composite tool's correctness can be
/// verified without standing up a Safeguard session or a real appliance.
/// </summary>
public class OpenAccessRequestPlannerTests
{
    private static OpenAccessRequestInputs ValidInputs(int? assetId = null) => new()
    {
        AccountId = 6,
        AssetId = assetId,
        AccessRequestType = "RemoteDesktop",
        DurationHours = 2,
        ReasonComment = "Routine maintenance",
    };

    [Fact]
    public void ValidateArgs_HappyPath_ReturnsNoErrors()
    {
        var errors = OpenAccessRequestPlanner.ValidateArgs(ValidInputs());
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateArgs_MissingAccessRequestType_Errors()
    {
        var inputs = new OpenAccessRequestInputs
        {
            AccountId = 6,
            DurationHours = 2,
            ReasonComment = "Maintenance",
        };
        var errors = OpenAccessRequestPlanner.ValidateArgs(inputs);
        Assert.Contains(errors, e => e.Contains("accessRequestType is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateArgs_UnknownAccessRequestType_Errors()
    {
        var inputs = ValidInputs();
        var bad = new OpenAccessRequestInputs
        {
            AccountId = inputs.AccountId,
            AccessRequestType = "RDP",
            DurationHours = 2,
            ReasonComment = "x",
        };
        var errors = OpenAccessRequestPlanner.ValidateArgs(bad);
        Assert.Contains(errors, e => e.Contains("'RDP' is not a valid AccessRequestType", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateArgs_NoDuration_Errors()
    {
        var inputs = new OpenAccessRequestInputs
        {
            AccountId = 6,
            AccessRequestType = "RemoteDesktop",
            ReasonComment = "x",
        };
        var errors = OpenAccessRequestPlanner.ValidateArgs(inputs);
        Assert.Contains(errors, e => e.Contains("duration is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateArgs_MissingReason_Errors()
    {
        var inputs = new OpenAccessRequestInputs
        {
            AccountId = 6,
            AccessRequestType = "RemoteDesktop",
            DurationHours = 1,
        };
        var errors = OpenAccessRequestPlanner.ValidateArgs(inputs);
        Assert.Contains(errors, e => e.Contains("reasonComment is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateArgs_MissingAccountId_Errors()
    {
        var inputs = new OpenAccessRequestInputs
        {
            AccessRequestType = "RemoteDesktop",
            DurationHours = 1,
            ReasonComment = "x",
        };
        var errors = OpenAccessRequestPlanner.ValidateArgs(inputs);
        Assert.Contains(errors, e => e.Contains("accountId is required", StringComparison.Ordinal));
    }

    private const string EntitlementsSingleAssetJson = """
        [
          {
            "Account": { "Id": 6, "Name": "Administrator" },
            "Asset":   { "Id": 262, "Name": "RDS-1" },
            "Policy":  { "Id": 12, "Name": "Simple RDP", "AccessRequestProperties": { "AccessRequestType": "RemoteDesktop" } }
          }
        ]
        """;

    private const string EntitlementsTwoAssetsJson = """
        [
          {
            "Account": { "Id": 6, "Name": "Administrator" },
            "Asset":   { "Id": 262, "Name": "RDS-1" },
            "Policy":  { "Name": "Simple RDP", "AccessRequestProperties": { "AccessRequestType": "RemoteDesktop" } }
          },
          {
            "Account": { "Id": 6, "Name": "Administrator" },
            "Asset":   { "Id": 400, "Name": "RDS-2" },
            "Policy":  { "Name": "Standby RDP", "AccessRequestProperties": { "AccessRequestType": "RemoteDesktop" } }
          }
        ]
        """;

    private const string EntitlementsWrongTypeJson = """
        [
          {
            "Account": { "Id": 6, "Name": "Administrator" },
            "Asset":   { "Id": 262, "Name": "RDS-1" },
            "Policy":  { "Name": "Linux Pwd", "AccessRequestProperties": { "AccessRequestType": "Password" } }
          }
        ]
        """;

    [Fact]
    public void ParseEntitlements_RawArray_ReturnsRows()
    {
        var rows = OpenAccessRequestPlanner.ParseEntitlements(EntitlementsSingleAssetJson);
        Assert.Single(rows);
        Assert.Equal(6, rows[0].AccountId);
        Assert.Equal(262, rows[0].AssetId);
        Assert.Equal("RDS-1", rows[0].AssetName);
        Assert.Equal("RemoteDesktop", rows[0].AccessRequestType);
        Assert.Equal("Simple RDP", rows[0].PolicyName);
    }

    [Fact]
    public void ParseEntitlements_EnvelopedArray_ExtractsData()
    {
        var envelope = $"{{ \"data\": {EntitlementsSingleAssetJson}, \"meta\": {{ \"notices\": [] }} }}";
        var rows = OpenAccessRequestPlanner.ParseEntitlements(envelope);
        Assert.Single(rows);
        Assert.Equal(262, rows[0].AssetId);
    }

    [Fact]
    public void ValidateEntitlements_Empty_FailsWithGuidance()
    {
        var result = OpenAccessRequestPlanner.ValidateEntitlements(
            new List<EntitlementRow>(), accountId: 6, accessRequestType: "RemoteDesktop", assetId: null);
        Assert.False(result.Ok);
        Assert.Contains("0 'RemoteDesktop' entitlements", result.ErrorMessage);
        Assert.Contains("/v4/Me/RequestEntitlements", result.ErrorMessage);
    }

    [Fact]
    public void ValidateEntitlements_SingleMatch_InfersAssetWhenOmitted()
    {
        var rows = OpenAccessRequestPlanner.ParseEntitlements(EntitlementsSingleAssetJson);
        var result = OpenAccessRequestPlanner.ValidateEntitlements(
            rows, accountId: 6, accessRequestType: "RemoteDesktop", assetId: null);
        Assert.True(result.Ok);
        Assert.Equal(262, result.ResolvedAssetId);
        Assert.Equal("RDS-1", result.ResolvedAssetName);
    }

    [Fact]
    public void ValidateEntitlements_WrongAsset_E036_FailsAndNamesAllowedAssets()
    {
        var rows = OpenAccessRequestPlanner.ParseEntitlements(EntitlementsSingleAssetJson);
        var result = OpenAccessRequestPlanner.ValidateEntitlements(
            rows, accountId: 6, accessRequestType: "RemoteDesktop", assetId: 20);

        Assert.False(result.Ok);
        Assert.Contains("AssetId=262", result.ErrorMessage);
        Assert.Contains("RDS-1", result.ErrorMessage);
        Assert.Contains("AssetId=20 is not in scope", result.ErrorMessage);
        // The diagnostic must spell out what would otherwise be the misleading 90408.
        Assert.Contains("90408", result.ErrorMessage);
    }

    [Fact]
    public void ValidateEntitlements_MatchingAsset_AcceptsExplicitChoice()
    {
        var rows = OpenAccessRequestPlanner.ParseEntitlements(EntitlementsTwoAssetsJson);
        var result = OpenAccessRequestPlanner.ValidateEntitlements(
            rows, accountId: 6, accessRequestType: "RemoteDesktop", assetId: 400);
        Assert.True(result.Ok);
        Assert.Equal(400, result.ResolvedAssetId);
        Assert.Equal("RDS-2", result.ResolvedAssetName);
    }

    [Fact]
    public void ValidateEntitlements_MultipleAssetsOmittedAssetId_AsksToPick()
    {
        var rows = OpenAccessRequestPlanner.ParseEntitlements(EntitlementsTwoAssetsJson);
        var result = OpenAccessRequestPlanner.ValidateEntitlements(
            rows, accountId: 6, accessRequestType: "RemoteDesktop", assetId: null);
        Assert.False(result.Ok);
        Assert.Contains("AssetId=262", result.ErrorMessage);
        Assert.Contains("AssetId=400", result.ErrorMessage);
        Assert.Contains("Re-call with assetId", result.ErrorMessage);
    }

    [Fact]
    public void ValidateEntitlements_TypeNotAllowed_FailsAndListsAvailableTypes()
    {
        var rows = OpenAccessRequestPlanner.ParseEntitlements(EntitlementsWrongTypeJson);
        var result = OpenAccessRequestPlanner.ValidateEntitlements(
            rows, accountId: 6, accessRequestType: "RemoteDesktop", assetId: null);

        Assert.False(result.Ok);
        Assert.Contains("no 'RemoteDesktop' entitlement", result.ErrorMessage);
        Assert.Contains("Password", result.ErrorMessage);
    }

    [Fact]
    public void BuildNewAccessRequestJson_EmitsAllRequiredFields()
    {
        var json = OpenAccessRequestPlanner.BuildNewAccessRequestJson(
            accountId: 6,
            assetId: 262,
            accessRequestType: "remotedesktop",
            durationDays: null,
            durationHours: 2,
            durationMinutes: null,
            reasonComment: "Maintenance",
            reasonCodeId: null,
            ticketNumber: null,
            isEmergency: false);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(6, root.GetProperty("AccountId").GetInt32());
        Assert.Equal(262, root.GetProperty("AssetId").GetInt32());
        Assert.Equal("RemoteDesktop", root.GetProperty("AccessRequestType").GetString());
        Assert.Equal("Maintenance", root.GetProperty("ReasonComment").GetString());
        Assert.Equal(2, root.GetProperty("RequestedDurationHours").GetInt32());
        Assert.False(root.GetProperty("IsEmergency").GetBoolean());
        // Optional fields must be omitted when not supplied.
        Assert.False(root.TryGetProperty("ReasonCodeId", out _));
        Assert.False(root.TryGetProperty("TicketNumber", out _));
        Assert.False(root.TryGetProperty("RequestedDurationDays", out _));
        Assert.False(root.TryGetProperty("RequestedDurationMinutes", out _));
    }

    [Fact]
    public void BuildNewAccessRequestJson_IncludesAllSuppliedDurations()
    {
        var json = OpenAccessRequestPlanner.BuildNewAccessRequestJson(
            accountId: 6,
            assetId: 262,
            accessRequestType: "Ssh",
            durationDays: 1,
            durationHours: 2,
            durationMinutes: 30,
            reasonComment: "x",
            reasonCodeId: 5,
            ticketNumber: "INC-42",
            isEmergency: true);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("RequestedDurationDays").GetInt32());
        Assert.Equal(2, root.GetProperty("RequestedDurationHours").GetInt32());
        Assert.Equal(30, root.GetProperty("RequestedDurationMinutes").GetInt32());
        Assert.Equal(5, root.GetProperty("ReasonCodeId").GetInt32());
        Assert.Equal("INC-42", root.GetProperty("TicketNumber").GetString());
        Assert.True(root.GetProperty("IsEmergency").GetBoolean());
    }

    [Fact]
    public void BuildEntitlementsQuery_IncludesAccountAndType()
    {
        var query = OpenAccessRequestPlanner.BuildEntitlementsQuery(6, "RemoteDesktop");
        Assert.Contains("accountIds=6", query);
        Assert.Contains("accessRequestType=RemoteDesktop", query);
        Assert.Contains("fields=", query);
    }

    [Fact]
    public void ExtractIdAndState_ParsesNumberAndString()
    {
        var json = """{ "Id": 42, "State": "Approved" }""";
        var (id, state) = OpenAccessRequestPlanner.ExtractIdAndState(json);
        Assert.Equal("42", id);
        Assert.Equal("Approved", state);
    }

    [Fact]
    public void BuildSuccessSummary_RemoteDesktop_PointsAtInitializeSession()
    {
        var summary = OpenAccessRequestPlanner.BuildSuccessSummary(
            accessRequestId: "abc-1", state: "Available",
            accessRequestType: "RemoteDesktop", accountId: 6, assetId: 262);
        Assert.Contains("InitializeSession", summary);
        Assert.Contains("RdpConnectionFile", summary);
        Assert.DoesNotContain("CheckOutPassword", summary);
    }

    [Fact]
    public void BuildSuccessSummary_Password_PointsAtCheckOutPassword()
    {
        var summary = OpenAccessRequestPlanner.BuildSuccessSummary(
            accessRequestId: "abc-2", state: "RequestAvailable",
            accessRequestType: "Password", accountId: 6, assetId: 262);
        Assert.Contains("CheckOutPassword", summary);
        Assert.Contains("sensitive", summary, StringComparison.OrdinalIgnoreCase);
    }
}
