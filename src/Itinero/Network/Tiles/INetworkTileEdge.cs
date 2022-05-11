using System.Collections.Generic;

namespace Itinero.Network.Tiles;

public interface INetworkTileEdge
{
    /// <summary>
    /// Gets the tile id.
    /// </summary>
    public uint TileId { get; }

    public bool IsEmpty { get; }

    /// <summary>
    /// Gets the shape of the given edge (not including vertex locations).
    /// </summary>
    public IEnumerable<(double longitude, double latitude, float? e)> Shape { get; }

    /// <summary>
    /// Gets the attributes of the given edge.
    /// </summary>
    public IEnumerable<(string key, string value)> Attributes { get; }

    /// <summary>
    /// Gets the first vertex.
    /// </summary>
    public VertexId Tail { get; }

    /// <summary>
    /// Gets the second vertex.
    /// </summary>
    public VertexId Head { get; }

    /// <summary>
    /// Gets the local edge id.
    /// </summary>
    public EdgeId EdgeId { get; }

    /// <summary>
    /// Gets the forward/backward flag.
    /// </summary>
    public bool Forward { get; }

    /// <summary>
    /// Gets the edge profile id, if any.
    /// </summary>
    public uint? EdgeTypeId { get; }

    /// <summary>
    /// Gets the length in centimeters, if any.
    /// </summary>
    public uint? Length { get; }

    /// <summary>
    /// Gets the head index of this edge.
    /// </summary>
    public byte? HeadOrder { get; }

    /// <summary>
    /// Gets the tail index of this edge.
    /// </summary>
    public byte? TailOrder { get; }

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