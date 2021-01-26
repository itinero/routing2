// using System.Linq;
// using Itinero.Network;
// using Itinero.Routes.Paths;
// using Itinero.Snapping;
// using Xunit;
//
// namespace Itinero.Tests.Routing.Flavours.Dijkstra
// {
//     public class DijkstraTurnCostTests
//     {
//         [Fact]
//         public void Dijkstra_OneToOne_TwoHopsShortest_WithoutTurnCost_ShouldFindTwoHopPath()
//         {
//             var routerDb = new RouterDb();
//             EdgeId edge1, edge2, edge3;
//             VertexId vertex1, vertex2, vertex3;
//             using (var mutable = routerDb.GetMutableNetwork())
//             {
//                 vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
//                 vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
//                 vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
//
//                 edge1 = mutable.AddEdge(vertex1, vertex2);
//                 edge2 = mutable.AddEdge(vertex2, vertex3);
//                 edge3 = mutable.AddEdge(vertex1, vertex3);
//             }
//
//             var latest = routerDb.Latest;
//             var path = Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.Run(latest,
//                 latest.Snap().To(vertex1),
//                 latest.Snap().To(vertex3),
//                 (e, ep) =>
//                 {
//                     var w = 1;
//                     if (e.Id == edge3) w = 3;
//
//                     var tcs = e.GetTurnCostTo(ep)
//                         .Select(x => (double)x.cost).Sum();
//                     return (w, tcs);
//                 });
//             Assert.NotNull(path);
//             path.Trim();
//             Assert.Equal(0, path.Offset1);
//             Assert.Equal(ushort.MaxValue, path.Offset2);
//             using var enumerator = path.GetEnumerator();
//             Assert.True(enumerator.MoveNext());
//             Assert.Equal(edge1, enumerator.Current.edge);
//             Assert.True(enumerator.Current.forward);
//             Assert.True(enumerator.MoveNext());
//             Assert.Equal(edge2, enumerator.Current.edge);
//             Assert.True(enumerator.Current.forward);
//             Assert.False(enumerator.MoveNext());
//         }
//
//         [Fact]
//         public void Dijkstra_OneToOne_OneHopsShortest_OnlyWithTurnCost_ShouldFindOneHopPath()
//         {
//             var routerDb = new RouterDb();
//             EdgeId edge1, edge2, edge3;
//             VertexId vertex1, vertex2, vertex3;
//             using (var mutable = routerDb.GetMutableNetwork())
//             {
//                 vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
//                 vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
//                 vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
//
//                 edge1 = mutable.AddEdge(vertex1, vertex2);
//                 edge2 = mutable.AddEdge(vertex2, vertex3);
//                 edge3 = mutable.AddEdge(vertex1, vertex3);
//                 
//                 mutable.AddTurnCosts(vertex2, Enumerable.Empty<(string key, string value)>(), 
//                     new [] { edge1, edge2 }, new uint[,] {{0,10},{10,0}});
//             }
//
//             var latest = routerDb.Latest;
//             var path = Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.Run(latest,
//                 latest.Snap().To(vertex1),
//                 latest.Snap().To(vertex3),
//                 (e, ep) =>
//                 {
//                     var w = 1;
//                     if (e.Id == edge3) w = 3;
//
//                     var tcs = e.GetTurnCostTo(ep)
//                         .Select(x => (double)x.cost).Sum();
//                     return (w, tcs);
//                 });
//             Assert.NotNull(path);
//             path.Trim();
//             Assert.Equal(0, path.Offset1);
//             Assert.Equal(ushort.MaxValue, path.Offset2);
//             using var enumerator = path.GetEnumerator();
//             Assert.True(enumerator.MoveNext());
//             Assert.Equal(edge3, enumerator.Current.edge);
//             Assert.True(enumerator.Current.forward);
//         }
//         
//         [Fact]
//         public void Dijkstra_OneToOne_TwoHopsShortest_InfiniteTurnCost_ShouldFindNoPath()
//         {
//             var routerDb = new RouterDb();
//             EdgeId edge1, edge2;
//             VertexId vertex1, vertex2, vertex3;
//             using (var mutable = routerDb.GetMutableNetwork())
//             {
//                 vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
//                 vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
//                 vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
//
//                 edge1 = mutable.AddEdge(vertex1, vertex2);
//                 edge2 = mutable.AddEdge(vertex2, vertex3);
//                 
//                 mutable.AddTurnCosts(vertex2, Enumerable.Empty<(string key, string value)>(), 
//                     new [] { edge1, edge2 }, new uint[,] {{0,1},{0,0}});
//             }
//
//             var latest = routerDb.Latest;
//             var path = Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.Run(latest,
//                 latest.Snap().To(vertex1),
//                 latest.Snap().To(vertex3),
//                 (e, ep) =>
//                 {
//                     var tcs = e.GetTurnCostTo(ep)
//                         .Select(x => (double)x.cost).Sum();
//                     if (tcs > 0) tcs = -1;
//                     return (1, tcs);
//                 });
//             Assert.Null(path);
//         }
//     }
// }

