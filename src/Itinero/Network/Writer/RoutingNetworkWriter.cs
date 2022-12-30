using System;
using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Network.Writer;

/// <summary>
/// A writer to write to a network. This writer will never change existing data, only add new data.
///
/// This writer can:
/// - add new vertices
/// - add new edges.
///
/// This writer cannot mutate existing data, only add new.
/// </summary>
public class RoutingNetworkWriter : IDisposable
{
    private readonly IRoutingNetworkWritable _network;

    internal RoutingNetworkWriter(IRoutingNetworkWritable network)
    {
        _network = network;
    }

    /// <summary>
    /// Gets an edge enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    internal RoutingNetworkEdgeEnumerator GetEdgeEnumerator()
    {
        return _network.GetEdgeEnumerator();
    }

    /// <summary>
    /// Adds a new vertex.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="elevation">The elevation.</param>
    /// <returns>The vertex id.</returns>
    public VertexId AddVertex(double longitude, double latitude, float? elevation = null)
    {
        // get the local tile id.
        var (x, y) = TileStatic.WorldToTile(longitude, latitude, _network.Zoom);
        var localTileId = TileStatic.ToLocalId(x, y, _network.Zoom);

        // get the tile (or create it).
        var (tile, _) = _network.GetTileForWrite(localTileId);

        return tile.AddVertex(longitude, latitude, elevation);
    }

    /// <summary>
    /// Adds a new edge.
    /// </summary>
    /// <param name="tail">The tail vertex.</param>
    /// <param name="head">The head vertex.</param>
    /// <param name="shape">The shape, if any.</param>
    /// <param name="attributes">The attributes, if any.</param>
    /// <param name="edgeTypeId">The edge type id, if any.</param>
    /// <param name="length">The length, if any.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public EdgeId AddEdge(VertexId tail, VertexId head,
        IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
        IEnumerable<(string key, string value)>? attributes = null, uint? edgeTypeId = null,
        uint? length = null)
    {
        // get the tile (or create it).
        var (tile, edgeTypeMap) = _network.GetTileForWrite(tail.TileId);
        if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");

        // get the edge type id.
        edgeTypeId ??= attributes != null ? edgeTypeMap(attributes) : null;

        // get the edge length in centimeters.
        if (!_network.TryGetVertex(tail, out var longitude, out var latitude, out var e))
        {
            throw new ArgumentOutOfRangeException(nameof(tail), $"Vertex {tail} not found.");
        }

        var vertex1Location = (longitude, latitude, e);
        if (!_network.TryGetVertex(head, out longitude, out latitude, out e))
        {
            throw new ArgumentOutOfRangeException(nameof(tail), $"Vertex {head} not found.");
        }

        var vertex2Location = (longitude, latitude, e);

        length ??= (uint)(vertex1Location.DistanceEstimateInMeterShape(
            vertex2Location, shape) * 100);

        var edge1 = tile.AddEdge(tail, head, shape, attributes, null, edgeTypeId, length);
        if (tail.TileId == head.TileId)
        {
            return edge1;
        }

        // this edge crosses tiles, also add an extra edge to the other tile.
        (tile, _) = _network.GetTileForWrite(head.TileId);
        tile.AddEdge(tail, head, shape, attributes, edge1, edgeTypeId, length);

        return edge1;
    }

    public void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
        EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix, uint? turnCostType = null)
    {
        prefix ??= ArraySegment<EdgeId>.Empty;

        // get the tile (or create it).
        var (tile, _) = _network.GetTileForWrite(vertex.TileId);
        if (tile == null)
        {
            throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.");
        }

        // get the turn cost type id.
        var turnCostMap = _network.RouterDb.GetTurnCostTypeMap();
        turnCostType ??= turnCostMap.func(attributes);

        // add the turn cost table using the type id.
        tile.AddTurnCosts(vertex, turnCostType.Value, edges, costs, attributes, prefix);
    }

    internal void AddTile(NetworkTile tile)
    {
        _network.SetTile(tile);
    }

    internal bool HasTile(uint localTileId)
    {
        return _network.HasTile(localTileId);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _network.ClearWriter();
    }
}
