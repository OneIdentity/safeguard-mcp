// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_EmailTemplates_GetEmailTemplates", Title = "EmailTemplates - GetEmailTemplates",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of templates.")]
    public Task<string> EmailTemplates_GetEmailTemplates(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/EmailTemplates" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_EmailTemplates_GetEmailTemplateById", Title = "EmailTemplates - GetEmailTemplateById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a single template.")]
    public Task<string> EmailTemplates_GetEmailTemplateById(McpServer server,
        [Description("Unique identifier of the EmailTemplate.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/EmailTemplates/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_EmailTemplates_UpdateEmailTemplate", Title = "EmailTemplates - UpdateEmailTemplate",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an email template.")]
    public Task<string> EmailTemplates_UpdateEmailTemplate(McpServer server,
        [Description("Unique identifier of the EmailTemplate to update.")] string id,
        [Description("Updated EmailTemplate.")] string body)
        => PutAsync(server, $"/v4/EmailTemplates/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_EmailTemplates_ResetEmailTemplate", Title = "EmailTemplates - ResetEmailTemplate",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Resets an email template to the default.")]
    public Task<string> EmailTemplates_ResetEmailTemplate(McpServer server,
        [Description("Unique identifier of the EmailTemplate.")] string id)
        => DeleteAsync(server, $"/v4/EmailTemplates/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_EmailTemplates_GetEmailTemplateEvent", Title = "EmailTemplates - GetEmailTemplateEvent",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the event associated with an email template.")]
    public Task<string> EmailTemplates_GetEmailTemplateEvent(McpServer server,
        [Description("Unique identifier of the EmailTemplate.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/EmailTemplates/{Uri.EscapeDataString(id)}/Event" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_EmailTemplates_RenderEmailTemplateTestEmailById", Title = "EmailTemplates - RenderEmailTemplateTestEmailById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Renders a sample event notification email using a template.")]
    public Task<string> EmailTemplates_RenderEmailTemplateTestEmailById(McpServer server,
        [Description("Unique identifier of the EmailTemplate.")] string id)
        => PostAsync(server, $"/v4/EmailTemplates/{Uri.EscapeDataString(id)}/RenderTestEmail");

    [McpServerTool(Name = "Core_EmailTemplates_SendEmailTemplateTestEmailById", Title = "EmailTemplates - SendEmailTemplateTestEmailById",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Sends a sample event notification email using a template.")]
    public Task<string> EmailTemplates_SendEmailTemplateTestEmailById(McpServer server,
        [Description("Unique identifier of the EmailTemplate.")] string id,
        [Description("email address to send test email to.")] string body = null)
        => PostAsync(server, $"/v4/EmailTemplates/{Uri.EscapeDataString(id)}/SendTestEmail", body);

    [McpServerTool(Name = "Core_EmailTemplates_RenderEmailTemplateTestEmail", Title = "EmailTemplates - RenderEmailTemplateTestEmail",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Renders a sample event notification email using template data.")]
    public Task<string> EmailTemplates_RenderEmailTemplateTestEmail(McpServer server,
        [Description("Email template to render test email for.")] string body = null)
        => PostAsync(server, "/v4/EmailTemplates/RenderTestEmail", body);

    [McpServerTool(Name = "Core_EmailTemplates_SendEmailTemplateTestEmail", Title = "EmailTemplates - SendEmailTemplateTestEmail",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Sends a sample event notification email using template data.")]
    public Task<string> EmailTemplates_SendEmailTemplateTestEmail(McpServer server,
        [Description("Email template to render test email for.")] string body = null)
        => PostAsync(server, "/v4/EmailTemplates/SendTestEmail", body);
}
