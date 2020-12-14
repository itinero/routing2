// using System;
// using System.Collections.Generic;
// using Itinero.Network.Tiles;
//
// namespace Itinero.Network.Indexes.EdgeTypes
// {
//     internal static class EdgeTypeIndexExtensions
//     {
//         public static NetworkTile Update(this NetworkTile tile, Func<IEnumerable<(string key, string value)>, uint> edgeTypeFunc)
//         {
//             if (tile == null) throw new ArgumentNullException(nameof(tile));
//             
//             return tile.ApplyNewEdgeTypeFunc(edgeTypeFunc);
//         }
//     }
// }