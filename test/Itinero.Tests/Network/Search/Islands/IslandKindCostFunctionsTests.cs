using System.Collections.Generic;
using Itinero;
using Itinero.Network;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Itinero.Routing.Costs;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

public class IslandKindCostFunctionsTests
{
    [Fact]
    public void GetFor_Full_ShouldReturnProfileCostFunctionUnwrapped()
    {
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var network = routerDb.Latest;

        var costFunction = IslandKindCostFunctions.GetFor(network, profile, IslandKind.Full);

        // Full should not wrap with the NonLocal decorator.
        Assert.IsNotType<NonLocalCostFunction>(costFunction);
    }

    [Fact]
    public void GetFor_NonLocal_ShouldReturnNonLocalCostFunctionDecorator()
    {
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var network = routerDb.Latest;

        var costFunction = IslandKindCostFunctions.GetFor(network, profile, IslandKind.NonLocal);

        // NonLocal must wrap with the decorator that masks L-edges off.
        Assert.IsType<NonLocalCostFunction>(costFunction);
    }
}
