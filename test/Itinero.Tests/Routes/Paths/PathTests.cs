using System.Linq;
using Itinero.Network;
using Itinero.Routes.Paths;
using Xunit;

namespace Itinero.Tests.Routes.Paths {
    public class PathTests {
        [Fact]
        public void Path_New_Offsets_ShouldIncludeByDefault() {
            var routerDb = new RouterDb();
            var path = new Path(routerDb.Latest);

            Assert.Equal(0, path.Offset1);
            Assert.Equal(ushort.MaxValue, path.Offset2);
        }

        [Fact]
        public void Path_Append_OneEdge_ShouldBeOneEdgePath() {
            var routerDb = new RouterDb();
            EdgeId edge;
            VertexId vertex1;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

                edge = writer.AddEdge(vertex1, vertex2);
            }

            var path = new Path(routerDb.Latest);
            path.Append(edge, vertex1);

            Assert.Single(path);
            Assert.Equal(edge, path.First.edge);
        }

        [Fact]
        public void Path_Append_SecondEdge_ShouldBeSecondEdge() {
            var routerDb = new RouterDb();
            EdgeId edge1;
            EdgeId edge2;
            VertexId vertex1;
            VertexId vertex2;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);
                var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }

            var path = new Path(routerDb.Latest);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);

            Assert.Equal(2, path.Count);
            Assert.Equal(edge1, path.First.edge);
            Assert.Equal(edge2, path.Last.edge);
        }

        [Fact]
        public void Path_Enumerate_TwoEdges_ShouldEnumerateEdges() {
            var routerDb = new RouterDb();
            EdgeId edge1;
            EdgeId edge2;
            VertexId vertex1;
            VertexId vertex2;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);
                var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }

            var path = new Path(routerDb.Latest);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);

            var edges = path.ToList();
            Assert.Equal(2, edges.Count);
            Assert.Equal(edge1, edges[0].edge);
            Assert.Equal(edge2, edges[1].edge);
        }

        [Fact]
        public void Path_Prepend_OneEdge_ShouldBeOneEdgePath() {
            var routerDb = new RouterDb();
            EdgeId edge1;
            VertexId vertex1;
            VertexId vertex2;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

                edge1 = writer.AddEdge(vertex1, vertex2);
            }

            var path = new Path(routerDb.Latest);
            path.Prepend(edge1, vertex1);

            Assert.Single(path);
            Assert.Equal(edge1, path.First.edge);
        }

        [Fact]
        public void Path_Prepend_SecondEdge_ShouldBeSecondEdge() {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }

            var path = new Path(routerDb.Latest);
            path.Prepend(edge2, vertex3);
            path.Prepend(edge1, vertex2);

            Assert.Equal(2, path.Count);
            Assert.Equal(edge1, path.First.edge);
            Assert.Equal(edge2, path.Last.edge);
        }

        [Fact]
        public void Path_Trim_NormalOffset_ShouldNotTrim() {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }

            var path = new Path(routerDb.Latest);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);
            path.Offset1 = ushort.MaxValue / 2;
            path.Offset2 = ushort.MaxValue / 2;

            path.Trim();

            Assert.Equal(2, path.Count);
        }

        [Fact]
        public void Path_Trim_Offset1Max_ShouldRemoveFirst() {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }

            var path = new Path(routerDb.Latest);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);
            path.Offset1 = ushort.MaxValue;
            path.Offset2 = ushort.MaxValue / 2;

            path.Trim();

            Assert.Single(path);
            Assert.Equal(0, path.Offset1);
            Assert.Equal(edge2, path.First.edge);
        }

        [Fact]
        public void Path_Trim_Offset2Is0_ShouldRemoveLast() {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2;
            VertexId vertex1, vertex2, vertex3;
            using (var writer = routerDb.GetMutableNetwork()) {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);
                vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex2, vertex3);
            }

            var path = new Path(routerDb.Latest);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);
            path.Offset1 = ushort.MaxValue / 2;
            path.Offset2 = 0;

            path.Trim();

            Assert.Single(path);
            Assert.Equal(ushort.MaxValue, path.Offset2);
            Assert.Equal(edge1, path.First.edge);
        }
    }
}