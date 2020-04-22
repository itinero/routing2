using Xunit;

namespace Itinero.Tests.Algorithms.Dijkstra.EdgeBased
{
    public class DijkstraTests
    {
        [Fact]
        public void Dijkstra_OneEdgeNetwork_Forward_ShouldFindOneHopPath()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge = routerDb.AddEdge(vertex1, vertex2);

            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(routerDb,
                (routerDb.Snap(vertex1), null),
                (routerDb.Snap(vertex2), null),
                (e) => 1);
            Assert.NotNull(path);
            Assert.Equal(0, path.Offset1);
            Assert.Equal(ushort.MaxValue, path.Offset2);
            using var enumerator = path.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void Dijkstra_OneEdgeNetwork_ForwardBackward_ShouldNotFindOneHopPath()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge = routerDb.AddEdge(vertex1, vertex2);

            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(routerDb,
                (routerDb.Snap(vertex1), true),
                (routerDb.Snap(vertex2), false),
                (e) => 1);
            Assert.Null(path);
        }

        [Fact]
        public void Dijkstra_FourEdgeNetwork_SameEdgeStartEnd_ShouldFindFourHopPath()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.792141914367670, 51.26297560389227);
            var vertex4 = routerDb.AddVertex(4.797334671020508, 51.26241166347257);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);
            var edge3 = routerDb.AddEdge(vertex3, vertex4);
            var edge4 = routerDb.AddEdge(vertex4, vertex1);
            
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(routerDb,
                (routerDb.Snap(vertex2, edge1), true),
                (routerDb.Snap(vertex1, edge1), true),
                (e) => 1);
            Assert.NotNull(path);
            Assert.Equal(ushort.MaxValue, path.Offset1);
            Assert.Equal(0, path.Offset2);
            using var enumerator = path.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge3, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge4, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Dijkstra_ThreeEdgeNetwork_SameEdge_PossibleUTurn_ShouldFindFourHopPath()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.792141914367670, 51.26297560389227);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);
            var edge3 = routerDb.AddEdge(vertex3, vertex3, new (double longitude, double latitude)[]
            {
                (4.797334671020508, 51.26241166347257)
            });
            
            var snapPoint = new SnapPoint(edge1, (ushort.MaxValue / 2));
            
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(routerDb,
                (snapPoint, true),(snapPoint, false),(e) => 1);
            Assert.NotNull(path);
            using var enumerator = path.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge3, enumerator.Current.edge);
            //Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.Current.edge);
            Assert.False(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.Current.edge);
            Assert.False(enumerator.Current.forward);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Dijkstra_OneEdgeNetwork_Backward_ShouldFindOneHopPath()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge = routerDb.AddEdge(vertex1, vertex2);

            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(routerDb,
                (routerDb.Snap(vertex2), null),
                (routerDb.Snap(vertex1), null),
                (e) => 1);
            Assert.NotNull(path);
            Assert.Equal(0, path.Offset1);
            Assert.Equal(ushort.MaxValue, path.Offset2);
            using var enumerator = path.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge, enumerator.Current.edge);
            Assert.False(enumerator.Current.forward);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Dijkstra_TwoEdgeNetwork_Forward_ShouldFindTwoHopPath()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);

            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(routerDb,
                (routerDb.Snap(vertex1), null),
                (routerDb.Snap(vertex3), null),
                (e) => 1);
            Assert.NotNull(path);
            Assert.Equal(0, path.Offset1);
            Assert.Equal(ushort.MaxValue, path.Offset2);
            using var enumerator = path.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.False(enumerator.MoveNext());
        }
        

        [Fact]
        public void Dijkstra_ThreeEdgeNetwork_ShouldFindThreeHopPath()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297,51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461,51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.792141914367670,51.26297560389227);
            var vertex4 = routerDb.AddVertex(4.797334671020508,51.26241166347257);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);
            var edge3 = routerDb.AddEdge(vertex3, vertex4);

            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(routerDb,
                (routerDb.Snap(vertex1), null),
                (routerDb.Snap(vertex4), null),
                (e) => 1);
            Assert.NotNull(path);
            Assert.Equal(0, path.Offset1);
            Assert.Equal(ushort.MaxValue, path.Offset2);
            using var enumerator = path.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge3, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.False(enumerator.MoveNext());
        }
    }
}