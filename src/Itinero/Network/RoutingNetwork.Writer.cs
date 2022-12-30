using System;
using System.Collections.Generic;
using Itinero.Network.DataStructures;
using Itinero.Network.Tiles;
using Itinero.Network.Writer;

namespace Itinero.Network;

public sealed partial class RoutingNetwork
{
    private readonly object _writeSync = new();
    private RoutingNetworkWriter? _writer;

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

            _writer = new RoutingNetworkWriter(this);
            return _writer;
        }
    }

    void IRoutingNetworkWritable.ClearWriter()
    {
        _writer = null;
    }

    (NetworkTile tile, Func<IEnumerable<(string key, string value)>, uint> func) IRoutingNetworkWritable.
        GetTileForWrite(uint localTileId)
    {
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
