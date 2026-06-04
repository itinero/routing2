using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;

namespace Itinero.Routing.Flavours.Dijkstra;

/// <summary>
/// Thin wrapper that pairs an edge enumerator with a cost function. Used by
/// <see cref="SnapPointExtensions.TrySingleHop"/> to compute per-direction costs on a
/// single edge without re-positioning the enumerator twice.
/// </summary>
internal class CostEdgeEnumerator
{
    private readonly RoutingNetworkEdgeEnumerator _edgeEnumerator;
    private readonly ICostFunction _costFunction;

    internal CostEdgeEnumerator(RoutingNetworkEdgeEnumerator edgeEnumerator, ICostFunction costFunction)
    {
        _edgeEnumerator = edgeEnumerator;
        _costFunction = costFunction;
    }

    public bool MoveTo(EdgeId edgeId, bool forward = true)
    {
        return _edgeEnumerator.MoveTo(edgeId, forward);
    }

    public (double cost, double turnCost) GetCost(bool tailToHead)
    {
        var (_, _, _, cost, turnCost) = _costFunction.Get(_edgeEnumerator, tailToHead, null);
        return (cost, turnCost);
    }
}
