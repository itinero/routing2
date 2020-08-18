using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Collections;
using Itinero.Data.Graphs.EdgeTypes;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Graphs.TurnCosts;
using Itinero.IO;

namespace Itinero.Data.Graphs.Serialization
{
    internal static class GraphSerializer
    { 
        public static void WriteGraph(this Stream stream, Graph.MutableGraph mutableGraph)
        {
            // write version #.
            stream.WriteVarInt32(1);
            
            // write zoom.
            stream.WriteVarInt32(mutableGraph.Zoom);
            
            // write edge types.
            mutableGraph.EdgeTypeIndex.Serialize(stream);

            // write turn cost types.
            mutableGraph.TurnCostTypeIndex.Serialize(stream);
            
            // write tiles.
            stream.WriteVarUInt32((uint)mutableGraph.GetTiles().Count());
            foreach (var tileId in mutableGraph.GetTiles())
            {
                var tile = mutableGraph.GetTile(tileId);

                tile?.Serialize(stream);
            }
        }

        public static Graph ReadGraph(this Stream stream, 
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? edgeTypeFunc = null,
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? turnCostTypeFunc = null)
        {
            // check version #.
            var version = stream.ReadVarInt32();
            if (version != 1) throw new InvalidDataException("Unknown version #.");
            
            // read zoom.
            var zoom = stream.ReadVarInt32();
            
            // read edge type index.
            var edgeTypeIndex = GraphEdgeTypeIndex.Deserialize(stream, edgeTypeFunc);
            
            // read turn cost type index.
            var turnCostTypeIndex = GraphTurnCostTypeIndex.Deserialize(stream, turnCostTypeFunc);

            var tileCount = stream.ReadVarUInt32();
            var tiles = new SparseArray<(GraphTile tile, int edgeTypesId)>(0);
            for (var t = 0; t < tileCount; t++)
            {
                var tile = GraphTile.Deserialize(stream);
                
                tiles.EnsureMinimumSize(tile.TileId);
                tiles[tile.TileId] = (tile, edgeTypeIndex.Id);
            }
            
            return new Graph(tiles, zoom, edgeTypeIndex, turnCostTypeIndex);
        }
    }
}