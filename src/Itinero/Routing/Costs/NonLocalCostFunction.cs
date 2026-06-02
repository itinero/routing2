using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Routing.Costs;

/// <summary>
/// Decorator that masks off local-access edges. Wraps an inner cost function and
/// reports <c>canAccess = false</c> (and cost 0, turn cost <see cref="double.MaxValue"/>)
/// for any edge the inner cost function flagged as <c>localAccess = true</c>.
///
/// Used by the N-only island classification (<c>IslandKind.NonLocal</c>): feeding this
/// into <see cref="Itinero.Network.Search.Islands.IslandClassifier"/> produces a
/// reachability classification over the subgraph that excludes <c>access=destination</c>
/// edges, i.e. the "main routable network" used by the access-aware routing rule.
/// </summary>
internal sealed class NonLocalCostFunction : ICostFunction
{
    private readonly ICostFunction _inner;

    public NonLocalCostFunction(ICostFunction inner)
    {
        _inner = inner;
    }

    public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
        IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool tailToHead = true,
        IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        var inner = _inner.Get(edgeEnumerator, tailToHead, previousEdges);
        if (inner.localAccess)
        {
            // Mask off: the island classifier checks `canAccess && turnCost < MaxValue`.
            // Returning either failing condition is enough; we set both for clarity.
            return (false, false, true, 0.0, double.MaxValue);
        }
        return inner;
    }
}
