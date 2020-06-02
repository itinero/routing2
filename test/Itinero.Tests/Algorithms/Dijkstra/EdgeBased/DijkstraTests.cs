using Itinero.Data.Graphs;
using Xunit;

namespace Itinero.Tests.Algorithms.Dijkstra.EdgeBased
{
    public class DijkstraTests
    {
        [Fact]
        public void Dijkstra_OneToOne_OneHopShortest_ShouldFindOneHopPath()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex1, vertex2;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge = writer.AddEdge(vertex1, vertex2);
            }

            var latest = routerDb.Network;
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
                (latest.Snap(vertex1), null),
                (latest.Snap(vertex2), null),
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
        public void Dijkstra_OneToOne_OneHopShortest_ForwardForward_ShouldFindOneHopPath()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex1, vertex2;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge = writer.AddEdge(vertex1, vertex2);
            }

            var latest = routerDb.Network;
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
                (latest.Snap(vertex1), true),
                (latest.Snap(vertex2), true),
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
        public void Dijkstra_OneToOne_OneHopShortest_ForwardBackward_ShouldNotFindPath()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex1, vertex2;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge = writer.AddEdge(vertex1, vertex2);
            }

            var latest = routerDb.Network;
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
                (latest.Snap(vertex1), true),
                (latest.Snap(vertex2), false),
                (e) => 1);
            Assert.Null(path);
        }
        
        [Fact]
        public void Dijkstra_OneToOne_OneHopShortest_BackwardBackward_ShouldNotFindPath()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex1, vertex2;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge = writer.AddEdge(vertex1, vertex2);
            }

            var latest = routerDb.Network;
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
                (latest.Snap(vertex1), true),
                (latest.Snap(vertex2), false),
                (e) => 1);
            Assert.Null(path);
        }
        
        [Fact]
        public void Dijkstra_OneToOne_TwoHopsShortest_ShouldFindTwoHopPath()
        {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }

            var latest = routerDb.Network;
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
                (latest.Snap(vertex1), null),
                (latest.Snap(vertex3), null),
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
        public void Dijkstra_OneToOne_ThreeHopsShortest_ShouldFindThreeHopPath()
        {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2, edge3;
            VertexId vertex1, vertex2, vertex3, vertex4;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.792141914367670, 51.26297560389227);
                vertex4 = writer.AddVertex(4.797334671020508, 51.26241166347257);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
                edge3 = writer.AddEdge(vertex3, vertex4);
            }

            var latest = routerDb.Network;
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
                (latest.Snap(vertex1), null),
                (latest.Snap(vertex4), null),
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
        
        [Fact]
        public void Dijkstra_OneToMany_TwoHopsShortest_ShouldFindTwoHopPaths()
        {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }
            
            var latest = routerDb.Network;
            var snap1 = latest.Snap(vertex1).Value;
            var snap2 = latest.Snap(vertex3).Value;
            var snap3 = new SnapPoint(edge2, ushort.MaxValue / 4);
            var snap4 = new SnapPoint(edge2, ushort.MaxValue / 2);
            var snap5 = new SnapPoint(edge2, ushort.MaxValue / 4 + ushort.MaxValue / 2);

            var paths = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
                (snap1, null), new (SnapPoint sp, bool? direction)[]
                {
                    (snap2, null), 
                    (snap3, null), 
                    (snap4, null), 
                    (snap5, null)
                },
                (e) => 1);
            Assert.NotNull(paths);
            Assert.Equal(4, paths.Length);

            var path2 = paths[0];
            Assert.Equal(0,path2.Offset1);
            Assert.Equal(ushort.MaxValue, path2.Offset2);
            using var enumerator2 = path2.GetEnumerator();
            Assert.True(enumerator2.MoveNext());
            Assert.Equal(edge1, enumerator2.Current.edge);
            Assert.True(enumerator2.Current.forward);
            Assert.True(enumerator2.MoveNext());
            Assert.Equal(edge2, enumerator2.Current.edge);
            Assert.True(enumerator2.Current.forward);
            Assert.False(enumerator2.MoveNext());

            var path3 = paths[1];
            Assert.Equal(0,path3.Offset1);
            Assert.Equal(ushort.MaxValue / 4, path3.Offset2);
            using var enumerator3 = path3.GetEnumerator();
            Assert.True(enumerator3.MoveNext());
            Assert.Equal(edge1, enumerator3.Current.edge);
            Assert.True(enumerator3.Current.forward);
            Assert.True(enumerator3.MoveNext());
            Assert.Equal(edge2, enumerator3.Current.edge);
            Assert.True(enumerator3.Current.forward);
            Assert.False(enumerator3.MoveNext());

            var path4 = paths[2];
            Assert.Equal(0,path4.Offset1);
            Assert.Equal(ushort.MaxValue / 2, path4.Offset2);
            using var enumerator4 = path4.GetEnumerator();
            Assert.True(enumerator4.MoveNext());
            Assert.Equal(edge1, enumerator4.Current.edge);
            Assert.True(enumerator4.Current.forward);
            Assert.True(enumerator4.MoveNext());
            Assert.Equal(edge2, enumerator4.Current.edge);
            Assert.True(enumerator4.Current.forward);
            Assert.False(enumerator4.MoveNext());

            var path5 = paths[3];
            Assert.Equal(0,path5.Offset1);
            Assert.Equal(ushort.MaxValue / 2 + ushort.MaxValue / 4, path5.Offset2);
            using var enumerator5 = path5.GetEnumerator();
            Assert.True(enumerator5.MoveNext());
            Assert.Equal(edge1, enumerator5.Current.edge);
            Assert.True(enumerator5.Current.forward);
            Assert.True(enumerator5.MoveNext());
            Assert.Equal(edge2, enumerator5.Current.edge);
            Assert.True(enumerator5.Current.forward);
            Assert.False(enumerator5.MoveNext());
        }
        
        [Fact]
        public void Dijkstra_OneToMany_OneHopShortest_ShouldFindOneHopPaths()
        {
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex1, vertex2;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge = writer.AddEdge(vertex1, vertex2);
            }

            var latest = routerDb.Network;
            var snap1 = latest.Snap(vertex1).Value;
            var snap2 = latest.Snap(vertex2).Value;
            var snap3 = new SnapPoint(edge, ushort.MaxValue / 4);
            var snap4 = new SnapPoint(edge, ushort.MaxValue / 2);
            var snap5 = new SnapPoint(edge, ushort.MaxValue / 4 + ushort.MaxValue / 2);

            var paths = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
                (snap1, null), new (SnapPoint sp, bool? direction)[]
                {
                    (snap2, null), 
                    (snap3, null), 
                    (snap4, null), 
                    (snap5, null)
                },
                (e) => 1);
            Assert.NotNull(paths);
            Assert.Equal(4, paths.Length);

            var path2 = paths[0];
            Assert.Equal(0,path2.Offset1);
            Assert.Equal(ushort.MaxValue, path2.Offset2);
            using var enumerator2 = path2.GetEnumerator();
            Assert.True(enumerator2.MoveNext());
            Assert.Equal(edge, enumerator2.Current.edge);
            Assert.True(enumerator2.Current.forward);
            Assert.False(enumerator2.MoveNext());

            var path3 = paths[1];
            Assert.Equal(0,path3.Offset1);
            Assert.Equal(ushort.MaxValue / 4, path3.Offset2);
            using var enumerator3 = path3.GetEnumerator();
            Assert.True(enumerator3.MoveNext());
            Assert.Equal(edge, enumerator3.Current.edge);
            Assert.True(enumerator3.Current.forward);
            Assert.False(enumerator3.MoveNext());

            var path4 = paths[2];
            Assert.Equal(0,path4.Offset1);
            Assert.Equal(ushort.MaxValue / 2, path4.Offset2);
            using var enumerator4 = path4.GetEnumerator();
            Assert.True(enumerator4.MoveNext());
            Assert.Equal(edge, enumerator4.Current.edge);
            Assert.True(enumerator4.Current.forward);
            Assert.False(enumerator4.MoveNext());

            var path5 = paths[3];
            Assert.Equal(0,path5.Offset1);
            Assert.Equal(ushort.MaxValue / 2 + ushort.MaxValue / 4, path5.Offset2);
            using var enumerator5 = path5.GetEnumerator();
            Assert.True(enumerator5.MoveNext());
            Assert.Equal(edge, enumerator5.Current.edge);
            Assert.True(enumerator5.Current.forward);
            Assert.False(enumerator5.MoveNext());
        }
        
        [Fact]
        public void Dijkstra_OneToOne_FourEdgeClosedNetwork_SameEdgeStartEnd_ForwardForward_ShouldFindFourHopPath()
        {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2, edge3, edge4;
            VertexId vertex1, vertex2, vertex3, vertex4;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.792141914367670, 51.26297560389227);
                vertex4 = writer.AddVertex(4.797334671020508, 51.26241166347257);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
                edge3 = writer.AddEdge(vertex3, vertex4);
                edge4 = writer.AddEdge(vertex4, vertex1);
            }
            
            var latest = routerDb.Network;
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
                (latest.Snap(vertex2, edge1), true),
                (latest.Snap(vertex1, edge1), true),
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
        public void Dijkstra_OneToOne_ThreeEdgeNetwork_SameEdge_ForwardBackward_PossibleUTurn_ShouldFindFourHopPath()
        {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2, edge3;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.792141914367670, 51.26297560389227);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
                edge3 = writer.AddEdge(vertex3, vertex3, new (double longitude, double latitude)[]
                {
                    (4.797334671020508, 51.26241166347257)
                });
            }
            
            var latest = routerDb.Network;
            var snapPoint = new SnapPoint(edge1, (ushort.MaxValue / 2));
            var path = Itinero.Algorithms.Dijkstra.EdgeBased.Dijkstra.Default.Run(latest,
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
            //Assert.True(enumerator.Current.forward); // this can be forward or backward, both is fine!
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.Current.edge);
            Assert.False(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.Current.edge);
            Assert.False(enumerator.Current.forward);
            Assert.False(enumerator.MoveNext());
        }
    }
}