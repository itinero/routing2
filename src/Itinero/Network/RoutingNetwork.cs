using System;
using System.Collections.Generic;
using Itinero.Data.Usage;
using Itinero.Network.DataStructures;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Enumerators.Vertices;
using Itinero.Network.Mutation;
using Itinero.Network.Tiles;
using Itinero.Network.Writer;

namespace Itinero.Network;

/// <summary>
/// The routing network.
/// </summary>
public sealed partial class RoutingNetwork : IEdgeEnumerable, IRoutingNetworkMutable, IRoutingNetworkWritable
{
    private readonly SparseArray<NetworkTile?> _tiles;

    /// <summary>
    /// Creates a new routing network.
    /// </summary>
    /// <param name="routerDb"></param>
    /// <param name="zoom"></param>
    public RoutingNetwork(RouterDb routerDb, int zoom = 14)
    {
        this.Zoom = zoom;
        this.RouterDb = routerDb;

        _tiles = new SparseArray<NetworkTile?>(0);
    }

    internal RoutingNetwork(RouterDb routerDb, SparseArray<NetworkTile?> tiles, int zoom)
    {
        this.Zoom = zoom;
        this.RouterDb = routerDb;
        _tiles = tiles;
    }

    internal NetworkTile? GetTileForRead(uint localTileId)
    {
        if (_tiles.Length <= localTileId) return null;

        // get tile, if any.
        var tile = _tiles[localTileId];
        if (tile == null) return null;

        // check edge type map.
        var edgeTypeMap = this.RouterDb.GetEdgeTypeMap();
        if (tile.EdgeTypeMapId == edgeTypeMap.id) return tile;

        // tile.EdgeTypeMapId indicates the version of the used edgeTypeMap
        // If the id is different, the loaded tile needs updating; e.g. because a cost function has been changed
        tile = tile.CloneForEdgeTypeMap(edgeTypeMap);
        _tiles[localTileId] = tile;

        return tile;
    }

    /// <summary>
    /// Gets the usage notifier.
    /// </summary>
    public DataUseNotifier UsageNotifier { get; } = new();

    internal IEnumerator<uint> GetTileEnumerator()
    {
        using var enumerator = _tiles.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return (uint)enumerator.Current.i;
        }
    }

    NetworkTile? IEdgeEnumerable.GetTileForRead(uint localTileId)
    {
        return this.GetTileForRead(localTileId);
    }

    /// <summary>
    /// Gets the zoom.
    /// </summary>
    public int Zoom { get; }

    /// <summary>
    /// Gets the routing network.
    /// </summary>
    public RouterDb RouterDb { get; }

    /// <summary>
    /// Tries to get the given vertex.
    /// </summary>
    /// <param name="vertex">The vertex.</param>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="elevation">The elevation.</param>
    /// <returns>The vertex.</returns>
    public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude, out float? elevation)
    {
        var localTileId = vertex.TileId;

        // get tile.
        var tile = this.GetTileForRead(localTileId);
        if (tile != null) return tile.TryGetVertex(vertex, out longitude, out latitude, out elevation);

        // no tile, no vertex.
        longitude = default;
        latitude = default;
        elevation = null;
        return false;
    }

    /// <summary>
    /// Gets an edge enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public RoutingNetworkEdgeEnumerator GetEdgeEnumerator()
    {
        return new RoutingNetworkEdgeEnumerator(this);
    }

    RoutingNetworkEdgeEnumerator IRoutingNetworkWritable.GetEdgeEnumerator()
    {
        return this.GetEdgeEnumerator();
    }

    /// <summary>
    /// Gets a vertex enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    internal RoutingNetworkVertexEnumerator GetVertexEnumerator()
    {
        return new(this);
    }

    /// <summary>
    /// Returns true if this network has the given tile loaded.
    /// </summary>
    /// <param name="localTileId"></param>
    /// <returns></returns>
    public bool HasTile(uint localTileId)
    {
        if (localTileId >= _tiles.Length) return false;

        return _tiles[localTileId] != null;
    }
}
