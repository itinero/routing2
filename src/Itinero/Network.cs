// using System;
// using System.Collections.Generic;
// using Itinero.Data.Graphs;
//
// namespace Itinero
// {
//     /// <summary>
//     /// A single consistent snapshot of the routing network.
//     /// </summary>
//     public sealed partial class Network : INetworkWritable
//     {
//         internal Network(RouterDb routerDb, int zoom = 14)
//         {
//             RouterDb = routerDb;
//             Graph = new Graph(zoom);
//         }
//
//         internal Network(RouterDb routerDb, Graph graph)
//         {
//             RouterDb = routerDb;
//             Graph = graph;
//         }
//
//         /// <summary>
//         /// Gets the network graph.
//         /// </summary>
//         internal Graph Graph { get; }
//
//         /// <summary>
//         /// Gets the router db.
//         /// </summary>
//         public RouterDb RouterDb { get; }
//
//         /// <summary>
//         /// Gets the given vertex.
//         /// </summary>
//         /// <param name="vertex">The vertex.</param>
//         /// <returns>The vertex.</returns>
//         public (double longitude, double latitude) GetVertex(VertexId vertex)
//         {
//             if (!Graph.TryGetVertex(vertex, out var longitude, out var latitude)) throw new ArgumentException($"{nameof(vertex)} does not exist.");
//             
//             return (longitude, latitude);
//         }
//
//         /// <summary>
//         /// Gets the edge enumerator.
//         /// </summary>
//         /// <returns></returns>
//         public NetworkEdgeEnumerator GetEdgeEnumerator()
//         {
//             return new NetworkEdgeEnumerator(this);
//         }
//     }
// }