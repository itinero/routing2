using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Routing.Costs;
using Itinero.Tests.Network;
using Xunit;

namespace Itinero.Tests.Routing.Costs
{
    public class AlternativeRouteCostFunctionTest
    {
        [Fact]
        public void AlternativeRouteCostFunction_WithOneVisitedEdge_PenalizedEdge_IsMoreCostly()
        {
            var originalCostFunction = new MockCostFunction(_ => 1);
            var altCostFunc = new AlternativeRouteCostFunction(originalCostFunction, new HashSet<EdgeId> {
                new(42, 42)
            });
            var edgeEnumerator = new EdgeEnumeratorMock(new EdgeId(42, 42));
            edgeEnumerator.MoveNext();
            var (_, _, cost, _) =
                altCostFunc.Get(edgeEnumerator, true, Enumerable.Empty<(EdgeId edgeId, byte? turn)>());
            Assert.Equal(2, cost);
        }

        [Fact]
        public void AlternativeRouteCostFunction_WithOneVisitedEdge_NonPenalizedEdge_HasSameCost()
        {
            var originalCostFunction = new MockCostFunction(_ => 1);
            var altCostFunc = new AlternativeRouteCostFunction(originalCostFunction, new HashSet<EdgeId> {
                new(42, 42)
            });


            var nonPenalizedEdge = new EdgeEnumeratorMock(new EdgeId(42, 41));
            nonPenalizedEdge.MoveNext();
            var (_, _, normalCost, _) =
                altCostFunc.Get(nonPenalizedEdge, true, Enumerable.Empty<(EdgeId edgeId, byte? turn)>());
            Assert.Equal(1, normalCost);
        }
    }
}