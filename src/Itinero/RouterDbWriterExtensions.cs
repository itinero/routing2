using System;
using Itinero.Data.Graphs;

namespace Itinero
{
    /// <summary>
    /// Contains extensions for the router db writer.
    /// </summary>
    public static class RouterDbWriterExtensions
    {
        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="routerDbWriter">The router db writer.</param>
        /// <param name="location">The location.</param>
        /// <returns>The ID of the new vertex.</returns>
        public static VertexId AddVertex(this RouterDbWriter routerDbWriter, (double longitude, double latitude) location)
        {
            return routerDbWriter.AddVertex(location.longitude, location.latitude);
        }

        /// <summary>
        /// Gets the vertex.
        /// </summary>
        /// <param name="routerDbWriter">The router db writer.</param>
        /// <param name="vertexId">The ID of the vertex.</param>
        /// <returns>The location of the ID.</returns>
        public static (double longitude, double latitude) GetVertex(this RouterDbWriter routerDbWriter,
            VertexId vertexId)
        {
            if (!routerDbWriter.TryGetVertex(vertexId, out var longitude, out var latitude)) throw new ArgumentException("Vertex not found!", nameof(vertexId));

            return (longitude, latitude);
        }
    }
}