using System.Collections.Generic;

namespace Itinero.Network.Enumerators.Edges
{
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
        byte? Head { get; }

        /// <summary>
        /// Gets the tail index.
        /// </summary>
        byte? Tail { get; }

        /// <summary>
        /// Gets the turn cost to the current edge given the from order.
        /// </summary>
        /// <param name="fromOrder">The order of the source edge.</param>
        /// <returns>The turn cost if any.</returns>
        IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo(byte fromOrder);

        /// <summary>
        /// Gets the turn cost from the current edge given the to order.
        /// </summary>
        /// <param name="toOrder">The order of the target edge.</param>
        /// <returns>The turn cost if any.</returns>
        IEnumerable<(uint turnCostType, uint cost)> GetTurnCostFrom(byte toOrder);
    }
}