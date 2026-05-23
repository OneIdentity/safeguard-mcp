// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Reports_GetV3ReportCategories", Title = "Reports - GetV3ReportCategories",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of report categories.")]
    public Task<string> Reports_GetV3ReportCategories(McpServer server)
        => GetAsync(server, "/v4/Reports");

    [McpServerTool(Name = "Core_Reports_GetEntitlementReportTypes", Title = "Reports - GetEntitlementReportTypes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of report categories.")]
    public Task<string> Reports_GetEntitlementReportTypes(McpServer server)
        => GetAsync(server, "/v4/Reports/Entitlements");

    [McpServerTool(Name = "Core_Reports_GetAccountEntitlements", Title = "Reports - GetAccountEntitlements",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of what users can access a set of accounts.")]
    public Task<string> Reports_GetAccountEntitlements(McpServer server,
        [Description("Comma-separated list of accounts to report access for. Will report on all accounts by default. (preferred over filter).")] string accountIds = null,
        [Description("Only report on access via a specific request type (preferred over filter).")] string accessRequestType = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Reports/Entitlements/AccountEntitlements" + Q(("accountIds", accountIds), ("accessRequestType", accessRequestType), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetAccountEntitlementSummaries", Title = "Reports - GetAccountEntitlementSummaries",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a summary report of how many users/assets that the accounts can be accessed by.")]
    public Task<string> Reports_GetAccountEntitlementSummaries(McpServer server,
        [Description("Comma-separated list of assets to report access for. Will report on all assets by default. (preferred over filter).")] string assetIds = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Reports/Entitlements/AccountEntitlements/Summary" + Q(("assetIds", assetIds), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetAssetEntitlements", Title = "Reports - GetAssetEntitlements",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of what users can access accounts on a set of policy assets.")]
    public Task<string> Reports_GetAssetEntitlements(McpServer server,
        [Description("Comma-separated list of policy assets to report access for. Will report on all assets by default. (preferred over filter).")] string assetIds = null,
        [Description("Only report on access via a specific request type (preferred over filter).")] string accessRequestType = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Reports/Entitlements/AssetEntitlements" + Q(("assetIds", assetIds), ("accessRequestType", accessRequestType), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetAssetEntitlementSummaries", Title = "Reports - GetAssetEntitlementSummaries",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a summary report of how many users/accounts that the assets can be accessed by.")]
    public Task<string> Reports_GetAssetEntitlementSummaries(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Reports/Entitlements/AssetEntitlements/Summary" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetUserEntitlements", Title = "Reports - GetUserEntitlements",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of what accounts can be accessed by a set of users.")]
    public Task<string> Reports_GetUserEntitlements(McpServer server,
        [Description("Comma-separated list of users to report access for. Will report on all users by default. (preferred over filter).")] string userIds = null,
        [Description("Only report on access via a specific request type (preferred over filter).")] string accessRequestType = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Reports/Entitlements/UserEntitlements" + Q(("userIds", userIds), ("accessRequestType", accessRequestType), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetUserEntitlementSummaries", Title = "Reports - GetUserEntitlementSummaries",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a summary report of how many assets/accounts can be accessed by users.")]
    public Task<string> Reports_GetUserEntitlementSummaries(McpServer server,
        [Description("Comma-separated list of assets to report access for. Will report on all assets by default. (preferred over filter).")] string assetIds = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Reports/Entitlements/UserEntitlements/Summary" + Q(("assetIds", assetIds), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportTypes", Title = "Reports - GetOwnershipReportTypes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of report categories.")]
    public Task<string> Reports_GetOwnershipReportTypes(McpServer server)
        => GetAsync(server, "/v4/Reports/Ownership");

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportByType", Title = "Reports - GetOwnershipReportByType",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of owned items by type: Account, Asset, Partition, Tag, User.")]
    public Task<string> Reports_GetOwnershipReportByType(McpServer server,
        [Description("Ownership Report Type.")] string reportType,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/{Uri.EscapeDataString(reportType)}" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportOwnersByAccountIdAssetIdPartitionId", Title = "Reports - GetOwnershipReportOwnersByAccountIdAssetIdParti...",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of owners by account with parent asset and parent partition.")]
    public Task<string> Reports_GetOwnershipReportOwnersByAccountIdAssetIdPartitionId(McpServer server,
        [Description("Ownership Account Id.")] string accountId,
        [Description("Ownership Asset Id.")] string assetId,
        [Description("Ownership Partition Id.")] string partitionId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/Account/{Uri.EscapeDataString(accountId)}/Asset/{Uri.EscapeDataString(assetId)}/AssetPartition/{Uri.EscapeDataString(partitionId)}/Owners" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportOwnersByAccountId", Title = "Reports - GetOwnershipReportOwnersByAccountId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of owners by account.")]
    public Task<string> Reports_GetOwnershipReportOwnersByAccountId(McpServer server,
        [Description("Ownership Account Id.")] string accountId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/Account/{Uri.EscapeDataString(accountId)}/Owners" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportOwnersByAssetIdPartitionId", Title = "Reports - GetOwnershipReportOwnersByAssetIdPartitionId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of owners by asset and parent partition.")]
    public Task<string> Reports_GetOwnershipReportOwnersByAssetIdPartitionId(McpServer server,
        [Description("Ownership Asset Id.")] string assetId,
        [Description("Ownership Partition Id.")] string partitionId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/Asset/{Uri.EscapeDataString(assetId)}/AssetPartition/{Uri.EscapeDataString(partitionId)}/Owners" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportOwnersByAssetId", Title = "Reports - GetOwnershipReportOwnersByAssetId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of owners by asset.")]
    public Task<string> Reports_GetOwnershipReportOwnersByAssetId(McpServer server,
        [Description("Ownership Asset Id.")] string assetId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/Asset/{Uri.EscapeDataString(assetId)}/Owners" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportOwnersByPartitionId", Title = "Reports - GetOwnershipReportOwnersByPartitionId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of owners by partition.")]
    public Task<string> Reports_GetOwnershipReportOwnersByPartitionId(McpServer server,
        [Description("Ownership Partition Id.")] string partitionId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/Partition/{Uri.EscapeDataString(partitionId)}/Owners" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportItemsByTagId", Title = "Reports - GetOwnershipReportItemsByTagId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of a set of items by tag.")]
    public Task<string> Reports_GetOwnershipReportItemsByTagId(McpServer server,
        [Description("Ownership Tag Id.")] string tagId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/Tag/{Uri.EscapeDataString(tagId)}/Items" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportOwnersByTagId", Title = "Reports - GetOwnershipReportOwnersByTagId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of owners by tag.")]
    public Task<string> Reports_GetOwnershipReportOwnersByTagId(McpServer server,
        [Description("Ownership Tag Id.")] string tagId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/Tag/{Uri.EscapeDataString(tagId)}/Owners" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportDetailsByUserIdItemId", Title = "Reports - GetOwnershipReportDetailsByUserIdItemId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of how a user owns an item.")]
    public Task<string> Reports_GetOwnershipReportDetailsByUserIdItemId(McpServer server,
        [Description("Ownership User Id.")] string userId,
        [Description("Ownership Item Id.")] string itemId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/User/{Uri.EscapeDataString(userId)}/Item/{Uri.EscapeDataString(itemId)}/Details" + Q(("filter", filter), ("page", page), ("limit", limit), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportDetailsByUserIdItemIdTagId", Title = "Reports - GetOwnershipReportDetailsByUserIdItemIdTagId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of how a user owns an item by tag.")]
    public Task<string> Reports_GetOwnershipReportDetailsByUserIdItemIdTagId(McpServer server,
        [Description("Ownership User Id.")] string userId,
        [Description("Ownership Item Id.")] string itemId,
        [Description("Ownership Tag Id.")] string tagId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/User/{Uri.EscapeDataString(userId)}/Item/{Uri.EscapeDataString(itemId)}/Tag/{Uri.EscapeDataString(tagId)}/Details" + Q(("filter", filter), ("page", page), ("limit", limit), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetOwnershipReportItemsByUserId", Title = "Reports - GetOwnershipReportItemsByUserId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of a set of items by user.")]
    public Task<string> Reports_GetOwnershipReportItemsByUserId(McpServer server,
        [Description("Ownership User Id.")] string userId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Ownership/User/{Uri.EscapeDataString(userId)}/Items" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GetTaskReportTypes", Title = "Reports - GetTaskReportTypes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of task report categories.")]
    public Task<string> Reports_GetTaskReportTypes(McpServer server)
        => GetAsync(server, "/v4/Reports/Tasks");

    [McpServerTool(Name = "Core_Reports_GenerateAccountSecretChangedReport", Title = "Reports - GenerateAccountSecretChangedReport",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of secrets changed for accounts.")]
    public Task<string> Reports_GenerateAccountSecretChangedReport(McpServer server,
        [Description("Specific secret type filter. Null for all types.")] string secretType = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Reports/Tasks/AccountSecretsChanged" + Q(("secretType", secretType), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GenerateAccountSecretInUseReport", Title = "Reports - GenerateAccountSecretInUseReport",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of secrets in use for accounts.")]
    public Task<string> Reports_GenerateAccountSecretInUseReport(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Reports/Tasks/AccountSecretsInUse" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GenerateScheduledAccountTasksReport", Title = "Reports - GenerateScheduledAccountTasksReport",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of schedules for account tasks.")]
    public Task<string> Reports_GenerateScheduledAccountTasksReport(McpServer server,
        [Description("Specific task type for which to generate a schedule report.")] string accountTaskName,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Tasks/AccountTaskSchedules/{Uri.EscapeDataString(accountTaskName)}" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GenerateTaskSummaryReport", Title = "Reports - GenerateTaskSummaryReport",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a summary report of account tasks.")]
    public Task<string> Reports_GenerateTaskSummaryReport(McpServer server)
        => GetAsync(server, "/v4/Reports/Tasks/AccountTaskSummary");

    [McpServerTool(Name = "Core_Reports_GenerateFailedAccountTaskReport", Title = "Reports - GenerateFailedAccountTaskReport",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of what accounts have failing tasks.")]
    public Task<string> Reports_GenerateFailedAccountTaskReport(McpServer server,
        [Description("Specific task type for which to generate a failed account report.")] string accountTaskName,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Tasks/FailedAccountTasks/{Uri.EscapeDataString(accountTaskName)}" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Reports_GenerateFailedAssetTaskReport", Title = "Reports - GenerateFailedAssetTaskReport",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates a report of what assets have failing tasks.")]
    public Task<string> Reports_GenerateFailedAssetTaskReport(McpServer server,
        [Description("Specific task type for which to generate a failed asset report.")] string assetTaskName,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Reports/Tasks/FailedAssetTasks/{Uri.EscapeDataString(assetTaskName)}" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));
}
