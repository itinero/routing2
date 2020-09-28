using System;
using System.Collections.Generic;
using Itinero.Network.DataStructures;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Indexes.EdgeTypes;
using Itinero.Network.Indexes.TurnCosts;
using Itinero.Network.Tiles;

namespace Itinero.Network.Mutation
{
    public class RoutingNetworkMutator : IDisposable, IEdgeEnumerable
    {
        private readonly SparseArray<bool> _modified;
        private readonly SparseArray<(NetworkTile? tile, int edgeTypesId)> _tiles;
        private readonly IRoutingNetworkMutable _network;

        internal RoutingNetworkMutator(IRoutingNetworkMutable network)
        {
            _network = network;
            
            _tiles = _network.Tiles.Clone();
            EdgeTypeIndex = _network.GraphEdgeTypeIndex;
            TurnCostTypeIndex = _network.GraphTurnCostTypeIndex;

            _modified = new SparseArray<bool>(_tiles.Length);
        }

        internal bool HasTile(uint localTileId)
        {
            return _tiles.Length > localTileId &&
                   _modified[localTileId] == true;
        }

        internal void SetTile(NetworkTile tile, int edgeTypesId)
        {
            _tiles[tile.TileId] = (tile, edgeTypesId);
            _modified[tile.TileId] = true;
        }

        private NetworkTile? GetTileForRead(uint localTileId)
        {
            // ensure minimum size.
            _tiles.EnsureMinimumSize(localTileId);

            // check if there is already a modified version.
            var (tile, edgeTypesId) = _tiles[localTileId];
            if (tile == null) return null;

            // update the tile if needed.
            if (edgeTypesId == EdgeTypeIndex.Id) return tile;
            tile = EdgeTypeIndex.Update(tile);
            _tiles[localTileId] = (tile, EdgeTypeIndex.Id);
            return tile;
        }

        NetworkTile? IEdgeEnumerable.GetTileForRead(uint localTileId)
        {
            return this.GetTileForRead(localTileId);
        }

        private NetworkTile GetTileForWrite(uint localTileId)
        {
            // ensure minimum size.
            _tiles.EnsureMinimumSize(localTileId);

            // check if there is already a modified version.
            var (tile, edgeTypesId) = _tiles[localTileId];
            if (tile != null)
            {
                if (edgeTypesId == EdgeTypeIndex.Id) return tile;

                tile = EdgeTypeIndex.Update(tile);
                _tiles[localTileId] = (tile, EdgeTypeIndex.Id);
                return tile;
            }

            // there is no tile, create a new one.
            tile = new NetworkTile(_network.Zoom, localTileId);

            // store in the local tiles.
            _tiles[localTileId] = (tile, EdgeTypeIndex.Id);
            return tile;
        }

        public RoutingNetworkMutatorEdgeEnumerator GetEdgeEnumerator()
        {
            return new RoutingNetworkMutatorEdgeEnumerator(this);
        }

        internal IEnumerable<uint> GetTiles()
        {
            foreach (var (i, value) in _tiles)
            {
                if (value.tile == null) continue;

                yield return (uint) i;
            }
        }

        internal NetworkTile? GetTile(uint localTileId)
        {
            return GetTileForRead(localTileId);
        }

        public VertexId AddVertex(double longitude, double latitude)
        {
            // get the local tile id.
            var (x, y) = TileStatic.WorldToTile(longitude, latitude, _network.Zoom);
            var localTileId = TileStatic.ToLocalId(x, y, _network.Zoom);

            // ensure minimum size.
            _tiles.EnsureMinimumSize(localTileId);

            // get the tile (or create it).
            var tile = this.GetTileForWrite(localTileId);

            // add the vertex.
            return tile.AddVertex(longitude, latitude);
        }

        internal bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
        {
            var localTileId = vertex.TileId;

            // get tile.
            if (_tiles.Length <= localTileId)
            {
                longitude = default;
                latitude = default;
                return false;
            }

            var tile = this.GetTileForRead(localTileId);
            if (tile == null)
            {
                longitude = default;
                latitude = default;
                return false;
            }

            // check if the vertex exists.
            return tile.TryGetVertex(vertex, out longitude, out latitude);
        }

        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null)
        {
            var tile = this.GetTileForWrite(vertex1.TileId);
            if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");

            var edgeTypeId = attributes != null ? (uint?) EdgeTypeIndex.Get(attributes) : null;
            var edge1 = tile.AddEdge(vertex1, vertex2, shape, attributes, null, edgeTypeId);
            if (vertex1.TileId != vertex2.TileId)
            {
                // this edge crosses tiles, also add an extra edge to the other tile.
                tile = this.GetTileForWrite(vertex2.TileId);
                tile.AddEdge(vertex1, vertex2, shape, attributes, edge1, edgeTypeId);
            }

            return edge1;
        }

        public void AddTurnCosts(VertexId vertex, IEnumerable<(string key, string value)> attributes,
            EdgeId[] edges, uint[,] costs, IEnumerable<EdgeId>? prefix = null)
        {
            if (prefix != null) throw new NotSupportedException($"Turn costs with {nameof(prefix)} not supported.");

            // get the tile (or create it).
            var tile = this.GetTileForWrite(vertex.TileId);
            if (tile == null) throw new ArgumentException($"Cannot add turn costs to a vertex that doesn't exist.");

            // get the turn cost type id.
            var turnCostTypeId = TurnCostTypeIndex.Get(attributes);

            // add the turn cost table using the type id.
            tile.AddTurnCosts(vertex, turnCostTypeId, edges, costs);
        }

        internal EdgeTypeFunc EdgeTypeFunc => EdgeTypeIndex.Func;

        internal void SetEdgeTypeFunc(EdgeTypeFunc graphEdgeTypeFunc)
        {
            EdgeTypeIndex = EdgeTypeIndex.Next(graphEdgeTypeFunc);
        }

        internal EdgeTypeIndex EdgeTypeIndex { get; private set; }

        internal TurnCostTypeFunc TurnCostTypeFunc => TurnCostTypeIndex.Func;

        internal void SetTurnCostTypeFunc(TurnCostTypeFunc graphTurnCostTypeFunc)
        {
            TurnCostTypeIndex = TurnCostTypeIndex.Next(graphTurnCostTypeFunc);
        }

        internal TurnCostTypeIndex TurnCostTypeIndex { get; private set; }

        internal RoutingNetwork ToRoutingNetwork()
        {
            return new RoutingNetwork(_network.RouterDb, _tiles, _network.Zoom, EdgeTypeIndex, TurnCostTypeIndex);
        }

        public void Dispose()
        {
            _network.ClearMutator();
            
            (_network.RouterDb as IRouterDbMutable).Finish(this.ToRoutingNetwork(), _network.RouterDb.ProfileConfiguration);
        }
    }
}