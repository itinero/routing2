using System;
using Itinero.Data.Tiles;
using Reminiscence.Arrays;

namespace Itinero.Data.Graphs
{
    internal class GraphTile
    {
        private const int CoordinateSizeInBytes = 3; // 3 bytes = 24 bits = 4096 x 4096, the needed resolution depends on the zoom-level, higher, less resolution.
        private const int TileResolutionInBits = CoordinateSizeInBytes * 8 / 2;
        private const int TileSizeInIndex = 5; // 4 bytes for the pointer, 1 for the size.
        
        private readonly uint _tileId;
        private readonly int _zoom; // the zoom level.

        // the next vertex id.
        private uint _nextVertexId = 0;
        // the vertex coordinates.
        private readonly ArrayBase<byte> _coordinates;
        // the pointers, per vertex, to their first edge.
        // TODO: investigate if it's worth storing these with less precision, one tile will never contain this much data.
        private readonly ArrayBase<uint> _pointers;
        
        // the next edge id.
        private uint _nextEdgeId = 0;
        // the edges.
        private readonly ArrayBase<byte> _edges;

        /// <summary>
        /// Creates a new tile.
        /// </summary>
        /// <param name="zoom">The zoom level.</param>
        /// <param name="tileId">The tile id.</param>
        public GraphTile(int zoom, uint tileId)
        {
            _zoom = zoom;
            _tileId = tileId;
            
            _coordinates = new MemoryArray<byte>(0);
            _pointers = new MemoryArray<uint>(0);
            
            _edges = new MemoryArray<byte>(0);
        }

        /// <summary>
        /// Gets the number of vertices.
        /// </summary>
        public uint VertexCount => _nextVertexId;

        /// <summary>
        /// Adds a new vertex and returns its id.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            // set coordinate.
            SetCoordinate(_nextVertexId, longitude, latitude);

            // create id.
            var vertexId = new VertexId(_tileId, _nextVertexId);
            _nextVertexId++;

            return vertexId;
        }

        /// <summary>
        /// Gets the vertex with the given id.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>True if the vertex exists.</returns>
        public bool TryGetVertex(VertexId vertex, out double longitude, out double latitude)
        {
            longitude = default;
            latitude = default;
            if (vertex.LocalId >= _nextVertexId) return false;
            
            GetCoordinate(vertex.LocalId, out longitude, out latitude);
            return true;
        }

        /// <summary>
        /// Adds a new edge and returns its id.
        /// </summary>
        /// <param name="vertex1">The first vertex.</param>
        /// <param name="vertex2">The second vertex.</param>
        /// <returns>The new edge id.</returns>
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2)
        {
            var edgeId = new EdgeId(_tileId, _nextEdgeId);

            var size = EncodeVertex(_nextEdgeId, vertex1);
            _nextEdgeId += size;
            size = EncodeVertex(_nextEdgeId, vertex2);
            _nextEdgeId += size;

            return edgeId;
        }

        private void SetCoordinate(uint localId, double longitude, double latitude)
        {    
            var tileCoordinatePointer = _nextVertexId * CoordinateSizeInBytes * 2;
            if (_coordinates.Length <= tileCoordinatePointer + CoordinateSizeInBytes * 2)
            {
                _coordinates.Resize(_coordinates.Length + 1024);
            }

            const int resolution = (1 << TileResolutionInBits) - 1;
            var (x, y) = TileStatic.ToLocalTileCoordinates(_zoom, _tileId, longitude, latitude, resolution);
            _coordinates.SetFixed(tileCoordinatePointer, CoordinateSizeInBytes, x);
            _coordinates.SetFixed(tileCoordinatePointer, CoordinateSizeInBytes, y);
        }

        private void GetCoordinate(uint localId, out double longitude, out double latitude)
        {
            throw new NotImplementedException();
        }

        private uint EncodeVertex(uint pointer, VertexId vertexId)
        {
            if (vertexId.TileId == _tileId)
            {
                return (uint)_edges.SetDynamicUInt32(pointer, vertexId.LocalId);
            }

            var encodedId = (((ulong) vertexId.TileId) << 32) + vertexId.LocalId;
            return (uint) _edges.SetDynamicUInt64(pointer, encodedId);
        }

        internal class EdgeEnumerator
        {
            private readonly GraphTile _graphTile;

            public EdgeEnumerator(GraphTile graphTile)
            {
                _graphTile = graphTile;
            }

            public bool MoveTo(VertexId vertex)
            {
                return false;
            }
        }
    }
}