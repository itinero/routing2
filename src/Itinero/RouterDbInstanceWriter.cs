using System;
using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero
{
    /// <summary>
    /// A writer to write to an instance. This writer will never change existing data, only add new data.
    /// </summary>
    public class RouterDbInstanceWriter : IDisposable
    {
        private readonly RouterDbInstance _routerDbInstance;

        internal RouterDbInstanceWriter(RouterDbInstance routerDbInstance)
        {
            _routerDbInstance = routerDbInstance;
        }

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            return _routerDbInstance.Network.AddVertex(longitude, latitude);
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
            return _routerDbInstance.Network.AddEdge(vertex1, vertex2, shape, attributes);
        }

        public void Dispose()
        {
            (_routerDbInstance as IRouterDbInstanceWritable).ClearWriter();
        }
    }

    internal interface IRouterDbInstanceWritable
    {
        void ClearWriter();
    }
}