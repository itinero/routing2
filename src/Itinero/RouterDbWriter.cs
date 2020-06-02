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
    public class RouterDbWriter : IDisposable
    {
        private readonly RouterDb _routerDb;

        internal RouterDbWriter(RouterDb routerDb)
        {
            _routerDb = routerDb;

            // make a copy of the latest network to write to.
            var latest = routerDb.Latest;
            
        }
        
        // TODO: implement all the writing/updating functionality and what not.
        
        /// <summary>
        /// Gets the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>The vertex.</returns>
        public (double longitude, double latitude) GetVertex(VertexId vertex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
        
        public void Dispose()
        {
            (_routerDb as IRouterDbWritable).ClearWriter();
        }
    }

    internal interface IRouterDbWritable
    {
        void SetLatest(Network latest);

        void ClearWriter();
    }
}