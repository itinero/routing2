using System.Collections.Generic;
using System.Threading;

namespace Itinero.Network.Search.Islands;

internal class Islands
{
    private readonly HashSet<uint> _tiles; // holds the tiles that have been processed.
    private readonly ReaderWriterLockSlim _tilesLock = new();
    private readonly HashSet<EdgeId> _islandEdges;
    private readonly ReaderWriterLockSlim _islandEdgesLock = new();
    private readonly HashSet<EdgeId> _localEdges;
    private readonly ReaderWriterLockSlim _localEdgesLock = new();

    // Transient state used during the NonLocal classification pass: each
    // edge the classifier identifies as Island in the N-only subgraph is
    // recorded here. Once both passes complete and the coordinator has
    // computed _localEdges from the gap (NonLocal-Island ∩ NotIsland-Full
    // ∩ non-L), this set can be cleared. Not part of the persistent
    // storage contract.
    private readonly HashSet<EdgeId> _nonLocalIslandEdges;
    private readonly ReaderWriterLockSlim _nonLocalIslandEdgesLock = new();

    internal Islands()
    {
        _tiles = [];
        _islandEdges = [];
        _localEdges = [];
        _nonLocalIslandEdges = [];
    }

    private Islands(HashSet<uint> tiles, HashSet<EdgeId> islandEdges, HashSet<EdgeId> localEdges)
    {
        _tiles = tiles;
        _islandEdges = islandEdges;
        _localEdges = localEdges;
        _nonLocalIslandEdges = [];
    }

    /// <summary>
    /// Sets the tile as done.
    /// </summary>
    /// <param name="tileId">Sets the tile as done.</param>
    /// <returns>True if the tile is done.</returns>
    public bool SetTileDone(uint tileId)
    {
        try
        {
            _tilesLock.EnterWriteLock();

            return _tiles.Add(tileId);
        }
        finally
        {
            _tilesLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Returns true if the given tile is done.
    /// </summary>
    /// <param name="tileId"></param>
    /// <returns></returns>
    public bool GetTileDone(uint tileId)
    {
        try
        {
            _tilesLock.EnterReadLock();

            return _tiles.Contains(tileId);
        }
        finally
        {
            _tilesLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Returns true if the given edge is on an island.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public bool IsEdgeOnIsland(EdgeId edge)
    {
        try
        {
            _islandEdgesLock.EnterReadLock();

            return _islandEdges.Contains(edge);
        }
        finally
        {
            _islandEdgesLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Marks the given edge as on an island.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public bool SetEdgeOnIsland(EdgeId edge)
    {
        try
        {
            _islandEdgesLock.EnterWriteLock();

            return _islandEdges.Add(edge);
        }
        finally
        {
            _islandEdgesLock.ExitWriteLock();
        }
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
            try
            {
                _nonLocalIslandEdgesLock.EnterReadLock();

                return _nonLocalIslandEdges.Contains(edge);
            }
            finally
            {
                _nonLocalIslandEdgesLock.ExitReadLock();
            }
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
            try
            {
                _nonLocalIslandEdgesLock.EnterWriteLock();

                return _nonLocalIslandEdges.Add(edge);
            }
            finally
            {
                _nonLocalIslandEdgesLock.ExitWriteLock();
            }
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
        try
        {
            _nonLocalIslandEdgesLock.EnterWriteLock();

            _nonLocalIslandEdges.Clear();
        }
        finally
        {
            _nonLocalIslandEdgesLock.ExitWriteLock();
        }
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
        try
        {
            _localEdgesLock.EnterReadLock();

            return _localEdges.Contains(edge);
        }
        finally
        {
            _localEdgesLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Marks the given edge as reachable only via local-access edges. See
    /// <see cref="IsEdgeLocal"/> for what that means.
    /// </summary>
    public bool SetEdgeLocal(EdgeId edge)
    {
        try
        {
            _localEdgesLock.EnterWriteLock();

            return _localEdges.Add(edge);
        }
        finally
        {
            _localEdgesLock.ExitWriteLock();
        }
    }

    internal Islands Clone()
    {
        try
        {
            _tilesLock.EnterWriteLock();
            try
            {
                _islandEdgesLock.EnterWriteLock();
                try
                {
                    _localEdgesLock.EnterWriteLock();

                    return new Islands([.. _tiles], [.. _islandEdges], [.. _localEdges]);
                }
                finally
                {
                    _localEdgesLock.ExitWriteLock();
                }
            }
            finally
            {
                _islandEdgesLock.ExitWriteLock();
            }
        }
        finally
        {
            _tilesLock.ExitWriteLock();
        }
    }
}
