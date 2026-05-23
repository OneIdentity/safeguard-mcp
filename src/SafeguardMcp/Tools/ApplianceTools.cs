// This code was auto generated.


using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

[McpServerToolType]
public class ApplianceTools(SafeguardAuth auth)
{
    private const string Service = "service/Appliance";

    private async Task<string> GetAsync(McpServer server, string path)
    {
        var host = await auth.EnsureAuthenticatedAsync(server, null, CancellationToken.None);
        return await auth.RequestAsync(host, HttpMethod.Get, auth.BuildUrl(host, Service, path));
    }

    private async Task<string> PostAsync(McpServer server, string path, string body = null)
    {
        var host = await auth.EnsureAuthenticatedAsync(server, null, CancellationToken.None);
        return await auth.RequestAsync(host, HttpMethod.Post, auth.BuildUrl(host, Service, path), body);
    }

    private async Task<string> PutAsync(McpServer server, string path, string body)
    {
        var host = await auth.EnsureAuthenticatedAsync(server, null, CancellationToken.None);
        return await auth.RequestAsync(host, HttpMethod.Put, auth.BuildUrl(host, Service, path), body);
    }

    private async Task<string> DeleteAsync(McpServer server, string path)
    {
        var host = await auth.EnsureAuthenticatedAsync(server, null, CancellationToken.None);
        return await auth.RequestAsync(host, HttpMethod.Delete, auth.BuildUrl(host, Service, path));
    }

