using Itinero.Data.Graphs;

namespace Itinero
{
    public static class NetworkWriterExtensions
    {
        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="location">The location.</param>
        /// <returns>The ID of the new vertex.</returns>
        public static VertexId AddVertex(this Network.NetworkWriter routerDb, (double longitude, double latitude) location)
        {
            return routerDb.AddVertex(location.longitude, location.latitude);
        }
    }
}