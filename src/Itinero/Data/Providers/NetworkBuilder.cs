using System;
using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.DataStructures;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Data.Providers;

/// <summary>
/// The means to write a network, handed to a provider by Itinero.
/// </summary>
/// <remarks>
/// Public so an out-of-tree provider can be given one, with an internal constructor so only
/// Itinero can make one: writing is impossible without being handed the means, and being handed
/// the means is what it is to be a provider.
/// </remarks>
/// <remarks>
/// It holds the network's write slot for as long as it lives, and disposing it is what publishes
/// what was written. So it is always used in a <c>using</c> — there is no separate commit to
/// remember, and no way to leave the slot held.
/// </remarks>
/// <remarks>
/// Writing is additive and a tile is written whole. Tiles this scope does not touch are carried
/// over by reference, and a tile it does touch is built new rather than copied — so the network
/// being replaced is never written to, and a request still reading it is unaffected.
/// </remarks>
public sealed class NetworkBuilder : IDisposable, IEdgeEnumerable
{
    private readonly RoutingNetwork _network;
    private readonly SparseArray<NetworkTile?> _tiles;

    // which tiles this transaction started, so a second write to one continues the tile this
    // transaction made rather than starting another — and so writing to one it only carried over
    // is a fault rather than a silent write into the network being replaced.
    private readonly SparseArray<bool> _written;

    internal NetworkBuilder(RoutingNetwork network)
    {
        _network = network;

        // references only, and it is what lets the new network share tiles with the one it
        // replaces while differing in the few this transaction writes.
        _tiles = _network.Tiles.Clone();
        _written = new SparseArray<bool>(_tiles.Length);
    }

    /// <summary>
    /// Publishes everything written as the db's latest network.
    /// </summary>
    public void Dispose()
    {
        _network.RouterDb.Finish(this.ToRoutingNetwork());
    }

    /// <summary>
    /// Adds a new vertex.
    /// </summary>
    public VertexId AddVertex(double longitude, double latitude, float? e = null)
    {
        var (x, y) = TileStatic.WorldToTile(longitude, latitude, _network.Zoom);
        var localTileId = TileStatic.ToLocalId(x, y, _network.Zoom);

        return this.GetTileForWrite(localTileId).AddVertex(longitude, latitude, e);
    }

    /// <summary>
    /// Adds a new vertex.
    /// </summary>
    public VertexId AddVertex((double longitude, double latitude, float? e) location)
    {
        return this.AddVertex(location.longitude, location.latitude, location.e);
    }

    /// <summary>
    /// Adds a new edge, with the edge type id the provider assigned it.
    /// </summary>
    /// <remarks>
    /// Returns the <see cref="EdgeId"/>, which is the only thing a provider is asked about an edge
    /// later, so recording it against whatever the provider kept back needs no id in the tile.
    /// The shape is measured and discarded: an edge's geometry is the provider's, and its length
    /// is all the search needs.
    /// </remarks>
    public EdgeId AddEdge(VertexId tail, VertexId head, uint edgeTypeId,
        IEnumerable<(double longitude, double latitude, float? e)>? shape = null)
    {
        var tile = this.GetTileForWrite(tail.TileId);

        // compute edge length in centimeters.
        if (!this.TryGetVertex(tail, out var lon1, out var lat1, out var e1))
            throw new ArgumentException($"Vertex {tail} not found.", nameof(tail));
        if (!this.TryGetVertex(head, out var lon2, out var lat2, out var e2))
            throw new ArgumentException($"Vertex {head} not found.", nameof(head));
        var length = (uint)((lon1, lat1, e1).DistanceEstimateInMeterShape(
            (lon2, lat2, e2), shape) * 100);

        var edge1 = tile.AddEdge(tail, head, edgeTypeId, null, length);
        if (tail.TileId == head.TileId) return edge1;

        // this edge crosses tiles, also add an extra edge to the other tile.
        tile = this.GetTileForWrite(head.TileId);
        tile.AddEdge(tail, head, edgeTypeId, edge1, length);

        return edge1;
    }

