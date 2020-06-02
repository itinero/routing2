using System.Linq;
using Itinero.Algorithms.Search;
using Itinero.Data;
using Itinero.Data.Graphs;
using Xunit;

namespace Itinero.Tests.Algorithms.Search
{
    public class EdgeSearchTests
    {
        [Fact]
        public void EdgeSearch_SearchEdgesInBox_ShouldReturnNothingWhenNoEdges()
        {
            var routerDb = new RouterDb();
            using (var writer = routerDb.GetWriter())
            {
                writer.AddVertex(4.792613983154297, 51.26535213392538);
                writer.AddVertex(4.797506332397461, 51.26674845584085);
            }

            var edges = routerDb.Network.SearchEdgesInBox(((4.796, 51.265), (4.798, 51.267)));
            Assert.NotNull(edges);
            Assert.False(edges.MoveNext());
        }
        
        [Fact]
        public void EdgeSearch_SearchEdgesInBox_ShouldReturnEdgeWhenOneVertexInBox()
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
            
            var edges = routerDb.Network.SearchEdgesInBox(((4.796, 51.267), (4.798, 51.265)));
            Assert.NotNull(edges);
            Assert.True(edges.MoveNext());
            Assert.Equal(edge, edges.Id);
            Assert.False(edges.MoveNext());
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToVertex1WhenVertex1Closest()
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
            
            var snapPoint = routerDb.Network.SnapInBox(((4.792613983154297 - 0.001, 51.26535213392538 + 0.001), 
                (4.792613983154297 + 0.001, 51.26535213392538 - 0.001)));
            Assert.Equal(edge, snapPoint.EdgeId);
            Assert.Equal(0, snapPoint.Offset);
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToVertex2WhenVertex2Closest()
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
            
            var snapPoint = routerDb.Network.SnapInBox(((4.797506332397461 - 0.001, 51.26674845584085 + 0.001), 
                (4.797506332397461 + 0.001, 51.26674845584085 - 0.001)));
            Assert.Equal(edge, snapPoint.EdgeId);
            Assert.Equal(ushort.MaxValue, snapPoint.Offset);
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToSegmentWhenMiddleIsClosest()
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

            (double lon, double lat) middle = ((4.79261398315429 + 4.797506332397461) / 2,(51.26535213392538 + 51.26674845584085) / 2);
            var snapPoint = routerDb.Network.SnapInBox(((middle.lon - 0.01, middle.lat + 0.01), 
                (middle.lon + 0.01, middle.lat - 0.01)));
            Assert.Equal(edge, snapPoint.EdgeId);
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToClosestSegment()
        {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2;
            VertexId vertex1, vertex2, vertex3, vertex4;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.796154499053955, 51.26912479079087);
                vertex2 = writer.AddVertex(4.799630641937256, 51.27015852526688);
                vertex3 = writer.AddVertex(4.796798229217529, 51.26835954379726);
                vertex4 = writer.AddVertex(4.800124168395996, 51.26937987029022);
                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex3, vertex4);
            }

            var snapPoint = routerDb.Network.SnapInBox(((4.798600673675537 - 0.01, 51.268748881579405 + 0.01), 
                (4.798600673675537 + 0.01, 51.268748881579405 - 0.01)));
            Assert.Equal(edge2, snapPoint.EdgeId);
        }

        [Fact]
        public void EdgeSearch_SnapAllInBox_NonOrthogonal_ShouldSnapToAll()
        {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2, edge3;
            VertexId vertex1, vertex2, vertex3, vertex4, vertex5, vertex6;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.796154499053955, 51.26912479079087);
                vertex2 = writer.AddVertex(4.799630641937256, 51.27015852526688);
                vertex3 = writer.AddVertex(4.796798229217529, 51.26835954379726);
                vertex4 = writer.AddVertex(4.800124168395996, 51.26937987029022);
                vertex5 = writer.AddVertex(4.799898862838745, 51.269762486884446);
                vertex6 = writer.AddVertex(4.802924394607544, 51.27068880860352);
                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex3, vertex4);
                edge3 = writer.AddEdge(vertex5, vertex6);
            }

            var snapPoints = routerDb.Network.SnapAllInBox(((4.798600673675537 - 0.01, 51.268748881579405 + 0.01), 
                (4.798600673675537 + 0.01, 51.268748881579405 - 0.01)), nonOrthogonalEdges: true).ToList();
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge1));
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge2));
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge3));
        }

        [Fact]
        public void EdgeSearch_SnapAllInBox_ShouldSnapToOrthogonal()
        {
            var routerDb = new RouterDb();
            EdgeId edge1, edge2, edge3;
            VertexId vertex1, vertex2, vertex3, vertex4, vertex5, vertex6;
            using (var writer = routerDb.GetWriter())
            {
                vertex1 = writer.AddVertex(4.796154499053955, 51.26912479079087);
                vertex2 = writer.AddVertex(4.799630641937256, 51.27015852526688);
                vertex3 = writer.AddVertex(4.796798229217529, 51.26835954379726);
                vertex4 = writer.AddVertex(4.800124168395996, 51.26937987029022);
                vertex5 = writer.AddVertex(4.799898862838745, 51.269762486884446);
                vertex6 = writer.AddVertex(4.802924394607544, 51.27068880860352);
                edge1 = writer.AddEdge(vertex1, vertex2);
                edge2 = writer.AddEdge(vertex3, vertex4);
                edge3 = writer.AddEdge(vertex5, vertex6);
            }

            var snapPoints = routerDb.Network.SnapAllInBox(((4.798600673675537 - 0.01, 51.268748881579405 + 0.01), 
                (4.798600673675537 + 0.01, 51.268748881579405 - 0.01)), nonOrthogonalEdges: false).ToList();
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge1));
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge2));
            Assert.False(snapPoints.Exists(x => x.EdgeId == edge3));
        }
    }
}