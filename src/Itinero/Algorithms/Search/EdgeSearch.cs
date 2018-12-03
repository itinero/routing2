using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using System.Linq;

namespace Itinero.Algorithms.Search
{
    public static class EdgeSearch
    {
        /// <summary>
        /// Enumerates all edges that have at least one vertex in the given bounding box.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="box">The box to enumerate in.</param>
        /// <returns>An enumerator with all the vertices and their location.</returns>
        public static EdgeEnumerator SearchEdgesInBox(this Graph graph, (float minLon, float minLat, float maxLon, float maxLat) box)
        {
            var vertices = graph.SearchVerticesInBox(box);
            return new EdgeEnumerator(graph, vertices.Select((i) => i.vertex));
        }

        /// <summary>
        /// Returns the closest edge to the center of the given box that has at least one vertex inside the given box.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="box">The box.</param>
        /// <returns>The closest edge to the center of the box inside the given box.</returns>
        public static (uint edgeId, ushort offset) SnapInBox(this Graph graph,
            (float minLon, float minLat, float maxLon, float maxLat) box)
        {
            var edges = graph.SearchEdgesInBox(box);
            while (edges.MoveNext())
            {
                // TODO: keep the closest edge and offset here per edge.
                // based on this code: https://github.com/itinero/routing/blob/develop/src/Itinero/Algorithms/Search/Hilbert/HilbertExtensions.cs#L729
            }
        }
    }
}