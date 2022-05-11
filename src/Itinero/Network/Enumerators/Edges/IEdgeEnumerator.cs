using System.Collections.Generic;

namespace Itinero.Network.Enumerators.Edges
{
    /// <summary>
    /// An edge enumerator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEdgeEnumerator<out T>
    {
        /// <summary>
        /// Resets this enumerator.
        /// </summary>
        void Reset();

        /// <summary>
        /// Moves this enumerator to the next edge.
        /// </summary>
        /// <returns>True if there is data available.</returns>
        bool MoveNext();

        /// <summary>
        /// Returns the network.
        /// </summary>
        T Network { get; }

        /// <summary>
        /// Returns true if the edge is from -> to, false otherwise.
        /// </summary>
        bool Forward { get; }

        (double longitude, double latitude, float? e) FromLocation { get; }

        /// <summary>
        /// Gets the source vertex.
        /// </summary>
        VertexId From { get; }

        (double longitude, double latitude, float? e) ToLocation { get; }

        /// <summary>
        /// Gets the target vertex.
        /// </summary>
        VertexId To { get; }

        /// <summary>
        /// Gets the edge id.
        /// </summary>
        EdgeId Id { get; }

        /// <summary>
        /// Gets the shape.
        /// </summary>
        /// <returns>The shape.</returns>
        IEnumerable<(double longitude, double latitude, float? e)> Shape { get; }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        /// <returns>The attributes.</returns>
        IEnumerable<(string key, string value)> Attributes { get; }

        /// <summary>
        /// Gets the edge profile id.
        /// </summary>
        uint? EdgeTypeId { get; }

        /// <summary>
        /// Gets the length in centimeters, if any.
        /// </summary>
        uint? Length { get; }

        /// <summary>
        /// Gets the head index.
        /// </summary>
        byte? HeadOrder { get; }

        /// <summary>
        /// Gets the tail index.
        /// </summary>
        byte? TailOrder { get; }

        /// <summary>
        /// Gets the turn cost at the tail turn (source -> [tail -> head]).
        /// </summary>
        /// <param name="sourceOrder">The order of the source edge.</param>
        /// <returns>The turn costs if any.</returns>
        IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToTail(
            byte sourceOrder);

        /// <summary>
        /// Gets the turn cost at the tail turn ([head -> tail] -> target).
        /// </summary>
        /// <param name="targetOrder">The order of the target edge.</param>
        /// <returns>The turn costs if any.</returns>
        IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromTail(
            byte targetOrder);

        /// <summary>
        /// Gets the turn cost at the tail turn (source -> [head -> tail]).
        /// </summary>
        /// <param name="sourceOrder">The order of the source edge.</param>
        /// <returns>The turn costs if any.</returns>
        IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToHead(
            byte sourceOrder);

        /// <summary>
        /// Gets the turn cost at the tail turn ([tail -> head] -> target).
        /// </summary>
        /// <param name="targetOrder">The order of the target edge.</param>
        /// <returns>The turn costs if any.</returns>
        IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromHead(
            byte targetOrder);
    }
}