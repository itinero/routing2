using System;
using System.Reflection;
using Itinero.Data.Tiles;
using Itinero.LocalGeo;
using Reminiscence.Arrays;

namespace Itinero.Data.Graphs
{
    public sealed class Graph
    {
        private const byte DefaultTileCapacityInBits = 0; 
        
        private readonly int _zoom; // the zoom level.
        private const int CoordinateSizeInBytes = 3; // 3 bytes = 24 bits = 4096 x 4096, the needed resolution depends on the zoom-level, higher, less resolution.
        
        // The tile-index.
        // - tile-id (4 bytes): the local tile-id, maximum possible zoom level 16.
        // - pointer (4 bytes): the pointer to the first vertex in the tile.
        // - capacity (in max bits, 1 byte) : the capacity in # of bytes.
        private readonly ArrayBase<byte> _tiles;
        private uint _tilePointer = 0;

        private readonly ArrayBase<byte> _vertices; // holds the vertex location, encoded relative to a tile.
        private readonly ArrayBase<uint> _edgePointers; // holds edge pointers, points to the first edge for each vertex.
        private readonly ArrayBase<byte> _edges; // holds the actual edges, in a dual linked-list.
        private uint _vertexPointer = 0; // the pointer to the next empty vertex.
        private uint _edgePointer = 0; // the pointer to the next empty edge.

        public Graph(int zoom = 14)
        {
            _zoom = zoom;
            
            _tiles = new MemoryArray<byte>(0);
            _vertices = new MemoryArray<byte>(CoordinateSizeInBytes);
            _edgePointers = new MemoryArray<uint>(1);
            _edges = new MemoryArray<byte>(0);

            for (var p = 0; p < _edgePointers.Length; p++)
            {
                _edgePointers[p] = GraphConstants.TileNotLoaded;
            }
        }

        /// <summary>
        /// Adds a new vertex and returns it's ID.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The ID of the new vertex.</returns>
        public VertexId AddVertex(double longitude, double latitude)
        {
            // get the local tile id.
            var tile = Tile.WorldToTile(longitude, latitude, _zoom);
            var localTileId = tile.LocalId;
            
            // try to find the tile.
            var (vertexPointer, tilePointer, capacity) = FindTile(localTileId);
            if (vertexPointer == GraphConstants.TileNotLoaded)
            {
                (vertexPointer, tilePointer, capacity) = AddTile(localTileId);
            }
            
            // if the tile is at max capacity increase it's capacity.
            long nextEmpty;
            if (_edgePointers[vertexPointer + capacity - 1] != GraphConstants.NoVertex)
            { // increase capacity.
                (vertexPointer, capacity) = IncreaseCapacityForTile(tilePointer, vertexPointer);
                nextEmpty = (vertexPointer + (capacity / 2));
            }
            else
            { // find the next empty slot.
                nextEmpty = (vertexPointer + capacity - 1);
                if (nextEmpty > vertexPointer)
                { // there may be others that are empty.
                    for (var p = nextEmpty - 1; p >= vertexPointer; p--)
                    {
                        if (_edgePointers[p] != GraphConstants.NoVertex)
                        {
                            break;
                        }
                        nextEmpty = p;
                    }
                }
            }
            var localVertexId = (uint)(nextEmpty - vertexPointer);
            
            // set the vertex data.
            _edgePointers[nextEmpty] = GraphConstants.NoEdges;
            SetEncodedVertex((uint)nextEmpty, tile, longitude, latitude);

            return new VertexId()
            {
                TileId = localTileId,
                LocalId = localVertexId
            };
        }

        /// <summary>
        /// Gets the given vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>The vertex.</returns>
        public Coordinate GetVertex(VertexId vertex)
        {
            var localTileId = vertex.TileId;
            
            // try to find the tile.
            var (vertexPointer, tilePointer, capacity) = FindTile(localTileId);
            if (vertexPointer == GraphConstants.TileNotLoaded)
            {
                throw new ArgumentException($"{vertex} does not exist.");
            }

            if (vertex.LocalId >= capacity)
            {
                throw new ArgumentException($"{vertex} does not exist.");
            }

            var tile = Tile.FromLocalId(localTileId, _zoom);
            return GetEncodedVertex(vertexPointer + vertex.LocalId, tile);
        }
        
        private void SetEncodedVertex(uint pointer, Tile tile, double longitude, double latitude)
        {
            // TODO: implement support for a variable resolution.
            const int TileResolutionInBits = CoordinateSizeInBytes * 8 / 2;
            
            var localCoordinates = tile.ToLocalCoordinates(longitude, latitude, 1 << TileResolutionInBits);
            var localCoordinatesEncoded = (localCoordinates.x << TileResolutionInBits) + localCoordinates.y;
            var localCoordinatesBits = BitConverter.GetBytes(localCoordinatesEncoded);
            var vertexPointer = pointer * (long)CoordinateSizeInBytes;
            for (var b = 0; b < CoordinateSizeInBytes; b++)
            {
                _vertices[vertexPointer + b] = localCoordinatesBits[b];
            }
        }

