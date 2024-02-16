using System;
using System.Collections.Generic;
using Itinero.Network.DataStructures;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search.Islands;
using Itinero.Network.Tiles;
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Network.Mutation;

/// <summary>
/// A routing network mutator. This mutator can be used to change anything except deleting vertices.
///
/// This mutator can be used to:
/// - add new vertices
/// - add new edges.
/// - delete edges.
/// 
/// </summary>
public class RoutingNetworkMutator : IDisposable, IEdgeEnumerable
{
    private readonly SparseArray<bool> _modified;
    private readonly SparseArray<NetworkTile?> _tiles;
    private readonly IRoutingNetworkMutable _network;
    private readonly RoutingNetworkIslandManager _islandManager;

    internal RoutingNetworkMutator(IRoutingNetworkMutable network)
    {
        _network = network;

        _tiles = _network.Tiles.Clone();
        _modified = new SparseArray<bool>(_tiles.Length);

        _islandManager = network.IslandManager.Clone();
    }

    NetworkTile? IEdgeEnumerable.GetTileForRead(uint localTileId)
    {
        return this.GetTileForRead(localTileId);
    }

    internal bool HasTile(uint localTileId)
    {
        return _tiles.Length > localTileId &&
               _modified[localTileId] == true;
    }

    internal void SetTile(NetworkTile tile)
    {
        _tiles[tile.TileId] = tile;
        _modified[tile.TileId] = true;
    }

    private NetworkTile? GetTileForRead(uint localTileId)
    {
        if (_tiles.Length <= localTileId) return null;


        // get tile, if any.
        var tile = _tiles[localTileId];
        if (tile == null) return null;

        // check edge type map.
        var edgeTypeMap = _network.RouterDb.GetEdgeTypeMap();
        if (tile.EdgeTypeMapId == edgeTypeMap.id) return tile;

        // tile.EdgeTypeMapId indicates the version of the used edgeTypeMap
        // If the id is different, the loaded tile needs updating; e.g. because a cost function has been changed
        tile = tile.CloneForEdgeTypeMap(edgeTypeMap);
        _tiles[localTileId] = tile;
        return tile;
    }

    /// <summary>
    /// Gets the edge associated with the given id.
    /// </summary>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="forward">The forward flag.</param>
    /// <returns>The edge details.</returns>
    public INetworkTileEdge GetEdge(EdgeId edgeId, bool forward)
    {
        var (tile, _) = this.GetTileForWrite(edgeId.TileId);

        var edge = new NetworkTileEnumerator();
        edge.MoveTo(tile);
        edge.MoveTo(edgeId, forward);
        return edge;
    }

    /// <summary>
    /// Gets a tile for writing.
    /// </summary>
    /// <param name="localTileId">The local tile id.</param>
    /// <returns></returns>
    /// <remarks>
    /// This makes sure tiles are first cloned before being written to.
    /// </remarks>
    private (NetworkTile tile, Func<IEnumerable<(string key, string value)>, uint> func) GetTileForWrite(
        uint localTileId)
    {
        var edgeTypeMap = _network.RouterDb.GetEdgeTypeMap();

        // ensure minimum size.
        _tiles.EnsureMinimumSize(localTileId);
        _modified.EnsureMinimumSize(localTileId);

        // check if there is already a modified version.
        var tile = _tiles[localTileId];
        if (tile != null)
        {
            // if edge types map doesn't match, clone and mark as modified.
            if (tile.EdgeTypeMapId != edgeTypeMap.id)
            {
                tile = tile.CloneForEdgeTypeMap(edgeTypeMap);
                _modified[localTileId] = true;
                _tiles[localTileId] = tile;
                return (tile, edgeTypeMap.func);
            }

            // if already modified, just return.
            var modified = _modified[localTileId];
            if (modified) return (tile, edgeTypeMap.func);

            // make sure all tiles being written to are cloned.
            tile = tile.Clone();
            _modified[localTileId] = true;
            _tiles[localTileId] = tile;
            return (tile, edgeTypeMap.func);
        }

        // there is no tile, create a new one.
        tile = new NetworkTile(_network.Zoom, localTileId, edgeTypeMap.id);

        // store in the local tiles.
        _tiles[localTileId] = tile;
        _modified[localTileId] = true;
        return (tile, edgeTypeMap.func);
    }

    /// <summary>
    /// Gets the edge enumerator.
    /// </summary>
    /// <returns></returns>
    public RoutingNetworkMutatorEdgeEnumerator GetEdgeEnumerator()
    {
        return new(this);
    }

    internal IEnumerable<uint> GetTiles()
    {
        foreach (var (i, value) in _tiles)
        {
            if (value == null)
            {
                continue;
            }

            yield return (uint)i;
        }
    }

    internal NetworkTile? GetTile(uint localTileId)
    {
        return this.GetTileForRead(localTileId);
    }

    /// <summary>
    /// The zoom level.
    /// </summary>
    public int Zoom => _network.Zoom;

    /// <summary>
    /// The max island size.
    /// </summary>
    internal RoutingNetworkIslandManager IslandManager => _network.IslandManager;
    
