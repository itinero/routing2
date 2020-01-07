using System;
using System.Collections.Generic;
using Itinero.Algorithms.Search;
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
        private readonly EdgeEnumerator _edgeEnumerator;

        internal RouterDbEdgeEnumerator(RouterDb routerDb)
        {
            _routerDb = routerDb ?? throw new ArgumentNullException(nameof(routerDb));

            _enumerator = _routerDb.Network.GetEnumerator();
        }

        internal RouterDbEdgeEnumerator(RouterDb routerDb, EdgeEnumerator edgeEnumerator)
        {
            _routerDb = routerDb ?? throw new ArgumentNullException(nameof(routerDb));
            _edgeEnumerator = edgeEnumerator ?? throw new ArgumentNullException(nameof(edgeEnumerator));;
        }

        // TODO: do we create a readonly version of this, if a reader moves this enumerator things go crazy.
        internal Graph.Enumerator Enumerator
        {
            get
            {
                if (_edgeEnumerator != null) return _edgeEnumerator.GraphEnumerator;
                return _enumerator;
            }
        }

        internal RouterDb RouterDb => _routerDb;
        
        /// <summary>
        /// Moves the enumerator to the first edge of the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>True if the vertex exists.</returns>
        public bool MoveTo(VertexId vertex)
        {
            if (_enumerator == null) throw new InvalidOperationException(
                $"Cannot reset an enumerator created from an {nameof(EdgeEnumerator)}.");
            return _enumerator.MoveTo(vertex);
        }
        
        /// <summary>
        /// Moves the enumerator to the given edge. 
        /// </summary>
        /// <param name="edgeId">The edge id.</param>
        /// <param name="forward">The forward flag, when false the enumerator is in a state as it was enumerated to the edge via its last vertex. When true the enumerator is in a state as it was enumerated to the edge via its first vertex.</param>
        public bool MoveToEdge(EdgeId edgeId, bool forward = true)
        {
            if (_enumerator == null) throw new InvalidOperationException(
                $"Cannot reset an enumerator created from an {nameof(EdgeEnumerator)}.");
            return _enumerator.MoveToEdge(edgeId, forward);
        }

        /// <summary>
        /// Moves this enumerator to the next edge.
        /// </summary>
        /// <returns>True if there is data available.</returns>
        public bool MoveNext()
        {
            if (_edgeEnumerator != null)
            {
                return _edgeEnumerator.MoveNext();
            }
            return _enumerator.MoveNext();
        }

        /// <summary>
        /// Returns true if the edge is from -> to, false otherwise.
        /// </summary>
        public bool Forward => this.Enumerator.Forward;

        /// <summary>
        /// Gets the source vertex.
        /// </summary>
        public VertexId From => this.Enumerator.From;

        /// <summary>
        /// Gets the target vertex.
        /// </summary>
        public VertexId To => this.Enumerator.To;

        /// <summary>
        /// Gets the edge id.
        /// </summary>
        public EdgeId Id => this.Enumerator.Id;

        /// <summary>
        /// Gets the shape, if any.
        /// </summary>
        /// <returns>The shape.</returns>
        public ShapeBase GetShape() => this.Enumerator.GetShape();

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <returns></returns>
        public IAttributeCollection GetAttributes() => _routerDb.GetAttributes(this.Enumerator.Id);
    }
    
    /// <summary>
    /// Contains extension methods for the router db edge enumerator.
    /// </summary>
    public static class RouterDbEdgeEnumeratorExtensions
    {
        /// <summary>
        /// Gets the location of the to vertex.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <returns>The location.</returns>
        public static Coordinate ToLocation(this RouterDbEdgeEnumerator enumerator)
        {
            return enumerator.RouterDb.GetVertex(enumerator.To);
        }
        
        /// <summary>
        /// Gets the location of the from vertex.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <returns>The location.</returns>
        public static Coordinate FromLocation(this RouterDbEdgeEnumerator enumerator)
        {
            return enumerator.RouterDb.GetVertex(enumerator.From);
        }
        
        /// <summary>
        /// Gets the complete shape, including start end end vertices.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <returns>The complete shape.</returns>
        public static IEnumerable<Coordinate> GetCompleteShape(this RouterDbEdgeEnumerator enumerator)
        {
            yield return enumerator.FromLocation();

            var shape = enumerator.GetShape();
            if (shape != null)
            {
                foreach (var s in shape)
                {
                    yield return s;
                }
            }

            yield return enumerator.ToLocation();
        }
    }
}