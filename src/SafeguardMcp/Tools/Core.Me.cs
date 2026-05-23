// This code was auto generated.

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SafeguardMcp.Tools;

public partial class CoreTools
{
    [McpServerTool(Name = "Core_Me_GetMe", Title = "Me - GetMe",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a fully expanded representation of an authenticated user.")]
    public Task<string> Me_GetMe(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Me" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_UpdateMe", Title = "Me - UpdateMe",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Allows the current user to update the email address, phone number(s), photo, and time zone.")]
    public Task<string> Me_UpdateMe(McpServer server,
        [Description("Updated User.")] string body)
        => PutAsync(server, "/v4/Me", body);

    [McpServerTool(Name = "Core_Me_GetAccessRequestAssets", Title = "Me - GetAccessRequestAssets",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of assets that have accounts requestable by the specified user.")]
    public Task<string> Me_GetAccessRequestAssets(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/AccessRequestAssets" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetAccessRequestAsset", Title = "Me - GetAccessRequestAsset",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific asset that can be requested by the specified user.")]
    public Task<string> Me_GetAccessRequestAsset(McpServer server,
        [Description("Unique identifier of the asset.")] string assetId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/AccessRequestAssets/{Uri.EscapeDataString(assetId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_GetAccountEntitlements", Title = "Me - GetAccountEntitlements",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the entitlements for the current user by account.")]
    public Task<string> Me_GetAccountEntitlements(McpServer server,
        [Description("Only report on access via a specific request type.")] string accessRequestType = null,
        [Description("List of asset IDs to get entitlements for (preferred over filter). Specify a comma delimited list to return all accessible accounts for each specified asset ID. If you also specify accountIds, the corresponding set of entitlements between all IDs ...")] string assetIds = null,
        [Description("List of account IDs to get entitlements for (preferred over filter). Specify a comma delimited list to return each accessible account. If you also specify assetIds, the corresponding set of entitlements between all IDs will be returned. If a value...")] string accountIds = null,
        [Description("Search text for asset name (preferred over filter). Performs a 'contains' style search. The cacheCookie parameter should be unique per search value specified.")] string assetName = null,
        [Description("Search text for account name (preferred over filter). Performs a 'contains' style search. The cacheCookie parameter should be unique per search value specified.")] string accountName = null,
        [Description("Whether to include information about active requests for same account.")] string includeActiveRequests = null,
        [Description("Whether to filter out entitlements and policies that don't have valid credential configurations.")] string filterByCredential = null,
        [Description("A client generated value, such that if specified, the results will be cached on the server and subsequent calls by the client with the same cookie value will return the same cached results, as permitted by the memory and other activity on the serv...")] string cacheCookie = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/AccountEntitlements" + Q(("accessRequestType", accessRequestType), ("assetIds", assetIds), ("accountIds", accountIds), ("assetName", assetName), ("accountName", accountName), ("includeActiveRequests", includeActiveRequests), ("filterByCredential", filterByCredential), ("cacheCookie", cacheCookie), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetActionableRequests", Title = "Me - GetActionableRequests",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all requests that the current user can perform an action on.")]
    public Task<string> Me_GetActionableRequests(McpServer server,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, "/v4/Me/ActionableRequests" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_GetActionableRequestsByRequestRole", Title = "Me - GetActionableRequestsByRequestRole",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all requests that the current user can perform an action on.")]
    public Task<string> Me_GetActionableRequestsByRequestRole(McpServer server,
        [Description("Return results based on user's access request role, i.e. Requester, Approver, Reviewer, Admin.")] string accessRequestRole,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Me/ActionableRequests/{Uri.EscapeDataString(accessRequestRole)}" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetAdministeredCertificates", Title = "Me - GetAdministeredCertificates",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the administered certificates for the current user.")]
    public Task<string> Me_GetAdministeredCertificates(McpServer server,
        [Description("Type of administered certificate to retrieve.")] string administeredCertificateType = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/Certificates" + Q(("administeredCertificateType", administeredCertificateType), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_AddAdministeredCertificate", Title = "Me - AddAdministeredCertificate",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new administered certificate.")]
    public Task<string> Me_AddAdministeredCertificate(McpServer server,
        [Description("AdministeredCertificate to add.")] string body = null)
        => PostAsync(server, "/v4/Me/Certificates", body);

    [McpServerTool(Name = "Core_Me_GetAdministeredCertificate", Title = "Me - GetAdministeredCertificate",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an administered certificate for the current user.")]
    public Task<string> Me_GetAdministeredCertificate(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_UpdateAdministeredCertificate", Title = "Me - UpdateAdministeredCertificate",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing administered certificate.")]
    public Task<string> Me_UpdateAdministeredCertificate(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("AdministeredCertificate to update.")] string body)
        => PutAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Me_DeleteAdministeredCertificate", Title = "Me - DeleteAdministeredCertificate",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove an administered certificate.")]
    public Task<string> Me_DeleteAdministeredCertificate(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id)
        => DeleteAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Me_GetAdministeredCertificateCertHistory", Title = "Me - GetAdministeredCertificateCertHistory",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an administered certificate history.")]
    public Task<string> Me_GetAdministeredCertificateCertHistory(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}/CertificateHistory" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetAdministeredCertificateCertHistoryById", Title = "Me - GetAdministeredCertificateCertHistoryById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific administered certificate history by event id.")]
    public Task<string> Me_GetAdministeredCertificateCertHistoryById(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("The id of the history event is the end date in milliseconds.")] string eventid)
        => GetAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}/CertificateHistory/{Uri.EscapeDataString(eventid)}");

    [McpServerTool(Name = "Core_Me_DownloadAdministeredCertificateCertHistoryById", Title = "Me - DownloadAdministeredCertificateCertHistoryById",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Downloads a specific administered certificate from history by event id.")]
    public Task<string> Me_DownloadAdministeredCertificateCertHistoryById(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("The id of the history event is the end date in milliseconds.")] string eventid)
        => GetAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}/CertificateHistory/{Uri.EscapeDataString(eventid)}/Download");

    [McpServerTool(Name = "Core_Me_GetAdministeredCertificateFile", Title = "Me - GetAdministeredCertificateFile",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Downloads an administered certificate file for the current user.")]
    public Task<string> Me_GetAdministeredCertificateFile(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("Download options. If the option \"includePrivateKey\" is set to true or a passphrase is provided, this will force the output to be in pkcs12 format even if the certificate does not have a private key. Otherwise the output will be in x509 format.")] string body = null)
        => PostAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}/Download", body);

    [McpServerTool(Name = "Core_Me_GetAdministeredCertificateShares", Title = "Me - GetAdministeredCertificateShares",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get the lists of users and groups with whom the administered certificate is shared.")]
    public Task<string> Me_GetAdministeredCertificateShares(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}/Share" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_ShareAdministeredCertificate", Title = "Me - ShareAdministeredCertificate",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Share an administered certificate with another user or user group.")]
    public Task<string> Me_ShareAdministeredCertificate(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("AdministeredCertificateShare information.")] string body = null)
        => PostAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}/Share", body);

    [McpServerTool(Name = "Core_Me_GetAdministeredCertificateShare", Title = "Me - GetAdministeredCertificateShare",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get an administered certificate share.")]
    public Task<string> Me_GetAdministeredCertificateShare(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("Unique identifier of the user or group whom the administered certificate is shared with.")] string sharedWithId,
        [Description("Type of administered certificate share, User or Group. (Default: User).")] string type = null,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}/Share/{Uri.EscapeDataString(sharedWithId)}" + Q(("type", type), ("fields", fields)));

    [McpServerTool(Name = "Core_Me_UpdateAdministeredCertificateShare", Title = "Me - UpdateAdministeredCertificateShare",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an administered certificate share.")]
    public Task<string> Me_UpdateAdministeredCertificateShare(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("Unique identifier of the user that the administered certificate is shared with.")] string sharedWithId,
        [Description("AdministeredCertificateShare information.")] string body)
        => PutAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}/Share/{Uri.EscapeDataString(sharedWithId)}", body);

    [McpServerTool(Name = "Core_Me_DeleteAdministeredCertificateShare", Title = "Me - DeleteAdministeredCertificateShare",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Delete the share of an administered certificate.")]
    public Task<string> Me_DeleteAdministeredCertificateShare(McpServer server,
        [Description("Unique identifier of the administered certificate.")] string id,
        [Description("Unique identifier of the administered certificate share.")] string sharedWithId,
        [Description("Share type: User or Group.")] string type = null)
        => DeleteAsync(server, $"/v4/Me/Certificates/{Uri.EscapeDataString(id)}/Share/{Uri.EscapeDataString(sharedWithId)}" + Q(("type", type)));

    [McpServerTool(Name = "Core_Me_GetAdministeredCsrs", Title = "Me - GetAdministeredCsrs",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the available CSR.")]
    public Task<string> Me_GetAdministeredCsrs(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/Certificates/Csr" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_AddAdministeredCsr", Title = "Me - AddAdministeredCsr",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new administered CSR.")]
    public Task<string> Me_AddAdministeredCsr(McpServer server,
        [Description("AdministeredCsr to create.")] string body = null)
        => PostAsync(server, "/v4/Me/Certificates/Csr", body);

    [McpServerTool(Name = "Core_Me_GetAdministeredCsr", Title = "Me - GetAdministeredCsr",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an administered CSR.")]
    public Task<string> Me_GetAdministeredCsr(McpServer server,
        [Description("Unique identifier of the administered Csr.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/Certificates/Csr/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_UpdateAdministeredCsr", Title = "Me - UpdateAdministeredCsr",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing administered CSR. Only the notes field can be updated.")]
    public Task<string> Me_UpdateAdministeredCsr(McpServer server,
        [Description("Unique identifier of the administered Csr.")] string id,
        [Description("AdministeredCsr to update.")] string body)
        => PutAsync(server, $"/v4/Me/Certificates/Csr/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Me_DeleteAdministeredCsr", Title = "Me - DeleteAdministeredCsr",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove an administered CSR.")]
    public Task<string> Me_DeleteAdministeredCsr(McpServer server,
        [Description("Unique identifier of the administered Csr.")] string id)
        => DeleteAsync(server, $"/v4/Me/Certificates/Csr/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Me_GetAdministeredCertificateShareWithUserGroups", Title = "Me - GetAdministeredCertificateShareWithUserGroups",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of user groups with whom a certificate can be shared.")]
    public Task<string> Me_GetAdministeredCertificateShareWithUserGroups(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/Certificates/ShareWithUserGroups" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetAdministeredCertificateShareWithUsers", Title = "Me - GetAdministeredCertificateShareWithUsers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of users with whom a certificate can be shared.")]
    public Task<string> Me_GetAdministeredCertificateShareWithUsers(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/Certificates/ShareWithUsers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetEnterpriseAccounts", Title = "Me - GetEnterpriseAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the accounts from the current user's enterprise vault or that are shared with the current user.")]
    public Task<string> Me_GetEnterpriseAccounts(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/EnterpriseAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_CreateEnterpriseVaultAccount", Title = "Me - CreateEnterpriseVaultAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new enterprise account.")]
    public Task<string> Me_CreateEnterpriseVaultAccount(McpServer server,
        [Description("EnterpriseAccount to add.")] string body = null)
        => PostAsync(server, "/v4/Me/EnterpriseAccounts", body);

    [McpServerTool(Name = "Core_Me_GetEnterpriseAccount", Title = "Me - GetEnterpriseAccount",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets an account from the current user's vault or that is shared with the current user.")]
    public Task<string> Me_GetEnterpriseAccount(McpServer server,
        [Description("Unique identifier of the enterprise account.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_UpdateEnterpriseAccount", Title = "Me - UpdateEnterpriseAccount",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing enterprise account.")]
    public Task<string> Me_UpdateEnterpriseAccount(McpServer server,
        [Description("Unique identifier of the enterprise account.")] string id,
        [Description("EnterpriseAccount to update.")] string body)
        => PutAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Me_DeleteEnterpriseAccount", Title = "Me - DeleteEnterpriseAccount",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Delete a enterprise account.")]
    public Task<string> Me_DeleteEnterpriseAccount(McpServer server,
        [Description("Unique identifier of the enterprise account.")] string id)
        => DeleteAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Me_GetEnterpriseAccountPassword", Title = "Me - GetEnterpriseAccountPassword",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the password assigned to the enterprise account.")]
    public Task<string> Me_GetEnterpriseAccountPassword(McpServer server,
        [Description("Unique identifier of the enterprise account.")] string id)
        => GetAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/Password");

    [McpServerTool(Name = "Core_Me_SetEnterpriseAccountPassword", Title = "Me - SetEnterpriseAccountPassword",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the enterprise account password.")]
    public Task<string> Me_SetEnterpriseAccountPassword(McpServer server,
        [Description("Unique identifier of the enterprise account to set password for.")] string id,
        [Description("Password to set for this enterprise account. Maximum length is 1 MB. If not specified then a new password will be generated using the account's password rule.")] string body)
        => PutAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/Password", body);

    [McpServerTool(Name = "Core_Me_RetrievePastPasswords", Title = "Me - RetrievePastPasswords",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets passwords previously assigned to the enterprise account.")]
    public Task<string> Me_RetrievePastPasswords(McpServer server,
        [Description("Unique identifier of the enterprise account to set password for.")] string id,
        [Description("Get past passwords that were active after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get past passwords that were active before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/Passwords" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetEnterpriseAccountShares", Title = "Me - GetEnterpriseAccountShares",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all ways this enterprise account is shared.")]
    public Task<string> Me_GetEnterpriseAccountShares(McpServer server,
        [Description("Unique identifier of the enterprise account.")] string id,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/Shares" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_SetEnterpriseAccountShares", Title = "Me - SetEnterpriseAccountShares",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the shares assigned to this enterprise account.")]
    public Task<string> Me_SetEnterpriseAccountShares(McpServer server,
        [Description("Unique identifier of the enterprise account.")] string id,
        [Description("Shares to assign to the account.")] string body)
        => PutAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/Shares", body);

    [McpServerTool(Name = "Core_Me_ModifyEnterpriseAccountShares", Title = "Me - ModifyEnterpriseAccountShares",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove shares assigned to this enterprise account.")]
    public Task<string> Me_ModifyEnterpriseAccountShares(McpServer server,
        [Description("Unique identifier of the enterprise account.")] string id,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Accounts to assign to the enterprise account.")] string body = null)
        => PostAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/Shares/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Me_GetTotpAuthenticatorByAccountId", Title = "Me - GetTotpAuthenticatorByAccountId",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the TOTP Authenticator on the enterprise account.")]
    public Task<string> Me_GetTotpAuthenticatorByAccountId(McpServer server,
        [Description("Unique ID of a EnterpriseAccount.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/TotpAuthenticator" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_SetAccountTotpAuthenticator", Title = "Me - SetAccountTotpAuthenticator",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the TOTP Authenticator on the enterprise account.")]
    public Task<string> Me_SetAccountTotpAuthenticator(McpServer server,
        [Description("Unique identifier of the EnterpriseAccount.")] string id,
        [Description("TOTP Authenticator to assign to enterprise account. Accepts Key URI or secret string.")] string body)
        => PutAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/TotpAuthenticator", body);

    [McpServerTool(Name = "Core_Me_DeleteTotpAuthenticator", Title = "Me - DeleteTotpAuthenticator",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes the TOTP Authenticator assigned to the enterprise account.")]
    public Task<string> Me_DeleteTotpAuthenticator(McpServer server,
        [Description("Unique identifier of the EnterpriseAccount.")] string id)
        => DeleteAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/TotpAuthenticator");

    [McpServerTool(Name = "Core_Me_GenerateTotpValues", Title = "Me - GenerateTotpValues",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Generates the TOTP codes for the enterprise account.")]
    public Task<string> Me_GenerateTotpValues(McpServer server,
        [Description("Unique identifier of the EnterpriseAccount.")] string id,
        [Description("TOTP time range in seconds.")] string timeRangeSeconds = null)
        => GetAsync(server, $"/v4/Me/EnterpriseAccounts/{Uri.EscapeDataString(id)}/TotpAuthenticator/Values" + Q(("timeRangeSeconds", timeRangeSeconds)));

    [McpServerTool(Name = "Core_Me_GeneratePassword", Title = "Me - GeneratePassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Generates a random password using this rule.")]
    public Task<string> Me_GeneratePassword(McpServer server,
        [Description("The password rule enforced when user passwords are set.")] string body = null)
        => PostAsync(server, "/v4/Me/EnterpriseAccounts/GeneratePassword", body);

    [McpServerTool(Name = "Core_Me_GetFido2Authenticators", Title = "Me - GetFido2Authenticators",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all registered FIDO2 authenticators for the current user.")]
    public Task<string> Me_GetFido2Authenticators(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/Fido2Authenticators" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetFido2Authenticator", Title = "Me - GetFido2Authenticator",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific FIDO2 authenticator that has been registered by the current user.")]
    public Task<string> Me_GetFido2Authenticator(McpServer server,
        [Description("Unique, opaque identifier of the authenticator, in Base64Url encoded format.")] string credentialId,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/Fido2Authenticators/{Uri.EscapeDataString(credentialId)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_SetFido2AuthenticatorName", Title = "Me - SetFido2AuthenticatorName",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the user supplied friendly name given to a FIDO2 authenticator owned by the current user.")]
    public Task<string> Me_SetFido2AuthenticatorName(McpServer server,
        [Description("Unique, opaque identifier of the authenticator, in Base64Url encoded format.")] string credentialId,
        [Description("Value to set for this preference.")] string body)
        => PutAsync(server, $"/v4/Me/Fido2Authenticators/{Uri.EscapeDataString(credentialId)}", body);

    [McpServerTool(Name = "Core_Me_DeleteFido2Authenticator", Title = "Me - DeleteFido2Authenticator",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a FIDO2 authenticator from the current user.")]
    public Task<string> Me_DeleteFido2Authenticator(McpServer server,
        [Description("Unique, opaque identifier of the authenticator, in Base64Url encoded format.")] string credentialId)
        => DeleteAsync(server, $"/v4/Me/Fido2Authenticators/{Uri.EscapeDataString(credentialId)}");

    [McpServerTool(Name = "Core_Me_GetFido2RegistrationRedirect", Title = "Me - GetFido2RegistrationRedirect",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get a secure string that is to be included as a query string parameter of an HTTP redirect request that will allow the current user to register a new FIDO2 authenticator.")]
    public Task<string> Me_GetFido2RegistrationRedirect(McpServer server)
        => GetAsync(server, "/v4/Me/Fido2Authenticators/RegistrationRedirect");

    [McpServerTool(Name = "Core_Me_GetLinkedAccounts", Title = "Me - GetLinkedAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Get policy accounts that have been linked to this user.")]
    public Task<string> Me_GetLinkedAccounts(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/LinkedPolicyAccounts" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetMyPartitions", Title = "Me - GetMyPartitions",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all asset partitions owned by the user.")]
    public Task<string> Me_GetMyPartitions(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/OwnedPartitions" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetMyOwnership", Title = "Me - GetMyOwnership",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets information about assets, partitions, accounts this user owns.")]
    public Task<string> Me_GetMyOwnership(McpServer server,
        [Description("Optional Ownership Object Type.")] string objectType = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/Ownership" + Q(("objectType", objectType), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_ChangeMyPassword", Title = "Me - ChangeMyPassword",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Changes the local user's password. Requires that you know the user's current password.")]
    public Task<string> Me_ChangeMyPassword(McpServer server,
        [Description("Current password and the new password to set.")] string body)
        => PutAsync(server, "/v4/Me/Password", body);

    [McpServerTool(Name = "Core_Me_GetPersonalAccounts", Title = "Me - GetPersonalAccounts",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the personal accounts for the current user.")]
    public Task<string> Me_GetPersonalAccounts(McpServer server,
        [Description("Type of personal accounts to retrieve.")] string personalAccountType = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/PersonalAccounts" + Q(("personalAccountType", personalAccountType), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_SavePersonalAccount", Title = "Me - SavePersonalAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Adds a new personal account.")]
    public Task<string> Me_SavePersonalAccount(McpServer server,
        [Description("PersonalAccount to add.")] string body = null)
        => PostAsync(server, "/v4/Me/PersonalAccounts", body);

    [McpServerTool(Name = "Core_Me_GetPersonalAccount", Title = "Me - GetPersonalAccount",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a personal account for the current user.")]
    public Task<string> Me_GetPersonalAccount(McpServer server,
        [Description("Unique identifier of the personal account.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/PersonalAccounts/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_UpdatePersonalAccount", Title = "Me - UpdatePersonalAccount",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing personal account.")]
    public Task<string> Me_UpdatePersonalAccount(McpServer server,
        [Description("Unique identifier of the personal account.")] string id,
        [Description("PersonalAccount to update.")] string body)
        => PutAsync(server, $"/v4/Me/PersonalAccounts/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Me_DeletePersonalAccount", Title = "Me - DeletePersonalAccount",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Remove a personal account.")]
    public Task<string> Me_DeletePersonalAccount(McpServer server,
        [Description("Unique identifier of the personal account.")] string id)
        => DeleteAsync(server, $"/v4/Me/PersonalAccounts/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Me_GetPersonalAccountPassword", Title = "Me - GetPersonalAccountPassword",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a personal account password for the current user.")]
    public Task<string> Me_GetPersonalAccountPassword(McpServer server,
        [Description("Unique identifier of the personal account password.")] string id)
        => GetAsync(server, $"/v4/Me/PersonalAccounts/{Uri.EscapeDataString(id)}/Password");

    [McpServerTool(Name = "Core_Me_UpdatePersonalAccountPassword", Title = "Me - UpdatePersonalAccountPassword",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an existing personal account password.")]
    public Task<string> Me_UpdatePersonalAccountPassword(McpServer server,
        [Description("Unique identifier of the personal account.")] string id,
        [Description("Personal account Password to update.")] string body)
        => PutAsync(server, $"/v4/Me/PersonalAccounts/{Uri.EscapeDataString(id)}/Password", body);

    [McpServerTool(Name = "Core_Me_GetPersonalAccountPasswordHistory", Title = "Me - GetPersonalAccountPasswordHistory",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a personal account password for the current user.")]
    public Task<string> Me_GetPersonalAccountPasswordHistory(McpServer server,
        [Description("Unique identifier of the personal account password.")] string id,
        [Description("Get activity that occurred after this date. Defaults to 1 day before endDate. (Preferred over 'filter').")] string startDate = null,
        [Description("Get activity that occurred before this date. Defaults to now. (Preferred over filter).")] string endDate = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Me/PersonalAccounts/{Uri.EscapeDataString(id)}/PasswordHistory" + Q(("startDate", startDate), ("endDate", endDate), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_SharePersonalAccount", Title = "Me - SharePersonalAccount",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Shares a personal account with another user.")]
    public Task<string> Me_SharePersonalAccount(McpServer server,
        [Description("Unique identifier of the personal account.")] string id,
        [Description("PersonalAccountShare information.")] string body = null)
        => PostAsync(server, $"/v4/Me/PersonalAccounts/{Uri.EscapeDataString(id)}/Share", body);

    [McpServerTool(Name = "Core_Me_UpdatePersonalAccountShare", Title = "Me - UpdatePersonalAccountShare",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates the personal account share.")]
    public Task<string> Me_UpdatePersonalAccountShare(McpServer server,
        [Description("Unique identifier of the personal account.")] string id,
        [Description("Unique identifier of the user that the personal account is shared with.")] string sharedWithId,
        [Description("PersonalAccountShare information.")] string body)
        => PutAsync(server, $"/v4/Me/PersonalAccounts/{Uri.EscapeDataString(id)}/Share/{Uri.EscapeDataString(sharedWithId)}", body);

    [McpServerTool(Name = "Core_Me_UnsharePersonalAccount", Title = "Me - UnsharePersonalAccount",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Unshares a personal account with another user.")]
    public Task<string> Me_UnsharePersonalAccount(McpServer server,
        [Description("Unique identifier of the personal account.")] string id,
        [Description("Unique identifier of the user that the personal account is shared with.")] string sharedWithId)
        => DeleteAsync(server, $"/v4/Me/PersonalAccounts/{Uri.EscapeDataString(id)}/Share/{Uri.EscapeDataString(sharedWithId)}");

    [McpServerTool(Name = "Core_Me_GeneratePersonalAccountPassword", Title = "Me - GeneratePersonalAccountPassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Generates a new personal account password.")]
    public Task<string> Me_GeneratePersonalAccountPassword(McpServer server,
        [Description("Personal account password generation rules.")] string body = null)
        => PostAsync(server, "/v4/Me/PersonalAccounts/GeneratePassword", body);

    [McpServerTool(Name = "Core_Me_GetUsers", Title = "Me - GetUsers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a list of users.")]
    public Task<string> Me_GetUsers(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/PersonalAccounts/ShareWithUsers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetMyPhoto", Title = "Me - GetMyPhoto",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the user's photo.")]
    public Task<string> Me_GetMyPhoto(McpServer server)
        => GetAsync(server, "/v4/Me/Photo");

    [McpServerTool(Name = "Core_Me_UpdateMyPhoto", Title = "Me - UpdateMyPhoto",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates current users photo.")]
    public Task<string> Me_UpdateMyPhoto(McpServer server,
        [Description("Updated Photo.")] string body)
        => PutAsync(server, "/v4/Me/Photo", body);

    [McpServerTool(Name = "Core_Me_DeleteMyPhoto", Title = "Me - DeleteMyPhoto",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes current user photo.")]
    public Task<string> Me_DeleteMyPhoto(McpServer server)
        => DeleteAsync(server, "/v4/Me/Photo");

    [McpServerTool(Name = "Core_Me_GetRawPhoto", Title = "Me - GetRawPhoto",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the user's photo in raw format.")]
    public Task<string> Me_GetRawPhoto(McpServer server)
        => GetAsync(server, "/v4/Me/Photo/Raw");

    [McpServerTool(Name = "Core_Me_GetMyPreferences", Title = "Me - GetMyPreferences",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all preferences for the current user.")]
    public Task<string> Me_GetMyPreferences(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/Preferences" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetMyPreference", Title = "Me - GetMyPreference",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific preference for the current user.")]
    public Task<string> Me_GetMyPreference(McpServer server,
        [Description("Unique identifier of the UserPreference.")] string name,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/Preferences/{Uri.EscapeDataString(name)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_SetMyPreference", Title = "Me - SetMyPreference",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates or create a preference for the current user.")]
    public Task<string> Me_SetMyPreference(McpServer server,
        [Description("Unique identifier of the UserPreference.")] string name,
        [Description("Value to set for this preference.")] string body)
        => PutAsync(server, $"/v4/Me/Preferences/{Uri.EscapeDataString(name)}", body);

    [McpServerTool(Name = "Core_Me_DeleteMyPreference", Title = "Me - DeleteMyPreference",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a preference for the current user.")]
    public Task<string> Me_DeleteMyPreference(McpServer server,
        [Description("Unique identifier of the UserPreference.")] string name)
        => DeleteAsync(server, $"/v4/Me/Preferences/{Uri.EscapeDataString(name)}");

    [McpServerTool(Name = "Core_Me_GetAccessRequestEntitlements", Title = "Me - GetAccessRequestEntitlements",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets the entitlements for the current user.")]
    public Task<string> Me_GetAccessRequestEntitlements(McpServer server,
        [Description("Only report on access via a specific request type.")] string accessRequestType = null,
        [Description("List of asset IDs to get entitlements for (preferred over filter).")] string assetIds = null,
        [Description("List of account IDs to get entitlements for (preferred over filter).")] string accountIds = null,
        [Description("Whether to include information about active requests for same account.")] string includeActiveRequests = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/RequestEntitlements" + Q(("accessRequestType", accessRequestType), ("assetIds", assetIds), ("accountIds", accountIds), ("includeActiveRequests", includeActiveRequests), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetRequestFavorites", Title = "Me - GetRequestFavorites",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all favorite requests for the current user.")]
    public Task<string> Me_GetRequestFavorites(McpServer server,
        [Description("Whether to include information about active requests for same account.")] string includeActiveRequests = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/RequestFavorites" + Q(("includeActiveRequests", includeActiveRequests), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_CreateRequestFavorite", Title = "Me - CreateRequestFavorite",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates an UserRequestFavorite.")]
    public Task<string> Me_CreateRequestFavorite(McpServer server,
        [Description("UserRequestFavorite to create.")] string body = null)
        => PostAsync(server, "/v4/Me/RequestFavorites", body);

    [McpServerTool(Name = "Core_Me_GetRequestFavorite", Title = "Me - GetRequestFavorite",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific request favorites for the current user.")]
    public Task<string> Me_GetRequestFavorite(McpServer server,
        [Description("Unique ID of the request favorite.")] string favoriteId,
        [Description("Whether to include information about active requests for same account.")] string includeActiveRequests = null,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/RequestFavorites/{Uri.EscapeDataString(favoriteId)}" + Q(("includeActiveRequests", includeActiveRequests), ("fields", fields)));

    [McpServerTool(Name = "Core_Me_UpdateRequestFavorite", Title = "Me - UpdateRequestFavorite",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates an UserRequestFavorite.")]
    public Task<string> Me_UpdateRequestFavorite(McpServer server,
        [Description("Unique identifier of the UserRequestFavorite.")] string favoriteId,
        [Description("Updated UserRequestFavorite.")] string body)
        => PutAsync(server, $"/v4/Me/RequestFavorites/{Uri.EscapeDataString(favoriteId)}", body);

    [McpServerTool(Name = "Core_Me_DeleteRequestFavorite", Title = "Me - DeleteRequestFavorite",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes an UserRequestFavorite.")]
    public Task<string> Me_DeleteRequestFavorite(McpServer server,
        [Description("Unique identifier of the UserRequestFavorite.")] string favoriteId)
        => DeleteAsync(server, $"/v4/Me/RequestFavorites/{Uri.EscapeDataString(favoriteId)}");

    [McpServerTool(Name = "Core_Me_GetFavoriteRequests", Title = "Me - GetFavoriteRequests",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all requests belonging to the specified request favorite.")]
    public Task<string> Me_GetFavoriteRequests(McpServer server,
        [Description("Unique ID of the request favorite.")] string favoriteId,
        [Description("Whether to include information about active requests for same account.")] string includeActiveRequests = null,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, $"/v4/Me/RequestFavorites/{Uri.EscapeDataString(favoriteId)}/Requests" + Q(("includeActiveRequests", includeActiveRequests), ("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_SetFavoriteRequests", Title = "Me - SetFavoriteRequests",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Sets the requests assigned to the specified request favorite.")]
    public Task<string> Me_SetFavoriteRequests(McpServer server,
        [Description("Unique ID of the request favorite.")] string favoriteId,
        [Description("Requests to assign to the UserRequestFavorite.")] string body)
        => PutAsync(server, $"/v4/Me/RequestFavorites/{Uri.EscapeDataString(favoriteId)}/Requests", body);

    [McpServerTool(Name = "Core_Me_ModifyFavoriteRequests", Title = "Me - ModifyFavoriteRequests",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Add/Remove the requests assigned to the specified request favorite.")]
    public Task<string> Me_ModifyFavoriteRequests(McpServer server,
        [Description("Unique ID of the request favorite.")] string favoriteId,
        [Description("Operation to perform on the list.")] string operation,
        [Description("Requests to assign to the UserRequestFavorite.")] string body = null)
        => PostAsync(server, $"/v4/Me/RequestFavorites/{Uri.EscapeDataString(favoriteId)}/Requests/{Uri.EscapeDataString(operation)}", body);

    [McpServerTool(Name = "Core_Me_DeleteMultipleRequestFavorites", Title = "Me - DeleteMultipleRequestFavorites",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple request favorites to delete.")]
    public Task<string> Me_DeleteMultipleRequestFavorites(McpServer server,
        [Description("Request favorite IDs to delete.")] string body = null)
        => PostAsync(server, "/v4/Me/RequestFavorites/BatchDelete", body);

    [McpServerTool(Name = "Core_Me_UpdateMultipleRequestFavorites", Title = "Me - UpdateMultipleRequestFavorites",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Processes multiple request favorites to update.")]
    public Task<string> Me_UpdateMultipleRequestFavorites(McpServer server,
        [Description("Request Favorites to process.")] string body = null)
        => PostAsync(server, "/v4/Me/RequestFavorites/BatchUpdate", body);

    [McpServerTool(Name = "Core_Me_GetMyRoles", Title = "Me - GetMyRoles",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets information about all roles the current user belongs to.")]
    public Task<string> Me_GetMyRoles(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/Roles" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetScheduledAuditLogReports", Title = "Me - GetScheduledAuditLogReports",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all scheduled audit log search reports for the current user.")]
    public Task<string> Me_GetScheduledAuditLogReports(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/ScheduledAuditLogReports" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_CreateScheduledAuditLogSearchReport", Title = "Me - CreateScheduledAuditLogSearchReport",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Creates a new scheduled audit log search report.")]
    public Task<string> Me_CreateScheduledAuditLogSearchReport(McpServer server,
        [Description("Scheduled report to create.")] string body = null)
        => PostAsync(server, "/v4/Me/ScheduledAuditLogReports", body);

    [McpServerTool(Name = "Core_Me_GetScheduledAuditLogReport", Title = "Me - GetScheduledAuditLogReport",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific scheduled audit log search report for the current user.")]
    public Task<string> Me_GetScheduledAuditLogReport(McpServer server,
        [Description("Unique ID of the search.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/ScheduledAuditLogReports/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_UpdateScheduledAuditLogSearchReport", Title = "Me - UpdateScheduledAuditLogSearchReport",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Updates a scheduled audit log search report.")]
    public Task<string> Me_UpdateScheduledAuditLogSearchReport(McpServer server,
        [Description("Unique identifier of the scheduled audit log search report.")] string id,
        [Description("Updated scheduled audit log search report.")] string body)
        => PutAsync(server, $"/v4/Me/ScheduledAuditLogReports/{Uri.EscapeDataString(id)}", body);

    [McpServerTool(Name = "Core_Me_DeleteScheduledAuditLogReport", Title = "Me - DeleteScheduledAuditLogReport",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Removes a scheduled audit log search report.")]
    public Task<string> Me_DeleteScheduledAuditLogReport(McpServer server,
        [Description("Unique identifier of the scheduled audit log search report.")] string id)
        => DeleteAsync(server, $"/v4/Me/ScheduledAuditLogReports/{Uri.EscapeDataString(id)}");

    [McpServerTool(Name = "Core_Me_ExecuteScheduledAuditLogReport", Title = "Me - ExecuteScheduledAuditLogReport",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Executes the audit log search using saved report configuration.")]
    public Task<string> Me_ExecuteScheduledAuditLogReport(McpServer server,
        [Description("Scheduled report to execute.")] string id)
        => PostAsync(server, $"/v4/Me/ScheduledAuditLogReports/{Uri.EscapeDataString(id)}/Execute");

    [McpServerTool(Name = "Core_Me_GetMySubscribers", Title = "Me - GetMySubscribers",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets all event subscribers for the current user.")]
    public Task<string> Me_GetMySubscribers(McpServer server,
        [Description("Filter expression.")] string filter = null,
        [Description("Page number.")] string page = null,
        [Description("Items per page.")] string limit = null,
        [Description("Include total count.")] string count = null,
        [Description("Comma-separated property names.")] string fields = null,
        [Description("Order by expression.")] string orderby = null,
        [Description("Search query.")] string q = null)
        => GetAsync(server, "/v4/Me/Subscribers" + Q(("filter", filter), ("page", page), ("limit", limit), ("count", count), ("fields", fields), ("orderby", orderby), ("q", q)));

    [McpServerTool(Name = "Core_Me_GetMySubscriber", Title = "Me - GetMySubscriber",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Gets a specific event subscriber for the current user.")]
    public Task<string> Me_GetMySubscriber(McpServer server,
        [Description("Unique ID of the subscriber.")] string id,
        [Description("Comma-separated property names.")] string fields = null)
        => GetAsync(server, $"/v4/Me/Subscribers/{Uri.EscapeDataString(id)}" + Q(("fields", fields)));

    [McpServerTool(Name = "Core_Me_DisableMySubscriber", Title = "Me - DisableMySubscriber",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Disable event subscriber for the current user.")]
    public Task<string> Me_DisableMySubscriber(McpServer server,
        [Description("Unique identifier of the Subscribers.")] string id)
        => PostAsync(server, $"/v4/Me/Subscribers/{Uri.EscapeDataString(id)}/Disable");

    [McpServerTool(Name = "Core_Me_EnableMySubscriber", Title = "Me - EnableMySubscriber",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enable event subscriber for the current user.")]
    public Task<string> Me_EnableMySubscriber(McpServer server,
        [Description("Unique identifier of the Subscribers.")] string id)
        => PostAsync(server, $"/v4/Me/Subscribers/{Uri.EscapeDataString(id)}/Enable");

    [McpServerTool(Name = "Core_Me_DisableSubscribers", Title = "Me - DisableSubscribers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Disable event subscribers for the current user.")]
    public Task<string> Me_DisableSubscribers(McpServer server,
        [Description("Unique identifier of the Subscribers.")] string body = null)
        => PostAsync(server, "/v4/Me/Subscribers/Disable", body);

    [McpServerTool(Name = "Core_Me_EnableSubscribers", Title = "Me - EnableSubscribers",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Enable event subscribers for the current user.")]
    public Task<string> Me_EnableSubscribers(McpServer server,
        [Description("Unique identifier of the Subscribers.")] string body = null)
        => PostAsync(server, "/v4/Me/Subscribers/Enable", body);

    [McpServerTool(Name = "Core_Me_ValidateMyPassword", Title = "Me - ValidateMyPassword",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = true)]
    [Description("Validates that a password meets requirements.")]
    public Task<string> Me_ValidateMyPassword(McpServer server,
        [Description("Password to validate.")] string body = null)
        => PostAsync(server, "/v4/Me/ValidatePassword", body);
}