    /// <summary>
    /// Adds a new vertex.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="elevation">The elevation.</param>
    /// <returns>The vertex id.</returns>
    public VertexId AddVertex(double longitude, double latitude, float? elevation = null)
    {
        // get the local tile id.
        var (x, y) = TileStatic.WorldToTile(longitude, latitude, _network.Zoom);
        var localTileId = TileStatic.ToLocalId(x, y, _network.Zoom);

        // ensure minimum size.
        _tiles.EnsureMinimumSize(localTileId);

        // get the tile (or create it).
        var (tile, _) = this.GetTileForWrite(localTileId);

        // add the vertex.
        return tile.AddVertex(longitude, latitude, elevation);
    }

    internal bool TryGetVertex(VertexId vertex, out double longitude, out double latitude, out float? e)
    {
        var localTileId = vertex.TileId;

        // get tile.
        if (_tiles.Length <= localTileId)
        {
            longitude = default;
            latitude = default;
            e = null;
            return false;
        }

        var tile = this.GetTileForRead(localTileId);
        if (tile != null) return tile.TryGetVertex(vertex, out longitude, out latitude, out e);

        // no tile, no vertex.
        longitude = default;
        latitude = default;
        e = null;
        return false;
    }

    /// <summary>
    /// Adds a new edge.
    /// </summary>
    /// <param name="tail">The tail vertex.</param>
    /// <param name="head">The head vertex.</param>
    /// <param name="shape">The shape points, if any.</param>
    /// <param name="attributes">The attributes, if any.</param>
    /// <returns>An edge id.</returns>
    /// <exception cref="ArgumentException"></exception>
    public EdgeId AddEdge(VertexId tail, VertexId head,
        IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
        IEnumerable<(string key, string value)>? attributes = null)
    {
        var (tile, edgeTypeFunc) = this.GetTileForWrite(tail.TileId);
        if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");

        var edgeTypeId = attributes != null ? (uint?)edgeTypeFunc(attributes) : null;
        var edge1 = tile.AddEdge(tail, head, shape, attributes, null, edgeTypeId);
        if (tail.TileId == head.TileId) return edge1;

        // this edge crosses tiles, also add an extra edge to the other tile.
        (tile, _) = this.GetTileForWrite(head.TileId);
        tile.AddEdge(tail, head, shape, attributes, edge1, edgeTypeId);

        return edge1;
    }

    /// <summary>
    /// Deletes the given edge.
    /// </summary>
    /// <param name="edgeId">The edge id.</param>
    /// <returns>True if the edge was found and deleted, false otherwise.</returns>
    public bool DeleteEdge(EdgeId edgeId)
    {
        var edgeEnumerator = this.GetEdgeEnumerator();
        if (!edgeEnumerator.MoveTo(edgeId)) return false;

        var vertex1 = edgeEnumerator.Tail;
        var vertex2 = edgeEnumerator.Head;

        var (tile, _) = this.GetTileForWrite(vertex1.TileId);
        if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");

        tile.DeleteEdge(edgeId);

        if (vertex1.TileId == vertex2.TileId) return true;

        // vertex2 is in different tile, delete edge from both.
        (tile, _) = this.GetTileForWrite(vertex2.TileId);
        tile.DeleteEdge(edgeId);

        return true;
    }

    /// <summary>
    /// Adds turn costs.
    /// </summary>
    /// <param name="vertex">The vertex.</param>
    /// <param name="attributes">The attributes.</param>
    /// <param name="edges">The edges.</param>
    /// <param name="costs">The costs as a matrix.</param>
    /// <param name="prefix">A path prefix, if any.</param>
    /// <exception cref="ArgumentException"></exception>
    public void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
        EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
    {
        prefix ??= ArraySegment<EdgeId>.Empty;

        // get the tile (or create it).
        var (tile, _) = this.GetTileForWrite(vertex.TileId);
        if (tile == null)
        {
            throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.");
        }

        // get the turn cost type id.
        var turnCostFunc = _network.RouterDb.GetTurnCostTypeMap();
        var turnCostTypeId = turnCostFunc.func(attributes);

        // add the turn cost table using the type id.
        tile.AddTurnCosts(vertex, turnCostTypeId, edges, costs, attributes, prefix);
    }

    /// <summary>
    /// Removes all data.
    /// </summary>
    public void Clear()
    {
        _tiles.Resize(0);
        _modified.Resize(0);
    }

    internal RoutingNetwork ToRoutingNetwork()
    {
        foreach (var tile in _tiles)
        {
            if (tile.value is { HasDeletedEdges: true })
            {
                // at this point edge ids change but it right when the mutator is disposed.
                // for a user perspective this is not that strange.
                tile.value.RemoveDeletedEdges();
            }
        }

        // create the new network.
        var routingNetwork = new RoutingNetwork(_network.RouterDb, _tiles, _network.Zoom, _islandManager);

        // check if listeners need to be cloned/copied over.
        foreach (var listener in _network.UsageNotifier.RegisteredListeners)
        {
            var clonedListener = listener.CloneForNewNetwork(routingNetwork);
            if (clonedListener == null) continue;

            routingNetwork.UsageNotifier.AddListener(clonedListener);
        }

        return routingNetwork;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _network.ClearMutator();

        (_network.RouterDb as IRouterDbMutable).Finish(this.ToRoutingNetwork());
    }
}
