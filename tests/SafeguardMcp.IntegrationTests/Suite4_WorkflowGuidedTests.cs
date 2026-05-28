using System.Text.Json;
using Xunit;

namespace SafeguardMcp.IntegrationTests;

[Collection("AgentSimulation")]
public class Suite4_WorkflowGuidedTests
{
    private readonly AgentSimulationFixture _fixture;

    public Suite4_WorkflowGuidedTests(AgentSimulationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Workflow_HealthCheck_AllStepsSucceed()
    {
        if (!_fixture.Available) return;

        var workflow = _fixture.GetWorkflows(search: "health");
        Assert.True(
            workflow.Contains("health-check", StringComparison.OrdinalIgnoreCase)
            || workflow.Contains("health", StringComparison.OrdinalIgnoreCase),
            $"Expected a health workflow, got: {workflow}");

        string status = null;
        try
        {
            status = await _fixture.ExecuteAsync("GET", "/v4/Status");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Workflow_HealthCheck] Skipping /v4/Status verification: {ex.Message}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            Assert.True(
                status.Contains("Online", StringComparison.OrdinalIgnoreCase)
                || status.Contains("State", StringComparison.OrdinalIgnoreCase),
                $"Expected /v4/Status to include Online or State, got: {status}");
        }

        var health = await _fixture.ExecuteAsync("GET", "/v4/ApplianceStatus/Health");
        using (var healthDoc = JsonDocument.Parse(health))
        {
            Assert.True(
                ContainsAnyProperty(healthDoc.RootElement, "Cpu", "Memory", "Disk"),
                $"Expected appliance health data to include CPU/memory/disk, got: {health}");
        }

        var members = await _fixture.ExecuteAsync("GET", "/v4/Cluster/Members");
        using var membersDoc = JsonDocument.Parse(members);
        Assert.Equal(JsonValueKind.Array, membersDoc.RootElement.ValueKind);
        Assert.True(membersDoc.RootElement.GetArrayLength() > 0, "Expected at least one cluster member.");
    }

    [Fact]
    public async Task Workflow_CreateEntitlement_CreatesWorkingRoleAndPolicy()
    {
        if (!_fixture.Available) return;

        var workflow = _fixture.GetWorkflows(id: "create-entitlement");
        Assert.True(
            workflow.Contains("Create an Entitlement", StringComparison.OrdinalIgnoreCase)
            || workflow.Contains("create-entitlement", StringComparison.OrdinalIgnoreCase),
            $"Expected the create-entitlement workflow, got: {workflow}");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var roleName = $"McpTest_Entitlement_{suffix}";
        var policyName = $"McpTest_Policy_{suffix}";

        var roleBody = JsonSerializer.Serialize(new
        {
            Name = roleName,
            Description = "Workflow-guided entitlement test"
        });
        var roleResult = await _fixture.ExecuteAsync("POST", "/v4/Roles", body: roleBody);
        using var roleDoc = JsonDocument.Parse(roleResult);
        var roleId = ReadRequiredInt32(roleDoc.RootElement, "Id");
        _fixture.RegisterCleanup("DELETE", $"/v4/Roles/{roleId}");

        var memberBody = JsonSerializer.Serialize(new[]
        {
            new { Id = _fixture.TestAdminUserId }
        });
        await _fixture.ExecuteAsync("POST", $"/v4/Roles/{roleId}/Members/Add", body: memberBody);

        var policyBody = JsonSerializer.Serialize(new
        {
            Name = policyName,
            RoleId = roleId,
            AccessRequestProperties = new
            {
                AccessRequestType = "Password",
                AllowSimultaneousAccess = false,
                MaximumDurationDays = 0,
                MaximumDurationHours = 4
            }
        });
        var policyResult = await _fixture.ExecuteAsync("POST", "/v4/AccessPolicies", body: policyBody);
        using var policyDoc = JsonDocument.Parse(policyResult);
        var policyId = ReadRequiredInt32(policyDoc.RootElement, "Id");
        _fixture.RegisterCleanup("DELETE", $"/v4/AccessPolicies/{policyId}");

        var verifyRole = await _fixture.ExecuteAsync("GET", $"/v4/Roles/{roleId}");
        using var verifyRoleDoc = JsonDocument.Parse(verifyRole);
        var memberCount = ReadRequiredInt32(verifyRoleDoc.RootElement, "MemberCount");
        Assert.True(memberCount > 0, $"Expected role {roleId} to have at least one member.");
    }

