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
        private readonly Graph.GraphEdgeEnumerator? _enumerator;
        private readonly EdgeEnumerator? _edgeEnumerator;

        internal NetworkEdgeEnumerator(Network routerDb)
        {
            _routerDb = routerDb ?? throw new ArgumentNullException(nameof(routerDb));

            _enumerator = _routerDb.Graph.GetEdgeEnumerator();
        }

        internal NetworkEdgeEnumerator(Network routerDb, EdgeEnumerator edgeEnumerator)
        {
            _routerDb = routerDb ?? throw new ArgumentNullException(nameof(routerDb));
            _edgeEnumerator = edgeEnumerator ?? throw new ArgumentNullException(nameof(edgeEnumerator));;
        }

        // TODO: do we create a readonly version of this, if a reader moves this enumerator things go crazy.
        internal Graph.GraphEdgeEnumerator GraphEdgeEnumerator
        {
            get
            {
                if (_edgeEnumerator != null) return _edgeEnumerator.GraphGraphEdgeEnumerator;
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
        public bool Forward => this.GraphEdgeEnumerator.Forward;

        /// <summary>
        /// Gets the source vertex.
        /// </summary>
        public VertexId From => this.GraphEdgeEnumerator.From;

        /// <summary>
        /// Gets the target vertex.
        /// </summary>
        public VertexId To => this.GraphEdgeEnumerator.To;

        /// <summary>
        /// Gets the edge id.
        /// </summary>
        public EdgeId Id => this.GraphEdgeEnumerator.Id;
            
        /// <summary>
        /// Gets the shape.
        /// </summary>
        /// <returns>The shape.</returns>
        public IEnumerable<(double longitude, double latitude)> Shape => this.GraphEdgeEnumerator.Shape; 
            
        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <returns>The attributes.</returns>
        public IEnumerable<(string key, string value)> Attributes => this.GraphEdgeEnumerator.Attributes; 

        /// <summary>
        /// Gets the edge profile id.
        /// </summary>
        public uint? EdgeTypeId => this.GraphEdgeEnumerator.EdgeTypeId;
        
        /// <summary>
        /// Gets the length in centimeters, if any.
        /// </summary>
        public uint? Length => this.GraphEdgeEnumerator.Length;

        /// <summary>
        /// Gets the head index.
        /// </summary>
        public byte? Head => this.GraphEdgeEnumerator.Head;

        /// <summary>
        /// Gets the tail index.
        /// </summary>
        public byte? Tail => this.GraphEdgeEnumerator.Tail;

        /// <summary>
        /// Gets the turn cost to the current edge given the from order.
        /// </summary>
        /// <param name="fromOrder">The order of the source edge.</param>
        /// <returns>The turn cost if any.</returns>
        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo(byte fromOrder) =>
            this.GraphEdgeEnumerator.GetTurnCostTo(fromOrder);

        /// <summary>
        /// Gets the turn cost from the current edge given the to order.
        /// </summary>
        /// <param name="toOrder">The order of the target edge.</param>
        /// <returns>The turn cost if any.</returns>
        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostFrom(byte toOrder) =>
            this.GraphEdgeEnumerator.GetTurnCostFrom(toOrder);
    }
}