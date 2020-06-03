using System;
using System.Collections.Generic;
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

        private Network(RouterDb routerDb, Graph graph)
        {
            _routerDb = routerDb;
            Graph = graph;
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
        
        /// <summary>
        /// Gets the attributes for the given edge type.
        /// </summary>
        /// <param name="edgeTypeId">The edge type id.</param>
        /// <returns>The attributes for the given edge type.</returns>
        public IEnumerable<(string key, string value)> GetEdgeType(uint edgeTypeId)
        {
            return this.Graph.GetEdgeType(edgeTypeId);
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

        private IMutableNetwork? _mutableGraph = null;

        internal IMutableNetwork GetAsMutable()
        {
            if (_mutableGraph != null) throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");
            
            _mutableGraph = new MutableNetwork(this);
            return _mutableGraph;
        }

        internal void ClearMutable()
        {
            _mutableGraph = null;
        }
        
        internal class MutableNetwork : IMutableNetwork
        {
            private readonly Network _network;
            private readonly IMutableGraph _graph;
            
            public MutableNetwork(Network network)
            {
                _network = network;

                _graph = network.Graph.GetAsMutable();
            }

            public VertexId AddVertex(double longitude, double latitude)
            {
                return _graph.AddVertex(longitude, latitude);
            }

            public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
            {
                return _graph.TryGetVertex(vertex, out longitude, out latitude);
            }

            public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
                IEnumerable<(double longitude, double latitude)>? shape = null,
                IEnumerable<(string key, string value)>? attributes = null)
            {
                return _graph.AddEdge(vertex1, vertex2, shape, attributes);
            }

            public Network ToNetwork()
            {
                return new Network(_network._routerDb, _graph.ToGraph());
            }

            public void Dispose()
            {
                _graph.Dispose();
                
                _network.ClearMutable();
            }
        }
    }

    /// <summary>
    /// A mutable version of the network.
    /// </summary>
    internal interface IMutableNetwork : IDisposable
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
        /// Gets the resulting network.
        /// </summary>
        /// <returns>The resulting network.</returns>
        Network ToNetwork();
    }
}