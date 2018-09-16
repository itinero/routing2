//using System;
//using Reminiscence.Arrays;
//
//namespace Itinero.Data.Graphs
//{
//    public sealed class Graph
//    {
//        private const int DefaultTileSize = 1024;
//        
//        // pointers/capacity pairs for each tile:
//        // - pointer: points to the first vertex in each tile.
//        // - capacity: the space available for vertices in each tile.
//        // - size: the # of vertices in the tile.
//        private readonly uint[] _tiles;
//
//        private const int TilePointerOffset = 0;
//        private const int TileCapacityOffset = 1;
//        private const int TileSizeOffset = 2;
//        private const int TileIndexSize = 3;
//        private const int TileCoordinateResolution = 8; // tile coordinate resolution in bits.
//        
//        // 3bytes for both lat and lon gives 4096 resolution per tile.
//        
//        // the zoom-level.
//        private readonly int _zoom;
//
//        // contains the location per vertex offset per tile.
//        private readonly ArrayBase<byte> _vertices;
//
//        // pointers to the edges.
//        //private readonly ArrayBase<uint> _edgePointers;
//        //private readonly ArrayBase<byte> _edges;
//
//        public Graph(int zoom = 15)
//        {
//            _zoom = zoom;
//            var count = 2 >> zoom;
//            _tiles = new uint[count];
//        }
//
//        private uint _nextTilePointer = 0;
//
//        /// <summary>
//        /// Adds a new vertex.
//        /// </summary>
//        /// <param name="longitude">The longitude.</param>
//        /// <param name="latitude">The latitude.</param>
//        /// <returns>The new ID of the vertex.</returns>
//        public ulong AddVertex(double longitude, double latitude)
//        {
//            // get the tile id and pointer.
//            var tileId = TileCalculator.WorldToTileIndex(latitude, longitude, (uint)_zoom);
//            var tileIndexPointer = tileId * TileIndexSize;
//
//            // make sure the tile exists.
//            var tilePointer = _tiles[tileIndexPointer + TilePointerOffset];
//            if (tilePointer == GraphConstants.TileNotLoaded)
//            { // TODO: do we load tiles on demand, if so then this should fail.
//                throw new Exception($"Cannot add data to a tile that has no been loaded yet.");
//            }
//            if (tilePointer == GraphConstants.TileEmpty)
//            { // start a new tile, with some default size.
//                _tiles[tileIndexPointer + TilePointerOffset] = _nextTilePointer;
//                _tiles[tileIndexPointer + TileCapacityOffset] = DefaultTileSize;
//                _tiles[tileIndexPointer + TileSizeOffset] = DefaultTileSize;
//            }
//            var tileCapacity = _tiles[tileId * TileIndexSize + TileCapacityOffset];
//            var tileSize = _tiles[tileId * TileIndexSize + TileSizeOffset];
//            
//            // check if the tile is big enough, if not move it.
//            if (tileCapacity == tileSize)
//            { // tile is at max capacity, double the capacity and add to the end.
//                var newTileCapacity = tileCapacity * 2;
//                var newTilePointer = _nextTilePointer;
//                for (var v = 0; v < tileCapacity; v++)
//                {
//                    var newPointer = v + _nextTilePointer;
//                    var pointer = v + tilePointer;
//                    _coordinates[newPointer * 2 + 0] = _coordinates[pointer * 2 + 0];
//                    _coordinates[newPointer * 2 + 1] = _coordinates[pointer * 2 + 1];
//                    _edgePointers[newPointer] = _edgePointers[pointer];
//                }
//                _tiles[tileIndexPointer + TilePointerOffset] = _nextTilePointer;
//                _tiles[tileIndexPointer + TileCapacityOffset] = newTileCapacity;
//                _tiles[tileIndexPointer + TileSizeOffset] = tileSize;
//
//                tilePointer = newTilePointer;
//                tileCapacity = newTileCapacity;
//            }
//            
//            // add the vertex to the end.
//            var nextVertexPointer = tilePointer + tileSize;
//            var coordiatePointer = nextVertexPointer * 2;
//            _coordinates[coordiatePointer + 0] = latitude;
//            _coordinates[coordiatePointer + 1] = longitude;
//            _edgePointers[nextVertexPointer] = GraphConstants.NoEdges;
//            tileSize++;
//            
//            // update the size.
//            _tiles[tileIndexPointer + TileSizeOffset] = tileSize;
//
//            return (tileId >> 32) + (tileSize);
//        }
//    }
//}