        private Coordinate GetEncodedVertex(uint pointer, Tile tile)
        {
            const int TileResolutionInBits = CoordinateSizeInBytes * 8 / 2;
            var vertexPointer = pointer * (long)CoordinateSizeInBytes;

            var bytes = new byte[4];
            for (var b = 0; b < CoordinateSizeInBytes; b++)
            {
                bytes[b] = _vertices[vertexPointer + b];
            }

            var localCoordinatesEncoded = BitConverter.ToInt32(bytes, 0);
            var y = localCoordinatesEncoded % (1 << TileResolutionInBits);
            var x = localCoordinatesEncoded >> TileResolutionInBits;

            return tile.FromLocalCoordinates(x, y, 1 << TileResolutionInBits);
        }

        private void CopyEncodedVertex(uint pointer1, uint pointer2)
        {
            var vertexPointer1 = pointer1 * CoordinateSizeInBytes;
            var vertexPointer2 = pointer2 * CoordinateSizeInBytes;

            for (var b = 0; b < CoordinateSizeInBytes; b++)
            {
                _vertices[vertexPointer2 + b] = _vertices[vertexPointer1 + b];
            }
        }

        private (uint vertexPointer, uint tilePointer, int capacity) FindTile(uint localTileId)
        {
            // find an allocation-less way of doing this:
            //   this is possible it .NET core 2.1 but not netstandard2.0,
            //   perhaps implement our own version of bitconverter.
            var tileBytes = new byte[4];
            //Span<byte> tileBytes = stackalloc byte[8];
            for (uint p = 0; p < _tiles.Length; p += 9)
            {
                for (var b = 0; b < 4; b++)
                {
                    tileBytes[b] = _tiles[p + b];
                }
                var tileId = BitConverter.ToUInt32(tileBytes, 0);
                if (tileId != localTileId) continue;
                
                for (var b = 0; b < 4; b++)
                {
                    tileBytes[b] = _tiles[p + b + 4];
                }

                return (BitConverter.ToUInt32(tileBytes, 0), p, 1 << _tiles[p + 8]);
            }

            return (GraphConstants.TileNotLoaded, uint.MaxValue, 0);
        }

        private (uint vertexPointer, uint tilePointer, int capacity) AddTile(uint localTileId)
        {
            if (_tilePointer + 9 >= _tiles.Length)
            {
                _tiles.Resize(_tiles.Length + 1024);
            }
            
            var tileBytes = BitConverter.GetBytes(localTileId);
            for (var b = 0; b < 4; b++)
            {
                _tiles[_tilePointer + b] = tileBytes[b];
            }
            tileBytes = BitConverter.GetBytes(_vertexPointer);
            for (var b = 0; b < 4; b++)
            {
                _tiles[_tilePointer + 4 + b] = tileBytes[b];
            }
            _tiles[_tilePointer + 9] = DefaultTileCapacityInBits;
            var tilePointer = _tilePointer;
            _tilePointer += 9;
            const int capacity = 1 << DefaultTileCapacityInBits;
            var pointer = _vertexPointer;
            _vertexPointer += capacity;
            return (pointer, tilePointer, capacity);
        }

        private (uint vertexPointer, int capacity) IncreaseCapacityForTile(uint tilePointer, uint pointer)
        {
            // copy current data, we assume current capacity is at max.
            
            // get current capacity and double it.
            var capacityInBits = _tiles[tilePointer + 8];
            _tiles[tilePointer + 8] = (byte)(capacityInBits + 1);
            var capacity = 1 << capacityInBits;

            // get the current pointer and update it.
            var newVertexPointer = _vertexPointer;
            _vertexPointer += (uint)(capacity * 2);
            var pointerBytes = BitConverter.GetBytes(newVertexPointer); 
            for (var b = 0; b < 4; b++)
            {
                _tiles[tilePointer + 4 + b] = pointerBytes[b];
            }
            
            // make sure edge pointers array and vertex coordinates arrays are the proper sizes.
            var length = _edgePointers.Length;
            while (_vertexPointer + capacity >= length)
            {
                length += 1024;
            }

            if (length != _edgePointers.Length)
            {
                var sizeBefore = _edgePointers.Length;
                _edgePointers.Resize(length);
                for (var p = sizeBefore; p < _edgePointers.Length; p++)
                {
                    _edgePointers[p] = GraphConstants.NoVertex;
                }
                _vertices.Resize(length * CoordinateSizeInBytes);
            }
            
            // copy all the data over.
            for (uint p = 0; p < capacity; p++)
            {
                _edgePointers[newVertexPointer + p] = _edgePointers[pointer + p];
                CopyEncodedVertex(pointer + p, newVertexPointer + p);
            }

            return (newVertexPointer, capacity * 2);
        }
    }
}