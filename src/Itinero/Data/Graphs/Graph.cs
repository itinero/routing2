using System.Collections.Generic;
using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Graphs.TurnCosts;

namespace Itinero.Data.Graphs
{
    internal sealed partial class Graph
    {
        private readonly SparseArray<(GraphTile? tile, int edgeTypesId)> _tiles;
        private readonly GraphEdgeTypeIndex _graphEdgeTypeIndex;
        private readonly GraphTurnCostTypeIndex _graphTurnCostTypeIndex;

        /// <summary>
        /// Creates a new graph.
        /// </summary>
        /// <param name="zoom">The zoom level.</param>
        public Graph(int zoom = 14)
        {
            Zoom = zoom;

            _tiles = new SparseArray<(GraphTile? tile, int edgeTypesId)>(0);
            _graphEdgeTypeIndex = new GraphEdgeTypeIndex();
            _graphTurnCostTypeIndex = new GraphTurnCostTypeIndex();
        }

        internal Graph(SparseArray<(GraphTile? tile, int edgeTypesId)> tiles, int zoom,
            GraphEdgeTypeIndex graphEdgeTypeIndex, GraphTurnCostTypeIndex graphTurnCostTypeIndex)
        {
            Zoom = zoom;
            _tiles = tiles;
            _graphEdgeTypeIndex = graphEdgeTypeIndex;
            _graphTurnCostTypeIndex = graphTurnCostTypeIndex;
        }

        private GraphTile? GetTileForRead(uint localTileId)
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

        /// <summary>
        /// Gets the zoom.
        /// </summary>
        public int Zoom { get; }

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
        internal GraphEdgeEnumerator GetEdgeEnumerator()
        {
            return new GraphEdgeEnumerator(this.GetTileForRead);
        }
        
        /// <summary>
        /// Gets a vertex enumerator.
        /// </summary>
        /// <returns></returns>
        internal GraphVertexEnumerator GetVertexEnumerator()
        {
            return new GraphVertexEnumerator(this);
        }

        internal class GraphVertexEnumerator
        {
            private readonly IEnumerator<(long tileId, (GraphTile tile, int edgeTypesId) tilePair)> _tileEnumerator;

            public GraphVertexEnumerator(Graph graph)
            {
                _tileEnumerator = graph._tiles.GetEnumerator();
                
                this.Current = VertexId.Empty;
            }

            private long _localId = -1;
            private uint _tileId = uint.MaxValue;

            private bool MoveNexTile()
            {
                while (true)
                {
                    if (!_tileEnumerator.MoveNext()) return false;

                    if (_tileEnumerator.Current.tilePair.tile != null) return true;
                }
            }
            
            public bool MoveNext()
            {
                if (this.Current.IsEmpty())
                {
                    if (!this.MoveNexTile()) return false;
                    _tileId = (uint)_tileEnumerator.Current.tileId;
                }

                var current = _tileEnumerator.Current;
                _localId++;
                this.Current = new VertexId(_tileId, (uint)_localId);
                if (!current.tilePair.tile.TryGetVertex(this.Current, out _, out _))
                {
                    if (!this.MoveNexTile()) return false;
                    _localId = -1;
                    _tileId = (uint)_tileEnumerator.Current.tileId;
                    return this.MoveNext();
                }

                return true;
            }
            
            public VertexId Current { get; private set; }

            public void Reset()
            {
                this.Current = VertexId.Empty;
                
                _tileEnumerator.Reset();
            }
        }
    }
}