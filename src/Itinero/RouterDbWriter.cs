using System;
using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero
{
    /// <summary>
    /// Writes to a router db by creating a new instance.
    ///
    /// This writer can:
    /// - mutate the network (update or delete) data.
    /// - add new data to the network.
    ///
    /// The data can only be used for routing after the data has been fully written.
    /// </summary>
    public sealed class RouterDbWriter : IDisposable
    {
        private readonly RouterDb _routerDb;
        private readonly IMutableNetwork _mutableNetwork;

        internal RouterDbWriter(RouterDb routerDb)
        {
            _routerDb = routerDb;

            // make a copy of the latest network to write to.
            var latest = routerDb.Network;
            _mutableNetwork = latest.GetAsMutable();
        }

        /// <summary>
        /// Gets the vertex with the given id.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>True if the vertex exists.</returns>
        public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
        {
            return _mutableNetwork.TryGetVertex(vertex, out longitude, out latitude);
        }

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            return _mutableNetwork.AddVertex(longitude, latitude);
        }
        
        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="shape">The shape points.</param>
        /// <returns>The edge id.</returns>
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2, IEnumerable<(double longitude, double latitude)>? shape = null, 
            IEnumerable<(string key, string value)>? attributes = null)
        {
            return _mutableNetwork.AddEdge(vertex1, vertex2, shape, attributes);
        }
        
        public void Dispose()
        {
            var routerDbWriteable = _routerDb as IRouterDbWritable;
            routerDbWriteable.SetLatest(_mutableNetwork.ToNetwork());
            _mutableNetwork.Dispose();
            routerDbWriteable.ClearWriter();
        }
    }

    internal interface IRouterDbWritable
    {
        void SetLatest(Network latest);

        void ClearWriter();
    }
}