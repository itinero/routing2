using System.Collections.Generic;

namespace Itinero.Network.Tiles.Standalone;

public interface IStandaloneNetworkTileEnumerator
{
    /// <summary>
    /// Gets the tile id.
    /// </summary>
    uint TileId { get; }

    /// <summary>
    /// True if the tile is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets the shape of the given edge (not including vertex locations).
    /// </summary>
    IEnumerable<(double longitude, double latitude, float? e)> Shape { get; }

    /// <summary>
    /// Gets the attributes of the given edge.
    /// </summary>
    IEnumerable<(string key, string value)> Attributes { get; }

    /// <summary>
    /// Gets the first vertex.
    /// </summary>
    VertexId Tail { get; }

    /// <summary>
    /// Gets the second vertex.
    /// </summary>
    VertexId Head { get; }

    /// <summary>
    /// Gets the local edge id.
    /// </summary>
    EdgeId EdgeId { get; }

    /// <summary>
    /// Gets the forward/backward flag.
    /// </summary>
    bool Forward { get; }

    /// <summary>
    /// Gets the edge profile id, if any.
    /// </summary>
    uint? EdgeTypeId { get; }

    /// <summary>
    /// Gets the length in centimeters, if any.
    /// </summary>
    uint? Length { get; }

    /// <summary>
    /// Gets the head index of this edge.
    /// </summary>
    byte? HeadOrder { get; }

    /// <summary>
    /// Gets the tail index of this edge.
    /// </summary>
    byte? TailOrder { get; }

    /// <summary>
    /// Move to the vertex.
    /// </summary>
    /// <param name="vertex">The vertex.</param>
    /// <returns>True if the move succeeds and the vertex exists.</returns>
    bool MoveTo(VertexId vertex);

    /// <summary>
    /// Move to the given edge.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <param name="forward">The forward flag.</param>
    /// <returns>True if the move succeeds and the edge exists.</returns>
    bool MoveTo(EdgeId edge, bool forward);

    /// <summary>
    /// Resets this enumerator.
    /// </summary>
    /// <remarks>
    /// Reset this enumerator to:
    /// - the first vertex for the currently selected edge.
    /// - the first vertex for the graph tile if there is no selected edge.
    /// - returns false if there is no data in the current tile or if there is no tile selected.
    /// </remarks>
    void Reset();

    /// <summary>
    /// Moves to the next edge for the current vertex.
    /// </summary>
    /// <returns>True when there is a new edge.</returns>
    bool MoveNext();

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