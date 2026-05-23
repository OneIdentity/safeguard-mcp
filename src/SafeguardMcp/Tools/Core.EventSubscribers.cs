// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_EventSubscribers_GetEventSubscribers", Title = "EventSubscribers - GetEventSubscribers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of event subscribers.")]
    public Task<string> EventSubscribers_GetEventSubscribers(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/EventSubscribers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_EventSubscribers_CreateEventSubscriber", Title = "EventSubscribers - CreateEventSubscriber",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new event subscriber.")]
    public Task<string> EventSubscribers_CreateEventSubscriber(McpServer server,
        [Description("EventSubscriber to create.")] string body = null)
        => PostAsync(server, "/v4/EventSubscribers", body);

    [McpServerTool(Name = "Core_EventSubscribers_GetEventSubscriberById", Title = "EventSubscribers - GetEventSubscriberById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an event subscriber.")]
    public Task<string> EventSubscribers_GetEventSubscriberById(McpServer server,
        [Description("Unique ID of EventSubscriber.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/EventSubscribers/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_EventSubscribers_UpdateSubscriber", Title = "EventSubscribers - UpdateSubscriber",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the event subscriber.")]
    public Task<string> EventSubscribers_UpdateSubscriber(McpServer server,
        [Description("Unique identifier of the EventSubscriber to update.")] string id,
        [Description("Updated EventSubscriber.")] string body)
        => PutAsync(server, $"/v4/EventSubscribers/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_EventSubscribers_DeleteEventSubscriber", Title = "EventSubscribers - DeleteEventSubscriber",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an event subscriber.")]
    public Task<string> EventSubscribers_DeleteEventSubscriber(McpServer server,
        [Description("Unique identifier of the EventSubscriber.")] string id)
        => DeleteAsync(server, $"/v4/EventSubscribers/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_EventSubscribers_GetEventSubscriptions", Title = "EventSubscribers - GetEventSubscriptions",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of events subscribed to.")]
    public Task<string> EventSubscribers_GetEventSubscriptions(McpServer server,
        [Description("Unique identifier of the EventSubscription.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/EventSubscribers/{Uri.EscapeDataString(id)}/Subscriptions" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_EventSubscribers_SetEventSubscriptions", Title = "EventSubscribers - SetEventSubscriptions",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Subscribes to receive notifications for the specified events.")]
    public Task<string> EventSubscribers_SetEventSubscriptions(McpServer server,
        [Description("Unique identifier of the EventSubscription.")] string id,
        [Description("Events to subscribe to.")] string body)
        => PutAsync(server, $"/v4/EventSubscribers/{Uri.EscapeDataString(id)}/Subscriptions", body);

    [McpServerTool(Name = "Core_EventSubscribers_ModifyEventSubscriptions", Title = "EventSubscribers - ModifyEventSubscriptions",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove subscriptions to receive notifications for the specified events.")]
    public Task<string> EventSubscribers_ModifyEventSubscriptions(McpServer server,
        [Description("Unique identifier of the EventSubscription.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Events to subscribe to.")] string body = null)
        => PostAsync(server, $"/v4/EventSubscribers/{Uri.EscapeDataString(id)}/Subscriptions/{Uri.EscapeDataString(operation)}", body);
}
