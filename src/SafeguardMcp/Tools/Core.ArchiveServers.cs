// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_ArchiveServers_Get", Title = "ArchiveServers - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of all archive servers.")]
    public Task<string> ArchiveServers_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/ArchiveServers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_ArchiveServers_CreateEntity", Title = "ArchiveServers - CreateEntity",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates an ArchiveServer configuration.")]
    public Task<string> ArchiveServers_CreateEntity(McpServer server,
        [Description("ArchiveServer to create.")] string body = null)
        => PostAsync(server, "/v4/ArchiveServers", body);

    [McpServerTool(Name = "Core_ArchiveServers_GetById", Title = "ArchiveServers - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single archive server.")]
    public Task<string> ArchiveServers_GetById(McpServer server,
        [Description("Unique ID of account group.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/ArchiveServers/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_ArchiveServers_UpdateEntity", Title = "ArchiveServers - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing ArchiveServer configuration.")]
    public Task<string> ArchiveServers_UpdateEntity(McpServer server,
        [Description("Unique identifier of the ArchiveServer.")] string id,
        [Description("ArchiveServer to create.")] string body)
        => PutAsync(server, $"/v4/ArchiveServers/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_ArchiveServers_Delete", Title = "ArchiveServers - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an ArchiveServer configuration.")]
    public Task<string> ArchiveServers_Delete(McpServer server,
        [Description("Unique identifier of the ArchiveServer.")] string id)
        => DeleteAsync(server, $"/v4/ArchiveServers/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_ArchiveServers_DiscoverSshHostKeyById", Title = "ArchiveServers - DiscoverSshHostKeyById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Gets the SSH host key for the target server.")]
    public Task<string> ArchiveServers_DiscoverSshHostKeyById(McpServer server,
        [Description("Unique ID of ArchiveServer to check.")] string id,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/ArchiveServers/{Uri.EscapeDataString(id)}/DiscoverSshHostKey" + Q(("extendedLogging", extendedLogging)));

    [McpServerTool(Name = "Core_ArchiveServers_InstallSshKeyById", Title = "ArchiveServers - InstallSshKeyById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Installs an SSH key for the service account.")]
    public Task<string> ArchiveServers_InstallSshKeyById(McpServer server,
        [Description("Unique identifier of the ArchiveServer.")] string id,
        [Description("Database ID of SSH Key to install (optional - will be generated if not specified). Also option to override existing asset connection settings.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, $"/v4/ArchiveServers/{Uri.EscapeDataString(id)}/InstallSshKey" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_ArchiveServers_GetSshHostKey", Title = "ArchiveServers - GetSshHostKey",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the SshHostKey identifying this archive server.")]
    public Task<string> ArchiveServers_GetSshHostKey(McpServer server,
        [Description("Unique identifier of the archive server.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/ArchiveServers/{Uri.EscapeDataString(id)}/SshHostKey" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_ArchiveServers_SetSshHostKey", Title = "ArchiveServers - SetSshHostKey",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the ssh host id of this archive server.")]
    public Task<string> ArchiveServers_SetSshHostKey(McpServer server,
        [Description("Unique identifier of the archive server.")] string id,
        [Description("SSH host id to assign to this asset.")] string body)
        => PutAsync(server, $"/v4/ArchiveServers/{Uri.EscapeDataString(id)}/SshHostKey", body);

    [McpServerTool(Name = "Core_ArchiveServers_RemoveSshHostKey", Title = "ArchiveServers - RemoveSshHostKey",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes the ssh host id of this archive server.")]
    public Task<string> ArchiveServers_RemoveSshHostKey(McpServer server,
        [Description("Unique identifier of the archive server.")] string id)
        => DeleteAsync(server, $"/v4/ArchiveServers/{Uri.EscapeDataString(id)}/SshHostKey");

    [McpServerTool(Name = "Core_ArchiveServers_TestConnectionById", Title = "ArchiveServers - TestConnectionById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests an existing ArchiveServer configuration.")]
    public Task<string> ArchiveServers_TestConnectionById(McpServer server,
        [Description("Unique ID of ArchiveServer to test.")] string id,
        [Description("Options for testing the connection.")] string body = null)
        => PostAsync(server, $"/v4/ArchiveServers/{Uri.EscapeDataString(id)}/TestConnection", body);

    [McpServerTool(Name = "Core_ArchiveServers_DiscoverSshHostKey", Title = "ArchiveServers - DiscoverSshHostKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Gets the SSH host key for the target server.")]
    public Task<string> ArchiveServers_DiscoverSshHostKey(McpServer server,
        [Description("Configuration of target server.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, "/v4/ArchiveServers/DiscoverSshHostKey" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_ArchiveServers_InstallSshKey", Title = "ArchiveServers - InstallSshKey",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Installs an SSH key for the service account.")]
    public Task<string> ArchiveServers_InstallSshKey(McpServer server,
        [Description("Information about which asset to install an SSH key for the service account.")] string body = null,
        [Description("Generate debug task log for action.")] string extendedLogging = null)
        => PostAsync(server, "/v4/ArchiveServers/InstallSshKey" + Q(("extendedLogging", extendedLogging)), body);

    [McpServerTool(Name = "Core_ArchiveServers_TestConnection", Title = "ArchiveServers - TestConnection",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Tests an ArchiveServer configuration.")]
    public Task<string> ArchiveServers_TestConnection(McpServer server,
        [Description("Custom archive server test connection parameters.")] string body = null)
        => PostAsync(server, "/v4/ArchiveServers/TestConnection", body);
}
