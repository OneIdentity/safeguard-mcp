// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Cluster_Get", Title = "Cluster - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("List of possible cluster actions.")]
    public Task<string> Cluster_Get(McpServer server)
        => GetAsync(server, "/v4/Cluster");

    [McpServerTool(Name = "Core_Cluster_GetApplications", Title = "Cluster - GetApplications",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Returns the list of applications.")]
    public Task<string> Cluster_GetApplications(McpServer server)
        => GetAsync(server, "/v4/Cluster/Applications");

    [McpServerTool(Name = "Core_Cluster_GetBackupProtectionSettings", Title = "Cluster - GetBackupProtectionSettings",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Returns Backup Protection Settings.")]
    public Task<string> Cluster_GetBackupProtectionSettings(McpServer server)
        => GetAsync(server, "/v4/Cluster/BackupProtectionSettings");

    [McpServerTool(Name = "Core_Cluster_SetBackupProtectionSettings", Title = "Cluster - SetBackupProtectionSettings",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the Backup Protection Settings.")]
    public Task<string> Cluster_SetBackupProtectionSettings(McpServer server,
        [Description("Specifies how a Safeguard backup file will be encrypted.")] string body)
        => PutAsync(server, "/v4/Cluster/BackupProtectionSettings", body);

    [McpServerTool(Name = "Core_Cluster_GetAllClusterCertificates", Title = "Cluster - GetAllClusterCertificates",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the list of cluster certificates.")]
    public Task<string> Cluster_GetAllClusterCertificates(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Cluster/Certificates" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Cluster_GetClusterCertificate", Title = "Cluster - GetClusterCertificate",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the public information for a cluster certificate by type.")]
    public Task<string> Cluster_GetClusterCertificate(McpServer server,
        [Description("Certificate type to get. Supported types are ClusterRoot, SecureTokenService, AuditLogSigning.")] string type,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Cluster/Certificates/{Uri.EscapeDataString(type)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Cluster_GetNetworks", Title = "Cluster - GetNetworks",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of managed networks configuration.")]
    public Task<string> Cluster_GetNetworks(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Cluster/ManagedNetworks" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Cluster_CreateEntity", Title = "Cluster - CreateEntity",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a managed network configuration.")]
    public Task<string> Cluster_CreateEntity(McpServer server,
        [Description("Entity to create.")] string body = null)
        => PostAsync(server, "/v4/Cluster/ManagedNetworks", body);

    [McpServerTool(Name = "Core_Cluster_GetById", Title = "Cluster - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific managed network configuration.")]
    public Task<string> Cluster_GetById(McpServer server,
        [Description("Unique ID of entity.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Cluster/ManagedNetworks/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Cluster_UpdateEntity", Title = "Cluster - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a managed network configuration.")]
    public Task<string> Cluster_UpdateEntity(McpServer server,
        [Description("Unique identifier of the entity.")] string id,
        [Description("Updated entity.")] string body)
        => PutAsync(server, $"/v4/Cluster/ManagedNetworks/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Cluster_Delete", Title = "Cluster - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a managed network configuration. Appliances and assets will be assigned to the default network.")]
    public Task<string> Cluster_Delete(McpServer server,
        [Description("Unique identifier of the entity.")] string id)
        => DeleteAsync(server, $"/v4/Cluster/ManagedNetworks/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Cluster_GetNetworkAssets", Title = "Cluster - GetNetworkAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all assets that have been explicitly assigned to the managed network configuration.")]
    public Task<string> Cluster_GetNetworkAssets(McpServer server,
        [Description("Unique identifier of the managed network.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Cluster/ManagedNetworks/{Uri.EscapeDataString(id)}/Assets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Cluster_SetNetworkSystems", Title = "Cluster - SetNetworkSystems",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Explicitly assign a set of assets to this managed network configuration.")]
    public Task<string> Cluster_SetNetworkSystems(McpServer server,
        [Description("Unique identifier of the entity.")] string id,
        [Description("Assets to assign.")] string body)
        => PutAsync(server, $"/v4/Cluster/ManagedNetworks/{Uri.EscapeDataString(id)}/Assets", body);

    [McpServerTool(Name = "Core_Cluster_ModifySystems", Title = "Cluster - ModifySystems",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove assets assigned to this managed network configuration.")]
    public Task<string> Cluster_ModifySystems(McpServer server,
        [Description("Unique identifier of the entity.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Assets to assign.")] string body = null)
        => PostAsync(server, $"/v4/Cluster/ManagedNetworks/{Uri.EscapeDataString(id)}/Assets/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Cluster_GetNetworkMembers", Title = "Cluster - GetNetworkMembers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all members that have been assigned to the managed network configuration.")]
    public Task<string> Cluster_GetNetworkMembers(McpServer server,
        [Description("Unique identifier of the managed network.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Cluster/ManagedNetworks/{Uri.EscapeDataString(id)}/Members" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Cluster_SetNetworkMembers", Title = "Cluster - SetNetworkMembers",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the members assigned to this managed network configuration.")]
    public Task<string> Cluster_SetNetworkMembers(McpServer server,
        [Description("Unique identifier of the entity.")] string id,
        [Description("Cluster members to assign.")] string body)
        => PutAsync(server, $"/v4/Cluster/ManagedNetworks/{Uri.EscapeDataString(id)}/Members", body);

    [McpServerTool(Name = "Core_Cluster_ModifyMembers", Title = "Cluster - ModifyMembers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove members assigned to this managed network configuration.")]
    public Task<string> Cluster_ModifyMembers(McpServer server,
        [Description("Unique identifier of the entity.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Member members to assign.")] string body = null)
        => PostAsync(server, $"/v4/Cluster/ManagedNetworks/{Uri.EscapeDataString(id)}/Members/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Cluster_TestNetworkAddress", Title = "Cluster - TestNetworkAddress",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Check which managed network the specified network address would be assigned to.")]
    public Task<string> Cluster_TestNetworkAddress(McpServer server,
        [Description("Network address to test.")] string body = null)
        => PostAsync(server, "/v4/Cluster/ManagedNetworks/TestAddress", body);

    [McpServerTool(Name = "Core_Cluster_GetMembers", Title = "Cluster - GetMembers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return information about all appliances in this cluster.")]
    public Task<string> Cluster_GetMembers(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Cluster/Members" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Cluster_EnrollMember", Title = "Cluster - EnrollMember",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enrolls a new member in this cluster.")]
    public Task<string> Cluster_EnrollMember(McpServer server,
        [Description("Information about the appliance enrolling.")] string body = null)
        => PostAsync(server, "/v4/Cluster/Members", body);

    [McpServerTool(Name = "Core_Cluster_GetMemberById", Title = "Cluster - GetMemberById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return information about a specific appliance in this cluster.")]
    public Task<string> Cluster_GetMemberById(McpServer server,
        [Description("Unique ID of cluster member.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Cluster/Members/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Cluster_DeleteMember", Title = "Cluster - DeleteMember",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Unjoin a specific member, by id, from the cluster.")]
    public Task<string> Cluster_DeleteMember(McpServer server,
        [Description("id of the member to be removed.")] string id)
        => DeleteAsync(server, $"/v4/Cluster/Members/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Cluster_GetClusterMemberNetworks", Title = "Cluster - GetClusterMemberNetworks",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all network configurations managed by this cluster member.")]
    public Task<string> Cluster_GetClusterMemberNetworks(McpServer server,
        [Description("Unique identifier of the cluster member.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Cluster/Members/{Uri.EscapeDataString(id)}/ManagedNetworks" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Cluster_SetClusterMemberNetworks", Title = "Cluster - SetClusterMemberNetworks",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Assigns this cluster member to a set of managed network configurations.")]
    public Task<string> Cluster_SetClusterMemberNetworks(McpServer server,
        [Description("Unique identifier of the entity.")] string id,
        [Description("Network configurations to assign this cluster member to.")] string body)
        => PutAsync(server, $"/v4/Cluster/Members/{Uri.EscapeDataString(id)}/ManagedNetworks", body);

    [McpServerTool(Name = "Core_Cluster_ModifyClusterMemberNetworks", Title = "Cluster - ModifyClusterMemberNetworks",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove network configuration assignments for this cluster member.")]
    public Task<string> Cluster_ModifyClusterMemberNetworks(McpServer server,
        [Description("Unique identifier of the entity.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Network configurations to assign this cluster member to.")] string body = null)
        => PostAsync(server, $"/v4/Cluster/Members/{Uri.EscapeDataString(id)}/ManagedNetworks/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Cluster_SetLeader", Title = "Cluster - SetLeader",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Designates a particular cluster member as the cluster leader.")]
    public Task<string> Cluster_SetLeader(McpServer server,
        [Description("id of the cluster member to promote.")] string id)
        => PostAsync(server, $"/v4/Cluster/Members/{Uri.EscapeDataString(id)}/Promote");

    [McpServerTool(Name = "Core_Cluster_ActivatePrimary", Title = "Cluster - ActivatePrimary",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Promotes this appliance from StandaloneReadOnly to Online./>.")]
    public Task<string> Cluster_ActivatePrimary(McpServer server)
        => PostAsync(server, "/v4/Cluster/Members/ActivatePrimary");

    [McpServerTool(Name = "Core_Cluster_ForceReset", Title = "Cluster - ForceReset",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Force the cluster configuration on this appliance to reset to a single appliance operating as a primary. After resetting the cluster configuration the appliance will come online in a deactivated mode to avoid conflicts with password check/change o...")]
    public Task<string> Cluster_ForceReset(McpServer server)
        => PostAsync(server, "/v4/Cluster/Members/ForceReset");

    [McpServerTool(Name = "Core_Cluster_ResetMember", Title = "Cluster - ResetMember",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Resets the cluster configuration regardless of consensus.")]
    public Task<string> Cluster_ResetMember(McpServer server,
        [Description("New cluster configuration.")] string body = null)
        => PostAsync(server, "/v4/Cluster/Members/Reset", body);

    [McpServerTool(Name = "Core_Cluster_GetSelf", Title = "Cluster - GetSelf",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Force a health check and return cluster information for this node.")]
    public Task<string> Cluster_GetSelf(McpServer server)
        => GetAsync(server, "/v4/Cluster/Members/Self");

    [McpServerTool(Name = "Core_Cluster_CheckPreconditions", Title = "Cluster - CheckPreconditions",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Returns precondition errors and warnings from the currently staged patch for the entire cluster.")]
    public Task<string> Cluster_CheckPreconditions(McpServer server)
        => GetAsync(server, "/v4/Cluster/Patch/PreconditionCheck");

    [McpServerTool(Name = "Core_Cluster_GetSessionModuleConnections", Title = "Cluster - GetSessionModuleConnections",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return information about all of the connected session modules for this cluster.")]
    public Task<string> Cluster_GetSessionModuleConnections(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null,
        [Description("Include disconnected session modules.")] string includeDisconnected = null)
        => GetAsync(server, "/v4/Cluster/SessionModules" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q), ("includeDisconnected", includeDisconnected)));

    [McpServerTool(Name = "Core_Cluster_ConnectSessionModule", Title = "Cluster - ConnectSessionModule",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Starts the trust to create a session connection to link an external session module to this cluster. Note: This will only be invoked from SPS. They don't know about ApiError so it is best to keep the response simple.")]
    public Task<string> Cluster_ConnectSessionModule(McpServer server,
        [Description("Connection information for the new external session module.")] string body = null)
        => PostAsync(server, "/v4/Cluster/SessionModules", body);

    [McpServerTool(Name = "Core_Cluster_GetSessionModuleConnectionById", Title = "Cluster - GetSessionModuleConnectionById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return information about a connected session module for this cluster.")]
    public Task<string> Cluster_GetSessionModuleConnectionById(McpServer server,
        [Description("Unique ID of the session module.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Cluster_UpdateConnectedSessionModule", Title = "Cluster - UpdateConnectedSessionModule",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update the connection information for a specific session module in this cluster.")]
    public Task<string> Cluster_UpdateConnectedSessionModule(McpServer server,
        [Description("Unique ID of the session module.")] string id,
        [Description("Connection information for the external session module. Only the description and IP address can be modified.")] string body)
        => PutAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Cluster_DeleteSessionModuleConnection", Title = "Cluster - DeleteSessionModuleConnection",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Disconnect a specific external session module from the cluster.")]
    public Task<string> Cluster_DeleteSessionModuleConnection(McpServer server,
        [Description("ID of the session module to be disconnected.")] string id)
        => DeleteAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Cluster_GetSessionModuleConnectionPoliciesById", Title = "Cluster - GetSessionModuleConnectionPoliciesById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return a list of connection policies by session module.")]
    public Task<string> Cluster_GetSessionModuleConnectionPoliciesById(McpServer server,
        [Description("Unique ID of the session module.")] string id,
        [Description("Filter the results by protocol (RDP | SSH (default)).")] string protocol = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null,
        [Description("Force a refresh of the session connection policies.")] string refresh = null)
        => GetAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}/ConnectionPolicies" + Q(("protocol", protocol), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q), ("refresh", refresh)));

    [McpServerTool(Name = "Core_Cluster_GetSessionModuleConnectionPolicyById", Title = "Cluster - GetSessionModuleConnectionPolicyById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return a connection policy by session module.")]
    public Task<string> Cluster_GetSessionModuleConnectionPolicyById(McpServer server,
        [Description("Unique ID of the session module.")] string id,
        [Description("Unique Policy ID of the session module.")] string policyId,
        [Description("Filter the results by protocol (RDP | SSH (default)).")] string protocol = null,
        [Description("Force a refresh of the session connection policies.")] string refresh = null)
        => GetAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}/ConnectionPolicies/{Uri.EscapeDataString(policyId)}" + Q(("protocol", protocol), ("refresh", refresh)));

    [McpServerTool(Name = "Core_Cluster_GetSessionModuleClusterNodes", Title = "Cluster - GetSessionModuleClusterNodes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return a list of the managed host members by session module.")]
    public Task<string> Cluster_GetSessionModuleClusterNodes(McpServer server,
        [Description("Unique ID of the session module.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}/Members" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Cluster_GetSessionModuleClusterNodeById", Title = "Cluster - GetSessionModuleClusterNodeById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return a managed host member by session module and member id.")]
    public Task<string> Cluster_GetSessionModuleClusterNodeById(McpServer server,
        [Description("Unique ID of the session module.")] string id,
        [Description("Unique ID of the member.")] string memberId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}/Members/{Uri.EscapeDataString(memberId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Cluster_GetManagedNetworksBySessionModuleAndMemberId", Title = "Cluster - GetManagedNetworksBySessionModuleAndMemberId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return the managed networks by session module and member id.")]
    public Task<string> Cluster_GetManagedNetworksBySessionModuleAndMemberId(McpServer server,
        [Description("Unique ID of the session module.")] string id,
        [Description("Unique ID of the member.")] string memberId,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}/Members/{Uri.EscapeDataString(memberId)}/ManagedNetworks" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Cluster_AssignManagedNetworkBySessionModuleAndMemberId", Title = "Cluster - AssignManagedNetworkBySessionModuleAndMemberId",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Assigns this managed host member to a set of managed network configurations.")]
    public Task<string> Cluster_AssignManagedNetworkBySessionModuleAndMemberId(McpServer server,
        [Description("Unique ID of the session module.")] string id,
        [Description("Unique ID of the member.")] string memberId,
        [Description("Network configurations to assign this managed host member.")] string body)
        => PutAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}/Members/{Uri.EscapeDataString(memberId)}/ManagedNetworks", body);

    [McpServerTool(Name = "Core_Cluster_AddRemoveManagedNetworkBySessionModuleAndMemberId", Title = "Cluster - AddRemoveManagedNetworkBySessionModuleAndMemberId",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Add/Remove network configuration assignments for this managed host member.")]
    public Task<string> Cluster_AddRemoveManagedNetworkBySessionModuleAndMemberId(McpServer server,
        [Description("Unique ID of the session module.")] string id,
        [Description("Unique ID of the member.")] string memberId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Network configurations to assign this managed host member.")] string body)
        => PutAsync(server, $"/v4/Cluster/SessionModules/{Uri.EscapeDataString(id)}/Members/{Uri.EscapeDataString(memberId)}/ManagedNetworks/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Cluster_GetAccessRequestBroker", Title = "Cluster - GetAccessRequestBroker",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the access request broker.")]
    public Task<string> Cluster_GetAccessRequestBroker(McpServer server)
        => GetAsync(server, "/v4/Cluster/SessionModules/AccessRequestBroker");

    [McpServerTool(Name = "Core_Cluster_UpdateAccessRequestBroker", Title = "Cluster - UpdateAccessRequestBroker",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Update the access request broker.")]
    public Task<string> Cluster_UpdateAccessRequestBroker(McpServer server,
        [Description("Access request broker.")] string body)
        => PutAsync(server, "/v4/Cluster/SessionModules/AccessRequestBroker", body);

    [McpServerTool(Name = "Core_Cluster_GetSessionModuleConnectionPolicies", Title = "Cluster - GetSessionModuleConnectionPolicies",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return a list of connection policies from the connected session modules in the cluster.")]
    public Task<string> Cluster_GetSessionModuleConnectionPolicies(McpServer server,
        [Description("Filter the results by protocol (RDP | SSH (default)).")] string protocol = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null,
        [Description("Force a refresh of the session connection policies.")] string refresh = null)
        => GetAsync(server, "/v4/Cluster/SessionModules/ConnectionPolicies" + Q(("protocol", protocol), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q), ("refresh", refresh)));

    [McpServerTool(Name = "Core_Cluster_GetAllClusterNodes", Title = "Cluster - GetAllClusterNodes",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Return a list of all cluster members.")]
    public Task<string> Cluster_GetAllClusterNodes(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Cluster/SessionModules/Members" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Cluster_GetStatus", Title = "Cluster - GetStatus",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Returns the cluster lock status.")]
    public Task<string> Cluster_GetStatus(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Cluster/Status" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Cluster_ForceComplete", Title = "Cluster - ForceComplete",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Force the current cluster operation to complete.")]
    public Task<string> Cluster_ForceComplete(McpServer server)
        => PostAsync(server, "/v4/Cluster/Status/ForceComplete");

    [McpServerTool(Name = "Core_Cluster_Isolate", Title = "Cluster - Isolate",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Isolate the appliance to be available for access requests despite its inability to communicate with the rest of the cluster.")]
    public Task<string> Cluster_Isolate(McpServer server)
        => PostAsync(server, "/v4/Cluster/Status/Isolate");

    [McpServerTool(Name = "Core_Cluster_PatchDistribution", Title = "Cluster - PatchDistribution",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the status of the patch distribution in cluster.")]
    public Task<string> Cluster_PatchDistribution(McpServer server)
        => GetAsync(server, "/v4/Cluster/Status/PatchDistribution");

    [McpServerTool(Name = "Core_Cluster_GetLoadStatus", Title = "Cluster - GetLoadStatus",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get information about platform tasks load.")]
    public Task<string> Cluster_GetLoadStatus(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Cluster/Status/PlatformTaskLoadStatus" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Cluster_GetScheduledPlatformTasks", Title = "Cluster - GetScheduledPlatformTasks",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get currently scheduled tasks.")]
    public Task<string> Cluster_GetScheduledPlatformTasks(McpServer server,
        [Description("Type of scheduled tasks to find.")] string taskName,
        [Description("Include list of running tasks.")] string includeRunningTasks = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Cluster/Status/PlatformTaskLoadStatus/{Uri.EscapeDataString(taskName)}" + Q(("includeRunningTasks", includeRunningTasks), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Cluster_GetScheduledPlatformTasksSummaries", Title = "Cluster - GetScheduledPlatformTasksSummaries",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get summarized counts of currently scheduled tasks based on an interval.")]
    public Task<string> Cluster_GetScheduledPlatformTasksSummaries(McpServer server,
        [Description("Interval to summarize tasks. Minimum 30 minutes. (Default = 30 minutes).")] string summaryIntervalMinutes = null,
        [Description("A comma-separated list of task names to include in entity output. (Default = all).")] string taskNames = null,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Cluster/Status/PlatformTaskLoadStatus/Summaries" + Q(("summaryIntervalMinutes", summaryIntervalMinutes), ("taskNames", taskNames), ("fields", fields)));

    [McpServerTool(Name = "Core_Cluster_Reintegrate", Title = "Cluster - Reintegrate",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Reverts the Isolate and reintegrates the appliance back in the cluster.")]
    public Task<string> Cluster_Reintegrate(McpServer server)
        => PostAsync(server, "/v4/Cluster/Status/Reintegrate");

    [McpServerTool(Name = "Core_Cluster_GetVMCompatibleAuthorization", Title = "Cluster - GetVMCompatibleAuthorization",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the VM Compatible Backup Authorization Setting.")]
    public Task<string> Cluster_GetVMCompatibleAuthorization(McpServer server)
        => GetAsync(server, "/v4/Cluster/VMCompatibleBackup/Authorization");

    [McpServerTool(Name = "Core_Cluster_DeleteVMCompatibleAuthorization", Title = "Cluster - DeleteVMCompatibleAuthorization",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Revokes the VM Compatible Backup Authorization.")]
    public Task<string> Cluster_DeleteVMCompatibleAuthorization(McpServer server)
        => DeleteAsync(server, "/v4/Cluster/VMCompatibleBackup/Authorization");

    [McpServerTool(Name = "Core_Cluster_PostVmCompatibleAuthorizationRequest", Title = "Cluster - PostVmCompatibleAuthorizationRequest",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Generates a OneIdentity Challenge to authorize VM Compatible Backup download.")]
    public Task<string> Cluster_PostVmCompatibleAuthorizationRequest(McpServer server,
        [Description("The user who is requesting the challenge.")] string userIdentifier = null,
        [Description("Specify true if you wish to invalidate the existing request and use this one.")] string invalidateExistingChallengeRequest = null)
        => PostAsync(server, "/v4/Cluster/VMCompatibleBackup/ChallengeRequest" + Q(("userIdentifier", userIdentifier), ("invalidateExistingChallengeRequest", invalidateExistingChallengeRequest)));

    [McpServerTool(Name = "Core_Cluster_DeleteAllChallengeRequests", Title = "Cluster - DeleteAllChallengeRequests",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Deletes all existing challenge requests.")]
    public Task<string> Cluster_DeleteAllChallengeRequests(McpServer server)
        => DeleteAsync(server, "/v4/Cluster/VMCompatibleBackup/ChallengeRequest");

    [McpServerTool(Name = "Core_Cluster_VmCompatibleAuthorizationResponse", Title = "Cluster - VmCompatibleAuthorizationResponse",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Using a OneIdentity Challenge Response json string, authorize download of VM compatible backups for the cluster.")]
    public Task<string> Cluster_VmCompatibleAuthorizationResponse(McpServer server,
        [Description("All of the information that is needed to match the response to the challenge should already be included in the response payload. If the response does not match, an appropriate error message should be returned.")] string body = null)
        => PostAsync(server, "/v4/Cluster/VMCompatibleBackup/ChallengeResponse", body);
}
