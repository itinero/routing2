using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.TurnCosts
{
    public static class RoutingNetworkEdgeEnumeratorExtensions
    {
        internal static IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostTo(
            this RoutingNetworkEdgeEnumerator enumerator,
            IEnumerable<(EdgeId edge, byte? turn)> previousEdges)
        {
            using var previousEdgesEnumerator = previousEdges.GetEnumerator();
            if (!previousEdgesEnumerator.MoveNext())
            {
                return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;
            }

            var fromOrder = previousEdgesEnumerator.Current.turn;
            if (fromOrder == null)
            {
                return ArraySegment<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)>.Empty;
            }

            return enumerator.GetTurnCostToTail(fromOrder.Value);
        }
    }
}
