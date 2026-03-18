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
            // fast path: use the struct enumerator (stack-allocated) to check
            // if there are turn costs. most edges don't have turn costs, so
            // we can call the cost function with null previousEdges and avoid
            // any heap allocation.
            var pe = previousEdges.GetEnumerator();
            if (!pe.MoveNext() || !pe.Current.turn.HasValue)
            {
                var (_, _, cost, _) = costFunction.Get(enumerator, true, null);
                return (cost, 0.0);
            }

            // slow path: turn costs exist — box the struct for ICostFunction.
            // this only happens at restricted intersections.
            var (_, _, cost2, turnCost) = costFunction.Get(enumerator, true, previousEdges);
            return (cost2, turnCost);
        };
    }
}
