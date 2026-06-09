using System;
using System.Collections.Generic;

namespace SafeguardMcp.Catalog;

/// <summary>
/// Path-aware default <c>fields=</c> projections for endpoints whose default response shape
/// is materially oversized. Each entry decides, given the normalized path, whether a
/// default <c>fields=</c> should be auto-injected when the caller did not supply one.
/// </summary>
/// <remarks>
/// Initial population covers the <c>/v4/AuditLog/ObjectChanges</c> list-style routes
/// (collection, by-type, by-type+by-id), where <c>OldValue</c>/<c>NewValue</c> carry
/// full-entity JSON-string snapshots that inflate payloads 10-100x. The structured diff in
/// <c>Changes[]</c> is retained. The singleton route
/// <c>/v4/AuditLog/ObjectChanges/{objectType}/{objectId}/{logId}</c> is intentionally
/// excluded -- callers who fetch a specific log id are signalling they want the snapshot.
///
/// Maintenance signal: when the appliance adds new properties to <c>ObjectChangeLog</c>
/// (<c>PangaeaAppliance/src/Data/Transfer/V4/Audit/ObjectChangeLog.cs</c>) they will
/// continue to flow through because this catalog uses the appliance's <c>fields=-Field1,Field2</c>
/// EXCLUDE syntax (leading dash on the list head). Only OldValue/NewValue are dropped;
/// every other column appears as the appliance defines it.
/// </remarks>
internal static class DefaultProjections
{
    /// <summary>Default <c>fields=</c> value applied to ObjectChanges list-style routes.</summary>
    internal const string ObjectChangesDefault = "-OldValue,-NewValue";

    /// <summary>
    /// Decide whether a default <c>fields=</c> should be auto-injected for the given
    /// request. Returns <c>true</c> and sets <paramref name="defaultFields"/> when a
    /// projection applies; otherwise <c>false</c>.
    /// </summary>
    /// <param name="method">HTTP method (e.g. "GET"). Only GET is eligible.</param>
    /// <param name="normalizedPath">Path as produced by ApiToolHelpers.NormalizePath.</param>
    /// <param name="callerSuppliedFields">
    /// True if the caller already passed a <c>fields=</c> parameter (in any case). When
    /// true the explicit value wins and no default is injected.
    /// </param>
    internal static bool TryGetDefaultFields(
        string method,
        string normalizedPath,
        bool callerSuppliedFields,
        out string defaultFields)
    {
        defaultFields = null;
        if (callerSuppliedFields)
            return false;
        if (string.IsNullOrEmpty(method) || !method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrEmpty(normalizedPath))
            return false;

        if (IsObjectChangesListRoute(normalizedPath))
        {
            defaultFields = ObjectChangesDefault;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Match the three list-style ObjectChanges routes (collection, by-type, by-type+by-id)
    /// but NOT the singleton route that targets a specific log id.
    /// </summary>
    private static bool IsObjectChangesListRoute(string normalizedPath)
    {
        const string prefix = "/v4/AuditLog/ObjectChanges";
        if (!normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalizedPath.Length == prefix.Length)
            return true; // /v4/AuditLog/ObjectChanges

        if (normalizedPath[prefix.Length] != '/')
            return false; // /v4/AuditLog/ObjectChangesOther...

        var tail = normalizedPath.Substring(prefix.Length + 1);
        if (tail.Length == 0)
            return true; // /v4/AuditLog/ObjectChanges/  (defensive: NormalizePath strips trailing /)

        // Count remaining path segments. 1 = by-type, 2 = by-type+by-id, 3 = singleton (skip).
        var segments = tail.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length <= 2;
    }
}
