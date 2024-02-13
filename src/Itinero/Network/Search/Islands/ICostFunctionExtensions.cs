using System;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;

namespace Itinero.Network.Search.Islands;

internal static class ICostFunctionExtensions
{
    public static Func<RoutingNetworkEdgeEnumerator, (bool forward, bool backward)> GetIslandBuilderWeightFunc(
        this ICostFunction costFunction)
    {
        return (enumerator) =>
        {
            var (forward, _, _, _) = costFunction.Get(enumerator, true, ArraySegment<(EdgeId edgeId, byte? turn)>.Empty);
            var (backward, _, _, _) = costFunction.Get(enumerator, false, ArraySegment<(EdgeId edgeId, byte? turn)>.Empty);

            return (forward, backward);
        };
    }
}
