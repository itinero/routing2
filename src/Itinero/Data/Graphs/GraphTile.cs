using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Itinero.Data.Tiles;
using Itinero.LocalGeo;
using Reminiscence.Arrays;

namespace Itinero.Data.Graphs
{
    internal partial class GraphTile
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
            
            _pointers = new MemoryArray<uint>(0);
            _edges = new MemoryArray<byte>(0);
            
            
            _coordinates = new MemoryArray<byte>(0);
            _shapes = new MemoryArray<byte>(0);
            _attributes = new MemoryArray<byte>(0);
            _strings = new MemoryArray<string>(0);
        }

        /// <summary>
        /// Gets the tile id.
        /// </summary>
        public uint TileId => _tileId;

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
            
            // make room for edges.
            if (vertexId.LocalId > _pointers.Length) _pointers.Resize(_pointers.Length + 1024);

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
        /// <param name="shape">The shape."</param>
        /// <param name="attributes">The attributes."</param>
        /// <returns>The new edge id.</returns>
        public EdgeId AddEdge(VertexId vertex1, VertexId vertex2, IEnumerable<(double longitude, double latitude)> shape = null,
            IEnumerable<(string key, string value)> attributes = null)
        {
            var edgeId = new EdgeId(_tileId, _nextEdgeId);

            // write the edge data.
            var newEdgePointer = _nextEdgeId;
            var size = EncodeVertex(_nextEdgeId, vertex1);
            _nextEdgeId += size;
            size = EncodeVertex(_nextEdgeId, vertex2);
            _nextEdgeId += size;
            
            // get previous pointers if vertices already has edges
            // set the new pointers.
            // TODO: save the offset pointers, this prevents the need to decode two vertices for every edge.
            // we also need to decode just one next pointer for each edge.
            // we do need to save the first pointer in the global pointers list.
            // we can check if it's the first while adding it again.
            uint? v1p = null;
            if (vertex1.TileId == _tileId)
            {
                v1p = _pointers[vertex1.LocalId].DecodeNullableData();
                _pointers[vertex1.LocalId] = newEdgePointer.EncodeToNullableData();
            }

            uint? v2p = null;
            if (vertex2.TileId == _tileId)
            {
                v2p = _pointers[vertex2.LocalId].DecodeNullableData();
                _pointers[vertex2.LocalId] = newEdgePointer.EncodeToNullableData();
            }

            // set next pointers.
            size = EncodePointer(_nextEdgeId, v1p);
            _nextEdgeId += size;
            size = EncodePointer(_nextEdgeId, v2p);
            _nextEdgeId += size;

            // take care of shape if any.
            uint? shapePointer = null;
            if (shape != null)
            {
                shapePointer = SetShape(shape);
            }
            size = EncodePointer(_nextEdgeId, shapePointer);
            _nextEdgeId += size;

            // take care of attributes if any.
            uint? attributesPointer = null;
            if (attributes != null)
            {
                attributesPointer = SetAttributes(attributes);
            }
            size = EncodePointer(_nextEdgeId, attributesPointer);
            _nextEdgeId += size;

            return edgeId;
        }

        private void SetCoordinate(uint localId, double longitude, double latitude)
        {    
            var tileCoordinatePointer = localId * CoordinateSizeInBytes * 2;
            if (_coordinates.Length <= tileCoordinatePointer + CoordinateSizeInBytes * 2)
            {
                _coordinates.Resize(_coordinates.Length + 1024);
            }

            const int resolution = (1 << TileResolutionInBits) - 1;
            var (x, y) = TileStatic.ToLocalTileCoordinates(_zoom, _tileId, longitude, latitude, resolution);
            _coordinates.SetFixed(tileCoordinatePointer, CoordinateSizeInBytes, x);
            _coordinates.SetFixed(tileCoordinatePointer + CoordinateSizeInBytes, CoordinateSizeInBytes, y);
        }

        private void GetCoordinate(uint localId, out double longitude, out double latitude)
        {
            var tileCoordinatePointer = localId * CoordinateSizeInBytes * 2;
            
            const int resolution = (1 << TileResolutionInBits) - 1;
            _coordinates.GetFixed(tileCoordinatePointer, CoordinateSizeInBytes, out var x);
            _coordinates.GetFixed(tileCoordinatePointer + CoordinateSizeInBytes, CoordinateSizeInBytes, out var y);

            TileStatic.FromLocalTileCoordinates(_zoom, _tileId, x, y, resolution, out longitude, out latitude);
        }

        internal uint VertexEdgePointer(uint vertex)
        {
            return this._pointers[vertex];
        }

        internal uint EncodeVertex(uint location, VertexId vertexId)
        {
            if (_edges.Length <= location + 5)
            {
                _edges.Resize(_edges.Length + 1024);
            }
            
            if (vertexId.TileId == _tileId)
            {
                return (uint)_edges.SetDynamicUInt32(location, vertexId.LocalId);
            }

            var encodedId = (((ulong) vertexId.TileId) << 32) + vertexId.LocalId;
            return (uint) _edges.SetDynamicUInt64(location, encodedId);
        }

        internal uint DecodeVertex(uint location, out uint localId, out uint tileId)
        {
            var size = (uint) _edges.GetDynamicUInt64(location, out var encodedId);
            if (encodedId < uint.MaxValue)
            {
                localId = (uint) encodedId;
                tileId = _tileId;
                return size;
            }

            tileId = (uint) (encodedId << 32);
            localId = (uint) (encodedId - ((ulong)tileId >> 32));
            return size;
        }

        internal uint EncodePointer(uint location, uint? pointer)
        {
            if (_edges.Length <= location + 5)
            {
                _edges.Resize(_edges.Length + 1024);
            }
            return (uint) _edges.SetDynamicUInt32(location, 
                pointer.EncodeAsNullableData());
        }

        internal uint DecodePointer(uint location, out uint? pointer)
        {
            var size = _edges.GetDynamicUInt32(location, out var data);
            pointer = data.DecodeNullableData();
            return (uint)size;
        }
    }
}