    /// <summary>Builds a query string from name/value pairs, omitting null or empty values.</summary>
    private static string Q(params (string name, string value)[] ps)
    {
        var parts = new List<string>();
        foreach (var (name, value) in ps)
        {
            if (!string.IsNullOrEmpty(value))
                parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
        }
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    // ─── A2AService ────────────────────────────────────────────────────

    [McpServerTool(Name = "A2AService_Get", Title = "Get A2A Service",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get A2A service status including service name, display name, and enabled status.")]
    public Task<string> A2AService_Get(McpServer server,
        [Description("Comma-separated property names to include in output. Prepend with '-' to exclude.")] string fields = null)
        => GetAsync(server, "/v4/A2AService" + Q(("fields", fields)));

    [McpServerTool(Name = "A2AService_GetConfig", Title = "Get A2A Service Configuration",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Display the current configuration of the A2A service.")]
    public Task<string> A2AService_GetConfig(McpServer server,
        [Description("Comma-separated property names to include in output. Prepend with '-' to exclude.")] string fields = null)
        => GetAsync(server, "/v4/A2AService/Config" + Q(("fields", fields)));

    [McpServerTool(Name = "A2AService_UpdateConfig", Title = "Update A2A Service Configuration",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the A2A service configuration. Requires ApplianceAdmin permission.")]
    public Task<string> A2AService_UpdateConfig(McpServer server,
        [Description("JSON body with A2A service configuration properties.")] string body)
        => PutAsync(server, "/v4/A2AService/Config", body);

    [McpServerTool(Name = "A2AService_Disable", Title = "Disable A2A Service",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Disables the A2A service. Requires ApplianceAdmin permission.")]
    public Task<string> A2AService_Disable(McpServer server)
        => PostAsync(server, "/v4/A2AService/Disable");

    [McpServerTool(Name = "A2AService_Enable", Title = "Enable A2A Service",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Enables the A2A service. Requires ApplianceAdmin permission.")]
    public Task<string> A2AService_Enable(McpServer server)
        => PostAsync(server, "/v4/A2AService/Enable");

    [McpServerTool(Name = "A2AService_GetStatus", Title = "Get A2A Service Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the status of the A2A service.")]
    public Task<string> A2AService_GetStatus(McpServer server,
        [Description("Comma-separated property names to include in output. Prepend with '-' to exclude.")] string fields = null)
        => GetAsync(server, "/v4/A2AService/Status" + Q(("fields", fields)));

    // ─── ApplianceStatus ───────────────────────────────────────────────

    [McpServerTool(Name = "ApplianceStatus_Get", Title = "Get Appliance Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the appliance status including name, state, version, network information, and current time.")]
    public Task<string> ApplianceStatus_Get(McpServer server,
        [Description("Comma-separated property names to include in output. Prepend with '-' to exclude.")] string fields = null)
        => GetAsync(server, "/v4/ApplianceStatus" + Q(("fields", fields)));

    [McpServerTool(Name = "ApplianceStatus_FactoryReset", Title = "Factory Reset Appliance",
        ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = true)]
    [Description("Factory resets the appliance. WARNING: This is a destructive, irreversible operation. Requires ApplianceAdmin permission.")]
    public Task<string> ApplianceStatus_FactoryReset(McpServer server,
        [Description("JSON body with factory reset options (e.g. Reason).")] string body = null)
        => PostAsync(server, "/v4/ApplianceStatus/FactoryReset", body);

    [McpServerTool(Name = "ApplianceStatus_GetHealth", Title = "Get Appliance Health",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the appliance's health status.")]
    public Task<string> ApplianceStatus_GetHealth(McpServer server)
        => GetAsync(server, "/v4/ApplianceStatus/Health");

    [McpServerTool(Name = "ApplianceStatus_SetHostDnsName", Title = "Set Host DNS Name",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the host DNS name of the appliance. Requires ApplianceAdmin permission.")]
    public Task<string> ApplianceStatus_SetHostDnsName(McpServer server,
        [Description("JSON string value for the new host DNS name.")] string body)
        => PutAsync(server, "/v4/ApplianceStatus/HostDnsName", body);

    [McpServerTool(Name = "ApplianceStatus_SetHostDnsSuffix", Title = "Set Host DNS Suffix",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the host DNS suffix of the appliance. Requires ApplianceAdmin permission.")]
    public Task<string> ApplianceStatus_SetHostDnsSuffix(McpServer server,
        [Description("JSON string value for the new host DNS suffix.")] string body)
        => PutAsync(server, "/v4/ApplianceStatus/HostDnsSuffix", body);

    [McpServerTool(Name = "ApplianceStatus_SetName", Title = "Set Appliance Name",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the appliance name. Requires ApplianceAdmin permission.")]
    public Task<string> ApplianceStatus_SetName(McpServer server,
        [Description("JSON string value for the new appliance name.")] string body)
        => PutAsync(server, "/v4/ApplianceStatus/Name", body);

    [McpServerTool(Name = "ApplianceStatus_Reboot", Title = "Reboot Appliance",
        ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = true)]
    [Description("Reboots the appliance. WARNING: This will restart the appliance. Requires ApplianceAdmin permission.")]
    public Task<string> ApplianceStatus_Reboot(McpServer server,
        [Description("JSON body with reboot options (e.g. Reason).")] string body = null)
        => PostAsync(server, "/v4/ApplianceStatus/Reboot", body);

    [McpServerTool(Name = "ApplianceStatus_GetSecureSsl", Title = "Get Secure SSL Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets whether secure SSL is enabled on the appliance.")]
    public Task<string> ApplianceStatus_GetSecureSsl(McpServer server)
        => GetAsync(server, "/v4/ApplianceStatus/SecureSsl");

    [McpServerTool(Name = "ApplianceStatus_SetSecureSsl", Title = "Set Secure SSL",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Enables or disables secure SSL on the appliance. Requires ApplianceAdmin permission.")]
    public Task<string> ApplianceStatus_SetSecureSsl(McpServer server,
        [Description("JSON boolean value (true or false) for the secure SSL setting.")] string body)
        => PutAsync(server, "/v4/ApplianceStatus/SecureSsl", body);

    [McpServerTool(Name = "ApplianceStatus_Shutdown", Title = "Shutdown Appliance",
        ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = true)]
    [Description("Shuts down the appliance. WARNING: The appliance will power off. Requires ApplianceAdmin permission.")]
    public Task<string> ApplianceStatus_Shutdown(McpServer server,
        [Description("JSON body with shutdown options (e.g. Reason).")] string body = null)
        => PostAsync(server, "/v4/ApplianceStatus/Shutdown", body);

    [McpServerTool(Name = "ApplianceStatus_GetStrictCrlChecking", Title = "Get Strict CRL Checking",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the current strict CRL checking configuration.")]
    public Task<string> ApplianceStatus_GetStrictCrlChecking(McpServer server)
        => GetAsync(server, "/v4/ApplianceStatus/StrictCrlChecking");

    [McpServerTool(Name = "ApplianceStatus_SetStrictCrlChecking", Title = "Set Strict CRL Checking",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Enables or disables strict CRL checking. Requires ApplianceAdmin permission.")]
    public Task<string> ApplianceStatus_SetStrictCrlChecking(McpServer server,
        [Description("JSON boolean value (true or false) for strict CRL checking.")] string body)
        => PutAsync(server, "/v4/ApplianceStatus/StrictCrlChecking", body);

    [McpServerTool(Name = "ApplianceStatus_SetVpnLogLevel", Title = "Set VPN Log Level",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the VPN log level. Requires ApplianceAdmin permission.")]
    public Task<string> ApplianceStatus_SetVpnLogLevel(McpServer server,
        [Description("JSON body with the VPN log level setting.")] string body)
        => PostAsync(server, "/v4/ApplianceStatus/Vpn/LogLevel", body);

    [McpServerTool(Name = "ApplianceStatus_GetVpnStatus", Title = "Get VPN Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the VPN connection status of the appliance.")]
    public Task<string> ApplianceStatus_GetVpnStatus(McpServer server,
        [Description("Comma-separated property names to include in output. Prepend with '-' to exclude.")] string fields = null)
        => GetAsync(server, "/v4/ApplianceStatus/Vpn/Status" + Q(("fields", fields)));

    // ─── Backups ───────────────────────────────────────────────────────

    [McpServerTool(Name = "Backups_Get", Title = "Get Backups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the list of backups on the appliance.")]
    public Task<string> Backups_Get(McpServer server,
        [Description("Filter expression (e.g. \"Name eq 'mybackup'\").")] string filter = null,
        [Description("Page number for pagination.")] string page = null,
        [Description("Maximum number of items per page.")] string limit = null,
        [Description("Set to 'true' to include total count in response.")] string count = null,
        [Description("Comma-separated property names to include in output.")] string fields = null,
        [Description("Order by expression (e.g. \"CreatedDate desc\").")] string orderby = null,
        [Description("Search query text.")] string q = null)
        => GetAsync(server, "/v4/Backups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Backups_Create", Title = "Create Backup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new backup of the appliance.")]
    public Task<string> Backups_Create(McpServer server,
        [Description("JSON body with backup options (e.g. Name).")] string body = null)
        => PostAsync(server, "/v4/Backups", body);

    [McpServerTool(Name = "Backups_GetById", Title = "Get Backup by ID",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific backup by its ID.")]
    public Task<string> Backups_GetById(McpServer server,
        [Description("The unique ID of the backup.")] string id,
        [Description("Comma-separated property names to include in output.")] string fields = null)
        => GetAsync(server, $"/v4/Backups/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Backups_Delete", Title = "Delete Backup",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes a backup by its ID.")]
    public Task<string> Backups_Delete(McpServer server,
        [Description("The unique ID of the backup to delete.")] string id,
        [Description("Set to 'true' to wait for a running backup to cancel before deleting.")] string waitForCancel = null)
        => DeleteAsync(server, $"/v4/Backups/{Uri.EscapeDataString(id)}" + Q(("waitForCancel", waitForCancel)));

    [McpServerTool(Name = "Backups_Archive", Title = "Archive Backup",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Archives a backup to an external archive server.")]
    public Task<string> Backups_Archive(McpServer server,
        [Description("The unique ID of the backup to archive.")] string id,
        [Description("JSON body with archive target configuration.")] string body = null)
        => PostAsync(server, $"/v4/Backups/{Uri.EscapeDataString(id)}/Archive", body);

    [McpServerTool(Name = "Backups_RestoreById", Title = "Restore Backup by ID",
        ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = true)]
    [Description("Restores the appliance from a specific backup. WARNING: This is a destructive operation that replaces current state.")]
    public Task<string> Backups_RestoreById(McpServer server,
        [Description("The unique ID of the backup to restore.")] string id,
        [Description("JSON body with restore options.")] string body = null)
        => PostAsync(server, $"/v4/Backups/{Uri.EscapeDataString(id)}/Restore", body);

    [McpServerTool(Name = "Backups_GetRestoreWarnings", Title = "Get Restore Precondition Warnings",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets restore precondition warnings for a specific backup before restoring.")]
    public Task<string> Backups_GetRestoreWarnings(McpServer server,
        [Description("The unique ID of the backup.")] string id,
        [Description("JSON body with restore options to check preconditions against.")] string body = null)
        => PostAsync(server, $"/v4/Backups/{Uri.EscapeDataString(id)}/Warnings", body);

    [McpServerTool(Name = "Backups_CancelUpload", Title = "Cancel Backup Upload",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Cancels an in-progress backup upload.")]
    public Task<string> Backups_CancelUpload(McpServer server)
        => DeleteAsync(server, "/v4/Backups/CancelUpload");

    [McpServerTool(Name = "Backups_FromRemote", Title = "Backup from Remote",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Instructs Safeguard to download a backup from a remote archive server.")]
    public Task<string> Backups_FromRemote(McpServer server,
        [Description("JSON body with remote archive server connection info and backup identifier.")] string body)
        => PostAsync(server, "/v4/Backups/FromRemote", body);

    [McpServerTool(Name = "Backups_GetRestoreStatus", Title = "Get Restore Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the current status of a restore operation.")]
    public Task<string> Backups_GetRestoreStatus(McpServer server)
        => GetAsync(server, "/v4/Backups/Restore");

    // ─── BmcConfiguration ──────────────────────────────────────────────

    [McpServerTool(Name = "BmcConfiguration_Get", Title = "Get BMC Configuration",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the Baseboard Management Controller (BMC/IPMI) configuration. Hardware appliances only.")]
    public Task<string> BmcConfiguration_Get(McpServer server,
        [Description("Comma-separated property names to include in output.")] string fields = null)
        => GetAsync(server, "/v4/BmcConfiguration" + Q(("fields", fields)));

    [McpServerTool(Name = "BmcConfiguration_Update", Title = "Update BMC Configuration",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the BMC/IPMI configuration. Hardware appliances only. Requires ApplianceAdmin permission.")]
    public Task<string> BmcConfiguration_Update(McpServer server,
        [Description("JSON body with BMC configuration properties (e.g. IpAddress, SubnetMask, DefaultGateway, Enabled).")] string body)
        => PutAsync(server, "/v4/BmcConfiguration", body);

    // ─── CpuStatuses ───────────────────────────────────────────────────

    [McpServerTool(Name = "CpuStatuses_Get", Title = "Get CPU Statuses",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the status of all CPUs in the appliance.")]
    public Task<string> CpuStatuses_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/CpuStatuses" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "CpuStatuses_GetById", Title = "Get CPU Status by ID",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the status of a specific CPU by its ID.")]
    public Task<string> CpuStatuses_GetById(McpServer server,
        [Description("The unique ID of the CPU.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/CpuStatuses/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    // ─── DiagnosticPackage ─────────────────────────────────────────────

    [McpServerTool(Name = "DiagnosticPackage_Get", Title = "Get Diagnostic Package",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets information about the currently staged diagnostic package.")]
    public Task<string> DiagnosticPackage_Get(McpServer server)
        => GetAsync(server, "/v4/DiagnosticPackage");

    [McpServerTool(Name = "DiagnosticPackage_Delete", Title = "Delete Diagnostic Package",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes the currently staged diagnostic package.")]
    public Task<string> DiagnosticPackage_Delete(McpServer server)
        => DeleteAsync(server, "/v4/DiagnosticPackage");

    [McpServerTool(Name = "DiagnosticPackage_Execute", Title = "Execute Diagnostic Package",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Executes the currently staged diagnostic package.")]
    public Task<string> DiagnosticPackage_Execute(McpServer server)
        => PostAsync(server, "/v4/DiagnosticPackage/Execute");

    [McpServerTool(Name = "DiagnosticPackage_FromRemote", Title = "Diagnostic Package from Remote",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Instructs Safeguard to download a diagnostic package from a remote location.")]
    public Task<string> DiagnosticPackage_FromRemote(McpServer server,
        [Description("JSON body with remote location info (e.g. Url, Username, Password).")] string body)
        => PostAsync(server, "/v4/DiagnosticPackage/FromRemote", body);

    [McpServerTool(Name = "DiagnosticPackage_GetLog", Title = "Get Diagnostic Package Log",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the execution log of the last diagnostic package run.")]
    public Task<string> DiagnosticPackage_GetLog(McpServer server)
        => GetAsync(server, "/v4/DiagnosticPackage/Log");

    // ─── DiskStatus ────────────────────────────────────────────────────

    [McpServerTool(Name = "DiskStatus_Get", Title = "Get Disk Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the disk status of the appliance including capacity and usage.")]
    public Task<string> DiskStatus_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/DiskStatus" + Q(("fields", fields)));

    // ─── HardwareStatus ────────────────────────────────────────────────

    [McpServerTool(Name = "HardwareStatus_Get", Title = "Get Hardware Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the hardware status of the appliance including hardware model and health indicators.")]
    public Task<string> HardwareStatus_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/HardwareStatus" + Q(("fields", fields)));

    // ─── Health ────────────────────────────────────────────────────────

    [McpServerTool(Name = "Health_Get", Title = "Get Health Results",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets health check results for the appliance.")]
    public Task<string> Health_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Health" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    // ─── MaintenanceSchedules ──────────────────────────────────────────

    [McpServerTool(Name = "MaintenanceSchedules_Get", Title = "Get Maintenance Schedules",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all maintenance schedules configured on the appliance.")]
    public Task<string> MaintenanceSchedules_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/MaintenanceSchedules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "MaintenanceSchedules_GetByType", Title = "Get Maintenance Schedule by Type",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific maintenance schedule by its type (e.g. 'Backup', 'Patch').")]
    public Task<string> MaintenanceSchedules_GetByType(McpServer server,
        [Description("The maintenance type identifier.")] string maintenanceType,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/MaintenanceSchedules/{Uri.EscapeDataString(maintenanceType)}" + Q(("fields", fields)));

    [McpServerTool(Name = "MaintenanceSchedules_Update", Title = "Update Maintenance Schedule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a maintenance schedule. Requires ApplianceAdmin or OperationsAdmin permission.")]
    public Task<string> MaintenanceSchedules_Update(McpServer server,
        [Description("The maintenance type identifier.")] string maintenanceType,
        [Description("JSON body with updated schedule properties.")] string body)
        => PutAsync(server, $"/v4/MaintenanceSchedules/{Uri.EscapeDataString(maintenanceType)}", body);

    // ─── MemoryStatus ──────────────────────────────────────────────────

    [McpServerTool(Name = "MemoryStatus_Get", Title = "Get Memory Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the memory status of the appliance including total and available memory.")]
    public Task<string> MemoryStatus_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/MemoryStatus" + Q(("fields", fields)));

    // ─── NetworkDiagnostics ────────────────────────────────────────────

    [McpServerTool(Name = "NetworkDiagnostics_Get", Title = "Get Network Diagnostics",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets available network diagnostic operations.")]
    public Task<string> NetworkDiagnostics_Get(McpServer server)
        => GetAsync(server, "/v4/NetworkDiagnostics");

    [McpServerTool(Name = "NetworkDiagnostics_Arp", Title = "Run ARP Diagnostic",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Runs the ARP network diagnostic tool to display the ARP cache.")]
    public Task<string> NetworkDiagnostics_Arp(McpServer server)
        => PostAsync(server, "/v4/NetworkDiagnostics/Arp");

    [McpServerTool(Name = "NetworkDiagnostics_CldapPing", Title = "Run CLDAP Ping",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Runs a CLDAP ping to test connectivity to a domain controller.")]
    public Task<string> NetworkDiagnostics_CldapPing(McpServer server,
        [Description("JSON body with CldapPingInfo: DomainController (required), DnsDomainName.")] string body)
        => PostAsync(server, "/v4/NetworkDiagnostics/CldapPing", body);

    [McpServerTool(Name = "NetworkDiagnostics_FlushDns", Title = "Flush DNS Cache",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Flushes the DNS resolver cache on the appliance.")]
    public Task<string> NetworkDiagnostics_FlushDns(McpServer server)
        => PostAsync(server, "/v4/NetworkDiagnostics/FlushDns");

    [McpServerTool(Name = "NetworkDiagnostics_Netstat", Title = "Run Netstat",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Runs netstat to display active network connections and listening ports.")]
    public Task<string> NetworkDiagnostics_Netstat(McpServer server)
        => PostAsync(server, "/v4/NetworkDiagnostics/Netstat");

    [McpServerTool(Name = "NetworkDiagnostics_Nslookup", Title = "Run Nslookup",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Runs nslookup to query DNS records.")]
    public Task<string> NetworkDiagnostics_Nslookup(McpServer server,
        [Description("JSON body with NslookupInfo: NetworkAddress (required), DnsServer.")] string body)
        => PostAsync(server, "/v4/NetworkDiagnostics/Nslookup", body);

    [McpServerTool(Name = "NetworkDiagnostics_Ping", Title = "Run Ping",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Runs ping to test network connectivity to a host.")]
    public Task<string> NetworkDiagnostics_Ping(McpServer server,
        [Description("JSON body with PingInfo: NetworkAddress (required), NumberOfEchoes, BufferSize, DontFragmentFlag, TimeToLive, MillisecondTimeout, SourceInternal.")] string body)
        => PostAsync(server, "/v4/NetworkDiagnostics/Ping", body);

    [McpServerTool(Name = "NetworkDiagnostics_Routes", Title = "Get Network Routes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the network routing table from the appliance.")]
    public Task<string> NetworkDiagnostics_Routes(McpServer server)
        => PostAsync(server, "/v4/NetworkDiagnostics/Routes");

    [McpServerTool(Name = "NetworkDiagnostics_Telnet", Title = "Run Telnet Test",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Runs a telnet connection test to verify TCP connectivity to a host and port.")]
    public Task<string> NetworkDiagnostics_Telnet(McpServer server,
        [Description("JSON body with TelnetInfo: NetworkAddress (required), Port (required), MillisecondTimeout.")] string body)
        => PostAsync(server, "/v4/NetworkDiagnostics/Telnet", body);

    [McpServerTool(Name = "NetworkDiagnostics_Throughput", Title = "Run Throughput Test",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Runs a network throughput test to measure bandwidth to another appliance.")]
    public Task<string> NetworkDiagnostics_Throughput(McpServer server,
        [Description("JSON body with ThroughputInfo: NetworkAddress (required), DurationInSeconds, NumberOfStreams.")] string body)
        => PostAsync(server, "/v4/NetworkDiagnostics/Throughput", body);

    [McpServerTool(Name = "NetworkDiagnostics_Traceroute", Title = "Run Traceroute",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Runs traceroute to trace the network path to a host.")]
    public Task<string> NetworkDiagnostics_Traceroute(McpServer server,
        [Description("JSON body with TracerouteInfo: NetworkAddress (required), MaximumHops, MillisecondTimeout, DontFragmentFlag, SourceInternal.")] string body)
        => PostAsync(server, "/v4/NetworkDiagnostics/Traceroute", body);

    // ─── NetworkDnsSuffixConfig ────────────────────────────────────────

    [McpServerTool(Name = "NetworkDnsSuffixConfig_Get", Title = "Get DNS Suffix Configuration",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the current DNS suffix search list configuration.")]
    public Task<string> NetworkDnsSuffixConfig_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/NetworkDnsSuffixConfig" + Q(("fields", fields)));

    [McpServerTool(Name = "NetworkDnsSuffixConfig_Update", Title = "Update DNS Suffix Configuration",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the DNS suffix search list configuration. Requires ApplianceAdmin permission.")]
    public Task<string> NetworkDnsSuffixConfig_Update(McpServer server,
        [Description("JSON body with DNS suffix configuration (e.g. SearchSuffixes array).")] string body)
        => PutAsync(server, "/v4/NetworkDnsSuffixConfig", body);

    // ─── NetworkInterfaces ─────────────────────────────────────────────

    [McpServerTool(Name = "NetworkInterfaces_Get", Title = "Get Network Interfaces",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the list of network interfaces configured on the appliance.")]
    public Task<string> NetworkInterfaces_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/NetworkInterfaces" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "NetworkInterfaces_Create", Title = "Create Network Interface",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new network interface. Hardware appliances only. Requires ApplianceAdmin permission.")]
    public Task<string> NetworkInterfaces_Create(McpServer server,
        [Description("JSON body with network interface properties (e.g. Name, IpAddress, SubnetMask, DefaultGateway).")] string body)
        => PostAsync(server, "/v4/NetworkInterfaces", body);

    [McpServerTool(Name = "NetworkInterfaces_GetById", Title = "Get Network Interface by ID",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific network interface by its ID.")]
    public Task<string> NetworkInterfaces_GetById(McpServer server,
        [Description("The unique ID of the network interface.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/NetworkInterfaces/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "NetworkInterfaces_Update", Title = "Update Network Interface",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a network interface configuration. Requires ApplianceAdmin permission.")]
    public Task<string> NetworkInterfaces_Update(McpServer server,
        [Description("The unique ID of the network interface.")] string id,
        [Description("JSON body with updated network interface properties.")] string body)
        => PutAsync(server, $"/v4/NetworkInterfaces/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "NetworkInterfaces_Delete", Title = "Delete Network Interface",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a network interface. System-owned interfaces cannot be removed. Hardware appliances only.")]
    public Task<string> NetworkInterfaces_Delete(McpServer server,
        [Description("The unique ID of the network interface to delete.")] string id)
        => DeleteAsync(server, $"/v4/NetworkInterfaces/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "NetworkInterfaces_GetProxy", Title = "Get Proxy Configuration",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the current proxy server interface configuration and statuses.")]
    public Task<string> NetworkInterfaces_GetProxy(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/NetworkInterfaces/Proxy" + Q(("fields", fields)));

    [McpServerTool(Name = "NetworkInterfaces_UpdateProxy", Title = "Update Proxy Configuration",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the relay proxy configuration. Requires ApplianceAdmin permission.")]
    public Task<string> NetworkInterfaces_UpdateProxy(McpServer server,
        [Description("JSON body with proxy configuration properties.")] string body)
        => PutAsync(server, "/v4/NetworkInterfaces/Proxy", body);

    [McpServerTool(Name = "NetworkInterfaces_DeleteProxy", Title = "Delete Proxy Configuration",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Clears the proxy server configuration.")]
    public Task<string> NetworkInterfaces_DeleteProxy(McpServer server)
        => DeleteAsync(server, "/v4/NetworkInterfaces/Proxy");

    // ─── NtpClientConfig ───────────────────────────────────────────────

    [McpServerTool(Name = "NtpClientConfig_Get", Title = "Get NTP Configuration",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the NTP client configuration.")]
    public Task<string> NtpClientConfig_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/NtpClientConfig" + Q(("fields", fields)));

    [McpServerTool(Name = "NtpClientConfig_Update", Title = "Update NTP Configuration",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the appliance NTP configuration. Requires ApplianceAdmin permission.")]
    public Task<string> NtpClientConfig_Update(McpServer server,
        [Description("JSON body with NTP configuration properties (e.g. Enabled, NtpServers array).")] string body)
        => PutAsync(server, "/v4/NtpClientConfig", body);

    [McpServerTool(Name = "NtpClientConfig_Reset", Title = "Reset NTP Configuration",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Resets the NTP configuration to the default settings.")]
    public Task<string> NtpClientConfig_Reset(McpServer server)
        => PostAsync(server, "/v4/NtpClientConfig/Reset");

    [McpServerTool(Name = "NtpClientConfig_Resync", Title = "Resync NTP Client",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Forces the NTP client to resynchronize with configured NTP servers. The NTP client must be enabled.")]
    public Task<string> NtpClientConfig_Resync(McpServer server)
        => PostAsync(server, "/v4/NtpClientConfig/Resync");

    // ─── NtpClientStatus ───────────────────────────────────────────────

    [McpServerTool(Name = "NtpClientStatus_Get", Title = "Get NTP Client Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the status of the NTP client including synchronization state.")]
    public Task<string> NtpClientStatus_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/NtpClientStatus" + Q(("fields", fields)));

    // ─── OfflineWorkflowConfig ─────────────────────────────────────────

    [McpServerTool(Name = "OfflineWorkflowConfig_Get", Title = "Get Offline Workflow Configuration",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the offline workflow configuration settings.")]
    public Task<string> OfflineWorkflowConfig_Get(McpServer server)
        => GetAsync(server, "/v4/OfflineWorkflowConfig");

    [McpServerTool(Name = "OfflineWorkflowConfig_Update", Title = "Update Offline Workflow Configuration",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the offline workflow configuration settings. Requires ApplianceAdmin permission.")]
    public Task<string> OfflineWorkflowConfig_Update(McpServer server,
        [Description("JSON body with offline workflow configuration properties.")] string body)
        => PutAsync(server, "/v4/OfflineWorkflowConfig", body);

    // ─── OperatingSystem ───────────────────────────────────────────────

    [McpServerTool(Name = "OperatingSystem_Get", Title = "Get Operating System Status",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the status of the appliance operating system including version and license information.")]
    public Task<string> OperatingSystem_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/OperatingSystem" + Q(("fields", fields)));

    [McpServerTool(Name = "OperatingSystem_Activate", Title = "Activate OS with KMS",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Sets the Windows KMS server and tries to license/activate the operating system.")]
    public Task<string> OperatingSystem_Activate(McpServer server,
        [Description("JSON body with KMS activation info (e.g. KmsServer).")] string body)
        => PostAsync(server, "/v4/OperatingSystem/Activate", body);

    [McpServerTool(Name = "OperatingSystem_ProductKey", Title = "Activate OS with Product Key",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Sets the Windows product key and tries to license/activate the operating system.")]
    public Task<string> OperatingSystem_ProductKey(McpServer server,
        [Description("JSON body with product key activation info (e.g. ProductKey).")] string body)
        => PostAsync(server, "/v4/OperatingSystem/ProductKey", body);

    // ─── Patch ─────────────────────────────────────────────────────────

    [McpServerTool(Name = "Patch_Get", Title = "Get Staged Patch",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Returns a descriptor for the currently staged patch including the last run precondition check errors and warnings.")]
    public Task<string> Patch_Get(McpServer server)
        => GetAsync(server, "/v4/Patch");

    [McpServerTool(Name = "Patch_Delete", Title = "Remove Staged Patch",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes the currently staged patch.")]
    public Task<string> Patch_Delete(McpServer server)
        => DeleteAsync(server, "/v4/Patch");

    [McpServerTool(Name = "Patch_CancelUpload", Title = "Cancel Patch Upload",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Cancels the streaming upload of a patch.")]
    public Task<string> Patch_CancelUpload(McpServer server)
        => DeleteAsync(server, "/v4/Patch/CancelUpload");

    [McpServerTool(Name = "Patch_Distribute", Title = "Distribute Patch",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Distributes the staged patch to the replica appliances.")]
    public Task<string> Patch_Distribute(McpServer server)
        => PostAsync(server, "/v4/Patch/Distribute");

    [McpServerTool(Name = "Patch_CancelDistribution", Title = "Cancel Patch Distribution",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Cancels the cluster patch distribution process. Always triggered on the primary appliance.")]
    public Task<string> Patch_CancelDistribution(McpServer server)
        => DeleteAsync(server, "/v4/Patch/Distribute");

    [McpServerTool(Name = "Patch_FromRemote", Title = "Patch from Remote",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Instructs Safeguard to download a patch from a remote location.")]
    public Task<string> Patch_FromRemote(McpServer server,
        [Description("JSON body with remote location info (e.g. Url, Username, Password, SshHostKey).")] string body)
        => PostAsync(server, "/v4/Patch/FromRemote", body);

    [McpServerTool(Name = "Patch_Install", Title = "Install Patch",
        ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = true)]
    [Description("Installs the staged patch. WARNING: The appliance will enter maintenance mode. Requires ApplianceAdmin permission.")]
    public Task<string> Patch_Install(McpServer server)
        => PostAsync(server, "/v4/Patch/Install");

    // ─── Service ───────────────────────────────────────────────────────

    [McpServerTool(Name = "Service_Get", Title = "Get Service Endpoints",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the list of Safeguard service endpoints.")]
    public Task<string> Service_Get(McpServer server)
        => GetAsync(server, "/v4/Service");

    [McpServerTool(Name = "Service_GetDebug", Title = "Get Service Debug Configuration",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the service debug configuration.")]
    public Task<string> Service_GetDebug(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Service/Debug" + Q(("fields", fields)));

    [McpServerTool(Name = "Service_UpdateDebug", Title = "Update Service Debug Configuration",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the service debug configuration. Requires ApplianceAdmin permission.")]
    public Task<string> Service_UpdateDebug(McpServer server,
        [Description("JSON body with debug configuration properties.")] string body)
        => PutAsync(server, "/v4/Service/Debug", body);

    // ─── Settings ──────────────────────────────────────────────────────

    [McpServerTool(Name = "Settings_Get", Title = "Get Appliance Settings",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all of the appliance's settings.")]
    public Task<string> Settings_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Settings" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Settings_GetById", Title = "Get Setting by ID",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific appliance setting by its ID.")]
    public Task<string> Settings_GetById(McpServer server,
        [Description("The unique ID of the setting.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Settings/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Settings_Update", Title = "Update Setting",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a setting's value. Requires ApplianceAdmin permission.")]
    public Task<string> Settings_Update(McpServer server,
        [Description("The unique ID of the setting to update.")] string id,
        [Description("JSON body with the updated setting value.")] string body)
        => PutAsync(server, $"/v4/Settings/{Uri.EscapeDataString(id)}", body);

    // ─── SystemTime ────────────────────────────────────────────────────

    [McpServerTool(Name = "SystemTime_Get", Title = "Get System Time",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the current time on the appliance.")]
    public Task<string> SystemTime_Get(McpServer server)
        => GetAsync(server, "/v4/SystemTime");

    [McpServerTool(Name = "SystemTime_Update", Title = "Update System Time",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the time on the appliance. NOTE: Changing the time may cause temporary issues with HTTP date headers; a reboot may be needed.")]
    public Task<string> SystemTime_Update(McpServer server,
        [Description("JSON body with the new system time value.")] string body)
        => PutAsync(server, "/v4/SystemTime", body);

    // ─── Version ───────────────────────────────────────────────────────

    [McpServerTool(Name = "Version_Get", Title = "Get Software Version",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the version of the Safeguard software running on the appliance.")]
    public Task<string> Version_Get(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Version" + Q(("fields", fields)));
}
