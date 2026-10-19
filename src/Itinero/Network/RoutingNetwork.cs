using System;
using System.Collections.Generic;
using Itinero.Data.Usage;
using Itinero.Network.DataStructures;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Enumerators.Vertices;
using Itinero.Network.Mutation;
using Itinero.Network.Tiles;

namespace Itinero.Network;

/// <summary>
/// The routing network.
/// </summary>
public sealed partial class RoutingNetwork : IEdgeEnumerable, IRoutingNetworkMutable
{
    private readonly SparseArray<NetworkTile?> _tiles;

    /// <summary>
    /// Creates a new routing network.
    /// </summary>
    /// <remarks>
    /// Internal: a db publishes its own networks. A second one built for the same db would be
    /// detached — written to and never seen.
    /// </remarks>
    /// <param name="routerDb"></param>
    /// <param name="zoom"></param>
    internal RoutingNetwork(RouterDb routerDb, int zoom = 14)
    {
        this.Zoom = zoom;
        this.RouterDb = routerDb;
        this.Provider = routerDb.Provider;

        _tiles = new SparseArray<NetworkTile?>(0);
    }

    internal RoutingNetwork(RouterDb routerDb, SparseArray<NetworkTile?> tiles, int zoom)
    {
        this.Zoom = zoom;
        this.RouterDb = routerDb;
        this.Provider = routerDb.Provider;
        _tiles = tiles;
    }

    /// <summary>
    /// What resolves this network's edge ids to everything the tiles do not hold.
    /// </summary>
    /// <remarks>
    /// Held by the network rather than reached through the db: an <see cref="EdgeId"/> is only
    /// meaningful in the network that issued it — a tile rewritten by a later generation reuses
    /// local ids — so resolving one against whatever the db currently points at is asking the
    /// wrong map. The network points at its provider and never the reverse, which is also what
    /// lets a retired network be collected.
    /// </remarks>
    Data.Providers.DataProviderBase IEdgeEnumerable.Provider => this.Provider;

    internal Data.Providers.DataProviderBase Provider { get; }

    internal NetworkTile? GetTileForRead(uint localTileId)
    {
        if (_tiles.Length <= localTileId) return null;

        // get tile, if any.
        return _tiles[localTileId];
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
