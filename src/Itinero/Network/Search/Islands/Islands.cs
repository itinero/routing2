using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Network.Search.Islands;

internal class Islands
{
    // Read once per edge relaxation from every routing thread, written only
    // during classification. ContainsKey takes no lock, so readers never
    // contend. The byte value is unused — ConcurrentDictionary is the only
    // lock-free set in the framework.
    private readonly ConcurrentDictionary<uint, byte> _tiles; // tiles that have been processed.
    private readonly ConcurrentDictionary<EdgeId, byte> _islandEdges;
    private readonly ConcurrentDictionary<EdgeId, byte> _localEdges;

    // Transient state used during the NonLocal classification pass: each
    // edge the classifier identifies as Island in the N-only subgraph is
    // recorded here. Once both passes complete and the coordinator has
    // computed _localEdges from the gap (NonLocal-Island ∩ NotIsland-Full
    // ∩ non-L), this set can be cleared. Not part of the persistent
    // storage contract.
    private readonly ConcurrentDictionary<EdgeId, byte> _nonLocalIslandEdges;

    internal Islands()
    {
        _tiles = new ConcurrentDictionary<uint, byte>();
        _islandEdges = new ConcurrentDictionary<EdgeId, byte>();
        _localEdges = new ConcurrentDictionary<EdgeId, byte>();
        _nonLocalIslandEdges = new ConcurrentDictionary<EdgeId, byte>();
    }

    private Islands(IEnumerable<uint> tiles, IEnumerable<EdgeId> islandEdges, IEnumerable<EdgeId> localEdges)
    {
        _tiles = new ConcurrentDictionary<uint, byte>(tiles.Select(t => new KeyValuePair<uint, byte>(t, 0)));
        _islandEdges = new ConcurrentDictionary<EdgeId, byte>(islandEdges.Select(e => new KeyValuePair<EdgeId, byte>(e, 0)));
        _localEdges = new ConcurrentDictionary<EdgeId, byte>(localEdges.Select(e => new KeyValuePair<EdgeId, byte>(e, 0)));
        _nonLocalIslandEdges = new ConcurrentDictionary<EdgeId, byte>();
    }

    /// <summary>
    /// Sets the tile as done.
    /// </summary>
    /// <param name="tileId">Sets the tile as done.</param>
    /// <returns>True if the tile is done.</returns>
    /// <remarks>
    /// Called last, after every SetEdgeOnIsland and SetEdgeLocal for the tile.
    /// Readers depend on that: a tile seen as done means its edge writes are
    /// already visible.
    /// </remarks>
    public bool SetTileDone(uint tileId)
    {
        return _tiles.TryAdd(tileId, 0);
    }

    /// <summary>
    /// Returns true if the given tile is done.
    /// </summary>
    /// <param name="tileId"></param>
    /// <returns></returns>
    public bool GetTileDone(uint tileId)
    {
        return _tiles.ContainsKey(tileId);
    }

    /// <summary>
    /// Returns true if the given edge is on an island.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public bool IsEdgeOnIsland(EdgeId edge)
    {
        return _islandEdges.ContainsKey(edge);
    }

    /// <summary>
    /// Marks the given edge as on an island.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public bool SetEdgeOnIsland(EdgeId edge)
    {
        return _islandEdges.TryAdd(edge, 0);
    }

    /// <summary>
    /// Kind-aware overload. Reads from <c>_islandEdges</c> for Full and from
    /// the transient <c>_nonLocalIslandEdges</c> for NonLocal. The latter is
    /// only meaningful while a NonLocal classification pass is in progress
    /// or hasn't been collapsed into <c>_localEdges</c> yet.
    /// </summary>
    public bool IsEdgeOnIsland(EdgeId edge, IslandKind kind)
    {
        if (kind == IslandKind.NonLocal)
        {
            return _nonLocalIslandEdges.ContainsKey(edge);
        }

        return this.IsEdgeOnIsland(edge);
    }

    /// <summary>
    /// Kind-aware overload. Writes to <c>_islandEdges</c> for Full and to the
    /// transient <c>_nonLocalIslandEdges</c> for NonLocal.
    /// </summary>
    public bool SetEdgeOnIsland(EdgeId edge, IslandKind kind)
    {
        if (kind == IslandKind.NonLocal)
        {
            return _nonLocalIslandEdges.TryAdd(edge, 0);
        }

        return this.SetEdgeOnIsland(edge);
    }

    /// <summary>
    /// Discards the transient <c>_nonLocalIslandEdges</c> set used during
    /// NonLocal classification. Called by the build coordinator after
    /// <c>_localEdges</c> has been computed from the gap. After this, the
    /// only persistent state is <c>_islandEdges</c> and <c>_localEdges</c>.
    /// </summary>
    internal void ClearNonLocalIslandEdges()
    {
        _nonLocalIslandEdges.Clear();
    }

    /// <summary>
    /// Returns true if the edge is reachable but only via local-access edges —
    /// i.e. it is not an island, but every route to it from the main network
    /// crosses at least one L-tagged edge first. A typical example is an
    /// N-tagged road inside a residential subdivision whose only connection
    /// to the main road network goes through an <c>access=destination</c>
    /// gate road.
    ///
    /// L-tagged edges themselves are NOT stored here — they are recognised
    /// by their tag, which is build-time information on <c>EdgeFactor</c>.
    /// </summary>
    public bool IsEdgeLocal(EdgeId edge)
    {
        return _localEdges.ContainsKey(edge);
    }

    /// <summary>
    /// Marks the given edge as reachable only via local-access edges. See
    /// <see cref="IsEdgeLocal"/> for what that means.
    /// </summary>
    public bool SetEdgeLocal(EdgeId edge)
    {
        return _localEdges.TryAdd(edge, 0);
    }

    internal Islands Clone()
    {
        // Snapshot _tiles before the edge sets, so a tile seen as done always
        // comes with its edges. Reversed, the clone could hold a done tile whose
        // island edges are missing, which IsMainN reads as main-N.
        var tiles = _tiles.Keys;
        return new Islands(tiles, _islandEdges.Keys, _localEdges.Keys);
    }
}
