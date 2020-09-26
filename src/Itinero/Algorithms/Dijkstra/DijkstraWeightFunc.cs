using System.Collections.Generic;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Reading;

namespace Itinero.Algorithms.Dijkstra
{
    /// <summary>
    /// The weight function.
    /// </summary>
    /// <param name="edgeEnumerator">The edge enumerator with the current edge and associated details.</param>
    /// <param name="previousEdges">The edge ids and the turn indexes of the previous edges.</param>
    /// <remarks>
    /// Translates an edge and all previous edges (if needed) into two costs:
    /// - cost: the cost of traversing the edge.
    /// - turnCost: the cost of turning onto the edge from the previous edges.
    /// </remarks>
    internal delegate (double cost, double turnCost) DijkstraWeightFunc(GraphEdgeEnumerator<Graph> edgeEnumerator, 
        IEnumerable<(EdgeId edge, byte? turn)> previousEdges);
}