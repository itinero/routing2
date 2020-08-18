// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Itinero.Algorithms.Sorting;
//
// namespace Itinero.Data.Graphs.TurnCosts
// {
//     internal static class TurnCostMatrixExtension
//     {
//         internal static void Sort(this TurnCostMatrix matrix)
//         {
//             QuickSort.Sort((i, j) =>
//             {
//                 var rowi = matrix.GetRow((int)i);
//                 var rowj = matrix.GetRow((int)j);
//
//                 return -rowi.CompareTo(rowj);
//             }, (i, j) =>
//             {
//                 matrix.SwitchRow((int)i, (int)j);
//             }, 0, matrix.N - 1);
//             
//             QuickSort.Sort((i, j) =>
//             {
//                 var columni = matrix.GetColumn((int)i);
//                 var columnj = matrix.GetColumn((int)j);
//
//                 return -columni.CompareTo(columnj);
//             }, (i, j) =>
//             {
//                 matrix.SwitchColumn((int)i, (int)j);
//             }, 0, matrix.N - 1);
//         }
//
//         internal static int CompareTo(this IReadOnlyList<uint> l1, IReadOnlyList<uint> l2)
//         {
//             var r = l1.Count.CompareTo(l2.Count);
//             if (r != 0) return r;
//
//             var l1sum = 0U;
//             foreach (var t in l1)
//             {
//                 l1sum += t;
//             }
//
//             var l2sum = 0U;
//             foreach (var t in l2)
//             {
//                 l2sum += t;
//             }
//
//             r = l1sum.CompareTo(l2sum);
//             if (r != 0) return r;
//
//             var l1sorted = new uint[l1.Count];
//             for (var i = 0; i < l1sorted.Length; i++)
//             {
//                 l1sorted[i] = l1[i];
//             }
//             Array.Sort(l1sorted);
//             
//             var l2sorted = new uint[l2.Count];
//             for (var i = 0; i < l2sorted.Length; i++)
//             {
//                 l2sorted[i] = l1[i];
//             }
//             Array.Sort(l2sorted);
//
//             for (var i = 0; i < l1.Count; i++)
//             {
//                 r = l1sorted[i].CompareTo(l2sorted[i]);
//                 if (r != 0) return r;
//             }
//
//             return 0;
//         }
//     }
// }