using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;
using Itinero.Tests.Network;
using Xunit;

namespace Itinero.Tests.Routing.Costs;

public class NonLocalCostFunctionTests
{
    [Fact]
    public void NonLocalCostFunction_NonLocalAccessEdge_ShouldPassThrough()
    {
        // Inner reports localAccess=false; the decorator should leave the result alone.
        var inner = new StaticCostFunction(canAccess: true, canStop: true, localAccess: false, cost: 5, turnCost: 0);
        var decorator = new NonLocalCostFunction(inner);
        var edgeEnumerator = new EdgeEnumeratorMock(new EdgeId(42, 42));
        edgeEnumerator.MoveNext();

        var costs = decorator.Get(edgeEnumerator, true, null);

        Assert.True(costs.canAccess);
        Assert.True(costs.canStop);
        Assert.False(costs.localAccess);
        Assert.Equal(5, costs.cost);
        Assert.Equal(0, costs.turnCost);
    }

    [Fact]
    public void NonLocalCostFunction_LocalAccessEdge_ShouldMaskOff()
    {
        // Inner reports localAccess=true; the decorator should mask it as inaccessible.
        var inner = new StaticCostFunction(canAccess: true, canStop: true, localAccess: true, cost: 5, turnCost: 0);
        var decorator = new NonLocalCostFunction(inner);
        var edgeEnumerator = new EdgeEnumeratorMock(new EdgeId(42, 42));
        edgeEnumerator.MoveNext();

        var costs = decorator.Get(edgeEnumerator, true, null);

        Assert.False(costs.canAccess);
        Assert.True(costs.localAccess);
        Assert.Equal(double.MaxValue, costs.turnCost);
    }

    [Fact]
    public void NonLocalCostFunction_MaskedOffEdge_ShouldFailIslandBuilderCheck()
    {
        // The island classifier consumes canAccess && turnCost < MaxValue. A masked-off
        // local-access edge should fail both, so it doesn't get traversed during the
        // N-only classification.
        var inner = new StaticCostFunction(canAccess: true, canStop: true, localAccess: true, cost: 5, turnCost: 0);
        var decorator = new NonLocalCostFunction(inner);
        var edgeEnumerator = new EdgeEnumeratorMock(new EdgeId(42, 42));
        edgeEnumerator.MoveNext();

        var costs = decorator.Get(edgeEnumerator, true, null);

        Assert.False(costs is { canAccess: true, turnCost: < double.MaxValue });
    }

    private sealed class StaticCostFunction : ICostFunction
    {
        private readonly bool _canAccess;
        private readonly bool _canStop;
        private readonly bool _localAccess;
        private readonly double _cost;
        private readonly double _turnCost;

        public StaticCostFunction(bool canAccess, bool canStop, bool localAccess, double cost, double turnCost)
        {
            _canAccess = canAccess;
            _canStop = canStop;
            _localAccess = localAccess;
            _cost = cost;
            _turnCost = turnCost;
        }

        public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
            IEdgeEnumerator<RoutingNetwork> edgeEnumerator,
            bool tailToHead = true,
            IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
            => (_canAccess, _canStop, _localAccess, _cost, _turnCost);
    }
}