    [Fact]
    public async Task Workflow_TaskTriage_QuerySyntaxIsExecutable()
    {
        if (!_fixture.Available) return;

        var workflow = _fixture.GetWorkflows(id: "task-triage");
        Assert.True(
            workflow.Contains("task-triage", StringComparison.OrdinalIgnoreCase)
            || workflow.Contains("Task Failures", StringComparison.OrdinalIgnoreCase),
            $"Expected the task-triage workflow, got: {workflow}");

        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/AssetAccounts",
            query: "filter=TaskProperties.HasAccountTaskFailure eq true&count=true");

        using var doc = JsonDocument.Parse(result);
        Assert.True(
            doc.RootElement.ValueKind is JsonValueKind.Array or JsonValueKind.Object or JsonValueKind.Number,
            $"Expected valid JSON from task triage query, got: {result}");
    }

    [Fact]
    public async Task Workflow_PasswordAccessRequest_CompletesCheckoutLifecycle()
    {
        if (!_fixture.Available) return;

        var workflow = _fixture.GetWorkflows(search: "password access request");
        Assert.True(
            workflow.Contains("password-access-request", StringComparison.OrdinalIgnoreCase)
            || workflow.Contains("Password Access Request", StringComparison.OrdinalIgnoreCase),
            $"Expected a password access request workflow, got: {workflow}");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var assetName = $"McpTest_AccessAsset_{suffix}";
        var accountName = $"McpTest_AccessAccount_{suffix}";
        var roleName = $"McpTest_AccessRole_{suffix}";
        var policyName = $"McpTest_AccessPolicy_{suffix}";

        var assetBody = JsonSerializer.Serialize(new
        {
            Name = assetName,
            NetworkAddress = $"10.{Random.Shared.Next(1, 254)}.{Random.Shared.Next(1, 254)}.{Random.Shared.Next(1, 254)}",
            PlatformId = await GetWindowsServerPlatformIdAsync(),
            AssetPartitionId = -1
        });
        var assetResult = await _fixture.ExecuteAsync("POST", "/v4/Assets", body: assetBody);
        using var assetDoc = JsonDocument.Parse(assetResult);
        var assetId = ReadRequiredInt32(assetDoc.RootElement, "Id");
        _fixture.RegisterCleanup("DELETE", $"/v4/Assets/{assetId}");

        var accountSchema = _fixture.Schema("/v4/AssetAccounts", "POST");
        var accountBody = await new SchemaBodyBuilder(_fixture)
            .WithOverride("Name", $"\"{accountName}\"")
            .WithOverride("Asset", $"{{\"Id\": {assetId}}}")
            .BuildAsync(accountSchema);
        var accountResult = await _fixture.ExecuteAsync("POST", "/v4/AssetAccounts", body: accountBody);
        using var accountDoc = JsonDocument.Parse(accountResult);
        var accountId = ReadRequiredInt32(accountDoc.RootElement, "Id");
        _fixture.RegisterCleanup("DELETE", $"/v4/AssetAccounts/{accountId}");

        var roleBody = JsonSerializer.Serialize(new
        {
            Name = roleName,
            Description = "Workflow-guided password checkout test"
        });
        var roleResult = await _fixture.ExecuteAsync("POST", "/v4/Roles", body: roleBody);
        using var roleDoc = JsonDocument.Parse(roleResult);
        var roleId = ReadRequiredInt32(roleDoc.RootElement, "Id");
        _fixture.RegisterCleanup("DELETE", $"/v4/Roles/{roleId}");

        var memberBody = JsonSerializer.Serialize(new[]
        {
            new { Id = _fixture.TestAdminUserId }
        });
        await _fixture.ExecuteAsync("POST", $"/v4/Roles/{roleId}/Members/Add", body: memberBody);

        var policyBody = JsonSerializer.Serialize(new
        {
            Name = policyName,
            RoleId = roleId,
            AccessRequestProperties = new
            {
                AccessRequestType = "Password",
                AllowSimultaneousAccess = false,
                MaximumDurationDays = 0,
                MaximumDurationHours = 1
            }
        });
        var policyResult = await _fixture.ExecuteAsync("POST", "/v4/AccessPolicies", body: policyBody);
        using var policyDoc = JsonDocument.Parse(policyResult);
        var policyId = ReadRequiredInt32(policyDoc.RootElement, "Id");
        _fixture.RegisterCleanup("DELETE", $"/v4/AccessPolicies/{policyId}");

        var scopeBody = JsonSerializer.Serialize(new[]
        {
            new { Id = accountId }
        });
        try
        {
            await _fixture.ExecuteAsync("POST", $"/v4/AccessPolicies/{policyId}/ScopeItems/Add", body: scopeBody);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to add account {accountId} to access policy {policyId} scope using body {scopeBody}.",
                ex);
        }

        var entitlementFound = await WaitForRequestEntitlementAsync(accountId, accountName);
        Assert.True(entitlementFound, $"Expected account {accountName} ({accountId}) to appear in /v4/Me/RequestEntitlements.");

        var requestBody = JsonSerializer.Serialize(new
        {
            AccountId = accountId,
            AssetId = assetId,
            AccessRequestType = "Password",
            ReasonComment = "Workflow-guided integration test",
            RequestedDurationDays = 0,
            RequestedDurationHours = 1,
            IsEmergency = false
        });
        var requestResult = await _fixture.ExecuteAsync("POST", "/v4/AccessRequests", body: requestBody);
        using var requestDoc = JsonDocument.Parse(requestResult);
        var requestId = ReadRequiredId(requestDoc.RootElement);

        var state = GetStringProperty(requestDoc.RootElement, "State");
        if (!string.Equals(state, "Available", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var approveBody = JsonSerializer.Serialize("Approved by workflow-guided integration test");
                await _fixture.ExecuteAsync("POST", $"/v4/AccessRequests/{requestId}/Approve", body: approveBody);
            }
            catch (Exception ex)
            {
                var refreshedState = await WaitForAccessRequestStateAsync(requestId, attempts: 1);
                if (!string.Equals(refreshedState, "Available", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Failed to approve access request {requestId}. Current state: {refreshedState ?? "<unknown>"}.",
                        ex);
                }
            }
        }

        var availableState = await WaitForAccessRequestStateAsync(requestId, 10, "Available", "CheckedOut");
        Assert.True(
            string.Equals(availableState, "Available", StringComparison.OrdinalIgnoreCase)
            || string.Equals(availableState, "CheckedOut", StringComparison.OrdinalIgnoreCase),
            $"Expected access request {requestId} to become Available, got: {availableState ?? "<unknown>"}");

        var credential = await _fixture.ExecuteAsync("POST", $"/v4/AccessRequests/{requestId}/CheckOutPassword");
        Assert.False(string.IsNullOrWhiteSpace(ExtractTextValue(credential)));

        await _fixture.ExecuteAsync("POST", $"/v4/AccessRequests/{requestId}/CheckIn");

        var checkedInState = await WaitForAccessRequestStateAsync(requestId, 10, "CheckedIn", "Expired", "Complete", "Completed");
        Assert.True(
            string.Equals(checkedInState, "CheckedIn", StringComparison.OrdinalIgnoreCase)
            || string.Equals(checkedInState, "Expired", StringComparison.OrdinalIgnoreCase)
            || string.Equals(checkedInState, "Complete", StringComparison.OrdinalIgnoreCase)
            || string.Equals(checkedInState, "Completed", StringComparison.OrdinalIgnoreCase),
            $"Expected access request {requestId} to finish after check-in, got: {checkedInState ?? "<unknown>"}");
    }

    private async Task<int> GetWindowsServerPlatformIdAsync()
    {
        var result = await _fixture.ExecuteAsync(
            "GET",
            "/v4/Platforms",
            query: "filter=DisplayName eq 'Windows Server'&fields=Id,DisplayName&limit=1");
        using var doc = JsonDocument.Parse(result);
        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
        {
            return ReadRequiredInt32(doc.RootElement[0], "Id");
        }

        result = await _fixture.ExecuteAsync("GET", "/v4/Platforms", query: "fields=Id,DisplayName&limit=1");
        using var fallbackDoc = JsonDocument.Parse(result);
        return ReadRequiredInt32(fallbackDoc.RootElement[0], "Id");
    }

    private async Task<bool> WaitForRequestEntitlementAsync(int accountId, string accountName, int attempts = 10)
    {
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var entitlements = await _fixture.ExecuteAsync(
                "GET",
                "/v4/Me/RequestEntitlements",
                query: "fields=Account.Id,Account.Name,Asset.Name,Policy.Name,Policy.AccessRequestProperties&limit=200");

            if (ContainsEntitlementForAccount(entitlements, accountId, accountName))
            {
                return true;
            }

            await Task.Delay(1000);
        }

        return false;
    }

    private Task<string> WaitForAccessRequestStateAsync(string requestId, params string[] expectedStates)
        => WaitForAccessRequestStateAsync(requestId, 10, expectedStates);

    private async Task<string> WaitForAccessRequestStateAsync(string requestId, int attempts, params string[] expectedStates)
    {
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var response = await _fixture.ExecuteAsync("GET", $"/v4/AccessRequests/{requestId}");
            using var doc = JsonDocument.Parse(response);
            var state = GetStringProperty(doc.RootElement, "State");
            if (expectedStates.Length == 0 || expectedStates.Any(expected => string.Equals(state, expected, StringComparison.OrdinalIgnoreCase)))
            {
                return state;
            }

            await Task.Delay(1000);
        }

        var finalResponse = await _fixture.ExecuteAsync("GET", $"/v4/AccessRequests/{requestId}");
        using var finalDoc = JsonDocument.Parse(finalResponse);
        return GetStringProperty(finalDoc.RootElement, "State");
    }

    private static bool ContainsEntitlementForAccount(string json, int accountId, string accountName)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return json.Contains(accountName, StringComparison.OrdinalIgnoreCase)
                || json.Contains(accountId.ToString(), StringComparison.Ordinal);
        }

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (TryGetNestedProperty(item, out var account, "Account"))
            {
                var id = GetInt32Property(account, "Id");
                var name = GetStringProperty(account, "Name");
                if (id == accountId || string.Equals(name, accountName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsAnyProperty(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (propertyNames.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                if (ContainsAnyProperty(property.Value, propertyNames))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (ContainsAnyProperty(item, propertyNames))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetNestedProperty(JsonElement element, out JsonElement value, params string[] path)
    {
        value = element;
        foreach (var segment in path)
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                value = default;
                return false;
            }

            var found = false;
            foreach (var property in value.EnumerateObject())
            {
                if (!string.Equals(property.Name, segment, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = property.Value;
                found = true;
                break;
            }

            if (!found)
            {
                value = default;
                return false;
            }
        }

        return true;
    }

    private static int ReadRequiredInt32(JsonElement element, string propertyName)
    {
        var value = GetInt32Property(element, propertyName);
        return value ?? throw new InvalidOperationException($"Property '{propertyName}' was missing or not an integer. JSON: {element.GetRawText()}");
    }

    private static int? GetInt32Property(JsonElement element, string propertyName)
    {
        if (!TryGetNestedProperty(element, out var value, propertyName))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static string ReadRequiredId(JsonElement element)
    {
        if (!TryGetNestedProperty(element, out var idValue, "Id"))
        {
            throw new InvalidOperationException($"Property 'Id' was missing. JSON: {element.GetRawText()}");
        }

        return idValue.ValueKind switch
        {
            JsonValueKind.String => idValue.GetString(),
            JsonValueKind.Number => idValue.GetRawText(),
            _ => throw new InvalidOperationException($"Property 'Id' was not a string or number. JSON: {element.GetRawText()}")
        };
    }

    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        if (!TryGetNestedProperty(element, out var value, propertyName))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
            _ => null
        };
    }

    private static string ExtractTextValue(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return doc.RootElement.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }

        return response.Trim().Trim('"');
    }
}
