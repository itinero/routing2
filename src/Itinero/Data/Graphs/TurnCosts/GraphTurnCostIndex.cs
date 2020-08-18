// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
//
// namespace Itinero.Data.Graphs.TurnCosts
// {
//     internal class GraphTurnCostIndex
//     {
//         public uint Add(IEnumerable<(string name, uint[] costs)> turnCosts)
//         {
//             return uint.MaxValue;
//         }
//
//         public IEnumerable<(string name, uint[] costs)> Get(uint turnCostId)
//         {
//             return Enumerable.Empty<(string name, uint[] costs)>();
//         }
//
//         public GraphTurnCostIndex Next(string name, Func<Network, VertexId, uint[]?> turnCostFunc)
//         {
//             return new GraphTurnCostIndex();
//         }
//
//         internal void Serialize(Stream stream)
//         {
//             
//         }
//
//         internal static GraphTurnCostIndex Deserialize(Stream stream, 
//             IEnumerable<(string name, Func<Network, VertexId, uint[]?> turnCostFunc)>? turnCostFunctions)
//         {
//             return new GraphTurnCostIndex();
//         }
//     }
// }