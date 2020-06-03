using System;
using System.Collections.Generic;
using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Tiles;

namespace Itinero.Data.Graphs
{
    internal sealed class Graph
    {
        private readonly SparseArray<(GraphTile tile, int edgeTypesId)> _tiles;
        private readonly GraphEdgeTypeIndex _graphEdgeTypeIndex;

        /// <summary>
        /// Creates a new graph.
        /// </summary>
        /// <param name="zoom">The zoom level.</param>
        public Graph(int zoom = 14)
        {
            Zoom = zoom;

            _tiles = new SparseArray<(GraphTile tile, int edgeTypesId)>(0);
            _graphEdgeTypeIndex = new GraphEdgeTypeIndex();
        }

        private Graph(SparseArray<(GraphTile tile, int edgeTypesId)> tiles, int zoom,
            GraphEdgeTypeIndex graphEdgeTypeIndex)
        {
            Zoom = zoom;
            _tiles = tiles;
            _graphEdgeTypeIndex = graphEdgeTypeIndex;
        }

        private GraphTile? GetTileForRead(uint localTileId)
        {
            if (_tiles.Length <= localTileId) return null;
            
            var (tile, edgeTypesId) = _tiles[localTileId];
            if (tile != null &&
                edgeTypesId != _graphEdgeTypeIndex.Id)
            {
                tile = _graphEdgeTypeIndex.Update(tile);
                _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
            }

            return tile;
        }