    /// <summary>
    /// Adds turn costs, with the turn cost type id the provider assigned them.
    /// </summary>
    /// <remarks>
    /// A simple restricted turn is edges [from, to] with costs [[0, 1], [0, 0]]. Costs added on
    /// top of each other add up and a single binary cost is enough to restrict the turn — what a
    /// cost means is the profile's to decide.
    /// </remarks>
    /// <returns>False when the vertex has no order budget left, see OrderCoder.</returns>
    public bool AddTurnCosts(VertexId vertex,
        EdgeId[] edges, uint[,] costs, uint turnCostType, IEnumerable<EdgeId>? prefix = null)
    {
        prefix ??= ArraySegment<EdgeId>.Empty;

        return this.GetTileForWrite(vertex.TileId)
            .AddTurnCosts(vertex, turnCostType, edges, costs, prefix);
    }

    /// <summary>
    /// Gets an enumerator over what has been written so far.
    /// </summary>
    /// <remarks>
    /// Needed while writing: resolving a restriction means finding the edges it names.
    /// </remarks>
    public NetworkBuilderEdgeEnumerator GetEdgeEnumerator()
    {
        return new NetworkBuilderEdgeEnumerator(this);
    }

    /// <summary>
    /// Tries to get the location of the given vertex.
    /// </summary>
    public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude,
        out float? elevation)
    {
        var tile = this.GetTileForRead(vertex.TileId);
        if (tile != null) return tile.TryGetVertex(vertex, out longitude, out latitude, out elevation);

        longitude = default;
        latitude = default;
        elevation = null;

        return false;
    }

    /// <summary>
    /// Gets a tile to write to.
    /// </summary>
    /// <remarks>
    /// A tile is written whole: the first write to one in this scope starts a new tile, and
    /// whatever the network being replaced held for it is replaced rather than appended to.
    /// Nothing is copied, and that network keeps its own instances untouched — which is what lets
    /// a request still reading it finish. A tile that needs changing is dropped and rebuilt from
    /// the provider's own data, never edited in place.
    /// </remarks>
    private NetworkTile GetTileForWrite(uint localTileId)
    {
        _tiles.EnsureMinimumSize(localTileId);
        _written.EnsureMinimumSize(localTileId);

        // already writing this one.
        if (_written[localTileId]) return _tiles[localTileId]!;

        if (_tiles[localTileId] != null)
        {
            throw new InvalidOperationException(
                $"Tile {localTileId} was carried over from the network being replaced: drop it " +
                "before writing it, or a request still reading that network sees it change.");
        }

        var tile = new NetworkTile(_network.Zoom, localTileId);

        _tiles[localTileId] = tile;
        _written[localTileId] = true;

        return tile;
    }

    private NetworkTile? GetTileForRead(uint localTileId)
    {
        return _tiles.Length <= localTileId ? null : _tiles[localTileId];
    }

    NetworkTile? IEdgeEnumerable.GetTileForRead(uint localTileId)
    {
        return this.GetTileForRead(localTileId);
    }

    RouterDb IEdgeEnumerable.RouterDb => _network.RouterDb;

    /// What resolves the ids being written, which is the provider doing the writing.
    DataProviderBase IEdgeEnumerable.Provider => _network.RouterDb.Provider;

    internal int Zoom => _network.Zoom;

    private RoutingNetwork ToRoutingNetwork()
    {
        var routingNetwork = new RoutingNetwork(_network.RouterDb, _tiles, _network.Zoom);

        // check if listeners need to be cloned/copied over.
        foreach (var listener in _network.UsageNotifier.RegisteredListeners)
        {
            var clonedListener = listener.CloneForNewNetwork(routingNetwork);
            if (clonedListener == null) continue;

            routingNetwork.UsageNotifier.AddListener(clonedListener);
        }

        return routingNetwork;
    }
}
