using Itinero.Network.Enumerators.Edges;

namespace Itinero.Routing.Flavours.Dijkstra;

/// <summary>
/// The weight function.
/// </summary>
/// <param name="edgeEnumerator">The edge enumerator with the current edge and associated details.</param>
/// <param name="previousEdges">A lazy enumerable over previous edges (struct, no allocation until iterated).</param>
/// <remarks>
/// Translates an edge and all previous edges (if needed) into two costs:
/// - cost: the cost of traversing the edge.
/// - turnCost: the cost of turning onto the edge from the previous edges.
/// </remarks>
internal delegate (double cost, double turnCost) DijkstraWeightFunc(RoutingNetworkEdgeEnumerator edgeEnumerator,
    PreviousEdgeEnumerable previousEdges);