        private GraphTile GetTileForWrite(uint localTileId)
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
                    this.CloneTileIfNeeded(tile, edgeTypesId);
                }
            }
            
            if (tile == null)
            {
                // create a new tile.
                tile = new GraphTile(this.Zoom, localTileId);
                _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
            }

            return tile;
        }
        
        /// <summary>
        /// Gets the zoom.
        /// </summary>
        public int Zoom { get; }

        /// <summary>
        /// Adds a new vertex and returns its ID.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            // get the local tile id.
            var (x, y) = TileStatic.WorldToTile(longitude, latitude, this.Zoom);
            var localTileId = TileStatic.ToLocalId(x, y, this.Zoom);

            // get the tile (or create it).
            var tile = this.GetTileForWrite(localTileId);

            return tile.AddVertex(longitude, latitude);
        }

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
        /// Adds a new edge.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="shape">The shape points.</param>
        /// <returns>The edge id.</returns>
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2,
            IEnumerable<(double longitude, double latitude)>? shape = null,
            IEnumerable<(string key, string value)>? attributes = null)
        {
            // get the tile (or create it).
            var tile = this.GetTileForWrite(vertex1.TileId);
            if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");
            
            var edge1 = tile.AddEdge(vertex1, vertex2, shape, attributes);
            if (vertex1.TileId == vertex2.TileId) return edge1;
            
            // this edge crosses tiles, also add an extra edge to the other tile.
            tile = this.GetTileForWrite(vertex2.TileId);
            tile.AddEdge(vertex1, vertex2, shape, attributes, edge1);

            return edge1;
        }

        /// <summary>
        /// Gets the attributes for the given edge type.
        /// </summary>
        /// <param name="edgeTypeId">The edge type id.</param>
        /// <returns>The attributes for the given edge type.</returns>
        public IEnumerable<(string key, string value)> GetEdgeType(uint edgeTypeId)
        {
            return _graphEdgeTypeIndex.GetById(edgeTypeId);
        }

        /// <summary>
        /// Gets an edge enumerator.
        /// </summary>
        /// <returns></returns>
        internal Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        internal class Enumerator
        {
            private readonly Graph _graph;
            private readonly GraphTileEnumerator _tileEnumerator;

            internal Enumerator(Graph graph)
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
        }

        private MutableGraph? _mutableGraph = null;

        internal IMutableGraph GetAsMutable()
        {
            if (_mutableGraph != null) throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");
            
            _mutableGraph = new MutableGraph(this);
            return _mutableGraph;
        }

        internal void CloneTileIfNeeded(GraphTile tile, int edgeTypesId)
        {
            var mutableGraph = _mutableGraph;
            if (mutableGraph != null && !mutableGraph.HasTile(tile.TileId)) mutableGraph.SetTile(tile.Clone(), edgeTypesId);
        }

        internal void ClearMutable()
        {
            _mutableGraph = null;
        }
        
        internal class MutableGraph : IMutableGraph
        {
            private readonly SparseArray<bool> _modified;
            private readonly SparseArray<(GraphTile tile, int edgeTypesId)> _tiles;
            private readonly Graph _graph;
            private GraphEdgeTypeIndex _graphEdgeTypeIndex;

            public MutableGraph(Graph graph)
            {
                _graph = graph;
                _tiles = graph._tiles.Clone();
                _graphEdgeTypeIndex = graph._graphEdgeTypeIndex;
                
                _modified = new SparseArray<bool>(_tiles.Length);
            }

            internal bool HasTile(uint localTileId)
            {
                return _tiles.Length > localTileId && 
                       _modified[localTileId] == true;
            }

            internal void SetTile(GraphTile tile, int edgeTypesId)
            {
                _tiles[tile.TileId] = (tile, edgeTypesId);
                _modified[tile.TileId] = true;
            }

            private GraphTile GetTileForWrite(uint localTileId)
            {
                // ensure minimum size.
                _tiles.EnsureMinimumSize(localTileId);
                
                // check if there is already a modified version.
                var (tile, edgeTypesId) = _tiles[localTileId];
                if (tile != null)
                {
                    if (edgeTypesId == _graphEdgeTypeIndex.Id) return tile;
                    
                    tile = _graphEdgeTypeIndex.Update(tile);
                    _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
                    return tile;
                }
                
                // there is no tile, get the one from the graph or create a new one.
                tile = new GraphTile(_graph.Zoom, localTileId);
                
                // store in the local tiles.
                _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
                return tile;
            }

            private GraphTile? GetTileForRead(uint localTileId)
            {
                // ensure minimum size.
                _tiles.EnsureMinimumSize(localTileId);
                
                // check if there is already a modified version.
                var (tile, edgeTypesId) = _tiles[localTileId];
                if (tile == null) return null;
                
                // update the tile if needed.
                if (edgeTypesId == _graphEdgeTypeIndex.Id) return tile;
                tile = _graphEdgeTypeIndex.Update(tile);
                _tiles[localTileId] = (tile, _graphEdgeTypeIndex.Id);
                return tile;
            }

            public VertexId AddVertex(double longitude, double latitude)
            {
                // get the local tile id.
                var (x, y) = TileStatic.WorldToTile(longitude, latitude, _graph.Zoom);
                var localTileId = TileStatic.ToLocalId(x, y, _graph.Zoom);

                // ensure minimum size.
                _tiles.EnsureMinimumSize(localTileId);

                // get the tile (or create it).
                var tile = this.GetTileForWrite(localTileId);
                
                // add the vertex.
                return tile.AddVertex(longitude, latitude);
            }

            public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
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

                var edge1 = tile.AddEdge(vertex1, vertex2, shape, attributes);
                if (vertex1.TileId != vertex2.TileId)
                {
                    // this edge crosses tiles, also add an extra edge to the other tile.
                    tile = this.GetTileForWrite(vertex2.TileId);
                    tile.AddEdge(vertex1, vertex2, shape, attributes, edge1);
                }

                return edge1;
            }

            public void SetEdgeTypeFunc(GraphEdgeTypeFunc graphEdgeTypeFunc)
            {
                _graphEdgeTypeIndex = _graphEdgeTypeIndex.Next(graphEdgeTypeFunc);
            }

            public Graph ToGraph()
            {
                return new Graph(_tiles, _graph.Zoom, _graphEdgeTypeIndex);
            }

            public void Dispose()
            {
                _graph.ClearMutable();
            }
        }
    }
}