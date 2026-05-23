using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

[McpServerToolType]
public class NotificationTools(SafeguardAuth auth)
{
    private const string Service = "service/Notification";

    private async Task<string> SafeguardGetAsync(McpServer server, string path)
    {
        var host = await auth.EnsureHostConfiguredAsync(server, null, CancellationToken.None);
        return await auth.RequestAsync(host, HttpMethod.Get, auth.BuildUrl(host, Service, path));
    }

    [McpServerTool(Name = "Status_Get", Title = "Get Appliance Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return the current status of the Safeguard appliance. "
        + "The response includes appliance state, identifier, name, version, current time, "
        + "last state change time, maintenance operation, cluster role flags (IsPrimary, IsReplica), "
        + "isolation/quarantine/read-only indicators, LCD status, IP address, OS details, "
        + "and session module join status.")]
    public Task<string> GetStatus(McpServer server)
        => SafeguardGetAsync(server, "/v4/Status");

    [McpServerTool(Name = "Status_GetAvailability", Title = "Get Appliance Availability",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return the current availability status of the Safeguard appliance. "
        + "The response is a superset of the standard status and additionally includes "
        + "service availability flags such as IsChangePasswordManagementAvailable, "
        + "IsCheckPasswordManagementAvailable, IsPasswordRequestAvailable, IsSessionRequestAvailable, "
        + "IsSshKeyRequestAvailable, IsApiKeyRequestAvailable, IsPolicyChangeAvailable, "
        + "IsProcessingScheduledTasks, IsClustered, last successful data/audit-log sync times, "
        + "and STA acceptance status.")]
    public Task<string> GetAvailability(McpServer server)
        => SafeguardGetAsync(server, "/v4/Status/Availability");

    [McpServerTool(Name = "Status_GetClusterStatus", Title = "Get Cluster Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return the current status of all cluster members. "
        + "The response is a JSON array of status objects, one per appliance in the cluster. "
        + "Each element contains the same fields as the single-appliance status endpoint.")]
    public Task<string> GetClusterStatus(McpServer server)
        => SafeguardGetAsync(server, "/v4/Status/Cluster");

    [McpServerTool(Name = "Status_GetClusterPatchStatus", Title = "Get Cluster Patch Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return the current state of a cluster patch operation. "
        + "When a patch is in progress the response includes start time, original and target versions, "
        + "patch title, per-appliance patch states (Waiting, InProgress, Complete, Error), "
        + "and whether primary services configuration has been restored. "
        + "Returns a descriptive message when no cluster patch is in progress (HTTP 204).")]
    public async Task<string> GetClusterPatchStatus(McpServer server)
    {
        var host = await auth.EnsureHostConfiguredAsync(server, null, CancellationToken.None);
        var result = await auth.RequestAsync(
            host, HttpMethod.Get, auth.BuildUrl(host, Service, "/v4/Status/ClusterPatch"));
        return string.IsNullOrEmpty(result) ? "No cluster patch in progress." : result;
    }

    [McpServerTool(Name = "Status_GetMaintenanceStatus", Title = "Get Maintenance Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return the current maintenance status of the Safeguard appliance. "
        + "The response includes maintenance start time, task type (e.g. Patch, Backup, Restore, "
        + "FactoryReset, Failover, etc.), task-specific data, completion flag, and an array of "
        + "maintenance steps each with state, progress percentage, and timing information.")]
    public Task<string> GetMaintenanceStatus(McpServer server)
        => SafeguardGetAsync(server, "/v4/Status/Maintenance");

    [McpServerTool(Name = "Status_GetState", Title = "Get Appliance State",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return the current state of the Safeguard appliance as a single string value. "
        + "Possible values: Unknown, EnrollingReplica, Initializing, LeavingCluster, Maintenance, "
        + "Offline, Online, PendingPatch, PendingPrimaryReboot, PrimaryNoQuorum, Quarantine, "
        + "ReplicaDisconnected, ReplicaNoQuorum, ReplicaWithQuorum, ShuttingDown, StandaloneReadOnly, "
        + "TransitioningToPrimary, TransitioningToReplica, Isolated, InitialSetupRequired, "
        + "HardwareSecurityModuleError.")]
    public Task<string> GetState(McpServer server)
        => SafeguardGetAsync(server, "/v4/Status/State");
}
