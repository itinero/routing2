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
    /// <param name="costFunction"></param>
    /// <param name="enumerator"></param>
    /// <param name="previousEdges"></param>
    /// <returns></returns>
    public static bool GetIslandBuilderCost(this ICostFunction costFunction,
        RoutingNetworkEdgeEnumerator enumerator, IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        var cost = costFunction.Get(enumerator, true, previousEdges);

        return cost is { canAccess: true, turnCost: < double.MaxValue };
    }
}
