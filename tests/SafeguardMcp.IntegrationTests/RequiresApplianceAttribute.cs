namespace SafeguardMcp.IntegrationTests;

/// <summary>
/// Skips the test if no Safeguard appliance is configured (SAFEGUARD_TEST_HOST not set).
/// Apply to integration test classes via [Collection("Appliance")].
/// </summary>
public sealed class RequiresApplianceFact : FactAttribute
{
    public RequiresApplianceFact()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SPP_HOST")))
        {
            Skip = "No appliance configured. Set SPP_HOST to run integration tests.";
        }
    }
}

public sealed class RequiresApplianceTheory : TheoryAttribute
{
    public RequiresApplianceTheory()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SPP_HOST")))
        {
            Skip = "No appliance configured. Set SPP_HOST to run integration tests.";
        }
    }
}
