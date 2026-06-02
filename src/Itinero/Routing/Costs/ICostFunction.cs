using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

[assembly: InternalsVisibleTo("Itinero.Tests")]
namespace Itinero.Routing.Costs;

/// <summary>
/// Abstract definition of a cost function.
/// </summary>
public interface ICostFunction
{
    /// <summary>
    /// Gets the costs associated with the given network edge.
    /// </summary>
    /// <param name="edgeEnumerator">The edge enumerator.</param>
    /// <param name="tailToHead">The tail-to-head flag, when true the cost is returned in the direction of the current edge enumerator, otherwise against.</param>
    /// <param name="previousEdges">The previous edges. Should correspond with what the tail-to-head flag indicates.</param>
    /// <returns>
    /// Access flag, stop flag, local-access flag, cost, and turn cost.
    /// <c>localAccess</c> is <c>true</c> when the edge is tagged <c>access=destination</c>
    /// (or mode-specific equivalent) for this profile — direction-independent, a
    /// property of the way itself. Local-access edges are legitimate only when
    /// origin or destination is on or beyond them; the routing engine forbids them
    /// as through-traffic.
    /// </returns>
    (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
        IEdgeEnumerator<RoutingNetwork> edgeEnumerator,
        bool tailToHead = true, IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null);
}
