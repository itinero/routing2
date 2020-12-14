// using System;
// using System.Collections.Generic;
// using System.IO;
// using Itinero.IO;
//
// namespace Itinero.Network.Indexes.TurnCosts
// {
//      /// <summary>
//     /// A turn cost type index.
//     ///
//     /// This has a:
//     /// - A function to determine turn cost type, maps attributes -> turn cost type attributes.
//     /// - A turn cost type collection that contains all unique sets of turn cost type attributes.
//     /// </summary>
//     internal class TurnCostTypeIndex
//     {
//         private readonly TurnCostTypeFunc _func;
//         private readonly TurnCostTypeCollection _turnCostTypes;
//
//         internal TurnCostTypeIndex()
//         {
//             _func = new TurnCostTypeFunc();
//             _turnCostTypes = new TurnCostTypeCollection();
//         }
//
//         internal TurnCostTypeIndex(TurnCostTypeCollection turnCostTypes, TurnCostTypeFunc func)
//         {
//             _turnCostTypes = turnCostTypes;
//             _func = func;
//         }
//
//         public int Id => _func.Id;
//
//         public TurnCostTypeFunc Func => _func;
//
//         internal TurnCostTypeCollection TurnCostTypeCollection => _turnCostTypes;
//
//         public uint Get(IEnumerable<(string key, string value)> attributes)
//         {
//             var edgeType = _func.ToEdgeType(attributes);
//             return _turnCostTypes.Get(edgeType);
//         }
//         
//         public IEnumerable<(string key, string value)> GetById(uint edgeTypeId)
//         {
//             return _turnCostTypes.GetById(edgeTypeId);
//         }
//
//         public TurnCostTypeIndex Next(TurnCostTypeFunc func)
//         {
//             return new TurnCostTypeIndex(_turnCostTypes, func);
//         }
//     }
// }