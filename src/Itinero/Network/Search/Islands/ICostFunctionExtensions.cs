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

    /// <summary>
    /// Gets the cost of traversing through a shared vertex from <paramref name="from"/> to
    /// <paramref name="to"/>, including the turn cost at the shared vertex.
    ///
    /// Both enumerators must be pre-positioned in their respective traversal directions
    /// such that the shared vertex is at <c>from.Head</c> and at <c>to.Tail</c>.
    /// </summary>
    /// <returns>True if <paramref name="from"/> is traversable in its direction,
    /// <paramref name="to"/> is traversable in its direction, AND the turn from
    /// <paramref name="from"/> through the shared vertex into <paramref name="to"/> is
    /// allowed (turn cost &lt; <see cref="double.MaxValue"/>).</returns>
    public static bool GetIslandBuilderCost(this ICostFunction costFunction,
        RoutingNetworkEdgeEnumerator from,
        RoutingNetworkEdgeEnumerator to)
    {
        // from-edge must be traversable in its current direction at all.
        // without this check a one-way edge whose only allowed direction goes the
        // OPPOSITE way of the requested traversal would still appear to enable a
        // turn into the to-edge.
        var fromCost = costFunction.Get(from, tailToHead: true, null);
        if (!fromCost.canAccess) return false;

        var fromOrder = from.HeadOrder;
        var previousEdges = fromOrder.HasValue
            ? new (EdgeId edgeId, byte? turn)[] { (from.EdgeId, fromOrder) }
            : null;

        var cost = costFunction.Get(to, tailToHead: true, previousEdges);
        return cost is { canAccess: true, turnCost: < double.MaxValue };
    }
}
