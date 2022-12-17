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
    /// <param name="forward">The forward flag, when true the cost in the direction of the current edge enumerator, otherwise against. When null returns true if can stop is true in at least one direction.</param>
    /// <param name="previousEdges">The previous edges. Should correspond with what the forward flag indicates.</param>
    /// <returns>The access flags, stop flags, cost and turn cost.</returns>
    public (bool canAccess, bool canStop, double cost, double turnCost) Get(
        IEdgeEnumerator<RoutingNetwork> edgeEnumerator,
        bool forward = true, IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null);
}
