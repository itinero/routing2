using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero
{
    internal class MutableNetworkEdgeEnumerator : IMutableNetworkEdgeEnumerator
    {
        internal MutableNetworkEdgeEnumerator(MutableNetwork network)
        {
            GraphEdgeEnumerator = network.Graph.GetEdgeEnumerator();
        }

        internal GraphEdgeEnumerator GraphEdgeEnumerator { get; }
        
        /// <summary>
        /// Moves the enumerator to the first edge of the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>True if the vertex exists.</returns>
        public bool MoveTo(VertexId vertex)
        {
            return GraphEdgeEnumerator.MoveTo(vertex);
        }
        
        /// <summary>
        /// Moves the enumerator to the given edge. 
        /// </summary>
        /// <param name="edgeId">The edge id.</param>
        /// <param name="forward">The forward flag, when false the enumerator is in a state as it was enumerated to the edge via its last vertex. When true the enumerator is in a state as it was enumerated to the edge via its first vertex.</param>
        public bool MoveToEdge(EdgeId edgeId, bool forward = true)
        {
            return GraphEdgeEnumerator.MoveToEdge(edgeId, forward);
        }

        /// <summary>
        /// Moves this enumerator to the next edge.
        /// </summary>
        /// <returns>True if there is data available.</returns>
        public bool MoveNext()
        {
            return GraphEdgeEnumerator.MoveNext();
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