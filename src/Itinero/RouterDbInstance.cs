using System;
using Itinero.Data.Graphs;

namespace Itinero
{
    /// <summary>
    /// A single consistent snapshot of the routing network.
    /// </summary>
    public class RouterDbInstance : IRouterDbInstanceWritable
    {
        private readonly Graph _network;
        private readonly RouterDb _routerDb;

        internal RouterDbInstance(RouterDb routerDb, int zoom = 14)
        {
            _routerDb = routerDb;
            _network = new Graph(zoom);
        }

        /// <summary>
        /// Gets the network graph.
        /// </summary>
        internal Graph Network => _network;

        /// <summary>
        /// Gets the router db.
        /// </summary>
        public RouterDb RouterDb => _routerDb; 
        
        /// <summary>
        /// Gets the edge enumerator for the graph in this network.
        /// </summary>
        /// <returns>The edge enumerator.</returns>
        public RouterDbEdgeEnumerator GetEdgeEnumerator()
        {
            return new RouterDbEdgeEnumerator(this);
        }

        /// <summary>
        /// Gets the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>The vertex.</returns>
        public (double longitude, double latitude) GetVertex(VertexId vertex)
        {
            if (!_network.TryGetVertex(vertex, out var longitude, out var latitude)) throw new ArgumentException($"{nameof(vertex)} does not exist.");
            
            return (longitude, latitude);
        }

        private RouterDbInstanceWriter? _writer;
        
        /// <summary>
        /// Returns true if there is already a writer.
        /// </summary>
        public bool HasWriter => _writer != null;
        
        /// <summary>
        /// Gets a writer.
        /// </summary>
        /// <returns>The writer.</returns>
        public RouterDbInstanceWriter GetWriter()
        {
            if (_writer != null) throw new InvalidOperationException($"Only one writer is allowed at one time." +
                                                                     $"Check {nameof(HasWriter)} to check for a current writer.");
            _writer = new RouterDbInstanceWriter(this);
            return _writer;
        }
        
        void IRouterDbInstanceWritable.ClearWriter()
        {
            _writer = null;
        }
    }
}