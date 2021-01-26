using Itinero.Network;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Routing.Flavours.Dijkstra {
    public class DijkstraTests {
        [Fact]
        public void Dijkstra_OneToOne_OneHopShortest_ShouldFindOneHopPath() {
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex1, vertex2;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge = writer.AddEdge(vertex1, vertex2);
            }

            var latest = routerDb.Latest;
            var path = Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.Run(latest,
                latest.Snap().To(vertex1),
                latest.Snap().To(vertex2),
                (e, pe) => (1, 0));
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
        public void Dijkstra_OneToOne_TwoHopsShortest_ShouldFindTwoHopPath() {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }

            var latest = routerDb.Latest;
            var path = Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.Run(latest,
                latest.Snap().To(vertex1),
                latest.Snap().To(vertex3),
                (e, ep) => (1, 0));
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
        public void Dijkstra_OneToOne_ThreeHopsShortest_ShouldFindThreeHopPath() {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2, edge3;
            VertexId vertex1, vertex2, vertex3, vertex4;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.792141914367670, 51.26297560389227);
                vertex4 = writer.AddVertex(4.797334671020508, 51.26241166347257);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
                edge3 = writer.AddEdge(vertex3, vertex4);
            }

            var latest = routerDb.Latest;
            var path = Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.Run(latest,
                latest.Snap().To(vertex1),
                latest.Snap().To(vertex4),
                (e, ep) => (1, 0));
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
        public void Dijkstra_OneToOne_PathWithinEdge_NotShortest_ShouldFindShortest() {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2, edge3;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
                edge3 = writer.AddEdge(vertex1, vertex3); // this edge has a weight of 10.
            }

            var latest = routerDb.Latest;
            var path = Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.Run(latest,
                latest.Snap().To(vertex1, edge3),
                latest.Snap().To(vertex3, edge3),
                (e, ep) => {
                    if (e.Id == edge3) {
                        return (10, 0);
                    }

                    return (1, 0);
                });

            // the path generate is from (vertex1 -> vertex2 -> vertex3) 
            // but it includes edge3 with:
            //  - an offset max at the start, the edge is not included.
            // -  an offset 0 at the end, the edge is not included.
            // the 'snapping' was done on edge3 and it should always be included in the output. 
            Assert.NotNull(path);
            Assert.Equal(4, path.Count);
            Assert.Equal(ushort.MaxValue, path.Offset1);
            Assert.Equal(0, path.Offset2);

            using var enumerator = path.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge3, enumerator.Current.edge);
            Assert.False(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge1, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge2, enumerator.Current.edge);
            Assert.True(enumerator.Current.forward);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edge3, enumerator.Current.edge);
            Assert.False(enumerator.Current.forward);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Dijkstra_OneToMany_OneHopShortest_ShouldFindOneHopPaths() {
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex1, vertex2;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge = writer.AddEdge(vertex1, vertex2);
            }

            var latest = routerDb.Latest;
            var snap1 = latest.Snap().To(vertex1).Value;
            var snap2 = latest.Snap().To(vertex2).Value;
            var snap3 = new SnapPoint(edge, ushort.MaxValue / 4);
            var snap4 = new SnapPoint(edge, ushort.MaxValue / 2);
            var snap5 = new SnapPoint(edge, ushort.MaxValue / 4 + ushort.MaxValue / 2);

            var paths = Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.Run(latest,
                snap1, new[] {snap2, snap3, snap4, snap5},
                (e, ep) => (1, 0));
            Assert.NotNull(paths);
            Assert.Equal(4, paths.Length);

            var path2 = paths[0];
            Assert.Equal(0, path2.Offset1);
            Assert.Equal(ushort.MaxValue, path2.Offset2);
            using var enumerator2 = path2.GetEnumerator();
            Assert.True(enumerator2.MoveNext());
            Assert.Equal(edge, enumerator2.Current.edge);
            Assert.True(enumerator2.Current.forward);
            Assert.False(enumerator2.MoveNext());

            var path3 = paths[1];
            Assert.Equal(0, path3.Offset1);
            Assert.Equal(ushort.MaxValue / 4, path3.Offset2);
            using var enumerator3 = path3.GetEnumerator();
            Assert.True(enumerator3.MoveNext());
            Assert.Equal(edge, enumerator3.Current.edge);
            Assert.True(enumerator3.Current.forward);
            Assert.False(enumerator3.MoveNext());

            var path4 = paths[2];
            Assert.Equal(0, path4.Offset1);
            Assert.Equal(ushort.MaxValue / 2, path4.Offset2);
            using var enumerator4 = path4.GetEnumerator();
            Assert.True(enumerator4.MoveNext());
            Assert.Equal(edge, enumerator4.Current.edge);
            Assert.True(enumerator4.Current.forward);
            Assert.False(enumerator4.MoveNext());

            var path5 = paths[3];
            Assert.Equal(0, path5.Offset1);
            Assert.Equal(ushort.MaxValue / 2 + ushort.MaxValue / 4, path5.Offset2);
            using var enumerator5 = path5.GetEnumerator();
            Assert.True(enumerator5.MoveNext());
            Assert.Equal(edge, enumerator5.Current.edge);
            Assert.True(enumerator5.Current.forward);
            Assert.False(enumerator5.MoveNext());
        }

        [Fact]
        public void Dijkstra_OneToMany_TwoHopsShortest_ShouldFindTwoHopPaths() {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }

            var latest = routerDb.Latest;
            var snap1 = latest.Snap().To(vertex1).Value;
            var snap2 = latest.Snap().To(vertex3).Value;
            var snap3 = new SnapPoint(edge2, ushort.MaxValue / 4);
            var snap4 = new SnapPoint(edge2, ushort.MaxValue / 2);
            var snap5 = new SnapPoint(edge2, ushort.MaxValue / 4 + ushort.MaxValue / 2);

            var paths = Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.Run(latest,
                snap1, new[] {snap2, snap3, snap4, snap5},
                (e, ep) => (1, 0));
            Assert.NotNull(paths);
            Assert.Equal(4, paths.Length);

            var path2 = paths[0];
            Assert.Equal(0, path2.Offset1);
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
            Assert.Equal(0, path3.Offset1);
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
            Assert.Equal(0, path4.Offset1);
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
            Assert.Equal(0, path5.Offset1);
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
    }
}