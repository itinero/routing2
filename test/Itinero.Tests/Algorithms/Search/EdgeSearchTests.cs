using Itinero.Algorithms.Search;
using Itinero.Data;
using Xunit;

namespace Itinero.Tests.Algorithms.Search
{
    public class EdgeSearchTests
    {
        [Fact]
        public void EdgeSearch_SearchEdgesInBox_ShouldReturnNothingWhenNoEdges()
        {
            var network = new Network();
            network.AddVertex(4.792613983154297, 51.26535213392538);
            network.AddVertex(4.797506332397461, 51.26674845584085);

            var edges = network.Graph.SearchEdgesInBox((4.796, 51.265, 4.798, 51.267));
            Assert.NotNull(edges);
            Assert.False(edges.MoveNext());
        }
        
        [Fact]
        public void EdgeSearch_SearchEdgesInBox_ShouldReturnEdgeWhenOneVertexInBox()
        {
            var network = new Network();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);
            var edge = network.AddEdge(vertex1, vertex2);
            
            var edges = network.Graph.SearchEdgesInBox((4.796, 51.265, 4.798, 51.267));
            Assert.NotNull(edges);
            Assert.True(edges.MoveNext());
            Assert.Equal(edge, edges.GraphEnumerator.Id);
            Assert.False(edges.MoveNext());
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToVertex1WhenVertex1Closest()
        {
            var network = new Network();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);
            var edge = network.AddEdge(vertex1, vertex2);
            
            var snapPoint = network.SnapInBox((4.792613983154297 - 0.001, 51.26535213392538 - 0.001, 
                4.792613983154297 + 0.001, 51.26535213392538 + 0.001));
            Assert.Equal(edge, snapPoint.EdgeId);
            Assert.Equal(0, snapPoint.Offset);
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToVertex2WhenVertex2Closest()
        {
            var network = new Network();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);
            var edge = network.AddEdge(vertex1, vertex2);
            
            var snapPoint = network.SnapInBox((4.797506332397461 - 0.001, 51.26674845584085 - 0.001, 
                4.797506332397461 + 0.001, 51.26674845584085 + 0.001));
            Assert.Equal(edge, snapPoint.EdgeId);
            Assert.Equal(ushort.MaxValue, snapPoint.Offset);
        }

        [Fact]
        public void EdgeSearch_SnapInBox_ShouldSnapToSegmentWhenMiddleIsClosest()
        {
            var network = new Network();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);
            var edge = network.AddEdge(vertex1, vertex2);

            (double lon, double lat) middle = ((4.79261398315429 + 4.797506332397461) / 2,(51.26535213392538 + 51.26674845584085) / 2);
            var snapPoint = network.SnapInBox((middle.lon - 0.01, middle.lat - 0.01, 
                middle.lon + 0.01, middle.lat + 0.01));
            Assert.Equal(edge, snapPoint.EdgeId);
        }
    }
}