using System;
using System.Collections.Generic;

namespace Itinero.Data.Graphs
{
    internal interface IMutableGraph : IDisposable
    {
        /// <summary>
        /// Adds a new vertex and returns its ID.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        VertexId AddVertex(double longitude, double latitude);

        /// <summary>
        /// Gets the vertex with the given id.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>True if the vertex exists.</returns>
        bool TryGetVertex(VertexId vertex, out double longitude, out double latitude);

        /// <summary>
        /// Adds a new edge.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="shape">The shape points.</param>
        /// <returns>The edge id.</returns>
        EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude)>? shape = null, IEnumerable<(string key, string value)>? attributes = null);
        
        /// <summary>
        /// Gets the resulting graph.
        /// </summary>
        /// <returns>The resulting graph.</returns>
        Graph ToGraph();
    }
}