using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Data;
using Itinero.Data.Attributes;
using Itinero.Data.Graphs;
using Itinero.Data.Providers;
using Itinero.Data.Shapes;
using Itinero.LocalGeo;

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
        private readonly Network _network;
        private readonly MappedAttributesIndex _edgesMeta;

        public RouterDb(int zoom = 14)
        {
            _network = new Network(zoom);
            _edgesMeta = new MappedAttributesIndex();
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
        public Coordinate GetVertex(VertexId vertex)
        {
            return _network.GetVertex(vertex);
        }

        /// <summary>
        /// Gets the number of edges.
        /// </summary>
        public uint EdgeCount => _network.EdgeCount;

        /// <summary>
        /// Gets the zoom.
        /// </summary>
        public int Zoom => _network.Zoom;

        /// <summary>
        /// Gets the network.
        /// </summary>
        internal Network Network => _network;
        
        /// <summary>
        /// Gets or sets the data provider.
        /// </summary>
        public ILiveDataProvider DataProvider { get; set; }

        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="attributes">The attributes associated with this edge.</param>
        /// <param name="shape">The shape points.</param>
        /// <returns>The edge id.</returns>
        public uint AddEdge(VertexId vertex1, VertexId vertex2, IEnumerable<Attribute> attributes = null,
            IEnumerable<Coordinate> shape = null)
        {
            var edgeId = _network.AddEdge(vertex1, vertex2, shape: shape);

            _edgesMeta[edgeId] = new AttributeCollection(attributes);
            
            return edgeId;
        }

        /// <summary>
        /// Gets the edge enumerator for the graph in this network.
        /// </summary>
        /// <returns>The edge enumerator.</returns>
        public Graph.Enumerator GetEdgeEnumerator()
        {
            return _network.GetEdgeEnumerator();
        }

        /// <summary>
        /// Gets the shape for the given edge, if any.
        /// </summary>
        /// <param name="edgeId">The edge id.</param>
        /// <returns>The shape.</returns>
        public ShapeBase GetShape(uint edgeId)
        {
            return _network.GetShape(edgeId);
        }

        /// <summary>
        /// Gets the attributes for the given edge, if any.
        /// </summary>
        /// <param name="edgeId">The edge id.</param>
        /// <returns>The attributes.</returns>
        public IAttributeCollection GetAttributes(uint edgeId)
        {
            return _edgesMeta[edgeId];
        }
    }
}