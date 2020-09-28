using System;
using System.Collections.Generic;
using Itinero.Network.DataStructures;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Indexes.EdgeTypes;
using Itinero.Network.Indexes.TurnCosts;
using Itinero.Network.Mutation;
using Itinero.Network.Tiles;

namespace Itinero.Network
{
    public class RoutingNetwork : IEdgeEnumerable, IRoutingNetworkMutable
    {
        private readonly SparseArray<(NetworkTile? tile, int edgeTypesId)> _tiles;
        private readonly EdgeTypeIndex _graphEdgeTypeIndex;
        private readonly TurnCostTypeIndex _graphTurnCostTypeIndex;
        
        public RoutingNetwork(RouterDb routerDb, int zoom = 14)
        {
            Zoom = zoom;
            RouterDb = routerDb;

            _tiles = new SparseArray<(NetworkTile? tile, int edgeTypesId)>(0);
            _graphEdgeTypeIndex = new EdgeTypeIndex();
            _graphTurnCostTypeIndex = new TurnCostTypeIndex();
        }

        internal RoutingNetwork(RouterDb routerDb, SparseArray<(NetworkTile? tile, int edgeTypesId)> tiles, int zoom,
            EdgeTypeIndex graphEdgeTypeIndex, TurnCostTypeIndex graphTurnCostTypeIndex)
        {
            Zoom = zoom;
            RouterDb = routerDb;
            _tiles = tiles;
            _graphEdgeTypeIndex = graphEdgeTypeIndex;
            _graphTurnCostTypeIndex = graphTurnCostTypeIndex;
        }

        internal NetworkTile? GetTileForRead(uint localTileId)
        {
            if (_tiles.Length <= localTileId) return null;
            
            var (tile, edgeTypesId) = _tiles[localTileId];
            if (tile == null) return null;
            if (edgeTypesId != _graphEdgeTypeIndex.Id)
            {
                tile = _graphEdgeTypeIndex.Update(tile);
                _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
            }
        
            return tile;
        }

        NetworkTile? IEdgeEnumerable.GetTileForRead(uint localTileId)
        {
            return this.GetTileForRead(localTileId);
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
        /// <returns>The vertex.</returns>
        public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
        {
            var localTileId = vertex.TileId;

            // get tile.
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

        /// <summary>
        /// Gets the attributes for the given turn cost type.
        /// </summary>
        /// <param name="turnCostType">The turn cost type.</param>
        /// <returns>The attributes for the given edge type.</returns>
        public IEnumerable<(string key, string value)> GetTurnCostTypeAttributes(uint turnCostType)
        {
            return _graphTurnCostTypeIndex.GetById(turnCostType);
        }

        /// <summary>
        /// Gets an edge enumerator.
        /// </summary>
        /// <returns></returns>
        internal RoutingNetworkEdgeEnumerator GetEdgeEnumerator()
        {
            return new RoutingNetworkEdgeEnumerator(this);
        }
        
        private readonly object _mutatorSync = new object();
        private RoutingNetworkMutator? _graphMutator;
        
        internal RoutingNetworkMutator GetAsMutable()
        {
            lock (_mutatorSync)
            {
                if (_graphMutator != null)
                    throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");

                _graphMutator = new RoutingNetworkMutator(this);
                return _graphMutator;
            }
        }

        SparseArray<(NetworkTile? tile, int edgeTypesId)> IRoutingNetworkMutable.Tiles => _tiles;

        TurnCostTypeIndex IRoutingNetworkMutable.GraphTurnCostTypeIndex => _graphTurnCostTypeIndex;

        EdgeTypeIndex IRoutingNetworkMutable.GraphEdgeTypeIndex => _graphEdgeTypeIndex;

        void IRoutingNetworkMutable.ClearMutator()
        {
            _graphMutator = null;
        }
    }
}