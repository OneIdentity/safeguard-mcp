using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Catalog;

/// <summary>
/// MCP Resource providing comprehensive Safeguard API query syntax reference.
/// Clients can preload this into context to avoid repeated Safeguard_QueryHelp calls.
/// </summary>
[McpServerResourceType]
internal sealed class QuerySyntaxResource
{
    private QuerySyntaxResource() { }

    [McpServerResource(UriTemplate = "safeguard://query-syntax")]
    [Description("Complete Safeguard API query syntax reference — filter operators, field selection, "
        + "ordering, pagination, and search. Preload this to write correct query parameters without tool calls.")]
    public static string GetQuerySyntax()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Safeguard API Query Syntax Reference");
        sb.AppendLine();
        sb.AppendLine("All GET collection endpoints support these query parameters passed via the `query` parameter in Safeguard_Execute.");
        sb.AppendLine();
        sb.AppendLine("## Filter Operators");
        sb.AppendLine();
        sb.AppendLine("| Operator | Meaning | Example |");
        sb.AppendLine("|----------|---------|---------|");
        sb.AppendLine("| eq | Equals | `filter=Name eq 'Admin'` |");
        sb.AppendLine("| ne | Not equals | `filter=Disabled ne true` |");
        sb.AppendLine("| gt | Greater than | `filter=Id gt 100` |");
        sb.AppendLine("| ge | Greater or equal | `filter=CreatedDate ge '2024-01-01'` |");
        sb.AppendLine("| lt | Less than | `filter=Id lt 50` |");
        sb.AppendLine("| le | Less or equal | `filter=CreatedDate le '2024-12-31'` |");
        sb.AppendLine("| contains | Substring (case-sensitive) | `filter=Name contains 'srv'` |");
        sb.AppendLine("| icontains | Substring (case-insensitive) | `filter=Name icontains 'admin'` |");
        sb.AppendLine("| ieq | Equals (case-insensitive) | `filter=Name ieq 'administrator'` |");
        sb.AppendLine("| sw | Starts with (case-sensitive) | `filter=Name sw 'DC'` |");
        sb.AppendLine("| isw | Starts with (case-insensitive) | `filter=Name isw 'dc'` |");
        sb.AppendLine("| ew | Ends with (case-sensitive) | `filter=Name ew '-prod'` |");
        sb.AppendLine("| iew | Ends with (case-insensitive) | `filter=Name iew '-PROD'` |");
        sb.AppendLine("| in | In list | `filter=Id in [1,2,3]` |");
        sb.AppendLine("| not_in | Not in list | `filter=Id not_in [4,5,6]` |");
        sb.AppendLine();
        sb.AppendLine("## Logical Operators");
        sb.AppendLine();
        sb.AppendLine("- `and` — both conditions must match: `filter=Disabled eq false and Name contains 'admin'`");
        sb.AppendLine("- `or` — either condition: `filter=State eq 'Available' or State eq 'Pending'`");
        sb.AppendLine("- `not` — negate: `filter=not (Disabled eq true)`");
        sb.AppendLine("- Parentheses for grouping: `filter=(Name sw 'DC') and (Platform.DisplayName eq 'Windows')`");
        sb.AppendLine();
        sb.AppendLine("## Nested Properties");
        sb.AppendLine();
        sb.AppendLine("Use dot notation to filter or select nested objects:");
        sb.AppendLine("- `filter=TaskProperties.HasAccountTaskFailure eq true`");
        sb.AppendLine("- `filter=Asset.Name icontains 'prod'`");
        sb.AppendLine("- `fields=Id,Name,Asset.Name,Asset.NetworkAddress`");
        sb.AppendLine();
        sb.AppendLine("## Field Selection");
        sb.AppendLine();
        sb.AppendLine("- Include specific fields: `fields=Id,Name,Description`");
        sb.AppendLine("- Exclude verbose fields: `fields=-TaskProperties,-Platform,-ConnectionProperties`");
        sb.AppendLine("- Reduces response size and improves performance");
        sb.AppendLine();
        sb.AppendLine("## Ordering");
        sb.AppendLine();
        sb.AppendLine("- Ascending: `orderby=Name`");
        sb.AppendLine("- Descending: `orderby=-CreatedDate`");
        sb.AppendLine("- Multiple fields: `orderby=Asset.Name,-CreatedDate`");
        sb.AppendLine();
        sb.AppendLine("## Pagination");
        sb.AppendLine();
        sb.AppendLine("- `page=0&limit=50` — page is 0-indexed, limit is items per page");
        sb.AppendLine("- Default limit varies by endpoint (typically 100)");
        sb.AppendLine("- `count=true` — returns only the count, not the data");
        sb.AppendLine();
        sb.AppendLine("## Quick Search");
        sb.AppendLine();
        sb.AppendLine("- `q=searchterm` — searches across multiple text fields (like a global search)");
        sb.AppendLine("- Simpler than filter but less precise");
        sb.AppendLine();
        sb.AppendLine("## Combined Examples");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("# Find disabled Windows accounts with failures, sorted by asset name");
        sb.AppendLine("fields=Id,Name,Asset.Name&filter=(Disabled eq false) and (Platform.DisplayName eq 'Windows') and (TaskProperties.HasAccountTaskFailure eq true)&orderby=Asset.Name&limit=50");
        sb.AppendLine();
        sb.AppendLine("# Recent access requests for a specific user, newest first");
        sb.AppendLine("fields=Id,AccessRequestType,State,AccountName,AssetName,CreatedDate&filter=RequesterName eq 'john.smith'&orderby=-CreatedDate&limit=20");
        sb.AppendLine();
        sb.AppendLine("# Count assets in a partition");
        sb.AppendLine("filter=AssetPartitionId eq 1&count=true");
        sb.AppendLine();
        sb.AppendLine("# Accounts with passwords not changed in 90+ days");
        sb.AppendLine("fields=Id,Name,Asset.Name,TaskProperties.LastSuccessPasswordChangeDate&filter=TaskProperties.LastSuccessPasswordChangeDate lt '2024-01-01'&orderby=TaskProperties.LastSuccessPasswordChangeDate&limit=50");
        sb.AppendLine("```");

        return sb.ToString();
    }
}
