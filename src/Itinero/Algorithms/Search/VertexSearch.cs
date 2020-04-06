using System.Collections.Generic;
using Itinero.Data.Graphs;
using Itinero.Data.Tiles;

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
            (double minLon, double minLat, double maxLon, double maxLat) box)
        {
            var range = new TileRange(box, graph.Zoom);
            var rangeVertices = new TileRangeVertexEnumerator(range, graph);

            while (rangeVertices.MoveNext())
            {
                var location = rangeVertices.Location;
                if (box.minLat > location.latitude ||
                    box.minLon > location.longitude ||
                    box.maxLat < location.latitude ||
                    box.maxLon < location.longitude)
                {
                    continue;
                }
                yield return (rangeVertices.Current, location);
            }
        }
    }
}