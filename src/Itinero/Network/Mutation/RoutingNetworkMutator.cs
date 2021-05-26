using System;
using System.Collections.Generic;
using Itinero.Network.DataStructures;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;

namespace Itinero.Network.Mutation
{
    public class RoutingNetworkMutator : IDisposable, IEdgeEnumerable
    {
        private readonly SparseArray<bool> _modified;
        private readonly SparseArray<NetworkTile?> _tiles;
        private readonly IRoutingNetworkMutable _network;

        internal RoutingNetworkMutator(IRoutingNetworkMutable network)
        {
            _network = network;

            _tiles = _network.Tiles.Clone();
            _modified = new SparseArray<bool>(_tiles.Length);
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
            var edgeTypeMap = _network.RouterDb.GetEdgeTypeMap();

            // ensure minimum size.
            _tiles.EnsureMinimumSize(localTileId);

            // check if there is already a modified version.
            var tile = _tiles[localTileId];
            if (tile == null) {
                return null;
            }

            // update the tile if needed.
            if (tile.EdgeTypeMapId == edgeTypeMap.id) {
                return tile;
            }

            _tiles[localTileId] = tile.CloneForEdgeTypeMap(edgeTypeMap);
            return tile;
        }

        NetworkTile? IEdgeEnumerable.GetTileForRead(uint localTileId)
        {
            return GetTileForRead(localTileId);
        }

        private (NetworkTile tile, Func<IEnumerable<(string key, string value)>, uint> func) GetTileForWrite(
            uint localTileId)
        {
            var edgeTypeMap = _network.RouterDb.GetEdgeTypeMap();

            // ensure minimum size.
            _tiles.EnsureMinimumSize(localTileId);

            // check if there is already a modified version.
            var tile = _tiles[localTileId];
            if (tile != null) {
                if (tile.EdgeTypeMapId == edgeTypeMap.id) {
                    return (tile, edgeTypeMap.func);
                }

                tile = tile.CloneForEdgeTypeMap(edgeTypeMap);
                _tiles[localTileId] = tile;
                return (tile, edgeTypeMap.func);
            }

            // there is no tile, create a new one.
            tile = new NetworkTile(_network.Zoom, localTileId, edgeTypeMap.id);

            // store in the local tiles.
            _tiles[localTileId] = tile;
            return (tile, edgeTypeMap.func);
        }

        public RoutingNetworkMutatorEdgeEnumerator GetEdgeEnumerator()
        {
            return new(this);
        }

        internal IEnumerable<uint> GetTiles()
        {
            foreach (var (i, value) in _tiles) {
                if (value == null) {
                    continue;
                }

                yield return (uint) i;
            }
        }

        internal NetworkTile? GetTile(uint localTileId)
        {
            return GetTileForRead(localTileId);
        }

        public int Zoom => _network.Zoom;

        public VertexId AddVertex(double longitude, double latitude, float? e = null)
        {
            // get the local tile id.
            var (x, y) = TileStatic.WorldToTile(longitude, latitude, _network.Zoom);
            var localTileId = TileStatic.ToLocalId(x, y, _network.Zoom);

            // ensure minimum size.
            _tiles.EnsureMinimumSize(localTileId);

            // get the tile (or create it).
            var (tile, _) = GetTileForWrite(localTileId);

            // add the vertex.
            return tile.AddVertex(longitude, latitude, e);
        }

        internal bool TryGetVertex(VertexId vertex, out double longitude, out double latitude, out float? e)
        {
            var localTileId = vertex.TileId;

            // get tile.
            if (_tiles.Length <= localTileId) {
                longitude = default;
                latitude = default;
                e = null;
                return false;
            }

            var tile = GetTileForRead(localTileId);
            if (tile == null) {
                longitude = default;
                latitude = default;
                e = null;
                return false;
            }

            // check if the vertex exists.
            return tile.TryGetVertex(vertex, out longitude, out latitude, out e);
        }

        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude, float? e)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null)
        {
            var (tile, edgeTypeFunc) = GetTileForWrite(vertex1.TileId);
            if (tile == null) {
                throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");
            }

            var edgeTypeId = attributes != null ? (uint?) edgeTypeFunc(attributes) : null;
            var edge1 = tile.AddEdge(vertex1, vertex2, shape, attributes, null, edgeTypeId);
            if (vertex1.TileId != vertex2.TileId) {
                // this edge crosses tiles, also add an extra edge to the other tile.
                (tile, edgeTypeFunc) = GetTileForWrite(vertex2.TileId);
                edgeTypeId = attributes != null ? (uint?) edgeTypeFunc(attributes) : null;
                tile.AddEdge(vertex1, vertex2, shape, attributes, edge1, edgeTypeId);
            }

            return edge1;
        }

        public void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
        {
            if (prefix != null) {
                throw new NotSupportedException($"Turn costs with {nameof(prefix)} not supported.");
            }

            // get the tile (or create it).
            var (tile, _) = GetTileForWrite(vertex.TileId);
            if (tile == null) {
                throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.");
            }

            // get the turn cost type id.
            var turnCostFunc = _network.RouterDb.GetTurnCostTypeMap();
            var turnCostTypeId = turnCostFunc.func(attributes);

            // add the turn cost table using the type id.
            tile.AddTurnCosts(vertex, turnCostTypeId, edges, costs);
        }

        internal RoutingNetwork ToRoutingNetwork()
        {
            return new(_network.RouterDb, _tiles, _network.Zoom);
        }

        public void Dispose()
        {
            _network.ClearMutator();

            (_network.RouterDb as IRouterDbMutable).Finish(ToRoutingNetwork());
        }
    }
}