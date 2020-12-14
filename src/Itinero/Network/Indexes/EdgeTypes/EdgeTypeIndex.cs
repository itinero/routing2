// using System;
// using System.Collections.Generic;
// using System.IO;
// using Itinero.IO;
//
// namespace Itinero.Network.Indexes.EdgeTypes
// {
//     /// <summary>
//     /// A graph type index.
//     ///
//     /// This has a:
//     /// - A function to determine edge type, maps edge attributes -> edge type attributes.
//     /// - A graph edge type collection keeping all unique edge type attributes.
//     /// </summary>
//     internal class EdgeTypeIndex
//     {
//         private readonly EdgeTypeFunc _func;
//         private readonly EdgeTypeCollection _edgeTypes;
//
//         public EdgeTypeIndex()
//         {
//             _func = new EdgeTypeFunc();
//             _edgeTypes = new EdgeTypeCollection();
//         }
//
//         public int Id => _func.Id;
//
//         public EdgeTypeFunc Func => _func;
//
//         internal EdgeTypeCollection EdgeTypeCollection => _edgeTypes;
//
//         private EdgeTypeIndex(EdgeTypeCollection edgeTypes, EdgeTypeFunc func)
//         {
//             _edgeTypes = edgeTypes;
//             _func = func;
//         }
//
//         public uint Get(IEnumerable<(string key, string value)> attributes)
//         {
//             var edgeType = _func.ToEdgeType(attributes);
//             return _edgeTypes.Get(edgeType);
//         }
//
//         public EdgeTypeIndex Next(EdgeTypeFunc func)
//         {
//             return new EdgeTypeIndex(_edgeTypes, func);
//         }
//     }
// }