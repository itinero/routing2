using Itinero.Network.Enumerators.Edges;

namespace Itinero.Routing.Flavours.Dijkstra;

/// <summary>
/// The weight function.
/// </summary>
/// <param name="edgeEnumerator">The edge enumerator with the current edge and associated details.</param>
/// <param name="previousEdges">A lazy enumerable over previous edges (struct, no allocation until iterated).</param>
/// <remarks>
/// Translates an edge and all previous edges (if needed) into:
/// - cost: the cost of traversing the edge.
/// - turnCost: the cost of turning onto the edge from the previous edges.
/// - localAccess: whether the edge is L-tagged (e.g. <c>access=destination</c>). Direction-
///   independent, a property of the way itself. Consumed by access-aware variants of the
///   edge-based Dijkstra to gate the leftMain state-bit transitions.
/// </remarks>
internal delegate (double cost, double turnCost, bool localAccess) DijkstraWeightFunc(RoutingNetworkEdgeEnumerator edgeEnumerator,
    PreviousEdgeEnumerable previousEdges);
