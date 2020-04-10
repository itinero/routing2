using Itinero.Data.Graphs;
using Xunit;

namespace Itinero.Tests
{
    public class SnapPointExtensions
    {
        [Fact]
        public void Snap_Empty_ShouldFail()
        {
            var routerDb = new RouterDb();

            var result = routerDb.Snap(new VertexId(0, 1));
            Assert.True(result.IsError);
        }
        
        [Fact]
        public void Snap_OneVertex_ShouldFail()
        {
            var routerDb = new RouterDb();
            var vertex = routerDb.AddVertex(3.146638870239258,
                51.31060357805506);

            var result = routerDb.Snap(vertex);
            Assert.True(result.IsError);
        }
        
        [Fact]
        public void Snap_OneEdge_Vertex1_ShouldReturnOffset0()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(3.1074142456054688,51.31012070202407);
            var vertex2 = routerDb.AddVertex(3.146638870239258,51.31060357805506);

            var edge = routerDb.AddEdge(vertex1, vertex2);

            var result = routerDb.Snap(vertex1);
            Assert.False(result.IsError);
            Assert.Equal(0, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }
        
        [Fact]
        public void Snap_OneEdge_Vertex2_ShouldReturnOffsetMax()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(3.1074142456054688,51.31012070202407);
            var vertex2 = routerDb.AddVertex(3.146638870239258,51.31060357805506);

            var edge = routerDb.AddEdge(vertex1, vertex2);

            var result = routerDb.Snap(vertex2);
            Assert.False(result.IsError);
            Assert.Equal(ushort.MaxValue, result.Value.Offset);
            Assert.Equal(edge, result.Value.EdgeId);
        }
    }
}