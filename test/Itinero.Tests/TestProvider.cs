using Itinero.Data.Providers;
using Itinero.Indexes;

namespace Itinero.Tests;

/// <summary>
/// Creates the default provider for fixtures. A provider creates its own db and is the only thing
/// that can write to it, so a fixture holds the provider and takes the db from it.
/// </summary>
internal static class TestProvider
{
    public static DefaultDataProvider New(AttributeSetMap? edgeTypeMap = null,
        AttributeSetMap? turnCostTypeMap = null)
    {
        return new DefaultDataProvider(edgeTypeMap, turnCostTypeMap);
    }
}
