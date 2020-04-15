using System.Collections.Generic;
using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Itinero.Geo;

namespace Itinero.Algorithms.Search
{
    /// <summary>
    /// Implements vertex searches.
    /// </summary>
    internal static class VertexSearch
    {
        /// <summary>
        /// Enumerates all vertices in the given bounding box.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="box">The box to enumerate in.</param>
        /// <returns>An enumerator with all the vertices and their location.</returns>
        internal static IEnumerable<(VertexId vertex, (double longitude, double latitude) location)> SearchVerticesInBox(this Graph graph,
            ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
        {
            var range = new TileRange(box, graph.Zoom);
            var rangeVertices = new TileRangeVertexEnumerator(range, graph);

            while (rangeVertices.MoveNext())
            {
                var location = rangeVertices.Location;
                if (!box.Overlaps(location))
                {
                    continue;
                }
                yield return (rangeVertices.Current, location);
            }
        }
    }
}