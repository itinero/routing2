using System.Linq;
using Itinero.Algorithms.DataStructures;
using Xunit;

namespace Itinero.Tests.Algorithms.DataStructures
{
    public class PathTests
    {
        [Fact]
        public void Path_New_Offsets_ShouldIncludeByDefault()
        {
            var routerDb = new RouterDb();
            var path = new Path(routerDb);

            Assert.Equal(0, path.Offset1);
            Assert.Equal(ushort.MaxValue, path.Offset2);
        }
        
        [Fact]
        public void Path_Append_OneEdge_ShouldBeOneEdgePath()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            
            var path = new Path(routerDb);
            path.Append(edge1, vertex1);

            Assert.Single(path);
            Assert.Equal(edge1, path.First.edge);
        }
        
        [Fact]
        public void Path_Append_SecondEdge_ShouldBeSecondEdge()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);
            
            var path = new Path(routerDb);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);

            Assert.Equal(2, path.Count);
            Assert.Equal(edge1, path.First.edge);
            Assert.Equal(edge2, path.Last.edge);
        }
        
        [Fact]
        public void Path_Enumerate_TwoEdges_ShouldEnumerateEdges()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);
            
            var path = new Path(routerDb);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);

            var edges = path.ToList();
            Assert.Equal(2, edges.Count);
            Assert.Equal(edge1, edges[0].edge);
            Assert.Equal(edge2, edges[1].edge);
        }
        
        [Fact]
        public void Path_Prepend_OneEdge_ShouldBeOneEdgePath()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            
            var path = new Path(routerDb);
            path.Prepend(edge1, vertex1);

            Assert.Single(path);
            Assert.Equal(edge1, path.First.edge);
        }
        
        [Fact]
        public void Path_Prepend_SecondEdge_ShouldBeSecondEdge()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);
            
            var path = new Path(routerDb);
            path.Prepend(edge2, vertex3);
            path.Prepend(edge1, vertex2);

            Assert.Equal(2, path.Count);
            Assert.Equal(edge1, path.First.edge);
            Assert.Equal(edge2, path.Last.edge);
        }
        
        [Fact]
        public void Path_Trim_NormalOffset_ShouldNotTrim()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);
            
            var path = new Path(routerDb);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);
            path.Offset1 = (ushort.MaxValue / 2);
            path.Offset2 = (ushort.MaxValue / 2);
            
            path.Trim();

            Assert.Equal(2, path.Count);
        }
        
        [Fact]
        public void Path_Trim_Offset1Max_ShouldRemoveFirst()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);
            
            var path = new Path(routerDb);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);
            path.Offset1 = ushort.MaxValue;
            path.Offset2 = (ushort.MaxValue / 2);
            
            path.Trim();

            Assert.Single(path);
            Assert.Equal(0, path.Offset1);
            Assert.Equal(edge2, path.First.edge);
        }
        
        [Fact]
        public void Path_Trim_Offset2Is0_ShouldRemoveLast()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = routerDb.AddEdge(vertex1, vertex2);
            var edge2 = routerDb.AddEdge(vertex2, vertex3);
            
            var path = new Path(routerDb);
            path.Append(edge1, vertex1);
            path.Append(edge2, vertex2);
            path.Offset1 = (ushort.MaxValue / 2);
            path.Offset2 = 0;
            
            path.Trim();

            Assert.Single(path);
            Assert.Equal(ushort.MaxValue, path.Offset2);
            Assert.Equal(edge1, path.First.edge);
        }
    }
}