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
            var network = new RouterDb();
            network.AddVertex(4.792613983154297, 51.26535213392538);
            network.AddVertex(4.797506332397461, 51.26674845584085);

            var edges = network.SearchEdgesInBox(((4.796, 51.265), (4.798, 51.267)));
            Assert.NotNull(edges);
            Assert.False(edges.MoveNext());
        }
        
        [Fact]
        public void EdgeSearch_SearchEdgesInBox_ShouldReturnEdgeWhenOneVertexInBox()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);
            var edge = network.AddEdge(vertex1, vertex2);
            
            var edges = network.SearchEdgesInBox(((4.796, 51.267), (4.798, 51.265)));
            Assert.NotNull(edges);
            Assert.True(edges.MoveNext());
            Assert.Equal(edge, edges.Id);
            Assert.False(edges.MoveNext());
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToVertex1WhenVertex1Closest()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);
            var edge = network.AddEdge(vertex1, vertex2);
            
            var snapPoint = network.SnapInBox(((4.792613983154297 - 0.001, 51.26535213392538 + 0.001), 
                (4.792613983154297 + 0.001, 51.26535213392538 - 0.001)));
            Assert.Equal(edge, snapPoint.EdgeId);
            Assert.Equal(0, snapPoint.Offset);
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToVertex2WhenVertex2Closest()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);
            var edge = network.AddEdge(vertex1, vertex2);
            
            var snapPoint = network.SnapInBox(((4.797506332397461 - 0.001, 51.26674845584085 + 0.001), 
                (4.797506332397461 + 0.001, 51.26674845584085 - 0.001)));
            Assert.Equal(edge, snapPoint.EdgeId);
            Assert.Equal(ushort.MaxValue, snapPoint.Offset);
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToSegmentWhenMiddleIsClosest()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);
            var edge = network.AddEdge(vertex1, vertex2);

            (double lon, double lat) middle = ((4.79261398315429 + 4.797506332397461) / 2,(51.26535213392538 + 51.26674845584085) / 2);
            var snapPoint = network.SnapInBox(((middle.lon - 0.01, middle.lat + 0.01), 
                (middle.lon + 0.01, middle.lat - 0.01)));
            Assert.Equal(edge, snapPoint.EdgeId);
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToClosestSegment()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(4.796154499053955,51.26912479079087);
            var vertex2 = network.AddVertex(4.799630641937256,51.27015852526688);
            var vertex3 = network.AddVertex(4.796798229217529,51.26835954379726);
            var vertex4 = network.AddVertex(4.800124168395996,51.26937987029022);
            var edge1 = network.AddEdge(vertex1, vertex2);
            var edge2 = network.AddEdge(vertex3, vertex4);

            var snapPoint = network.SnapInBox(((4.798600673675537 - 0.01, 51.268748881579405 + 0.01), 
                (4.798600673675537 + 0.01, 51.268748881579405 - 0.01)));
            Assert.Equal(edge2, snapPoint.EdgeId);
        }

        [Fact]
        public void EdgeSearch_SnapAllInBox_NonOrthogonal_ShouldSnapToAll()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(4.796154499053955,51.26912479079087);
            var vertex2 = network.AddVertex(4.799630641937256,51.27015852526688);
            var vertex3 = network.AddVertex(4.796798229217529,51.26835954379726);
            var vertex4 = network.AddVertex(4.800124168395996,51.26937987029022);
            var vertex5 = network.AddVertex(4.799898862838745,51.269762486884446);
            var vertex6 = network.AddVertex(4.802924394607544,51.27068880860352);
            var edge1 = network.AddEdge(vertex1, vertex2);
            var edge2 = network.AddEdge(vertex3, vertex4);
            var edge3 = network.AddEdge(vertex5, vertex6);

            var snapPoints = network.SnapAllInBox(((4.798600673675537 - 0.01, 51.268748881579405 + 0.01), 
                (4.798600673675537 + 0.01, 51.268748881579405 - 0.01)), nonOrthogonalEdges: true).ToList();
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge1));
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge2));
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge3));
        }

        [Fact]
        public void EdgeSearch_SnapAllInBox_ShouldSnapToOrthogonal()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(4.796154499053955,51.26912479079087);
            var vertex2 = network.AddVertex(4.799630641937256,51.27015852526688);
            var vertex3 = network.AddVertex(4.796798229217529,51.26835954379726);
            var vertex4 = network.AddVertex(4.800124168395996,51.26937987029022);
            var vertex5 = network.AddVertex(4.799898862838745,51.269762486884446);
            var vertex6 = network.AddVertex(4.802924394607544,51.27068880860352);
            var edge1 = network.AddEdge(vertex1, vertex2);
            var edge2 = network.AddEdge(vertex3, vertex4);
            var edge3 = network.AddEdge(vertex5, vertex6);

            var snapPoints = network.SnapAllInBox(((4.798600673675537 - 0.01, 51.268748881579405 + 0.01), 
                (4.798600673675537 + 0.01, 51.268748881579405 - 0.01)), nonOrthogonalEdges: false).ToList();
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge1));
            Assert.True(snapPoints.Exists(x => x.EdgeId == edge2));
            Assert.False(snapPoints.Exists(x => x.EdgeId == edge3));
        }
    }
}