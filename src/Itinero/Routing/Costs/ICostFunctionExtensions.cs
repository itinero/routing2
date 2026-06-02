using System.Collections.Generic;
using Itinero.Network;
using Itinero.Routing.Flavours.Dijkstra;

namespace Itinero.Routing.Costs;

internal static class ICostFunctionExtensions
{
    public static DijkstraWeightFunc GetDijkstraWeightFunc(this ICostFunction costFunction)
    {
        return (enumerator, previousEdges) =>
        {
            // fast path: when there are no previous edges, pass null to avoid boxing.
            if (previousEdges.IsEmpty)
            {
                var (_, _, _, cost, _) = costFunction.Get(enumerator, true, null);
                return (cost, 0.0);
            }

            // box the struct for ICostFunction.
            var (_, _, _, cost2, turnCost) = costFunction.Get(enumerator, true, previousEdges);
            return (cost2, turnCost);
        };
    }
}
