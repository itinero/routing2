using Itinero.Data.Attributes;
using Itinero.Data.Graphs;
using Itinero.Data.Shapes;
using Itinero.LocalGeo;

namespace Itinero
{
    /// <summary>
    /// An edge enumerator for the router db.
    /// </summary>
    public class RouterDbEdgeEnumerator
    {
        private readonly RouterDb _routerDb;
        private readonly Graph.Enumerator _enumerator;

        internal RouterDbEdgeEnumerator(RouterDb routerDb)
        {
            _routerDb = routerDb;

            _enumerator = _routerDb.Network.GetEnumerator();
        }

        internal Graph.Enumerator Enumerator => _enumerator;
        
        /// <summary>
        /// Moves the enumerator to the first edge of the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>True if the vertex exists.</returns>
        public bool MoveTo(VertexId vertex)
        {
            return _enumerator.MoveTo(vertex);
        }
        
        /// <summary>
        /// Moves the enumerator to the given edge. 
        /// </summary>
        /// <param name="edgeId">The edge id.</param>
        /// <param name="forward">The forward flag, when false the enumerator is in a state as it was enumerated to the edge via its last vertex. When true the enumerator is in a state as it was enumerated to the edge via its first vertex.</param>
        public bool MoveToEdge(uint edgeId, bool forward = true)
        {
            return _enumerator.MoveToEdge(edgeId, forward);
        }

        /// <summary>
        /// Moves this enumerator to the next edge.
        /// </summary>
        /// <returns>True if there is data available.</returns>
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <summary>
        /// Returns true if the edge is from -> to, false otherwise.
        /// </summary>
        public bool Forward => _enumerator.Forward;

        /// <summary>
        /// Gets the source vertex.
        /// </summary>
        public VertexId From => _enumerator.From;

        /// <summary>
        /// Gets the from location.
        /// </summary>
        public Coordinate FromLocation => _routerDb.GetVertex(this.From);

        /// <summary>
        /// Gets the target vertex.
        /// </summary>
        public VertexId To => _enumerator.To;

        /// <summary>
        /// Gets the to location.
        /// </summary>
        public Coordinate ToLocation => _routerDb.GetVertex(this.To);

        /// <summary>
        /// Gets the edge id.
        /// </summary>
        public uint Id => _enumerator.Id;

        /// <summary>
        /// Gets the shape, if any.
        /// </summary>
        /// <returns>The shape.</returns>
        public ShapeBase GetShape() => _enumerator.GetShape();

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <returns></returns>
        public IAttributeCollection GetAttributes() => _routerDb.GetAttributes(_enumerator.Id);
    }
}