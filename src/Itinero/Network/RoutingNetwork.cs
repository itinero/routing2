﻿using System;
using System.Collections.Generic;
using Itinero.Network.DataStructures;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Enumerators.Vertices;
using Itinero.Network.Mutation;
using Itinero.Network.Tiles;
using Itinero.Network.Writer;

namespace Itinero.Network;

public class RoutingNetwork : IEdgeEnumerable, IRoutingNetworkMutable, IRoutingNetworkWritable
{
    private readonly SparseArray<NetworkTile?> _tiles;

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
        if (_tiles.Length <= localTileId)
        {
            return null;
        }

        var tile = _tiles[localTileId];
        if (tile == null)
        {
            return null;
        }

        var edgeTypeMap = RouterDb.GetEdgeTypeMap();
        if (tile.EdgeTypeMapId != edgeTypeMap.id)
        {
            // tile.EdgeTypeMapId indicates the version of the used edgeTypeMap
            // If the id is different, the loaded tile needs updating; e.g. because a cost function has been changed
            tile = tile.CloneForEdgeTypeMap(edgeTypeMap);
            _tiles[localTileId] = tile;
        }

        return tile;
    }

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
        return GetTileForRead(localTileId);
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
        var tile = GetTileForRead(localTileId);
        if (tile == null)
        {
            longitude = default;
            latitude = default;
            elevation = null;
            return false;
        }

        // check if the vertex exists.
        return tile.TryGetVertex(vertex, out longitude, out latitude, out elevation);
    }

    /// <summary>
    /// Gets an edge enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    internal RoutingNetworkEdgeEnumerator GetEdgeEnumerator()
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

    private readonly object _mutatorSync = new();
    private RoutingNetworkMutator? _graphMutator;

    internal RoutingNetworkMutator GetAsMutable()
    {
        lock (_mutatorSync)
        {
            if (_graphMutator != null)
            {
                throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");
            }

            _graphMutator = new RoutingNetworkMutator(this);
            return _graphMutator;
        }
    }

    SparseArray<NetworkTile?> IRoutingNetworkMutable.Tiles => _tiles;

    void IRoutingNetworkMutable.ClearMutator()
    {
        _graphMutator = null;
    }

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
            if (_writer != null)
            {
                throw new InvalidOperationException($"Only one writer is allowed at one time." +
                                                    $"Check {nameof(HasWriter)} to check for a current writer.");
            }

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

        var edgeTypeMap = RouterDb.GetEdgeTypeMap();
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
                CloneTileIfNeededForMutator(tile);
            }

            return (tile, edgeTypeMap.func);
        }

        // create a new tile.
        tile = new NetworkTile(Zoom, localTileId, edgeTypeMap.id);
        _tiles[localTileId] = tile;

        return (tile, edgeTypeMap.func);
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

    void IRoutingNetworkWritable.SetTile(NetworkTile tile)
    {
        var edgeTypeMap = RouterDb.GetEdgeTypeMap();
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
