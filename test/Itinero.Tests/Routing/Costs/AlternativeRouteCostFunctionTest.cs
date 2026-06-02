using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Routing.Costs;
using Itinero.Tests.Network;
using Xunit;

namespace Itinero.Tests.Routing.Costs;

public class AlternativeRouteCostFunctionTest
{
    [Fact]
    public void AlternativeRouteCostFunction_WithOneVisitedEdge_PenalizedEdge_IsMoreCostly()
    {
        var originalCostFunction = new MockCostFunction(_ => 1);
        var altCostFunc = new AlternativeRouteCostFunction(originalCostFunction, new Dictionary<EdgeId, int> {
            { new EdgeId(42, 42), 1 }
            });
        var edgeEnumerator = new EdgeEnumeratorMock(new EdgeId(42, 42));
        edgeEnumerator.MoveNext();
        var (_, _, _, cost, _) =
            altCostFunc.Get(edgeEnumerator, true, []);
        Assert.Equal(2, cost);
    }

    [Fact]
    public void AlternativeRouteCostFunction_WithOneVisitedEdge_NonPenalizedEdge_HasSameCost()
    {
        var originalCostFunction = new MockCostFunction(_ => 1);
        var altCostFunc = new AlternativeRouteCostFunction(originalCostFunction, new Dictionary<EdgeId, int> {
            {new EdgeId(42, 42), 2}
        });

        var nonPenalizedEdge = new EdgeEnumeratorMock(new EdgeId(42, 41));
        nonPenalizedEdge.MoveNext();
        var (_, _, _, normalCost, _) =
            altCostFunc.Get(nonPenalizedEdge, true, Enumerable.Empty<(EdgeId edgeId, byte? turn)>());
        Assert.Equal(1, normalCost);
    }

    [Fact]
    public void AlternativeRouteCostFunction_LocalAccessFlag_ShouldPassThroughFromInner()
    {
        // Inner reports localAccess=true; AlternativeRouteCostFunction should propagate it,
        // both for the penalized branch and the pass-through branch.
        var inner = new LocalAccessAwareMockCostFunction(localAccess: true, cost: 1);
        var alt = new AlternativeRouteCostFunction(inner, new Dictionary<EdgeId, int> {
            { new EdgeId(42, 42), 1 }
        });

        var penalized = new EdgeEnumeratorMock(new EdgeId(42, 42));
        penalized.MoveNext();
        var nonPenalized = new EdgeEnumeratorMock(new EdgeId(42, 41));
        nonPenalized.MoveNext();

        var penalizedCosts = alt.Get(penalized, true, Enumerable.Empty<(EdgeId edgeId, byte? turn)>());
        var passThroughCosts = alt.Get(nonPenalized, true, Enumerable.Empty<(EdgeId edgeId, byte? turn)>());

        Assert.True(penalizedCosts.localAccess);
        Assert.True(passThroughCosts.localAccess);
    }

    private sealed class LocalAccessAwareMockCostFunction : Itinero.Routing.Costs.ICostFunction
    {
        private readonly bool _localAccess;
        private readonly double _cost;
        public LocalAccessAwareMockCostFunction(bool localAccess, double cost)
        {
            _localAccess = localAccess;
            _cost = cost;
        }
        public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
            Itinero.Network.Enumerators.Edges.IEdgeEnumerator<RoutingNetwork> edgeEnumerator,
            bool tailToHead = true,
            IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
            => (true, true, _localAccess, _cost, 0);
    }
}
