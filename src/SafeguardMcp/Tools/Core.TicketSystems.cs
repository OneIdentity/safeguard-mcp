// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_TicketSystems_Get", Title = "TicketSystems - Get",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of all ticket systems.")]
    public Task<string> TicketSystems_Get(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/TicketSystems" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_TicketSystems_CreateEntity", Title = "TicketSystems - CreateEntity",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new ticket system.")]
    public Task<string> TicketSystems_CreateEntity(McpServer server,
        [Description("ticket system to create.")] string body = null)
        => PostAsync(server, "/v4/TicketSystems", body);

    [McpServerTool(Name = "Core_TicketSystems_GetById", Title = "TicketSystems - GetById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a ticket system.")]
    public Task<string> TicketSystems_GetById(McpServer server,
        [Description("Unique ID of ticket system.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/TicketSystems/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_TicketSystems_UpdateEntity", Title = "TicketSystems - UpdateEntity",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing ticket system.")]
    public Task<string> TicketSystems_UpdateEntity(McpServer server,
        [Description("Unique identifier of the ticket system.")] string id,
        [Description("Updated ticket system.")] string body)
        => PutAsync(server, $"/v4/TicketSystems/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_TicketSystems_Delete", Title = "TicketSystems - Delete",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a ticket system.")]
    public Task<string> TicketSystems_Delete(McpServer server,
        [Description("Unique identifier of the ticket system.")] string id)
        => DeleteAsync(server, $"/v4/TicketSystems/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_TicketSystems_TestConnectionById", Title = "TicketSystems - TestConnectionById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Test connection parameters to a ticket system.")]
    public Task<string> TicketSystems_TestConnectionById(McpServer server,
        [Description("Unique identifier of the ticket system.")] string id)
        => PostAsync(server, $"/v4/TicketSystems/{Uri.EscapeDataString(id)}/TestConnection");

    [McpServerTool(Name = "Core_TicketSystems_ValidateTicket", Title = "TicketSystems - ValidateTicket",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Validate ticket number against the ticket system.")]
    public Task<string> TicketSystems_ValidateTicket(McpServer server,
        [Description("Unique identifier of the ticket system.")] string id,
        [Description("Ticket number from ticket system.")] string ticket)
        => PostAsync(server, $"/v4/TicketSystems/{Uri.EscapeDataString(id)}/ValidateTicket/{Uri.EscapeDataString(ticket)}");

    [McpServerTool(Name = "Core_TicketSystems_TestConnection", Title = "TicketSystems - TestConnection",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Test connection parameters to a ticket system.")]
    public Task<string> TicketSystems_TestConnection(McpServer server,
        [Description("ticket system configuration to test.")] string body = null)
        => PostAsync(server, "/v4/TicketSystems/TestConnection", body);

    [McpServerTool(Name = "Core_TicketSystems_ValidateAnyTicket", Title = "TicketSystems - ValidateAnyTicket",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Validate ticket number against all the ticket systems.")]
    public Task<string> TicketSystems_ValidateAnyTicket(McpServer server,
        [Description("Ticket number from ticket system.")] string ticket)
        => PostAsync(server, $"/v4/TicketSystems/ValidateTicket/{Uri.EscapeDataString(ticket)}");
}
