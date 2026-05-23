// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_AssetPartitions_GetAssetPartitions", Title = "AssetPartitions - GetAssetPartitions",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of asset partitions.")]
    public Task<string> AssetPartitions_GetAssetPartitions(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateAssetPartition", Title = "AssetPartitions - CreateAssetPartition",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new AssetPartition.")]
    public Task<string> AssetPartitions_CreateAssetPartition(McpServer server,
        [Description("AssetPartition to create.")] string body = null)
        => PostAsync(server, "/v4/AssetPartitions", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetAssetPartitionById", Title = "AssetPartitions - GetAssetPartitionById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single AssetPartition entity.")]
    public Task<string> AssetPartitions_GetAssetPartitionById(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateAssetPartition", Title = "AssetPartitions - UpdateAssetPartition",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an AssetPartition.")]
    public Task<string> AssetPartitions_UpdateAssetPartition(McpServer server,
        [Description("Unique identifier of the AssetPartition to update.")] string id,
        [Description("Updated AssetPartition.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteAssetPartition", Title = "AssetPartitions - DeleteAssetPartition",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an AssetPartition.")]
    public Task<string> AssetPartitions_DeleteAssetPartition(McpServer server,
        [Description("Unique identifier of the AssetPartition to remove.")] string id,
        [Description("Database ID of the partition that assets should be moved to.")] string failoverPartitionId = null)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}" + Q(("failoverPartitionId", failoverPartitionId)));

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionAccountDiscovery", Title = "AssetPartitions - GetPartitionAccountDiscovery",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of PartitionProfileAccountDiscoverySchedules.")]
    public Task<string> AssetPartitions_GetPartitionAccountDiscovery(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateAccountDiscovery", Title = "AssetPartitions - CreateAccountDiscovery",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new PartitionProfileAccountDiscoverySchedule.")]
    public Task<string> AssetPartitions_CreateAccountDiscovery(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("PartitionProfileAccountDiscoverySchedule to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionAccountDiscoveryScheduleById", Title = "AssetPartitions - GetPartitionAccountDiscoveryScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a PartitionProfileAccountDiscoverySchedule.")]
    public Task<string> AssetPartitions_GetPartitionAccountDiscoveryScheduleById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of PartitionProfileAccountDiscoverySchedule.")] string scheduleId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateAccountDiscovery", Title = "AssetPartitions - UpdateAccountDiscovery",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing PartitionProfileAccountDiscoverySchedule.")]
    public Task<string> AssetPartitions_UpdateAccountDiscovery(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfileAccountDiscoverySchedule.")] string scheduleId,
        [Description("Updated PartitionProfileAccountDiscoverySchedule.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteAccountDiscovery", Title = "AssetPartitions - DeleteAccountDiscovery",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a PartitionProfileAccountDiscoverySchedule.")]
    public Task<string> AssetPartitions_DeleteAccountDiscovery(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfileAccountDiscoverySchedule.")] string scheduleId,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAccountDiscoveryAssets", Title = "AssetPartitions - GetAccountDiscoveryAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets assets that the specified schedule is assigned to.")]
    public Task<string> AssetPartitions_GetAccountDiscoveryAssets(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the profile schedule.")] string scheduleId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}/Assets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_SetAccountDiscoveryAssets", Title = "AssetPartitions - SetAccountDiscoveryAssets",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the assets that are assigned to this account discovery schedule.")]
    public Task<string> AssetPartitions_SetAccountDiscoveryAssets(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the account discovery schedule.")] string scheduleId,
        [Description("Users to assign to the account discovery schedule.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}/Assets", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifyAccountDiscoveryAssets", Title = "AssetPartitions - ModifyAccountDiscoveryAssets",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove assets assigned to an existing account discovery schedule.")]
    public Task<string> AssetPartitions_ModifyAccountDiscoveryAssets(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the account discovery schedule.")] string scheduleId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Users to assign to the account discovery schedule.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}/Assets/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetAccountDiscoveryRules", Title = "AssetPartitions - GetAccountDiscoveryRules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets account discovery rules from an existing profile schedule.")]
    public Task<string> AssetPartitions_GetAccountDiscoveryRules(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the profile schedule.")] string scheduleId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}/Rules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateAccountDiscoveryRules", Title = "AssetPartitions - UpdateAccountDiscoveryRules",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets an existing profile schedule's rules.")]
    public Task<string> AssetPartitions_UpdateAccountDiscoveryRules(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the profile schedule.")] string scheduleId,
        [Description("rules to assign to the profile schedule.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}/Rules", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifyAccountDiscoveryRules", Title = "AssetPartitions - ModifyAccountDiscoveryRules",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove profile schedule's rules.")]
    public Task<string> AssetPartitions_ModifyAccountDiscoveryRules(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the profile schedule.")] string scheduleId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("rules to assign to the profile schedule.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}/Rules/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_TestAccountDiscovery", Title = "AssetPartitions - TestAccountDiscovery",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Runs Discovery on the given PartitionProfileAccountDiscoverySchedule.")]
    public Task<string> AssetPartitions_TestAccountDiscovery(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Discovery parameters.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/TestDiscovery" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_AssetPartitions_TestServiceDiscovery", Title = "AssetPartitions - TestServiceDiscovery",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Runs Service Discovery on the given PartitionProfileAccountDiscoverySchedule.")]
    public Task<string> AssetPartitions_TestServiceDiscovery(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Discovery parameters.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/AccountDiscoverySchedules/TestServiceDiscovery" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_AssetPartitions_GetAssetAccounts", Title = "AssetPartitions - GetAssetAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all accounts belonging to assets assigned to this partition.")]
    public Task<string> AssetPartitions_GetAssetAccounts(McpServer server,
        [Description("Unique identifier of the AssetPartition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAssets", Title = "AssetPartitions - GetAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all assets that belong to the specified partition.")]
    public Task<string> AssetPartitions_GetAssets(McpServer server,
        [Description("Unique identifier of the AssetAccount to get tasks for.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Assets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionChangeSchedules", Title = "AssetPartitions - GetPartitionChangeSchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition change schedules.")]
    public Task<string> AssetPartitions_GetPartitionChangeSchedules(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/ChangeSchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateChangeSchedule", Title = "AssetPartitions - CreateChangeSchedule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new PartitionProfileChangeSchedule.")]
    public Task<string> AssetPartitions_CreateChangeSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("The entity to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/ChangeSchedules", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionChangeScheduleById", Title = "AssetPartitions - GetPartitionChangeScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single PartitionProfileChangeSchedule.")]
    public Task<string> AssetPartitions_GetPartitionChangeScheduleById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of a PartitionProfileChangeSchedule.")] string scheduleId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/ChangeSchedules/{Uri.EscapeDataString(scheduleId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateChangeSchedule", Title = "AssetPartitions - UpdateChangeSchedule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a PartitionProfileChangeSchedule.")]
    public Task<string> AssetPartitions_UpdateChangeSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfileChangeSchedule to update.")] string scheduleId,
        [Description("Updated PartitionProfileChangeSchedule.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/ChangeSchedules/{Uri.EscapeDataString(scheduleId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteChangeSchedule", Title = "AssetPartitions - DeleteChangeSchedule",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a PartitionProfileChangeSchedule.")]
    public Task<string> AssetPartitions_DeleteChangeSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfileChangeSchedule to remove.")] string scheduleId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/ChangeSchedules/{Uri.EscapeDataString(scheduleId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetChangeScheduleAccounts", Title = "AssetPartitions - GetChangeScheduleAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets accounts that the specified schedule is assigned to.")]
    public Task<string> AssetPartitions_GetChangeScheduleAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the profile schedule.")] string scheduleId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/ChangeSchedules/{Uri.EscapeDataString(scheduleId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionCheckSchedules", Title = "AssetPartitions - GetPartitionCheckSchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition password check schedules.")]
    public Task<string> AssetPartitions_GetPartitionCheckSchedules(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/CheckSchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateCheckSchedule", Title = "AssetPartitions - CreateCheckSchedule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new PartitionProfileCheckSchedule.")]
    public Task<string> AssetPartitions_CreateCheckSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("PartitionProfileCheckSchedule to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/CheckSchedules", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionCheckScheduleById", Title = "AssetPartitions - GetPartitionCheckScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single PartitionProfileCheckSchedule.")]
    public Task<string> AssetPartitions_GetPartitionCheckScheduleById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of a PartitionProfileCheckSchedule.")] string scheduleId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/CheckSchedules/{Uri.EscapeDataString(scheduleId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateCheckSchedule", Title = "AssetPartitions - UpdateCheckSchedule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a PartitionProfileCheckSchedule.")]
    public Task<string> AssetPartitions_UpdateCheckSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfileCheckSchedule to update.")] string scheduleId,
        [Description("Updated PartitionProfileCheckSchedule.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/CheckSchedules/{Uri.EscapeDataString(scheduleId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteCheckSchedule", Title = "AssetPartitions - DeleteCheckSchedule",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a PartitionProfileCheckSchedule.")]
    public Task<string> AssetPartitions_DeleteCheckSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfileCheckSchedule to remove.")] string scheduleId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/CheckSchedules/{Uri.EscapeDataString(scheduleId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetCheckScheduleAccounts", Title = "AssetPartitions - GetCheckScheduleAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets accounts that the specified schedule is assigned to.")]
    public Task<string> AssetPartitions_GetCheckScheduleAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the profile schedule.")] string scheduleId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/CheckSchedules/{Uri.EscapeDataString(scheduleId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CheckUniqueProfileName", Title = "AssetPartitions - CheckUniqueProfileName",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if the current name is unique prior to create/update.")]
    public Task<string> AssetPartitions_CheckUniqueProfileName(McpServer server,
        [Description("Unique identifier of the AssetPartition.")] string id,
        [Description("Parameters for checking for unique name.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/CheckUniqueProfileName", body);

    [McpServerTool(Name = "Core_AssetPartitions_CheckUniqueSshKeyProfileName", Title = "AssetPartitions - CheckUniqueSshKeyProfileName",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Checks if the current name is unique prior to create/update.")]
    public Task<string> AssetPartitions_CheckUniqueSshKeyProfileName(McpServer server,
        [Description("Unique identifier of the AssetPartition.")] string id,
        [Description("Parameters for checking for unique name.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/CheckUniqueSshKeyProfileName", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionDiscoveredAccounts", Title = "AssetPartitions - GetPartitionDiscoveredAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an asset partition's discovered accounts.")]
    public Task<string> AssetPartitions_GetPartitionDiscoveredAccounts(McpServer server,
        [Description("Unique ID of a Partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_DeletePartitionDiscoveredAccounts", Title = "AssetPartitions - DeletePartitionDiscoveredAccounts",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Marks all partition discovered accounts as deleted.")]
    public Task<string> AssetPartitions_DeletePartitionDiscoveredAccounts(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts");

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionDiscoveredAccountsByAsset", Title = "AssetPartitions - GetPartitionDiscoveredAccountsByAsset",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an asset's discovered accounts.")]
    public Task<string> AssetPartitions_GetPartitionDiscoveredAccountsByAsset(McpServer server,
        [Description("Unique ID of a Partition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/{Uri.EscapeDataString(assetId)}" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_DeleteAssetDiscoveredAccounts", Title = "AssetPartitions - DeleteAssetDiscoveredAccounts",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Marks all asset discovered account as deleted.")]
    public Task<string> AssetPartitions_DeleteAssetDiscoveredAccounts(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/{Uri.EscapeDataString(assetId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionDiscoveredAccount", Title = "AssetPartitions - GetPartitionDiscoveredAccount",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a discovered account.")]
    public Task<string> AssetPartitions_GetPartitionDiscoveredAccount(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Name of a discovered account. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string accountKey,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(accountKey)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_IgnorePartitionDiscoveredAccount", Title = "AssetPartitions - IgnorePartitionDiscoveredAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Marks a discovered account as ignored.")]
    public Task<string> AssetPartitions_IgnorePartitionDiscoveredAccount(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Name of a discovered account. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string accountKey)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(accountKey)}/Ignore");

    [McpServerTool(Name = "Core_AssetPartitions_DeletePartitionDiscoveredAccount", Title = "AssetPartitions - DeletePartitionDiscoveredAccount",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Marks a discovered account as deleted.")]
    public Task<string> AssetPartitions_DeletePartitionDiscoveredAccount(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Name of a discovered account. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string accountName)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(accountName)}");

    [McpServerTool(Name = "Core_AssetPartitions_ManagePartitionDiscoveredAccount", Title = "AssetPartitions - ManagePartitionDiscoveredAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Brings a discovered account under management.")]
    public Task<string> AssetPartitions_ManagePartitionDiscoveredAccount(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Name of a discovered account. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string accountName,
        [Description("Additional optional parameters for configuring the new account.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(accountName)}/Manage", body);

    [McpServerTool(Name = "Core_AssetPartitions_ShowPartitionDiscoveredAccount", Title = "AssetPartitions - ShowPartitionDiscoveredAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Marks a discovered account as visible.")]
    public Task<string> AssetPartitions_ShowPartitionDiscoveredAccount(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Name of a discovered account. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string accountName)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(accountName)}/Show");

    [McpServerTool(Name = "Core_AssetPartitions_IgnoreMultipleAccounts", Title = "AssetPartitions - IgnoreMultipleAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple asset account ignore requests.")]
    public Task<string> AssetPartitions_IgnoreMultipleAccounts(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Discovered asset accounts to process.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/BatchIgnore", body);

    [McpServerTool(Name = "Core_AssetPartitions_ManageMultipleAccounts", Title = "AssetPartitions - ManageMultipleAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple asset account manage requests.")]
    public Task<string> AssetPartitions_ManageMultipleAccounts(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Parameters for managing multiple discovered accounts.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/BatchManage", body);

    [McpServerTool(Name = "Core_AssetPartitions_ShowMultipleAccounts", Title = "AssetPartitions - ShowMultipleAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple asset account show requests.")]
    public Task<string> AssetPartitions_ShowMultipleAccounts(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Discovered asset accounts to process.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredAccounts/BatchShow", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetDiscoveredServices", Title = "AssetPartitions - GetDiscoveredServices",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an asset partition's discovered services.")]
    public Task<string> AssetPartitions_GetDiscoveredServices(McpServer server,
        [Description("Unique ID of a Partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredServices" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_DeletePartitionDiscoveredServices", Title = "AssetPartitions - DeletePartitionDiscoveredServices",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes all services discovered for a specific partition.")]
    public Task<string> AssetPartitions_DeletePartitionDiscoveredServices(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredServices");

    [McpServerTool(Name = "Core_AssetPartitions_GetDiscoveredServicesByAsset", Title = "AssetPartitions - GetDiscoveredServicesByAsset",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an asset's discovered services.")]
    public Task<string> AssetPartitions_GetDiscoveredServicesByAsset(McpServer server,
        [Description("Unique ID of a Partition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredServices/{Uri.EscapeDataString(assetId)}" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_DeleteAssetDiscoveredServices", Title = "AssetPartitions - DeleteAssetDiscoveredServices",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes all services discovered for a specific asset.")]
    public Task<string> AssetPartitions_DeleteAssetDiscoveredServices(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredServices/{Uri.EscapeDataString(assetId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetDiscoveredServicesByServiceName", Title = "AssetPartitions - GetDiscoveredServicesByServiceName",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an asset's discovered services.")]
    public Task<string> AssetPartitions_GetDiscoveredServicesByServiceName(McpServer server,
        [Description("Unique ID of a Partition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Name of a discovered service. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string serviceName,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredServices/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(serviceName)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_DeleteDiscoveredService", Title = "AssetPartitions - DeleteDiscoveredService",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Marks a discovered account as deleted.")]
    public Task<string> AssetPartitions_DeleteDiscoveredService(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Name of a discovered account. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string serviceName)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredServices/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(serviceName)}");

    [McpServerTool(Name = "Core_AssetPartitions_IgnoreDiscoveredService", Title = "AssetPartitions - IgnoreDiscoveredService",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Marks a discovered service as ignored.")]
    public Task<string> AssetPartitions_IgnoreDiscoveredService(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Name of a discovered service. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string serviceName)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredServices/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(serviceName)}/Ignore");

    [McpServerTool(Name = "Core_AssetPartitions_ShowDiscoveredService", Title = "AssetPartitions - ShowDiscoveredService",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Marks a discovered service as visible.")]
    public Task<string> AssetPartitions_ShowDiscoveredService(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Name of a discovered service. For directory accounts you must also specify the domain name e.g., {accountName}@{domainName}.")] string serviceName)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredServices/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(serviceName)}/Show");

    [McpServerTool(Name = "Core_AssetPartitions_GetDiscoveredSshKeys", Title = "AssetPartitions - GetDiscoveredSshKeys",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the discovered SSH keys for an asset partition.")]
    public Task<string> AssetPartitions_GetDiscoveredSshKeys(McpServer server,
        [Description("Unique ID of the asset.")] string id,
        [Description("The format of the SSH private key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredSshKeys" + Q(("keyFormat", keyFormat), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_DeletePartitionDiscoveredSshKeys", Title = "AssetPartitions - DeletePartitionDiscoveredSshKeys",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes all SSH keys discovered for a specific partition.")]
    public Task<string> AssetPartitions_DeletePartitionDiscoveredSshKeys(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredSshKeys");

    [McpServerTool(Name = "Core_AssetPartitions_GetDiscoveredSshKeysByAsset", Title = "AssetPartitions - GetDiscoveredSshKeysByAsset",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets discovered SSH keys for an asset.")]
    public Task<string> AssetPartitions_GetDiscoveredSshKeysByAsset(McpServer server,
        [Description("Unique ID of a Partition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("The format of the SSH private key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredSshKeys/{Uri.EscapeDataString(assetId)}" + Q(("keyFormat", keyFormat), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_DeleteAssetDiscoveredSshKeys", Title = "AssetPartitions - DeleteAssetDiscoveredSshKeys",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes all SSH keys discovered for a specific asset.")]
    public Task<string> AssetPartitions_DeleteAssetDiscoveredSshKeys(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredSshKeys/{Uri.EscapeDataString(assetId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetDiscoveredSshKeysByAccount", Title = "AssetPartitions - GetDiscoveredSshKeysByAccount",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets discovered SSH keys for an account.")]
    public Task<string> AssetPartitions_GetDiscoveredSshKeysByAccount(McpServer server,
        [Description("Unique ID of a Partition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Unique ID of an account.")] string accountId,
        [Description("The format of the SSH private key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredSshKeys/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(accountId)}" + Q(("keyFormat", keyFormat), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_DeleteAssetAccountDiscoveredSshKeys", Title = "AssetPartitions - DeleteAssetAccountDiscoveredSshKeys",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes all SSH keys discovered for a specific account.")]
    public Task<string> AssetPartitions_DeleteAssetAccountDiscoveredSshKeys(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Unique ID of an account.")] string accountId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredSshKeys/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(accountId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetDiscoveredSshKeyByFingerprint", Title = "AssetPartitions - GetDiscoveredSshKeyByFingerprint",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an discovered SSH key.")]
    public Task<string> AssetPartitions_GetDiscoveredSshKeyByFingerprint(McpServer server,
        [Description("Unique ID of a Partition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Unique ID of an account.")] string accountId,
        [Description("MD5 fingerprint of SSH Key.")] string fingerprint,
        [Description("The format of the SSH private key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredSshKeys/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(fingerprint)}" + Q(("keyFormat", keyFormat), ("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_DeleteDiscoveredSshKey", Title = "AssetPartitions - DeleteDiscoveredSshKey",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Marks a discovered SSH key as deleted.")]
    public Task<string> AssetPartitions_DeleteDiscoveredSshKey(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of an Asset.")] string assetId,
        [Description("Unique ID of an account.")] string accountId,
        [Description("MD5 fingerprint of the SSH key.")] string fingerprint)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveredSshKeys/{Uri.EscapeDataString(assetId)}/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(fingerprint)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionDiscoveryJobs", Title = "AssetPartitions - GetPartitionDiscoveryJobs",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of asset discovery jobs.")]
    public Task<string> AssetPartitions_GetPartitionDiscoveryJobs(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateDiscoveryJob", Title = "AssetPartitions - CreateDiscoveryJob",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates an asset discovery job.")]
    public Task<string> AssetPartitions_CreateDiscoveryJob(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("AssetDiscoveryJob to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionDiscoveryJob", Title = "AssetPartitions - GetPartitionDiscoveryJob",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific asset discovery job.")]
    public Task<string> AssetPartitions_GetPartitionDiscoveryJob(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of the asset discovery job.")] string jobId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs/{Uri.EscapeDataString(jobId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateDiscoveryJob", Title = "AssetPartitions - UpdateDiscoveryJob",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an asset discovery job.")]
    public Task<string> AssetPartitions_UpdateDiscoveryJob(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the AssetDiscoveryJob.")] string jobId,
        [Description("Updated AssetDiscoveryJob.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs/{Uri.EscapeDataString(jobId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteDiscoveryJob", Title = "AssetPartitions - DeleteDiscoveryJob",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an asset discovery job.")]
    public Task<string> AssetPartitions_DeleteDiscoveryJob(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the AssetDiscoveryJob.")] string jobId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs/{Uri.EscapeDataString(jobId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetDiscoveryJobRules", Title = "AssetPartitions - GetDiscoveryJobRules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all rules that belong to an asset discovery rule.")]
    public Task<string> AssetPartitions_GetDiscoveryJobRules(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the AssetDiscoveryJob.")] string jobId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs/{Uri.EscapeDataString(jobId)}/Rules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_SetDiscoveryJobRules", Title = "AssetPartitions - SetDiscoveryJobRules",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the rules assigned to an asset discovery job.")]
    public Task<string> AssetPartitions_SetDiscoveryJobRules(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the AssetDiscoveryJob.")] string jobId,
        [Description("Accounts to assign to the AssetDiscoveryJob.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs/{Uri.EscapeDataString(jobId)}/Rules", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifyDiscoveryJobRules", Title = "AssetPartitions - ModifyDiscoveryJobRules",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove rules assigned to an asset discovery job.")]
    public Task<string> AssetPartitions_ModifyDiscoveryJobRules(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the AssetDiscoveryJob.")] string jobId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Accounts to assign to the AssetDiscoveryJob.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs/{Uri.EscapeDataString(jobId)}/Rules/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_RunDiscoveryJob", Title = "AssetPartitions - RunDiscoveryJob",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Run asset discovery job now.")]
    public Task<string> AssetPartitions_RunDiscoveryJob(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Database ID of asset discovery job.")] string jobId,
        [Description("Whether to include extended logging for the platform operation.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs/{Uri.EscapeDataString(jobId)}/RunDiscovery" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetPartitions_TestDiscovery", Title = "AssetPartitions - TestDiscovery",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests the set of specific asset discovery conditions. Assets will only be discovered but not added to the database.")]
    public Task<string> AssetPartitions_TestDiscovery(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Parameters for testing asset discovery conditions.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/DiscoveryJobs/TestDiscovery" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionManagedBy", Title = "AssetPartitions - GetPartitionManagedBy",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all owners of the specified partition.")]
    public Task<string> AssetPartitions_GetPartitionManagedBy(McpServer server,
        [Description("Unique identifier of the AssetAccount to get tasks for.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/ManagedBy" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_SetPartitionManagedBy", Title = "AssetPartitions - SetPartitionManagedBy",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the assigned owners of this partition.")]
    public Task<string> AssetPartitions_SetPartitionManagedBy(McpServer server,
        [Description("Unique identifier of the AssetPartition.")] string id,
        [Description("List of owners to assign to this partition.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/ManagedBy", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifyManagedBy", Title = "AssetPartitions - ModifyManagedBy",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove assigned owners of this partition.")]
    public Task<string> AssetPartitions_ModifyManagedBy(McpServer server,
        [Description("Unique identifier of the AssetPartition.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("List of owners to assign to this partition.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/ManagedBy/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPasswordRules", Title = "AssetPartitions - GetPasswordRules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of password rules.")]
    public Task<string> AssetPartitions_GetPasswordRules(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/PasswordRules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreatePasswordRule", Title = "AssetPartitions - CreatePasswordRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new password rule.")]
    public Task<string> AssetPartitions_CreatePasswordRule(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("AccountGroup to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/PasswordRules", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPasswordRule", Title = "AssetPartitions - GetPasswordRule",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific password rule.")]
    public Task<string> AssetPartitions_GetPasswordRule(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique ID of a PasswordRule.")] string ruleId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/PasswordRules/{Uri.EscapeDataString(ruleId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdatePasswordRule", Title = "AssetPartitions - UpdatePasswordRule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing password rule.")]
    public Task<string> AssetPartitions_UpdatePasswordRule(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the AccountPasswordRule to update.")] string ruleId,
        [Description("Updated AccountPasswordRule.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/PasswordRules/{Uri.EscapeDataString(ruleId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeletePasswordRule", Title = "AssetPartitions - DeletePasswordRule",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a password rule.")]
    public Task<string> AssetPartitions_DeletePasswordRule(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the PartitionAccountPasswordRule to remove.")] string ruleId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/PasswordRules/{Uri.EscapeDataString(ruleId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetPasswordRuleAccounts", Title = "AssetPartitions - GetPasswordRuleAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets accounts that the specified rule is assigned to.")]
    public Task<string> AssetPartitions_GetPasswordRuleAccounts(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the password rule.")] string ruleId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/PasswordRules/{Uri.EscapeDataString(ruleId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GeneratePasswordFromRule", Title = "AssetPartitions - GeneratePasswordFromRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Generates a random password using this rule.")]
    public Task<string> AssetPartitions_GeneratePasswordFromRule(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the PartitionAccountPasswordRule to generate password from.")] string ruleId)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/PasswordRules/{Uri.EscapeDataString(ruleId)}/GeneratePassword");

    [McpServerTool(Name = "Core_AssetPartitions_ValidateAccountPasswordByRule", Title = "AssetPartitions - ValidateAccountPasswordByRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Validates a proposed password against the given rule.")]
    public Task<string> AssetPartitions_ValidateAccountPasswordByRule(McpServer server,
        [Description("Unique ID of an AssetPartition.")] string id,
        [Description("Unique identifier of the PartitionAccountPasswordRule to update.")] string ruleId,
        [Description("Password to validate against this rule.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/PasswordRules/{Uri.EscapeDataString(ruleId)}/ValidatePassword", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionProfiles", Title = "AssetPartitions - GetPartitionProfiles",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of profiles for a specific partition.")]
    public Task<string> AssetPartitions_GetPartitionProfiles(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateProfile", Title = "AssetPartitions - CreateProfile",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new asset configuration profile.")]
    public Task<string> AssetPartitions_CreateProfile(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("PartitionProfile to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionProfileById", Title = "AssetPartitions - GetPartitionProfileById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a profile.")]
    public Task<string> AssetPartitions_GetPartitionProfileById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of PartitionProfile.")] string profileId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateProfile", Title = "AssetPartitions - UpdateProfile",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing application asset configuration profile.")]
    public Task<string> AssetPartitions_UpdateProfile(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId,
        [Description("Updated PartitionProfile.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteProfile", Title = "AssetPartitions - DeleteProfile",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an application asset configuration profile.")]
    public Task<string> AssetPartitions_DeleteProfile(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetProfileAssetAccounts", Title = "AssetPartitions - GetProfileAssetAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the accounts that are explicitly using this profile.")]
    public Task<string> AssetPartitions_GetProfileAssetAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_SetProfileAssetAccounts", Title = "AssetPartitions - SetProfileAssetAccounts",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the accounts that are explicitly using this profile.")]
    public Task<string> AssetPartitions_SetProfileAssetAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId,
        [Description("Users to assign to the profile.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/Accounts", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifyProfileAssetAccounts", Title = "AssetPartitions - ModifyProfileAssetAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove accounts to an existing profile.")]
    public Task<string> AssetPartitions_ModifyProfileAssetAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Users to assign to the profile.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/Accounts/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetProfileAssets", Title = "AssetPartitions - GetProfileAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the Assets that are explicitly using this profile.")]
    public Task<string> AssetPartitions_GetProfileAssets(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/Assets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_SetProfileAssets", Title = "AssetPartitions - SetProfileAssets",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the assets that are explicitly using this profile.")]
    public Task<string> AssetPartitions_SetProfileAssets(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId,
        [Description("Users to assign to the profile.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/Assets", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifyProfileAssets", Title = "AssetPartitions - ModifyProfileAssets",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove assets to an existing profile.")]
    public Task<string> AssetPartitions_ModifyProfileAssets(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Users to assign to the profile.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/Assets/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetProfileChangeSchedule", Title = "AssetPartitions - GetProfileChangeSchedule",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the change schedule on the selected partition profile.")]
    public Task<string> AssetPartitions_GetProfileChangeSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/ChangeSchedule");

    [McpServerTool(Name = "Core_AssetPartitions_GetProfileCheckSchedule", Title = "AssetPartitions - GetProfileCheckSchedule",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the check schedule on the selected partition profile.")]
    public Task<string> AssetPartitions_GetProfileCheckSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/CheckSchedule");

    [McpServerTool(Name = "Core_AssetPartitions_GetProfileAccountPasswordRule", Title = "AssetPartitions - GetProfileAccountPasswordRule",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the account password rule on the selected partition profile.")]
    public Task<string> AssetPartitions_GetProfileAccountPasswordRule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/PasswordRule");

    [McpServerTool(Name = "Core_AssetPartitions_GetProfileSyncGroups", Title = "AssetPartitions - GetProfileSyncGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of sync groups for a specific profile.")]
    public Task<string> AssetPartitions_GetProfileSyncGroups(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateSyncGroup", Title = "AssetPartitions - CreateSyncGroup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new profile sync group.")]
    public Task<string> AssetPartitions_CreateSyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("PasswordSyncGroup to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetProfileSyncGroupById", Title = "AssetPartitions - GetProfileSyncGroupById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific profile sync group.")]
    public Task<string> AssetPartitions_GetProfileSyncGroupById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateSyncGroup", Title = "AssetPartitions - UpdateSyncGroup",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing profile sync group.")]
    public Task<string> AssetPartitions_UpdateSyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Updated PasswordSyncGroup.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteSyncGroup", Title = "AssetPartitions - DeleteSyncGroup",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a profile sync group.")]
    public Task<string> AssetPartitions_DeleteSyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetSyncGroupAccounts", Title = "AssetPartitions - GetSyncGroupAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all accounts that belong to a profile sync group.")]
    public Task<string> AssetPartitions_GetSyncGroupAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_SetAccounts", Title = "AssetPartitions - SetAccounts",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the accounts assigned to this group.")]
    public Task<string> AssetPartitions_SetAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Accounts to assign to the AccountGroup.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}/Accounts", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifyAccounts", Title = "AssetPartitions - ModifyAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove accounts assigned to this group.")]
    public Task<string> AssetPartitions_ModifyAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Accounts to assign to the AccountGroup.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}/Accounts/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DisableSyncGroup", Title = "AssetPartitions - DisableSyncGroup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Disables an existing profile sync group.")]
    public Task<string> AssetPartitions_DisableSyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}/Disable");

    [McpServerTool(Name = "Core_AssetPartitions_EnableSyncGroup", Title = "AssetPartitions - EnableSyncGroup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enables an existing profile sync group.")]
    public Task<string> AssetPartitions_EnableSyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}/Enable");

    [McpServerTool(Name = "Core_AssetPartitions_SetPassword", Title = "AssetPartitions - SetPassword",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the sync group password.")]
    public Task<string> AssetPartitions_SetPassword(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Password to set for this sync group.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}/Password", body);

    [McpServerTool(Name = "Core_AssetPartitions_SyncAccounts", Title = "AssetPartitions - SyncAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Sync credentials for all accounts in sync group.")]
    public Task<string> AssetPartitions_SyncAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Profiles/{Uri.EscapeDataString(profileId)}/SyncGroups/{Uri.EscapeDataString(syncGroupId)}/Sync" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionSshKeyChangeSchedules", Title = "AssetPartitions - GetPartitionSshKeyChangeSchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition change schedules.")]
    public Task<string> AssetPartitions_GetPartitionSshKeyChangeSchedules(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyChangeSchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateSshKeyChangeSchedule", Title = "AssetPartitions - CreateSshKeyChangeSchedule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new SshKeyChangeSchedule.")]
    public Task<string> AssetPartitions_CreateSshKeyChangeSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("The entity to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyChangeSchedules", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionSshKeyChangeScheduleById", Title = "AssetPartitions - GetPartitionSshKeyChangeScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single SshKeyChangeSchedule.")]
    public Task<string> AssetPartitions_GetPartitionSshKeyChangeScheduleById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of a SshKeyChangeSchedule.")] string scheduleId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyChangeSchedules/{Uri.EscapeDataString(scheduleId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateSshKeyChangeSchedule", Title = "AssetPartitions - UpdateSshKeyChangeSchedule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a SshKeyChangeSchedule.")]
    public Task<string> AssetPartitions_UpdateSshKeyChangeSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyChangeSchedule to update.")] string scheduleId,
        [Description("Updated SshKeyChangeSchedule.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyChangeSchedules/{Uri.EscapeDataString(scheduleId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteSshKeyChangeSchedule", Title = "AssetPartitions - DeleteSshKeyChangeSchedule",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a SshKeyChangeSchedule.")]
    public Task<string> AssetPartitions_DeleteSshKeyChangeSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyChangeSchedule to remove.")] string scheduleId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyChangeSchedules/{Uri.EscapeDataString(scheduleId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyChangeScheduleAccounts", Title = "AssetPartitions - GetSshKeyChangeScheduleAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets accounts that the specified schedule is assigned to.")]
    public Task<string> AssetPartitions_GetSshKeyChangeScheduleAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the profile schedule.")] string scheduleId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyChangeSchedules/{Uri.EscapeDataString(scheduleId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyCheckSchedules", Title = "AssetPartitions - GetSshKeyCheckSchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition SSH key check schedules.")]
    public Task<string> AssetPartitions_GetSshKeyCheckSchedules(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyCheckSchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateSshKeyCheckSchedule", Title = "AssetPartitions - CreateSshKeyCheckSchedule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new SshKeyCheckSchedule.")]
    public Task<string> AssetPartitions_CreateSshKeyCheckSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("SshKeyCheckSchedule to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyCheckSchedules", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionSshKeyCheckScheduleById", Title = "AssetPartitions - GetPartitionSshKeyCheckScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single SshKeyCheckSchedule.")]
    public Task<string> AssetPartitions_GetPartitionSshKeyCheckScheduleById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of a SshKeyCheckSchedule.")] string scheduleId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyCheckSchedules/{Uri.EscapeDataString(scheduleId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateSshKeyCheckSchedule", Title = "AssetPartitions - UpdateSshKeyCheckSchedule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a SshKeyCheckSchedule.")]
    public Task<string> AssetPartitions_UpdateSshKeyCheckSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyCheckSchedule to update.")] string scheduleId,
        [Description("Updated SshKeyCheckSchedule.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyCheckSchedules/{Uri.EscapeDataString(scheduleId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteSshKeyCheckSchedule", Title = "AssetPartitions - DeleteSshKeyCheckSchedule",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a SshKeyCheckSchedule.")]
    public Task<string> AssetPartitions_DeleteSshKeyCheckSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyCheckSchedule to remove.")] string scheduleId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyCheckSchedules/{Uri.EscapeDataString(scheduleId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyCheckScheduleAccounts", Title = "AssetPartitions - GetSshKeyCheckScheduleAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets accounts that the specified schedule is assigned to.")]
    public Task<string> AssetPartitions_GetSshKeyCheckScheduleAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the profile schedule.")] string scheduleId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyCheckSchedules/{Uri.EscapeDataString(scheduleId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionSshKeyDiscoverySchedules", Title = "AssetPartitions - GetPartitionSshKeyDiscoverySchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition Discovery schedules.")]
    public Task<string> AssetPartitions_GetPartitionSshKeyDiscoverySchedules(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyDiscoverySchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateSshKeyDiscoverySchedule", Title = "AssetPartitions - CreateSshKeyDiscoverySchedule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new SshKeyDiscoverySchedule.")]
    public Task<string> AssetPartitions_CreateSshKeyDiscoverySchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("The entity to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyDiscoverySchedules", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionSshKeyDiscoveryScheduleById", Title = "AssetPartitions - GetPartitionSshKeyDiscoveryScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single SshKeyDiscoverySchedule.")]
    public Task<string> AssetPartitions_GetPartitionSshKeyDiscoveryScheduleById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of a SshKeyDiscoverySchedule.")] string scheduleId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateSshKeyDiscoverySchedule", Title = "AssetPartitions - UpdateSshKeyDiscoverySchedule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a SshKeyDiscoverySchedule.")]
    public Task<string> AssetPartitions_UpdateSshKeyDiscoverySchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyDiscoverySchedule to update.")] string scheduleId,
        [Description("Updated SshKeyDiscoverySchedule.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteSshKeyDiscoverySchedule", Title = "AssetPartitions - DeleteSshKeyDiscoverySchedule",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a SshKeyDiscoverySchedule.")]
    public Task<string> AssetPartitions_DeleteSshKeyDiscoverySchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyDiscoverySchedule to remove.")] string scheduleId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyDiscoveryScheduleAccounts", Title = "AssetPartitions - GetSshKeyDiscoveryScheduleAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets accounts that the specified schedule is assigned to.")]
    public Task<string> AssetPartitions_GetSshKeyDiscoveryScheduleAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the profile schedule.")] string scheduleId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyDiscoverySchedules/{Uri.EscapeDataString(scheduleId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyProfiles", Title = "AssetPartitions - GetSshKeyProfiles",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of SSH key profiles for a specific partition.")]
    public Task<string> AssetPartitions_GetSshKeyProfiles(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateSshKeyProfile", Title = "AssetPartitions - CreateSshKeyProfile",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new SSH key profile.")]
    public Task<string> AssetPartitions_CreateSshKeyProfile(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("SshKeyProfile to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyProfileById", Title = "AssetPartitions - GetSshKeyProfileById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an SSH key profile.")]
    public Task<string> AssetPartitions_GetSshKeyProfileById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of SshKeyProfile.")] string profileId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateSshKeyProfile", Title = "AssetPartitions - UpdateSshKeyProfile",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing SSH key profile.")]
    public Task<string> AssetPartitions_UpdateSshKeyProfile(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId,
        [Description("Updated SshKeyProfile.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteSshKeyProfile", Title = "AssetPartitions - DeleteSshKeyProfile",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an application asset configuration SSH key profile.")]
    public Task<string> AssetPartitions_DeleteSshKeyProfile(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyProfileAssetAccounts", Title = "AssetPartitions - GetSshKeyProfileAssetAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the accounts that are explicitly using this SSH key profile.")]
    public Task<string> AssetPartitions_GetSshKeyProfileAssetAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the PartitionProfile.")] string profileId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_SetSshKeyProfileAssetAccounts", Title = "AssetPartitions - SetSshKeyProfileAssetAccounts",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the accounts that are explicitly using this SSH key profile.")]
    public Task<string> AssetPartitions_SetSshKeyProfileAssetAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId,
        [Description("Users to assign to the profile.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/Accounts", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifySshKeyProfileAssetAccounts", Title = "AssetPartitions - ModifySshKeyProfileAssetAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove accounts to an existing SSH key profile.")]
    public Task<string> AssetPartitions_ModifySshKeyProfileAssetAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Users to assign to the profile.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/Accounts/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyProfileAssets", Title = "AssetPartitions - GetSshKeyProfileAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the Assets that are explicitly using this SSH key profile.")]
    public Task<string> AssetPartitions_GetSshKeyProfileAssets(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/Assets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_SetSshKeyProfileAssets", Title = "AssetPartitions - SetSshKeyProfileAssets",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the assets that are explicitly using this SSH key profile.")]
    public Task<string> AssetPartitions_SetSshKeyProfileAssets(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId,
        [Description("Users to assign to the profile.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/Assets", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifySshKeyProfileAssets", Title = "AssetPartitions - ModifySshKeyProfileAssets",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove accounts to an existing SSH key profile.")]
    public Task<string> AssetPartitions_ModifySshKeyProfileAssets(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Users to assign to the profile.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/Assets/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyChangeSchedule", Title = "AssetPartitions - GetSshKeyChangeSchedule",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the change schedule on the selected partition profile.")]
    public Task<string> AssetPartitions_GetSshKeyChangeSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/ChangeSchedule");

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyCheckSchedule", Title = "AssetPartitions - GetSshKeyCheckSchedule",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the check schedule on the selected partition profile.")]
    public Task<string> AssetPartitions_GetSshKeyCheckSchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/CheckSchedule");

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyProfileDiscoverySchedule", Title = "AssetPartitions - GetSshKeyProfileDiscoverySchedule",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the discovery schedule on the selected partition profile.")]
    public Task<string> AssetPartitions_GetSshKeyProfileDiscoverySchedule(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique identifier of the SshKeyProfile.")] string profileId)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/DiscoverySchedule");

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeySyncGroups", Title = "AssetPartitions - GetSshKeySyncGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of sync groups for a specific profile.")]
    public Task<string> AssetPartitions_GetSshKeySyncGroups(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateSshKeySyncGroup", Title = "AssetPartitions - CreateSshKeySyncGroup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new profile sync group.")]
    public Task<string> AssetPartitions_CreateSshKeySyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("SshKeySyncGroup to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeySyncGroupById", Title = "AssetPartitions - GetSshKeySyncGroupById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific profile sync group.")]
    public Task<string> AssetPartitions_GetSshKeySyncGroupById(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateSshKeySyncGroup", Title = "AssetPartitions - UpdateSshKeySyncGroup",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing profile sync group.")]
    public Task<string> AssetPartitions_UpdateSshKeySyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Updated SshKeySyncGroup.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteSshKeySyncGroup", Title = "AssetPartitions - DeleteSshKeySyncGroup",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a profile sync group.")]
    public Task<string> AssetPartitions_DeleteSshKeySyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}");

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeySyncGroupAccounts", Title = "AssetPartitions - GetSshKeySyncGroupAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all accounts that belong to a profile sync group.")]
    public Task<string> AssetPartitions_GetSshKeySyncGroupAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}/Accounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_SetSshKeyAccounts", Title = "AssetPartitions - SetSshKeyAccounts",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the accounts assigned to this group.")]
    public Task<string> AssetPartitions_SetSshKeyAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Accounts to assign to the AccountGroup.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}/Accounts", body);

    [McpServerTool(Name = "Core_AssetPartitions_ModifySshKeyAccounts", Title = "AssetPartitions - ModifySshKeyAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove accounts assigned to this group.")]
    public Task<string> AssetPartitions_ModifySshKeyAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Accounts to assign to the AccountGroup.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}/Accounts/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DisableSshKeySyncGroup", Title = "AssetPartitions - DisableSshKeySyncGroup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Disables an existing profile sync group.")]
    public Task<string> AssetPartitions_DisableSshKeySyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}/Disable");

    [McpServerTool(Name = "Core_AssetPartitions_EnableSshKeySyncGroup", Title = "AssetPartitions - EnableSshKeySyncGroup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enables an existing profile sync group.")]
    public Task<string> AssetPartitions_EnableSshKeySyncGroup(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}/Enable");

    [McpServerTool(Name = "Core_AssetPartitions_SetSshKey", Title = "AssetPartitions - SetSshKey",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the sync group SSH key.")]
    public Task<string> AssetPartitions_SetSshKey(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("SSH keyto set for this sync group.")] string body,
        [Description("The format of the SSH private key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}/SshKey" + Q(("keyFormat", keyFormat)), body);

    [McpServerTool(Name = "Core_AssetPartitions_SyncSshKeyAccounts", Title = "AssetPartitions - SyncSshKeyAccounts",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Sync credentials for all accounts in sync group.")]
    public Task<string> AssetPartitions_SyncSshKeyAccounts(McpServer server,
        [Description("Unique ID of asset partition.")] string id,
        [Description("Unique ID of profile.")] string profileId,
        [Description("Unique ID of sync group.")] string syncGroupId,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/SshKeyProfiles/{Uri.EscapeDataString(profileId)}/SshKeySyncGroups/{Uri.EscapeDataString(syncGroupId)}/Sync" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionTags", Title = "AssetPartitions - GetPartitionTags",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition tags.")]
    public Task<string> AssetPartitions_GetPartitionTags(McpServer server,
        [Description("Unique ID of partition.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_CreateTag", Title = "AssetPartitions - CreateTag",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new partition tag.")]
    public Task<string> AssetPartitions_CreateTag(McpServer server,
        [Description("Unique ID of partition.")] string id,
        [Description("Tag to create.")] string body = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionTagById", Title = "AssetPartitions - GetPartitionTagById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a partition tag.")]
    public Task<string> AssetPartitions_GetPartitionTagById(McpServer server,
        [Description("Unique ID of the partition.")] string id,
        [Description("Unique ID of the tag.")] string tagId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags/{Uri.EscapeDataString(tagId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_UpdateTag", Title = "AssetPartitions - UpdateTag",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a partition tag.")]
    public Task<string> AssetPartitions_UpdateTag(McpServer server,
        [Description("Unique ID of the partition.")] string id,
        [Description("Unique identifier of the tag.")] string tagId,
        [Description("Updated Tag.")] string body)
        => PutAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags/{Uri.EscapeDataString(tagId)}", body);

    [McpServerTool(Name = "Core_AssetPartitions_DeleteTag", Title = "AssetPartitions - DeleteTag",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a partition tag.")]
    public Task<string> AssetPartitions_DeleteTag(McpServer server,
        [Description("Unique ID of the partition.")] string id,
        [Description("Unique identifier of the tag.")] string tagId,
        [Description("Include 'X-Force-Delete' HTTP header or this query string parameter set to true to force delete despite dependencies when given 50104 error.")] string forceDelete = null)
        => DeleteAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags/{Uri.EscapeDataString(tagId)}" + Q(("forceDelete", forceDelete)));

    [McpServerTool(Name = "Core_AssetPartitions_GetObjectsWithTag", Title = "AssetPartitions - GetObjectsWithTag",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the tagged objects.")]
    public Task<string> AssetPartitions_GetObjectsWithTag(McpServer server,
        [Description("Unique ID of the partition.")] string id,
        [Description("Unique ID of the tag.")] string tagId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags/{Uri.EscapeDataString(tagId)}/Occurrences" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_TestTagAssetAccountRule", Title = "AssetPartitions - TestTagAssetAccountRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests an asset account partition tagging rule change against partition asset accounts.")]
    public Task<string> AssetPartitions_TestTagAssetAccountRule(McpServer server,
        [Description("Unique ID of the partition.")] string id,
        [Description("Unique ID of the partition tag.")] string tagId,
        [Description("Tagging rule to test.")] string body = null,
        [Description("Do not return no-op results.")] string operationalOnly = null,
        [Description("Items per page.")] string limit = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags/{Uri.EscapeDataString(tagId)}/TestAssetAccountRule" + Q(("operationalOnly", operationalOnly), ("limit", limit)), body);

    [McpServerTool(Name = "Core_AssetPartitions_TestTagAssetRule", Title = "AssetPartitions - TestTagAssetRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests an asset partition tagging rule change against partition assets.")]
    public Task<string> AssetPartitions_TestTagAssetRule(McpServer server,
        [Description("Unique ID of the partition.")] string id,
        [Description("Unique ID of the partition tag.")] string tagId,
        [Description("Tagging rule to test.")] string body = null,
        [Description("Do not return no-op results.")] string operationalOnly = null,
        [Description("Items per page.")] string limit = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags/{Uri.EscapeDataString(tagId)}/TestAssetRule" + Q(("operationalOnly", operationalOnly), ("limit", limit)), body);

    [McpServerTool(Name = "Core_AssetPartitions_TestAssetAccountRule", Title = "AssetPartitions - TestAssetAccountRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests an asset account partition tagging rule change against partition asset accounts.")]
    public Task<string> AssetPartitions_TestAssetAccountRule(McpServer server,
        [Description("Unique ID of the partition.")] string id,
        [Description("Tagging rule to test.")] string body = null,
        [Description("Do not return no-op results.")] string operationalOnly = null,
        [Description("Items per page.")] string limit = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags/TestAssetAccountRule" + Q(("operationalOnly", operationalOnly), ("limit", limit)), body);

    [McpServerTool(Name = "Core_AssetPartitions_TestAssetRule", Title = "AssetPartitions - TestAssetRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests an asset partition tagging rule change against partition assets.")]
    public Task<string> AssetPartitions_TestAssetRule(McpServer server,
        [Description("Unique ID of the partition.")] string id,
        [Description("Tagging rule to test.")] string body = null,
        [Description("Do not return no-op results.")] string operationalOnly = null,
        [Description("Items per page.")] string limit = null)
        => PostAsync(server, $"/v4/AssetPartitions/{Uri.EscapeDataString(id)}/Tags/TestAssetRule" + Q(("operationalOnly", operationalOnly), ("limit", limit)), body);

    [McpServerTool(Name = "Core_AssetPartitions_GetAllAccountDiscovery", Title = "AssetPartitions - GetAllAccountDiscovery",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of PartitionProfileAccountDiscoverySchedules.")]
    public Task<string> AssetPartitions_GetAllAccountDiscovery(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/AccountDiscoverySchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAccountDiscoveryScheduleById", Title = "AssetPartitions - GetAccountDiscoveryScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a PartitionProfileAccountDiscoverySchedule.")]
    public Task<string> AssetPartitions_GetAccountDiscoveryScheduleById(McpServer server,
        [Description("Unique ID of PartitionProfileAccountDiscoverySchedule.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/AccountDiscoverySchedules/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllChangeSchedules", Title = "AssetPartitions - GetAllChangeSchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition change schedules across all partitions.")]
    public Task<string> AssetPartitions_GetAllChangeSchedules(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/ChangeSchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetChangeScheduleById", Title = "AssetPartitions - GetChangeScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single PartitionProfileChangeSchedule.")]
    public Task<string> AssetPartitions_GetChangeScheduleById(McpServer server,
        [Description("Unique ID of a PartitionProfileChangeSchedule.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/ChangeSchedules/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllCheckSchedules", Title = "AssetPartitions - GetAllCheckSchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition password check schedules.")]
    public Task<string> AssetPartitions_GetAllCheckSchedules(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/CheckSchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetCheckScheduleById", Title = "AssetPartitions - GetCheckScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single PartitionProfileCheckSchedule.")]
    public Task<string> AssetPartitions_GetCheckScheduleById(McpServer server,
        [Description("Unique ID of a PartitionProfileCheckSchedule.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/CheckSchedules/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllDiscoveredAccounts", Title = "AssetPartitions - GetAllDiscoveredAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all asset partition's discovered accounts.")]
    public Task<string> AssetPartitions_GetAllDiscoveredAccounts(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/DiscoveredAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllDiscoveredServices", Title = "AssetPartitions - GetAllDiscoveredServices",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all asset partition's discovered services.")]
    public Task<string> AssetPartitions_GetAllDiscoveredServices(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/DiscoveredServices" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllDiscoveredSshKeys", Title = "AssetPartitions - GetAllDiscoveredSshKeys",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the discovered SSH keys for all asset partitions.")]
    public Task<string> AssetPartitions_GetAllDiscoveredSshKeys(McpServer server,
        [Description("The format of the SSH private key (defaults to OpenSsh) - OpenSsh - OpenSSH legacy PEM format - Ssh2 - Tectia format for use with tools from SSH.com - Putty - Putty format for use with PuTTY tools.")] string keyFormat = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/DiscoveredSshKeys" + Q(("keyFormat", keyFormat), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllDiscoveryJobs", Title = "AssetPartitions - GetAllDiscoveryJobs",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of asset discovery jobs.")]
    public Task<string> AssetPartitions_GetAllDiscoveryJobs(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/DiscoveryJobs" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetDiscoveryJobById", Title = "AssetPartitions - GetDiscoveryJobById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific asset discovery job.")]
    public Task<string> AssetPartitions_GetDiscoveryJobById(McpServer server,
        [Description("Unique ID of the asset discovery job.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/DiscoveryJobs/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllPasswordRules", Title = "AssetPartitions - GetAllPasswordRules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of password rules across all partitions.")]
    public Task<string> AssetPartitions_GetAllPasswordRules(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/PasswordRules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetPasswordRuleById", Title = "AssetPartitions - GetPasswordRuleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific password rule.")]
    public Task<string> AssetPartitions_GetPasswordRuleById(McpServer server,
        [Description("Unique ID of a PasswordRule.")] string ruleId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/PasswordRules/{Uri.EscapeDataString(ruleId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GeneratePasswordFromCustomRule", Title = "AssetPartitions - GeneratePasswordFromCustomRule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Generates a random password using this rule.")]
    public Task<string> AssetPartitions_GeneratePasswordFromCustomRule(McpServer server,
        [Description("A password rule used to generate account password during password change automation applied to a partition profile.")] string body = null)
        => PostAsync(server, "/v4/AssetPartitions/PasswordRules/GeneratePassword", body);

    [McpServerTool(Name = "Core_AssetPartitions_GetAllProfiles", Title = "AssetPartitions - GetAllProfiles",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of profiles.")]
    public Task<string> AssetPartitions_GetAllProfiles(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/Profiles" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetProfileById", Title = "AssetPartitions - GetProfileById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a profile.")]
    public Task<string> AssetPartitions_GetProfileById(McpServer server,
        [Description("Unique ID of PartitionProfile.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/Profiles/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllScheduleSummaries", Title = "AssetPartitions - GetAllScheduleSummaries",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a summary of all profiles schedules across all partitions.")]
    public Task<string> AssetPartitions_GetAllScheduleSummaries(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/ScheduleSummaries" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllSshKeyChangeSchedules", Title = "AssetPartitions - GetAllSshKeyChangeSchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition change schedules across all partitions.")]
    public Task<string> AssetPartitions_GetAllSshKeyChangeSchedules(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/SshKeyChangeSchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyChangeScheduleById", Title = "AssetPartitions - GetSshKeyChangeScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single SshKeyChangeSchedule.")]
    public Task<string> AssetPartitions_GetSshKeyChangeScheduleById(McpServer server,
        [Description("Unique ID of a SshKeyChangeSchedule.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/SshKeyChangeSchedules/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllSshKeyCheckSchedules", Title = "AssetPartitions - GetAllSshKeyCheckSchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition SSH key check schedules.")]
    public Task<string> AssetPartitions_GetAllSshKeyCheckSchedules(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/SshKeyCheckSchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyCheckScheduleById", Title = "AssetPartitions - GetSshKeyCheckScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single SshKeyCheckSchedule.")]
    public Task<string> AssetPartitions_GetSshKeyCheckScheduleById(McpServer server,
        [Description("Unique ID of a SshKeyCheckSchedule.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/SshKeyCheckSchedules/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllSshKeyDiscoverySchedules", Title = "AssetPartitions - GetAllSshKeyDiscoverySchedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition Discovery schedules across all partitions.")]
    public Task<string> AssetPartitions_GetAllSshKeyDiscoverySchedules(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/SshKeyDiscoverySchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetSshKeyDiscoveryScheduleById", Title = "AssetPartitions - GetSshKeyDiscoveryScheduleById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single SshKeyDiscoverySchedule.")]
    public Task<string> AssetPartitions_GetSshKeyDiscoveryScheduleById(McpServer server,
        [Description("Unique ID of a SshKeyDiscoverySchedule.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/SshKeyDiscoverySchedules/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllSshKeyProfiles", Title = "AssetPartitions - GetAllSshKeyProfiles",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of SSH key profiles across all partitions.")]
    public Task<string> AssetPartitions_GetAllSshKeyProfiles(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/SshKeyProfiles" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllSshKeyProfileById", Title = "AssetPartitions - GetAllSshKeyProfileById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an SSH key profile.")]
    public Task<string> AssetPartitions_GetAllSshKeyProfileById(McpServer server,
        [Description("Unique ID of SshKeyProfile.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/SshKeyProfiles/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllSshKeySyncGroups", Title = "AssetPartitions - GetAllSshKeySyncGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of all SSH key sync groups.")]
    public Task<string> AssetPartitions_GetAllSshKeySyncGroups(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/SshKeySyncGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllSshKeySyncGroupsById", Title = "AssetPartitions - GetAllSshKeySyncGroupsById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific SSH key sync group.")]
    public Task<string> AssetPartitions_GetAllSshKeySyncGroupsById(McpServer server,
        [Description("Unique ID of the SSH key sync group.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/SshKeySyncGroups/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllSyncGroups", Title = "AssetPartitions - GetAllSyncGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of all partition sync groups.")]
    public Task<string> AssetPartitions_GetAllSyncGroups(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/SyncGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetSyncGroupsById", Title = "AssetPartitions - GetSyncGroupsById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific profile sync group.")]
    public Task<string> AssetPartitions_GetSyncGroupsById(McpServer server,
        [Description("Unique ID of the sync group.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/SyncGroups/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetAllTags", Title = "AssetPartitions - GetAllTags",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of partition tags across partitions.")]
    public Task<string> AssetPartitions_GetAllTags(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/AssetPartitions/Tags" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_AssetPartitions_GetTagById", Title = "AssetPartitions - GetTagById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a partition tag.")]
    public Task<string> AssetPartitions_GetTagById(McpServer server,
        [Description("Unique ID of the tag.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/AssetPartitions/Tags/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_AssetPartitions_GetPartitionTagManagedBy", Title = "AssetPartitions - GetPartitionTagManagedBy",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a partition tag managed by.")]
    public Task<string> AssetPartitions_GetPartitionTagManagedBy(McpServer server,
        [Description("Unique ID of the tag.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/AssetPartitions/Tags/{Uri.EscapeDataString(id)}/ManagedBy" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));
}
