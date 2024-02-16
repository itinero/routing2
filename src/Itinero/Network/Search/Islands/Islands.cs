using System.Collections.Generic;
using System.Threading;

namespace Itinero.Network.Search.Islands;

internal class Islands
{
    private readonly HashSet<uint> _tiles; // holds the tiles that have been processed.
    private readonly ReaderWriterLockSlim _tilesLock = new();
    private readonly HashSet<EdgeId> _islandEdges;
    private readonly ReaderWriterLockSlim _islandEdgesLock = new();

    internal Islands()
    {
        _tiles = [];
        _islandEdges = [];
    }

    private Islands(HashSet<uint> tiles, HashSet<EdgeId> islandEdges)
    {
        _tiles = tiles;
        _islandEdges = islandEdges;
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

    internal Islands Clone()
    {
        try
        {
            _tilesLock.EnterWriteLock();


            try
            {
                _islandEdgesLock.EnterWriteLock();

                return new Islands([.. _tiles], [.. _islandEdges]);
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
