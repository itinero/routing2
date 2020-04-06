using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Itinero.Data;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Coders;

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
            this.EdgeDataLayout = configuration.EdgeDataLayout ?? new EdgeDataLayout();

            _network = new Graph(configuration.Zoom);
        }

        private RouterDb(EdgeDataLayout edgeDataLayout, Graph network)
        {
            this.EdgeDataLayout = edgeDataLayout;
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
        /// Gets the number of edges.
        /// </summary>
        public uint EdgeCount => 0;

        /// <summary>
        /// Gets the zoom.
        /// </summary>
        public int Zoom => _network.Zoom;

        /// <summary>
        /// Gets the network graph.
        /// </summary>
        internal Graph Network => _network;

        /// <summary>
        /// Gets the data layout.
        /// </summary>
        internal EdgeDataLayout EdgeDataLayout { get; }

        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="shape">The shape points.</param>
        /// <returns>The edge id.</returns>
        public (EdgeId edge1, EdgeId edge2) AddEdge(VertexId vertex1, VertexId vertex2, IEnumerable<(double longitude, double latitude)> shape = null, 
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

        /// <summary>
        /// Writes to the given stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The number of bytes written.</returns>
        public long WriteTo(Stream stream)
        {
            var p = stream.Position;
            
            // write header and version.
            stream.WriteWithSize($"{nameof(RouterDb)}");
            stream.WriteByte(1);

            this.EdgeDataLayout.WriteTo(stream);
            _network.WriteTo(stream);

            return stream.Position - p;
        }

        /// <summary>
        /// Reads from the given stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The router db.</returns>
        /// <exception cref="InvalidDataException"></exception>
        public static RouterDb ReadFrom(Stream stream)
        {
            // read & verify header.
            var header = stream.ReadWithSizeString();
            var version = stream.ReadByte();
            if (header != nameof(RouterDb)) throw new InvalidDataException($"Cannot read {nameof(RouterDb)}: Header invalid.");
            if (version != 1) throw new InvalidDataException($"Cannot read {nameof(RouterDb)}: Version # invalid.");

            var edgeDataLayout = Data.Graphs.Coders.EdgeDataLayout.ReadFrom(stream);
            var graph = Graph.ReadFrom(stream);
            
            var db = new RouterDb(edgeDataLayout, graph);
            return db;
        }
    }
}