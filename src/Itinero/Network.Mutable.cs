// using System;
// using System.Collections.Generic;
// using Itinero.Data.Graphs;
//
// namespace Itinero
// {
//     public sealed partial class Network 
//     {
//         private MutableNetwork? _mutableNetwork = null;
//
//         internal MutableNetwork GetAsMutable()
//         {
//             if (_mutableNetwork != null) throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");
//             
//             _mutableNetwork = new MutableNetwork(this);
//             return _mutableNetwork;
//         }
//
//         internal void ClearMutable()
//         {
//             _mutableNetwork = null;
//         }
//     }
// }