using System;
using Itinero.Data.Graphs;

namespace Itinero
{
    /// <summary>
    /// A single consistent snapshot of the routing network.
    /// </summary>
    public class Network : IRouterDbInstanceWritable
    {
        private readonly RouterDb _routerDb;

        internal Network(RouterDb routerDb, int zoom = 14)
        {
            _routerDb = routerDb;
            Graph = new Graph(zoom);
        }

        /// <summary>
        /// Gets the network graph.
        /// </summary>
        internal Graph Graph { get; }

        /// <summary>
        /// Gets the router db.
        /// </summary>
        public RouterDb RouterDb => _routerDb; 
        
        /// <summary>
        /// Gets the edge enumerator for the graph in this network.
        /// </summary>
        /// <returns>The edge enumerator.</returns>
        public NetworkEdgeEnumerator GetEdgeEnumerator()
        {
            return new NetworkEdgeEnumerator(this);
        }

        /// <summary>
        /// Gets the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>The vertex.</returns>
        public (double longitude, double latitude) GetVertex(VertexId vertex)
        {
            if (!Graph.TryGetVertex(vertex, out var longitude, out var latitude)) throw new ArgumentException($"{nameof(vertex)} does not exist.");
            
            return (longitude, latitude);
        }

        private NetworkWriter? _writer;
        
        /// <summary>
        /// Returns true if there is already a writer.
        /// </summary>
        public bool HasWriter => _writer != null;
        
        /// <summary>
        /// Gets a writer.
        /// </summary>
        /// <returns>The writer.</returns>
        public NetworkWriter GetWriter()
        {
            if (_writer != null) throw new InvalidOperationException($"Only one writer is allowed at one time." +
                                                                     $"Check {nameof(HasWriter)} to check for a current writer.");
            _writer = new NetworkWriter(this);
            return _writer;
        }
        
        void IRouterDbInstanceWritable.ClearWriter()
        {
            _writer = null;
        }
    }
}