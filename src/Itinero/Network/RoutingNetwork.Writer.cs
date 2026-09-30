using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Itinero.Network.DataStructures;
using Itinero.Network.Tiles;
using Itinero.Network.Writer;

namespace Itinero.Network;

public sealed partial class RoutingNetwork
{
    private readonly object _writeSync = new();
    private RoutingNetworkWriter? _writer;

    /// <summary>
    /// Concurrent tile-insert writers currently open.
    /// </summary>
    /// <remarks>
    /// Tile insertion is the one write that happens constantly and from many threads, so it gets
    /// its own admission rule: any number at once, but never alongside the exclusive writer.
    ///
    /// That exclusion is not new behaviour, it is the old behaviour made explicit. Tile insertion
    /// used to call <see cref="GetWriter"/>, so an open mutator made it throw — the two were
    /// already mutually exclusive, by exception rather than by design. Counting the tile writers
    /// keeps that guarantee while letting them run in parallel with each other.
    /// </remarks>
    private int _tileWriters;

    /// <summary>
    /// A lock per tile, created on demand.
    /// </summary>
    /// <remarks>
    /// Writes to one tile cannot overlap: a NetworkTile is an append-only byte buffer with its own
    /// offsets, so two threads appending at once corrupt it. Different tiles are independent,
    /// which is what makes concurrent insertion possible at all.
    ///
    /// Never grows beyond the tiles actually written, and the entries are plain objects.
    /// </remarks>
    /// <summary>
    /// Striped write locks, indexed by tile id modulo the stripe count.
    /// </summary>
    /// <remarks>
    /// Striped rather than one lock per tile, because both obvious alternatives are worse. A
    /// <c>ConcurrentDictionary</c> keyed by tile id put 31% of warm blocked time inside its own
    /// <c>GetOrAdd</c> — the lookup cost more than the work it guarded. A flat array indexed by
    /// tile id is far worse: local tile ids at zoom 14 run to 2^28, and a country's tiles sit
    /// around id 90,000,000, so the array grows to ~134M references — about 1 GB — to guard some
    /// 7,000 real tiles. That is exactly why the tiles themselves live in a
    /// <see cref="SparseArray{T}"/>.
    ///
    /// Stripes give bounded memory and an index calculation instead of a hash lookup. Two
    /// unrelated tiles sharing a stripe is harmless: it over-excludes, briefly serialising two
    /// writes that need not have been, and never under-excludes.
    /// </remarks>
    private const int TileLockStripes = 4096;

    private readonly object[] _tileLocks = CreateTileLocks();

    private static object[] CreateTileLocks()
    {
        var locks = new object[TileLockStripes];
        for (var i = 0; i < locks.Length; i++) locks[i] = new object();

        return locks;
    }

    /// <summary>
    /// Returns true if there is already a writer.
    /// </summary>
    public bool HasWriter => _writer != null;

    /// <summary>
    /// Gets a writer.
    /// </summary>
    /// <returns>The writer.</returns>
    public RoutingNetworkWriter GetWriter()
    {
        lock (_writeSync)
        {
            if (_writer != null) throw new InvalidOperationException($"Only one writer is allowed at one time." +
                                                    $"Check {nameof(this.HasWriter)} for a current writer.");
            if (_tileWriters > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot open a writer while {_tileWriters} tile-insert writer(s) are open.");
            }

            _writer = new RoutingNetworkWriter(this);
            return _writer;
        }
    }

