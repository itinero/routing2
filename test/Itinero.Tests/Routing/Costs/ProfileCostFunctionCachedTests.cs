using System.Linq;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routing.Costs;
using Itinero.Routing.Costs.Caches;
using Itinero.Tests.Network;
using Xunit;

namespace Itinero.Tests.Routing.Costs;

public class ProfileCostFunctionCachedTests
{
    [Fact]
    public void ProfileCostFunctionCached_ForwardEdge_ForwardCosts_ShouldUseForwardFactor()
    {
        // stage.
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1));
        var costFunction = new ProfileCostFunctionCached(profile, new EdgeFactorCache(), new TurnCostFactorCache());
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
    public void ProfileCostFunctionCached_BackwardEdgeCached_ForwardEdge_ForwardCosts_ShouldUseForwardFactor()
    {
        // stage.
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1));
        var costFunction = new ProfileCostFunctionCached(profile, new EdgeFactorCache(), new TurnCostFactorCache());
        var edgeEnumerator = new EdgeEnumeratorMock((new EdgeId(42, 42), 100, false, 24));
        edgeEnumerator.MoveNext();
        costFunction.Get(edgeEnumerator, true,
            Enumerable.Empty<(EdgeId edgeId, byte? turn)>());
        edgeEnumerator = new EdgeEnumeratorMock((new EdgeId(42, 42), 100, true, 24));
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
    public void ProfileCostFunctionCached_ForwardEdge_BackwardCosts_ShouldUseBackwardFactor()
    {
        // stage.
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1));
        var costFunction = new ProfileCostFunctionCached(profile, new EdgeFactorCache(), new TurnCostFactorCache());
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
    public void ProfileCostFunctionCached_BackwardEdge_ForwardCosts_ShouldUseBackwardFactor()
    {
        // stage.
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1));
        var costFunction = new ProfileCostFunctionCached(profile, new EdgeFactorCache(), new TurnCostFactorCache());
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
    public void ProfileCostFunctionCached_BackwardEdge_BackwardCosts_ShouldUseForwardFactor()
    {
        // stage.
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1));
        var costFunction = new ProfileCostFunctionCached(profile, new EdgeFactorCache(), new TurnCostFactorCache());
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
    public void ProfileCostFunctionCached_LocalAccessEdge_ShouldReportLocalAccessTrue()
    {
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1, isLocalAccess: true));
        var costFunction = new ProfileCostFunctionCached(profile, new EdgeFactorCache(), new TurnCostFactorCache());
        var edgeEnumerator = new EdgeEnumeratorMock((new EdgeId(42, 42), 100, true, 24));
        edgeEnumerator.MoveNext();

        var costs = costFunction.Get(edgeEnumerator, true, Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

        Assert.True(costs.localAccess);
    }

    [Fact]
    public void ProfileCostFunctionCached_NonLocalAccessEdge_ShouldReportLocalAccessFalse()
    {
        var profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(10, 1, 10, 1, isLocalAccess: false));
        var costFunction = new ProfileCostFunctionCached(profile, new EdgeFactorCache(), new TurnCostFactorCache());
        var edgeEnumerator = new EdgeEnumeratorMock((new EdgeId(42, 42), 100, true, 24));
        edgeEnumerator.MoveNext();

        var costs = costFunction.Get(edgeEnumerator, true, Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

        Assert.False(costs.localAccess);
    }

    [Fact]
    public void ProfileCostFunctionCached_LocalAccessEdge_SecondCallUsesCache_ShouldStillReportLocalAccessTrue()
    {
        // Counter-counts how often the profile is consulted; should be 1, not 2.
        var hits = 0;
        var profile = new DefaultProfile(getEdgeFactor: (_) =>
        {
            hits++;
            return new EdgeFactor(10, 1, 10, 1, isLocalAccess: true);
        });
        var costFunction = new ProfileCostFunctionCached(profile, new EdgeFactorCache(), new TurnCostFactorCache());
        var edgeEnumerator = new EdgeEnumeratorMock((new EdgeId(42, 42), 100, true, 24));
        edgeEnumerator.MoveNext();

        var first = costFunction.Get(edgeEnumerator, true, Enumerable.Empty<(EdgeId edgeId, byte? turn)>());
        var second = costFunction.Get(edgeEnumerator, true, Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

        Assert.True(first.localAccess);
        Assert.True(second.localAccess);
        Assert.Equal(1, hits);
    }
}
