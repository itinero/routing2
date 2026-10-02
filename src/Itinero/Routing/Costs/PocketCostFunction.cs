using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Flavours.Dijkstra;

namespace Itinero.Routing.Costs;

/// <summary>
/// Reports the edges of a local pocket as local-access, whatever their tags say.
/// </summary>
/// <remarks>
/// An untagged segment enclosed by access=destination edges behaves as part of the pocket, so the
/// L to L, L to N, N to N rule carries a half through it instead of stopping at it.
/// </remarks>
internal sealed class PocketCostFunction : ICostFunction
{
    private readonly ICostFunction _inner;
    private readonly HashSet<EdgeId> _members;

    public PocketCostFunction(ICostFunction inner, HashSet<EdgeId> members)
    {
        _inner = inner;
        _members = members;
    }

    public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
        IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool tailToHead = true,
        PreviousEdgeEnumerable previousEdges = default)
    {
        var inner = _inner.Get(edgeEnumerator, tailToHead, previousEdges);
        if (inner.localAccess || !_members.Contains(edgeEnumerator.EdgeId)) return inner;

        return (inner.canAccess, inner.canStop, true, inner.cost, inner.turnCost);
    }
}
