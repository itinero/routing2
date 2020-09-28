using System;
using System.Collections.Generic;
using Itinero.Network.DataStructures;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Indexes.EdgeTypes;
using Itinero.Network.Indexes.TurnCosts;
using Itinero.Network.Mutation;
using Itinero.Network.Tiles;
using Itinero.Network.Writer;

namespace Itinero.Network
{
    public class RoutingNetwork : IEdgeEnumerable, IRoutingNetworkMutable, IRoutingNetworkWritable
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
        
        private readonly object _writeSync = new object();
        private RoutingNetworkWriter? _writer;
        
        /// <summary>
        /// Returns true if there is already a writer.
        /// </summary>
        internal bool HasWriter => _writer != null;
        
        /// <summary>
        /// Gets a writer.
        /// </summary>
        /// <returns>The writer.</returns>
        public RoutingNetworkWriter GetWriter()
        {
            lock (_writeSync)
            {
                if (_writer != null)
                    throw new InvalidOperationException($"Only one writer is allowed at one time." +
                                                        $"Check {nameof(HasWriter)} to check for a current writer.");
                _writer = new RoutingNetworkWriter(this);
                return _writer;
            }
        }
        
        void IRoutingNetworkWritable.ClearWriter()
        {
            _writer = null;
        }

        TurnCostTypeIndex IRoutingNetworkWritable.GraphTurnCostTypeIndex => _graphTurnCostTypeIndex;
        
        NetworkTile IRoutingNetworkWritable.GetTileForWrite(uint localTileId)
        {
            // ensure minimum size.
            _tiles.EnsureMinimumSize(localTileId);
            
            var (tile, edgeTypesId) = _tiles[localTileId];
            if (tile != null)
            {
                if (edgeTypesId != _graphEdgeTypeIndex.Id)
                {
                    tile = _graphEdgeTypeIndex.Update(tile);
                    _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
                }
                else
                {
                    // check if there is a mutable graph.
                    this.CloneTileIfNeededForMutator(tile, edgeTypesId);
                }
            }
            
            if (tile == null)
            {
                // create a new tile.
                tile = new NetworkTile(this.Zoom, localTileId);
                _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
            }

            return tile;
        }

        private void CloneTileIfNeededForMutator(NetworkTile tile, int edgeTypesId)
        {
            // this is weird right?
            //
            // the combination these features make this needed:
            // - we don't want to clone every tile when we read data in the mutable graph so we use the exiting tiles.
            // - a graph can be written to at all times (to lazy load data) but can be mutated at any time too.
            // 
            // this makes it possible the graph is being written to and mutated at the same time.
            // we need to check, when writing to a graph, a mutator doesn't have the tile in use or
            // date from the write could bleed into the mutator creating an invalid state.
            // so **we have to clone tiles before writing to them and give them to the mutator**
            var mutableGraph = _graphMutator;
            if (mutableGraph != null && !mutableGraph.HasTile(tile.TileId)) mutableGraph.SetTile(tile.Clone(), edgeTypesId);
        }
    }
}