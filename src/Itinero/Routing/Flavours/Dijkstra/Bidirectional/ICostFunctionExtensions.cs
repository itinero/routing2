using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;

namespace Itinero.Routing.Flavours.Dijkstra.Bidirectional;

internal static class ICostFunctionExtensions
{
    public static (double cost, double turnCost) GetCost(this ICostFunction costFunction,
        RoutingNetworkEdgeEnumerator edgeEnumerator, bool tailToHead, PreviousEdgeEnumerable previousEdges)
    {
        // fast path: when there are no previous edges at all, pass null to avoid boxing.
        if (previousEdges.IsEmpty)
        {
            var (_, _, c, _) = costFunction.Get(edgeEnumerator, tailToHead, null);
            return (c, 0.0);
        }

        // box the struct for ICostFunction.
        var (_, _, cost, turnCost) = costFunction.Get(edgeEnumerator, tailToHead, previousEdges);
        return (cost, turnCost);
    }

    public static (double cost, double turnCost) GetCost(this ICostFunction costFunction,
        RoutingNetworkEdgeEnumerator edgeEnumerator, bool tailToHead, IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        var (_, _, cost, turnCost) = costFunction.Get(edgeEnumerator, tailToHead, previousEdges);
        return (cost, turnCost);
    }

    public static (double cost, double turnCost) MoveToAndGetCost(this ICostFunction costFunction,
        RoutingNetworkEdgeEnumerator edgeEnumerator, EdgeId edgeId, bool forward, bool tailToHead, IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        var (_, _, cost, turnCost) = costFunction.Get(edgeEnumerator, tailToHead, previousEdges);
        return (cost, turnCost);
    }
}
