using System;
using System.Collections.Generic;
using Itinero.Collections;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Tiles;

namespace Itinero.Data.Graphs
{
    public sealed class Graph
    {
        // The tile pointers index, a sparse array containing pointer t
        private readonly SparseArray<GraphTile> _tiles;

        /// <summary>
        /// Creates a new graph.
        /// </summary>
        /// <param name="zoom">The zoom level.</param>
        public Graph(int zoom = 14)
        {
            Zoom = zoom;

            _tiles = new SparseArray<GraphTile>(0);
        }

        // TODO: make this internal because it exposes the internal data structure of the graph.
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
            
            // ensure minimum size.
            _tiles.EnsureMinimumSize(localTileId);
            
            // get the tile (or create it).
            var tile = _tiles[localTileId];
            if (tile == null)
            {
                tile = new GraphTile(this.Zoom, localTileId);
                _tiles[localTileId] = tile;
            }

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
            if (_tiles.Length <= localTileId)
            {
                longitude = default;
                latitude = default;
                return false;
            }
            var tile = _tiles[localTileId];
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
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2, IEnumerable<(double longitude, double latitude)>? shape = null, 
            IEnumerable<(string key, string value)>? attributes = null)
        {
            var tile = _tiles[vertex1.TileId];
            if (tile == null) throw new ArgumentException($"Cannot add edge with a vertex that doesn't exist.");
            
            var edge1 = tile.AddEdge(vertex1, vertex2, shape, attributes);
            if (vertex1.TileId != vertex2.TileId)
            {
                // this edge crosses tiles, also add an extra edge to the other tile.
                tile = _tiles[vertex2.TileId];
                tile.AddEdge(vertex1, vertex2, shape, attributes, edge1);
            }
            
            return edge1;
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
                var tile = _graph._tiles[vertex.TileId];
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
                var tile = _graph._tiles[edgeId.TileId];
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
    }
}