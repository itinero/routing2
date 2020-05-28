using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Data.Events;
using Itinero.Data.Graphs;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]
namespace Itinero
{
    /// <summary>
    /// Represents a router db.
    /// </summary>
    public class RouterDb
    {
        private readonly Graph _network;

        /// <summary>
        /// Creates a new router db.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public RouterDb(RouterDbConfiguration configuration = null)
        {
            configuration ??= RouterDbConfiguration.Default;

            _network = new Graph(configuration.Zoom);
        }

        private RouterDb(Graph network)
        {
            _network = network;
        }

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            return _network.AddVertex(longitude, latitude);
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
        
        /// <summary>
        /// Gets the usage notifier.
        /// </summary>
        public DataUseNotifier UsageNotifier { get; } = new DataUseNotifier();

        /// <summary>
        /// Gets the network graph.
        /// </summary>
        internal Graph Network => _network;

        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="shape">The shape points.</param>
        /// <returns>The edge id.</returns>
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2, IEnumerable<(double longitude, double latitude)> shape = null, 
            IEnumerable<(string key, string value)> attributes = null)
        {
            return _network.AddEdge(vertex1, vertex2, shape, attributes);
        }

        /// <summary>
        /// Gets the edge enumerator for the graph in this network.
        /// </summary>
        /// <returns>The edge enumerator.</returns>
        public RouterDbEdgeEnumerator GetEdgeEnumerator()
        {
            return new RouterDbEdgeEnumerator(this);
        }
    }
}