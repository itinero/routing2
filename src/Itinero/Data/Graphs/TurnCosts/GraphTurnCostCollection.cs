// using System;
// using System.Collections.Generic;
//
// namespace Itinero.Data.Graphs.TurnCosts
// {
//     internal class GraphTurnCostCollection
//     {
//         private readonly List<TurnCostMatrix> _matrices;
//
//         public GraphTurnCostCollection()
//         {
//             _matrices = new List<TurnCostMatrix>();
//         }
//         
//         public uint Count => (uint)_matrices.Count;
//         
//         public TurnCostMatrix GetById(uint turnCostId)
//         {
//             if (turnCostId > this.Count) throw new ArgumentOutOfRangeException(nameof(turnCostId));
//
//             return _matrices[(int)turnCostId];
//         }
//         
//         public uint Get(TurnCostMatrix turnCostMatrix)
//         {
//             // sorts the turn cost matrix.
//             turnCostMatrix.Sort();
//             
//             // check if profile already there.
//             var turnCostId = _matrices.IndexOf(turnCostMatrix);
//             if (turnCostId > 0) return (uint) turnCostId;
//             
//             // add new profile.
//             turnCostId = _matrices.Count;
//             _matrices.Add(turnCostMatrix);
//
//             return (uint) turnCostId;
//         }
//     }
// }