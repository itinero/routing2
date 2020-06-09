using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Tiles;
using Itinero.IO;

namespace Itinero.Data.Graphs.Serialization
{
    internal class GraphSerializer : IDisposable
    {
        private readonly Graph.MutableGraph _mutableGraph;

        public GraphSerializer(Graph.MutableGraph mutableGraph)
        {
            _mutableGraph = mutableGraph;
        }

        public void Serialize(Stream stream)
        {
            // write version #.
            stream.WriteVarInt32(1);
            
            // write zoom.
            stream.WriteVarInt32(_mutableGraph.Zoom);
            
            // write edge types.
            _mutableGraph.EdgeTypeIndex.Serialize(stream);

            // write tiles.
            stream.WriteVarUInt32((uint)_mutableGraph.GetTiles().Count());
            foreach (var tileId in _mutableGraph.GetTiles())
            {
                var tile = _mutableGraph.GetTile(tileId);

                tile?.Serialize(stream);
            }
        }

        public static Graph Deserialize(Stream stream, Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? edgeTypeFunc = null)
        {
            // check version #.
            var version = stream.ReadVarInt32();
            if (version != 1) throw new InvalidDataException("Unknown version #.");
            
            // read zoom.
            var zoom = stream.ReadVarInt32();
            
            // read edge type index.
            var edgeTypeIndex = GraphEdgeTypeIndex.Deserialize(stream, edgeTypeFunc);

            var tileCount = stream.ReadVarUInt32();
            var tiles = new SparseArray<(GraphTile tile, int edgeTypesId)>(0);
            for (var t = 0; t < tileCount; t++)
            {
                var tile = GraphTile.Deserialize(stream);
                
                tiles.EnsureMinimumSize(tile.TileId);
                tiles[tile.TileId] = (tile, edgeTypeIndex.Id);
            }
            
            return new Graph(tiles, zoom, edgeTypeIndex);
        }

        public void Dispose()
        {
            _mutableGraph.Dispose();
        }
    }
}