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
    private readonly (int id, Func<IEnumerable<(string key, string value)>, uint> func) _edgeTypeMap;

    internal StandaloneNetworkTileWriter(StandaloneNetworkTile tile, int zoom,
        (int id, Func<IEnumerable<(string key, string value)>, uint> func) edgeTypeMap)
    {
        _tile = tile;
        _edgeTypeMap = edgeTypeMap;
        _zoom = zoom;
    }

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
    /// <param name="vertex2">The to vertex.</param>
    /// <param name="shape">The shape, if any.</param>
    /// <param name="attributes">The attributes, if any.</param>
    /// <returns>The edge id.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
        IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
        IEnumerable<(string key, string value)>? attributes = null)
    {
        if (_tile.TileId != vertex1.TileId) throw new ArgumentException("Vertex not in tile");
        if (_tile.TileId != vertex2.TileId) throw new ArgumentException("Vertex not in tile");
        
        // get the edge type id.
        var edgeTypeId = attributes != null ? (uint?) _edgeTypeMap.func(attributes) : null;

        // get the edge length in centimeters.
        if (!_tile.NetworkTile.TryGetVertex(vertex1, out var longitude, out var latitude, out var e)) {
            throw new ArgumentOutOfRangeException(nameof(vertex1), $"Vertex {vertex1} not found.");
        }

        var vertex1Location = (longitude, latitude, e);
        if (!_tile.NetworkTile.TryGetVertex(vertex2, out longitude, out latitude, out e)) {
            throw new ArgumentOutOfRangeException(nameof(vertex1), $"Vertex {vertex2} not found.");
        }

        var vertex2Location = (longitude, latitude, e);

        var length = (uint) (vertex1Location.DistanceEstimateInMeterShape(
            vertex2Location, shape) * 100);

        return _tile.NetworkTile.AddEdge(vertex1, vertex2, shape, attributes, null, edgeTypeId, length);
    }

    /// <summary>
    /// Adds a new boundary crossing.
    /// </summary>
    /// <param name="from">The from node and vertex, inside the tile.</param>
    /// <param name="to">The to node.</param>
    /// <param name="attributes">The attributes.</param>
    /// <param name="length">The length in centimeters.</param>
    public void AddBoundaryCrossing((VertexId vertex, long node) from, long to,
        IEnumerable<(string key, string value)> attributes, uint length)
    {
        var edgeTypeId = _edgeTypeMap.func(attributes);
        
        _tile.AddBoundaryCrossing(false, from.node, to, from.vertex, attributes, edgeTypeId, length);
    }

    /// <summary>
    /// Adds a new boundary crossing.
    /// </summary>
    /// <param name="from">The from node.</param>
    /// <param name="to">The to node and vertex, inside the tile.</param>
    /// <param name="attributes">The attributes.</param>
    /// <param name="length">The length in centimeters.</param>
    public void AddBoundaryCrossing(long from, (VertexId vertex, long node) to,
        IEnumerable<(string key, string value)> attributes, uint length)
    {
        var edgeTypeId = _edgeTypeMap.func(attributes);
        
        _tile.AddBoundaryCrossing(true, from, to.node, to.vertex, attributes, edgeTypeId, length);
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