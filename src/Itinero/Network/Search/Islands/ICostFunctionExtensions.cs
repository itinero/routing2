using System;
using System.Collections.Generic;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;

namespace Itinero.Network.Search.Islands;

internal static class ICostFunctionExtensions
{
    /// <summary>
    /// Gets the cost of a turn from the previous edges sequence to the given edge in the enumerator in a forward direction.
    /// 
    /// This does NOT include the cost of the previous edges.
    /// </summary>
    /// <param name="costFunction">The cost function.</param>
    /// <param name="enumerator">The edge to test.</param>
    /// <param name="forward">Then true, calculate the cost including turn cost from (previousEdge ->) enumerator.tail -> enumerator.edge -> enumerator.head, when false (previousEdge ->) enumerator.head -> enumerator.edge -> enumerator.tail</param>
    /// <param name="previousEdges">A sequence of previously traversed edge, if any.</param>
    /// <returns>True if the current edge is traversable and the turn cost allows the turn.</returns>
    public static bool GetIslandBuilderCost(this ICostFunction costFunction,
        RoutingNetworkEdgeEnumerator enumerator, bool forward = true, IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        var cost = costFunction.Get(enumerator, forward, previousEdges);

        return cost is { canAccess: true, turnCost: < double.MaxValue };
    }
}
