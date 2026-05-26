using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Exposes the Safeguard terminology map as an MCP resource that AI agents can read
/// for context about product-to-API naming differences.
/// </summary>
[McpServerResourceType]
public static class TerminologyResource
{
    [McpServerResource(UriTemplate = "safeguard://terminology")]
    [Description("Safeguard product terminology to API terminology mapping. "
        + "Read this to understand how Safeguard UI/documentation terms map to REST API endpoint names. "
        + "For example, what the product calls 'Entitlements' is the /v4/Roles endpoint in the API.")]
    public static string GetTerminologyMap()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Safeguard Terminology Map");
        sb.AppendLine();
        sb.AppendLine("The Safeguard product UI and documentation use different terminology than the REST API.");
        sb.AppendLine("When a user asks about a concept, use this mapping to find the correct API endpoint.");
        sb.AppendLine();
        sb.AppendLine("| Product/UI Term | API Endpoint | Notes |");
        sb.AppendLine("|-----------------|--------------|-------|");
        sb.AppendLine("| Entitlement | /v4/Roles | 'Roles' in the API are what the UI calls 'Entitlements' — they group users and define what they can request access to |");
        sb.AppendLine("| Access Request Policy | /v4/AccessPolicies | Defines rules for how access requests are handled (approval, time limits, etc.) |");
        sb.AppendLine("| Partition | /v4/AssetPartitions | Logical grouping of assets that share password management settings |");
        sb.AppendLine("| Managed System, Managed Asset | /v4/Assets | Any system (server, network device, etc.) managed by Safeguard |");
        sb.AppendLine("| Managed Account | /v4/AssetAccounts | A privileged account on a managed asset |");
        sb.AppendLine("| Password Profile, Change Profile | /v4/PasswordProfiles | Rules for password generation and rotation |");
        sb.AppendLine("| Platform, Connection Template | /v4/Platforms | Defines how Safeguard connects to a type of system |");
        sb.AppendLine("| Linked Account, Personal Account | /v4/PersonalAccounts | Accounts linked to a user for personal credential access |");
        sb.AppendLine("| Session Recording | /v4/Sessions | Recorded privileged sessions (RDP, SSH, etc.) |");
        sb.AppendLine();
        sb.AppendLine("## Tips");
        sb.AppendLine();
        sb.AppendLine("- Use Safeguard_Discover with either the product term or the API term — both will work.");
        sb.AppendLine("- The Roles endpoint manages entitlements: members (who can request) and policies (what they can request).");
        sb.AppendLine("- An 'Entitlement' (Role) contains Access Policies (AccessPolicies), which define scope and approval rules.");
        sb.AppendLine("- 'Asset' and 'AssetAccount' are the foundational objects — most workflows start by finding these.");

        return sb.ToString();
    }
}
