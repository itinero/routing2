﻿using System.Linq;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routing.Costs;
using Itinero.Tests.Network;
using Xunit;

namespace Itinero.Tests.Routing.Costs;

public class ProfileCostFunctionTests
{
    [Fact]
    public void ProfileCostFunction_ForwardEdge_ForwardCosts_ShouldUseForwardFactor()
    {
        // stage.
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1));
        var costFunction = new ProfileCostFunction(profile);
        var edgeEnumerator = new EdgeEnumeratorMock((new EdgeId(42, 42), 100, true, 24));
        edgeEnumerator.MoveNext();

        // run
        var costs = costFunction.Get(edgeEnumerator, true,
            Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

        // test.
        Assert.Equal(100 * 10, costs.cost);
        Assert.Equal(0, costs.turnCost);
        Assert.True(costs.canStop);
        Assert.True(costs.canAccess);
    }

    [Fact]
    public void ProfileCostFunction_ForwardEdge_BackwardCosts_ShouldUseBackwardFactor()
    {
        // stage.
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1));
        var costFunction = new ProfileCostFunction(profile);
        var edgeEnumerator = new EdgeEnumeratorMock((new EdgeId(42, 42), 100, true, 24));
        edgeEnumerator.MoveNext();

        // run
        var costs = costFunction.Get(edgeEnumerator, false,
            Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

        // test.
        Assert.Equal(100 * 1, costs.cost);
        Assert.Equal(0, costs.turnCost);
        Assert.True(costs.canStop);
        Assert.True(costs.canAccess);
    }

    [Fact]
    public void ProfileCostFunction_BackwardEdge_ForwardCosts_ShouldUseBackwardFactor()
    {
        // stage.
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1));
        var costFunction = new ProfileCostFunction(profile);
        var edgeEnumerator = new EdgeEnumeratorMock((new EdgeId(42, 42), 100, false, 24));
        edgeEnumerator.MoveNext();

        // run
        var costs = costFunction.Get(edgeEnumerator, true,
            Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

        // test.
        Assert.Equal(100 * 1, costs.cost);
        Assert.Equal(0, costs.turnCost);
        Assert.True(costs.canStop);
        Assert.True(costs.canAccess);
    }

    [Fact]
    public void ProfileCostFunction_BackwardEdge_BackwardCosts_ShouldUseForwardFactor()
    {
        // stage.
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1));
        var costFunction = new ProfileCostFunction(profile);
        var edgeEnumerator = new EdgeEnumeratorMock((new EdgeId(42, 42), 100, true, 24));
        edgeEnumerator.MoveNext();

        // run
        var costs = costFunction.Get(edgeEnumerator, true,
            Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

        // test.
        Assert.Equal(100 * 10, costs.cost);
        Assert.Equal(0, costs.turnCost);
        Assert.True(costs.canStop);
        Assert.True(costs.canAccess);
    }
}
