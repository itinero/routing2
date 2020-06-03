using System;
using System.Collections.Generic;
using Itinero.Algorithms.Search;
using Itinero.Data.Graphs;

namespace Itinero
{
    /// <summary>
    /// An edge enumerator for the router db.
    /// </summary>
    public class NetworkEdgeEnumerator
    {
        private readonly Network _routerDb;
        private readonly Graph.Enumerator? _enumerator;
        private readonly EdgeEnumerator? _edgeEnumerator;

        internal NetworkEdgeEnumerator(Network routerDb)
        {
            _routerDb = routerDb ?? throw new ArgumentNullException(nameof(routerDb));

            _enumerator = _routerDb.Graph.GetEnumerator();
        }

        internal NetworkEdgeEnumerator(Network routerDb, EdgeEnumerator edgeEnumerator)
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
                if (_enumerator == null) throw new InvalidOperationException("Enumerator in an impossible state!");
                return _enumerator;
            }
        }

        internal Network RouterDb => _routerDb;
        
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
            if (_enumerator == null) throw new InvalidOperationException("Enumerator in an impossible state!");
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
        /// Gets the shape.
        /// </summary>
        /// <returns>The shape.</returns>
        public IEnumerable<(double longitude, double latitude)> Shape => this.Enumerator.Shape; 
            
        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <returns>The attributes.</returns>
        public IEnumerable<(string key, string value)> Attributes => this.Enumerator.Attributes; 

        /// <summary>
        /// Gets the edge profile id.
        /// </summary>
        public uint? EdgeTypeId => this.Enumerator.EdgeTypeId;
    }
}