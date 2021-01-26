using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.TurnCosts {
    public static class RoutingNetworkEdgeEnumeratorExtensions {
        internal static IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo(
            this RoutingNetworkEdgeEnumerator enumerator,
            IEnumerable<(EdgeId edge, byte? turn)> previousEdges) {
            using var previousEdgesEnumerator = previousEdges.GetEnumerator();
            if (!previousEdgesEnumerator.MoveNext()) {
                return Enumerable.Empty<(uint turnCostType, uint cost)>();
            }

            var fromOrder = previousEdgesEnumerator.Current.turn;
            if (fromOrder == null) {
                return Enumerable.Empty<(uint turnCostType, uint cost)>();
            }

            return enumerator.GetTurnCostTo(fromOrder.Value);
        }
    }
}