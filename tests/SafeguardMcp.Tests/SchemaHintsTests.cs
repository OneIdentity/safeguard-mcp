using SafeguardMcp.Catalog;

namespace SafeguardMcp.Tests;

public class SchemaHintsTests
{
    [Fact]
    public void GetHints_DoesNotCarry_BatchOperations_Key()
    {
        // Regression guard: the dead "BatchOperations" hint was keyed by a property name
        // that no Safeguard schema actually carries (the appliance exposes Batch* via path
        // segments, not as a schema property). The authoritative bulk-operations guidance
        // now lives in Safeguard_Execute's tool description and the bulk-asset-operations
        // recipe. Make sure the dead key never returns.
        var schema = new ApiSchema(
            typeName: "AnyType",
            properties: new[] { new SchemaProperty("BatchOperations", "string", null, false) },
            requiredFields: System.Array.Empty<string>());

        var hints = SchemaHints.GetHints(schema);
        if (hints is not null)
        {
            Assert.DoesNotContain(hints, h => h.PropertyName == "BatchOperations");
        }
    }
}
