using System;
using Itinero.Network.Tiles;

namespace Itinero.Network.Indexes.EdgeTypes
{
    internal static class EdgeTypeIndexExtensions
    {
        public static NetworkTile Update(this EdgeTypeIndex graphEdgeTypeIndex, NetworkTile tile)
        {
            if (tile == null) throw new ArgumentNullException(nameof(tile));
            
            return tile.ApplyNewEdgeTypeFunc(graphEdgeTypeIndex);
        }
    }
}