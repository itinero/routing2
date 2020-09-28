using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Network.Enumerators.Vertices;
using Itinero.Network.Tiles;

namespace Itinero.Network.Search
{
    /// <summary>
    /// Implements vertex searches.
    /// </summary>
    internal static class VertexSearch
    {
        /// <summary>
        /// Enumerates all vertices in the given bounding box.
        /// </summary>
        /// <param name="network">The network.</param>
        /// <param name="box">The box to enumerate in.</param>
        /// <returns>An enumerator with all the vertices and their location.</returns>
        internal static IEnumerable<(VertexId vertex, (double longitude, double latitude) location)> SearchVerticesInBox(this RoutingNetwork network,
            ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
        {
            var rangeVertices = new TilesVertexEnumerator(network, box.TileRange(network.Zoom));

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