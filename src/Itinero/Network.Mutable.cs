using System;
using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero
{
    public sealed partial class Network
    {
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

            void IMutableNetwork.SetEdgeTypeFunc(Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func)
            {
                _graph.SetEdgeTypeFunc(_graph.EdgeTypeFunc.NextVersion(func));
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

        internal void SetEdgeTypeFunc(
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> func);

        /// <summary>
        /// Gets the resulting network.
        /// </summary>
        /// <returns>The resulting network.</returns>
        Network ToNetwork();
    }
}