    /// <summary>
    /// Gets a writer for inserting a tile, which may run concurrently with other tile inserts.
    /// </summary>
    /// <remarks>
    /// Any number of these may be open at once; none may be open alongside the exclusive
    /// <see cref="GetWriter"/>, and vice versa.
    ///
    /// Safe to use concurrently without taking any lock yourself: the write methods on the
    /// returned writer take the tile locks they need, in ascending tile id, which is what keeps
    /// concurrent inserts from deadlocking on a shared boundary.
    ///
    /// Intended for loading tiles into a live network, which is the one write that happens
    /// continuously and from many threads. Anything that mutates existing data still wants the
    /// exclusive writer.
    /// </remarks>
    public RoutingNetworkWriter GetTileInsertWriter()
    {
        lock (_writeSync)
        {
            if (_writer != null)
            {
                throw new InvalidOperationException(
                    "Cannot insert a tile while an exclusive writer is open.");
            }

            _tileWriters++;
            return new RoutingNetworkWriter(this, exclusive: false);
        }
    }

    void IRoutingNetworkWritable.ClearWriter()
    {
        _writer = null;
    }

    void IRoutingNetworkWritable.ReleaseTileWriter()
    {
        lock (_writeSync)
        {
            _tileWriters--;
        }
    }

    int IRoutingNetworkWritable.TileLockIndex(uint localTileId) =>
        (int)(localTileId & (TileLockStripes - 1));

    object IRoutingNetworkWritable.GetTileLockByIndex(int index) => _tileLocks[index];

    (NetworkTile tile, Func<IEnumerable<(string key, string value)>, uint> func) IRoutingNetworkWritable.
        GetTileForWrite(uint localTileId)
    {
        // No lock here. Every caller already holds this tile's stripe — the write methods on
        // RoutingNetworkWriter all go through WithTileLocks, which is where the ordering invariant
        // lives. Taking it again was correct (Monitor is re-entrant) but not free, and this is
        // called several times per inserted tile.
        //
        // ensure minimum size.
        _tiles.EnsureMinimumSize(localTileId);

        var edgeTypeMap = this.RouterDb.GetEdgeTypeMap();
        var tile = _tiles[localTileId];
        if (tile != null)
        {
            if (tile.EdgeTypeMapId != edgeTypeMap.id)
            {
                tile = tile.CloneForEdgeTypeMap(edgeTypeMap);
                _tiles[localTileId] = tile;
            }
            else
            {
                // check if there is a mutable graph.
                this.CloneTileIfNeededForMutator(tile);
            }

            return (tile, edgeTypeMap.func);
        }

        // create a new tile.
        tile = new NetworkTile(this.Zoom, localTileId, edgeTypeMap.id);
        _tiles[localTileId] = tile;

        return (tile, edgeTypeMap.func);
    }

    void IRoutingNetworkWritable.SetTile(NetworkTile tile)
    {
        var edgeTypeMap = this.RouterDb.GetEdgeTypeMap();
        if (tile.EdgeTypeMapId != edgeTypeMap.id) throw new ArgumentException("Cannot add an entire tile without a matching edge type map");

        // No lock: the caller holds this tile's stripe for the whole write session. Check-then-set
        // is still one step under that lock, which is what keeps two inserts of the same tile from
        // losing one — though the caller also dedupes in-flight loads per tile, so it should not
        // arise.
        //
        // ensure minimum size.
        _tiles.EnsureMinimumSize(tile.TileId);

        // set tile if not yet there.
        var existingTile = _tiles[tile.TileId];
        if (existingTile != null) throw new ArgumentException("Cannot overwrite a tile");
        _tiles[tile.TileId] = tile;
    }

    private void CloneTileIfNeededForMutator(NetworkTile tile)
    {
        // this is weird right?
        //
        // the combination these features make this needed:
        // - we don't want to clone every tile when we read data in the mutable graph so we use the exiting tiles.
        // - a graph can be written to at all times (to lazy load data) but can be mutated at any time too.
        // 
        // this makes it possible the graph is being written to and mutated at the same time.
        // we need to check, when writing to a graph, a mutator doesn't have the tile in use or
        // data from the write could bleed into the mutator creating an invalid state.
        // so **we have to clone tiles before writing to them and give them to the mutator**
        var mutableGraph = _graphMutator;
        if (mutableGraph != null && !mutableGraph.HasTile(tile.TileId))
        {
            mutableGraph.SetTile(tile.Clone());
        }
    }
}
