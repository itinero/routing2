using System;
using System.Net;
using Itinero.Data.Tiles;
using Itinero.LocalGeo;
using Reminiscence.Arrays;

namespace Itinero.Data.Graphs
{
    public sealed class Graph
    {
        private const byte DefaultTileCapacityInBits = 0; 
        
        private readonly int _zoom; // the zoom level.
        private readonly int _tileResolutionInBits = 12; // 12 bytes = 4096, the needed resolution depends on the zoom-level, higher, less resolution.
        
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
        }

        public ulong AddVertex(double longitude, double latitude)
        {
            // get the local tile id.
            var tile = Tile.WorldToTile(longitude, latitude, _zoom);
            var localTileId = tile.LocalId;
            
            // try to find the tile.
            var tilePointer = FindOrAddTile(localTileId);

            return ulong.MaxValue;
        }

        private uint FindOrAddTile(uint localTileId)
        {
            // find an allocation-less way of doing this:
            //   this is possible it .NET core 2.1 but not netstandard2.0,
            //   perhaps implement our own version of bitconverter.
            var tileBytes = new byte[4];
            //Span<byte> tileBytes = stackalloc byte[8];
            for (var p = 0; p < _tiles.Length; p += 8)
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

                return BitConverter.ToUInt32(tileBytes, 0);
            }

            // tile not found yet.
            tileBytes = BitConverter.GetBytes(localTileId);
            for (var b = 0; b < 4; b++)
            {
                _tiles[_tilePointer + b] = tileBytes[b];
            }
            tileBytes = BitConverter.GetBytes(_vertexPointer);
            for (var b = 0; b < 4; b++)
            {
                _tiles[_tilePointer + 4 + b] = tileBytes[b];
            }
            _tiles[_tilePointer + 9] = 0;
            _tilePointer += 9;
            return _tilePointer;
        }
    }
}