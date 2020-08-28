using System;
using System.Collections.Generic;
using Itinero.Data.Graphs;
using Itinero.Profiles;

namespace Itinero
{
    /// <summary>
    /// Abstract representation of a router db that can be mutated (deletes and updates).
    ///
    /// This can be used to:
    /// - mutate the network (update or delete) data.
    /// - add new data to the network.
    ///
    /// The data can only be used for routing after the data has been fully written and this object has been disposed.
    /// </summary>
    public interface IMutableRouterDb : IDisposable
    {
        /// <summary>
        /// Gets the vertex with the given id.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>True if the vertex exists.</returns>
        bool TryGetVertex(VertexId vertex, out double longitude, out double latitude);

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        VertexId AddVertex(double longitude, double latitude);

        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="shape">The shape points.</param>
        /// <returns>The edge id.</returns>
        EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null);
        
        /// <summary>
        /// Adds a new turn cost table.
        /// </summary>
        /// <param name="vertex">The vertex the table is for.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="edges">The edges array in the order matching the costs.</param>
        /// <param name="costs">The cost matrix, dimensions matching the edges array.</param>
        /// <param name="prefix">A prefix path if any. The turn cost table will only apply to a path if the prefix is part of the path.</param>
        void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes, 
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null);

        /// <summary>
        /// Gets the profile configuration.
        /// </summary>
        internal RouterDbProfileConfiguration ProfileConfiguration { get; }
    }
}