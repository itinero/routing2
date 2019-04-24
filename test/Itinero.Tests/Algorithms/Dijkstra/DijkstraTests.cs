//using Itinero.Data.Graphs;
//using Xunit;
//
//namespace Itinero.Tests.Algorithms.Dijkstra
//{
//    public class DijkstraTests
//    {
//        [Fact]
//        public void Dijkstra_ShouldFindOneHopPath()
//        {
//            var graph = new Graph();
//            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
//            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);
//
//            var edgeId = graph.AddEdge(vertex1, vertex2);
//
//            var dijkstra = new Itinero.Algorithms.Dijkstra.Dijkstra(graph,
//                new (VertexId vertex, uint edge, float cost)[]
//                {
//                    (vertex2, edgeId, 0)
//                },
//                new (VertexId vertex, uint edge, float cost)[]
//                {
//                    (vertex2, edgeId, 0)
//                });
//            var path = dijkstra.Run();
//            Assert.NotNull(path);
//        }
//
//        [Fact]
//        public void Dijkstra_ShouldFindTwoHopPath()
//        {
//            var graph = new Graph();
//            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
//            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);
//            var vertex3 = graph.AddVertex(4.797506332397461, 51.26674845584085);
//
//            var edgeId1 = graph.AddEdge(vertex1, vertex2);
//            var edgeId2 = graph.AddEdge(vertex2, vertex3);
//
//            var dijkstra = new Itinero.Algorithms.Dijkstra.Dijkstra(graph,
//                new (VertexId vertex, uint edge, float cost)[]
//                {
//                    (vertex2, edgeId1, 0)
//                },
//                new (VertexId vertex, uint edge, float cost)[]
//                {
//                    (vertex3, edgeId2, 0)
//                });
//            var path = dijkstra.Run();
//            Assert.NotNull(path);
//        }
//    }
//}