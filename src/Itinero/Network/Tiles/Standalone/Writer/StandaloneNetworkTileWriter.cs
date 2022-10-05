using System;
using System.Collections.Generic;
using Itinero.Geo;
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Network.Tiles.Standalone.Writer;

/// <summary>
/// A writer class to write to a standalone tile.
/// </summary>
public class StandaloneNetworkTileWriter
{
    private readonly StandaloneNetworkTile _tile;
    private readonly int _zoom;
    private readonly (Guid id, Func<IEnumerable<(string key, string value)>, uint> func) _turnCostTypeMap;

    internal StandaloneNetworkTileWriter(StandaloneNetworkTile tile, int zoom,
        (Guid id, Func<IEnumerable<(string key, string value)>, uint> func) edgeTypeMap,
        (Guid id, Func<IEnumerable<(string key, string value)>, uint> func) turnCostTypeMap)
    {
        _tile = tile;
        this.EdgeTypeMap = edgeTypeMap;
        _turnCostTypeMap = turnCostTypeMap;
        _zoom = zoom;
    }

    /// <summary>
    /// Gets the edge associated with the given id.
    /// </summary>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="forward">The forward flag.</param>
    /// <returns>The edge details.</returns>
    public INetworkTileEdge GetEdge(EdgeId edgeId, bool forward)
    {
        var edge = new NetworkTileEnumerator();
        edge.MoveTo(_tile.NetworkTile);
        edge.MoveTo(edgeId, forward);
        return edge;
    }

    /// <summary>
    /// Gets the edge type map.
    /// </summary>
    public (Guid id, Func<IEnumerable<(string key, string value)>, uint> func) EdgeTypeMap { get; }

    /// <summary>
    /// Returns true if the given coordinates are inside the tile boundaries.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <returns>True if inside, false otherwise.</returns>
    public bool IsInTile(double longitude, double latitude)
    {
        // get the local tile id.
        var (x, y) = TileStatic.WorldToTile(longitude, latitude, _zoom);
        return _tile.TileId == TileStatic.ToLocalId(x, y, _zoom);
    }

    /// <summary>
    /// Gets the tile id.
    /// </summary>
    public uint TileId => _tile.TileId;

    /// <summary>
    /// Adds a new vertex.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="elevation">The elevation.</param>
    /// <returns>The new vertex id.</returns>
    /// <exception cref="ArgumentException"></exception>
    public VertexId AddVertex(double longitude, double latitude, float? elevation = null)
    {
        // check the local tile id.
        var (x, y) = TileStatic.WorldToTile(longitude, latitude, _zoom);
        var localTileId = TileStatic.ToLocalId(x, y, _zoom);
        if (_tile.TileId != localTileId) throw new ArgumentException("Coordinate are not inside the tile");

        return _tile.NetworkTile.AddVertex(longitude, latitude, elevation);
    }

    /// <summary>
    /// Adds a new edge.
    /// </summary>
    /// <param name="vertex1">The from vertex.</param>
    /// <param name="vertex2">The to vertex.</param>>
    /// <param name="edgeTypeId">The edge type id.</param>
    /// <param name="shape">The shape, if any.</param>
    /// <param name="attributes">The attributes, if any.</param>
    /// <returns>The edge id.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public EdgeId AddEdge(VertexId vertex1, VertexId vertex2, uint edgeTypeId,
        IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
        IEnumerable<(string key, string value)>? attributes = null)
    {
        if (_tile.TileId != vertex1.TileId) throw new ArgumentException("Vertex not in tile");
        if (_tile.TileId != vertex2.TileId) throw new ArgumentException("Vertex not in tile");

        // get the edge length in centimeters.
        if (!_tile.NetworkTile.TryGetVertex(vertex1, out var longitude, out var latitude, out var e))
        {
            throw new ArgumentOutOfRangeException(nameof(vertex1), $"Vertex {vertex1} not found.");
        }

        var vertex1Location = (longitude, latitude, e);
        if (!_tile.NetworkTile.TryGetVertex(vertex2, out longitude, out latitude, out e))
        {
            throw new ArgumentOutOfRangeException(nameof(vertex1), $"Vertex {vertex2} not found.");
        }

        var vertex2Location = (longitude, latitude, e);

        var length = (uint)(vertex1Location.DistanceEstimateInMeterShape(
            vertex2Location, shape) * 100);

        return _tile.NetworkTile.AddEdge(vertex1, vertex2, shape, attributes, null, edgeTypeId, length);
    }

    public void AddGlobalIdFor(EdgeId edgeId, Guid globalEdgeId)
    {
        _tile.AddGlobalIdFor(edgeId, globalEdgeId);
    }

    public void AddGlobalIdFor(BoundaryEdgeId boundaryEdgeId, Guid globalEdgeId)
    {
        _tile.AddGlobalIdFor(boundaryEdgeId, globalEdgeId);
    }


    /// <summary>
    /// Adds turn costs.
    /// </summary>
    /// <param name="vertex">The vertex where the costs are located.</param>
    /// <param name="attributes">The attributes representing the type of costs.</param>
    /// <param name="edges">The edges involved in the costs.</param>
    /// <param name="costs">The costs.</param>
    /// <param name="prefix">When the costs are only valid after first traversing a sequence of edges.</param>
    public void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
        EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
    {
        prefix ??= ArraySegment<EdgeId>.Empty;

        // get the turn cost type id.
        var turnCostTypeId = _turnCostTypeMap.func(attributes);

        // add the turn cost table using the type id.
        _tile.NetworkTile.AddTurnCosts(vertex, turnCostTypeId, edges, costs, attributes, prefix);
    }

    /// <summary>
    /// Adds a new boundary crossing.
    /// </summary>
    /// <param name="from">The from node and vertex, inside the tile.</param>
    /// <param name="to">The to node.</param>
    /// <param name="edgeTypeId">The edge type id.</param>
    /// <param name="attributes">The attributes.</param>
    /// <param name="length">The length in centimeters.</param>
    public BoundaryEdgeId AddBoundaryCrossing((VertexId vertex, long node) from, long to,
        uint edgeTypeId, IEnumerable<(string key, string value)> attributes, uint length)
    {
        return _tile.AddBoundaryCrossing(false, from.node, to, from.vertex, attributes, edgeTypeId, length);
    }

    /// <summary>
    /// Adds a new boundary crossing.
    /// </summary>
    /// <param name="from">The from node.</param>
    /// <param name="to">The to node and vertex, inside the tile.</param>
    /// <param name="globalEdgeId">The global edge id.</param>
    /// <param name="edgeTypeId">The edge type id.</param>
    /// <param name="attributes">The attributes.</param>
    /// <param name="length">The length in centimeters.</param>
    public BoundaryEdgeId AddBoundaryCrossing(long from, (VertexId vertex, long node) to,
        uint edgeTypeId, IEnumerable<(string key, string value)> attributes, uint length)
    {
        return _tile.AddBoundaryCrossing(true, from, to.node, to.vertex, attributes, edgeTypeId, length);
    }

    /// <summary>
    /// Gets the resulting tile.
    /// </summary>
    /// <returns></returns>
    public StandaloneNetworkTile GetResultingTile()
    {
        return _tile;
    }
}
