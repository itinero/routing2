using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;

namespace Itinero.Routing.Flavours.Dijkstra.Bidirectional;

internal static class ICostFunctionExtensions
{
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
