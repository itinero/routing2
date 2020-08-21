using System;
using Itinero.Data.Graphs.Tiles;

namespace Itinero.Data.Graphs.EdgeTypes
{
    internal static class GraphEdgeTypeIndexExtensions
    {
        public static GraphTile Update(this GraphEdgeTypeIndex graphEdgeTypeIndex, GraphTile tile)
        {
            if (tile == null) throw new ArgumentNullException(nameof(tile));
            
            return tile.ApplyNewEdgeTypeFunc(graphEdgeTypeIndex);
        }
    }
}