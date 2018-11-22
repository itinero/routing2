using System.Collections.Generic;
using Itinero.Data.Graphs;
using Itinero.Data.Shapes;
using Itinero.LocalGeo;

namespace Itinero.Data
{
    public class Network
    {
        private readonly Graph _graph;
        private readonly ShapesArray _shapes;

        public Network(int zoom = 14)
        {
            _graph = new Graph();
            _shapes = new ShapesArray();
        }

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            return _graph.AddVertex(longitude, latitude);
        }

        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="data"></param>
        /// <param name="shape"></param>
        /// <returns></returns>
        public uint AddEdge(VertexId vertex1, VertexId vertex2, IReadOnlyList<byte> data = null,
            IEnumerable<Coordinate> shape = null)
        {
            // add the new edge and keep its id around.
            var edgeId = _graph.AddEdge(vertex1, vertex2, data);

            // add shape if any.
            if (shape != null)
            {
                _shapes[edgeId] = new ShapeEnumerable(shape);
            }
            
            return edgeId;
        }

        /// <summary>
        /// Gets the edge enumerator for the graph in this network.
        /// </summary>
        /// <returns>The edge enumerator.</returns>
        public Graph.EdgeEnumerator GetEdgeEnumerator()
        {
            return _graph.GetEdgeEnumerator();
        }

        /// <summary>
        /// Gets the shape for the given edge, if any.
        /// </summary>
        /// <param name="edgeId">The edge id.</param>
        /// <returns>The shape.</returns>
        public ShapeBase GetShape(uint edgeId)
        {
            return _shapes[edgeId];
        }
    }
}