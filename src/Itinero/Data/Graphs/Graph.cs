using System.Collections.Generic;
using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Graphs.TurnCosts;

namespace Itinero.Data.Graphs
{
    internal sealed partial class Graph
    {
        private readonly SparseArray<(GraphTile tile, int edgeTypesId)> _tiles;
        private readonly GraphEdgeTypeIndex _graphEdgeTypeIndex;
        private readonly GraphTurnCostTypeIndex _graphTurnCostTypeIndex;

        /// <summary>
        /// Creates a new graph.
        /// </summary>
        /// <param name="zoom">The zoom level.</param>
        public Graph(int zoom = 14)
        {
            Zoom = zoom;

            _tiles = new SparseArray<(GraphTile tile, int edgeTypesId)>(0);
            _graphEdgeTypeIndex = new GraphEdgeTypeIndex();
            _graphTurnCostTypeIndex = new GraphTurnCostTypeIndex();
        }

        internal Graph(SparseArray<(GraphTile tile, int edgeTypesId)> tiles, int zoom,
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

        // /// <summary>
        // /// Gets the attributes for the given edge type.
        // /// </summary>
        // /// <param name="edgeTypeId">The edge type id.</param>
        // /// <returns>The attributes for the given edge type.</returns>
        // public IEnumerable<(string key, string value)> GetEdgeType(uint edgeTypeId)
        // {
        //     return _graphEdgeTypeIndex.GetById(edgeTypeId);
        // }

        /// <summary>
        /// Gets an edge enumerator.
        /// </summary>
        /// <returns></returns>
        internal GraphEdgeEnumerator GetEdgeEnumerator()
        {
            return new GraphEdgeEnumerator(this);
        }

        internal class GraphEdgeEnumerator
        {
            private readonly Graph _graph;
            private readonly GraphTileEnumerator _tileEnumerator;

            internal GraphEdgeEnumerator(Graph graph)
            {
                _graph = graph;

                _tileEnumerator = new GraphTileEnumerator();
            }

            /// <summary>
            /// Moves the enumerator to the first edge of the given vertex.
            /// </summary>
            /// <param name="vertex">The vertex.</param>
            /// <returns>True if the vertex exists.</returns>
            public bool MoveTo(VertexId vertex)
            {
                if (_tileEnumerator.TileId == vertex.TileId) return _tileEnumerator.MoveTo(vertex);

                // move to the tile.
                if (_graph._tiles.Length <= vertex.TileId) return false;
                var tile = _graph.GetTileForRead(vertex.TileId);
                if (tile == null) return false;
                _tileEnumerator.MoveTo(tile);

                return _tileEnumerator.MoveTo(vertex);
            }

            /// <summary>
            /// Moves the enumerator to the given edge. 
            /// </summary>
            /// <param name="edgeId">The edge id.</param>
            /// <param name="forward">The forward flag, when false the enumerator is in a state as it was enumerated to the edge via its last vertex. When true the enumerator is in a state as it was enumerated to the edge via its first vertex.</param>
            public bool MoveToEdge(EdgeId edgeId, bool forward = true)
            {
                if (_tileEnumerator.TileId == edgeId.TileId) return _tileEnumerator.MoveTo(edgeId, forward);

                // move to the tile.
                var tile = _graph.GetTileForRead(edgeId.TileId);
                if (tile == null) return false;
                _tileEnumerator.MoveTo(tile);

                return _tileEnumerator.MoveTo(edgeId, forward);
            }

            /// <summary>
            /// Resets this enumerator.
            /// </summary>
            /// <remarks>
            /// Reset this enumerator to:
            /// - the first vertex for the currently selected edge.
            /// - the first vertex for the graph tile if there is no selected edge.
            /// - returns false if there is no data in the current tile or if there is no tile selected.
            /// </remarks>
            public bool Reset()
            {
                return _tileEnumerator.Reset();
            }

            /// <summary>
            /// Moves this enumerator to the next edge.
            /// </summary>
            /// <returns>True if there is data available.</returns>
            public bool MoveNext()
            {
                return _tileEnumerator.MoveNext();
            }

            /// <summary>
            /// Returns true if the edge is from -> to, false otherwise.
            /// </summary>
            public bool Forward => _tileEnumerator.Forward;

            /// <summary>
            /// Gets the source vertex.
            /// </summary>
            public VertexId From => _tileEnumerator.Vertex1;

            /// <summary>
            /// Gets the target vertex.
            /// </summary>
            public VertexId To => _tileEnumerator.Vertex2;

            /// <summary>
            /// Gets the edge id.
            /// </summary>
            public EdgeId Id => _tileEnumerator.EdgeId;

            /// <summary>
            /// Gets the shape.
            /// </summary>
            /// <returns>The shape.</returns>
            public IEnumerable<(double longitude, double latitude)> Shape => _tileEnumerator.Shape;

            /// <summary>
            /// Gets the attributes.
            /// </summary>
            /// <returns>The attributes.</returns>
            public IEnumerable<(string key, string value)> Attributes => _tileEnumerator.Attributes;

            /// <summary>
            /// Gets the edge profile id.
            /// </summary>
            public uint? EdgeTypeId => _tileEnumerator.EdgeTypeId;
        
            /// <summary>
            /// Gets the length in centimeters, if any.
            /// </summary>
            public uint? Length => _tileEnumerator.Length;

            /// <summary>
            /// Gets the head index.
            /// </summary>
            public byte? Head => _tileEnumerator.Head;

            /// <summary>
            /// Gets the tail index.
            /// </summary>
            public byte? Tail => _tileEnumerator.Tail;

            /// <summary>
            /// Gets the turn cost to the current edge given the from order.
            /// </summary>
            /// <param name="fromOrder">The order of the source edge.</param>
            /// <returns>The turn cost if any.</returns>
            public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo(byte fromOrder) =>
                _tileEnumerator.GetTurnCostTo(fromOrder);

            /// <summary>
            /// Gets the turn cost from the current edge given the to order.
            /// </summary>
            /// <param name="toOrder">The order of the target edge.</param>
            /// <returns>The turn cost if any.</returns>
            public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostFrom(byte toOrder) =>
                _tileEnumerator.GetTurnCostFrom(toOrder);
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