using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Restrictions;
using Xunit;

namespace Itinero.Tests.Data.Graphs.Restrictions
{
    public class RestrictionExtensionsTests
    {
        [Fact]
        public void Invert_NoInvertPossible_ShouldReturnEmpty()
        {
            var routerDb = new RouterDb();
            
            using var mutable = routerDb.GetAsMutable();
            var vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = mutable.AddEdge(vertex1, vertex2);
            var edge2 = mutable.AddEdge(vertex2, vertex3);
                
            var sequence = new (EdgeId edge, bool forward)[] { (edge1, true), (edge2, true)};
            var inverse = sequence.Invert(mutable.GetEdgeEnumerator()).ToList();
            
            Assert.Empty(inverse);
        }
        
        [Fact]
        public void Invert_OneInvertPossible_ForwardForward_ShouldReturnSingle()
        {
            var routerDb = new RouterDb();
            
            using var mutable = routerDb.GetAsMutable();
            var vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = mutable.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = mutable.AddEdge(vertex1, vertex2);
            var edge2 = mutable.AddEdge(vertex2, vertex3);
            var edge3 = mutable.AddEdge(vertex2, vertex4);
                
            var sequence = new (EdgeId edge, bool forward)[] { (edge1, true), (edge2, true)};
            var inverse = sequence.Invert(mutable.GetEdgeEnumerator()).ToList();
            
            Assert.Single(inverse);
            var first = inverse.First().ToList();
            Assert.Equal(edge1, first[0].edge);
            Assert.True(first[0].forward);
            Assert.Equal(edge3, first[1].edge);
            Assert.True(first[1].forward);
        }
        
        [Fact]
        public void Invert_OneInvertPossible_BackwardForward_ShouldReturnSingle()
        {
            var routerDb = new RouterDb();
            
            using var mutable = routerDb.GetAsMutable();
            var vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = mutable.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = mutable.AddEdge(vertex2, vertex1);
            var edge2 = mutable.AddEdge(vertex2, vertex3);
            var edge3 = mutable.AddEdge(vertex2, vertex4);
                
            var sequence = new (EdgeId edge, bool forward)[] { (edge1, false), (edge2, true)};
            var inverse = sequence.Invert(mutable.GetEdgeEnumerator()).ToList();
            
            Assert.Single(inverse);
            var first = inverse.First().ToList();
            Assert.Equal(edge1, first[0].edge);
            Assert.False(first[0].forward);
            Assert.Equal(edge3, first[1].edge);
            Assert.True(first[1].forward);
        }
        
        [Fact]
        public void Invert_OneInvertPossible_ForwardBackward_ShouldReturnSingle()
        {
            var routerDb = new RouterDb();
            
            using var mutable = routerDb.GetAsMutable();
            var vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = mutable.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = mutable.AddEdge(vertex1, vertex2);
            var edge2 = mutable.AddEdge(vertex3, vertex2);
            var edge3 = mutable.AddEdge(vertex2, vertex4);
                
            var sequence = new (EdgeId edge, bool forward)[] { (edge1, true), (edge2, false)};
            var inverse = sequence.Invert(mutable.GetEdgeEnumerator()).ToList();
            
            Assert.Single(inverse);
            var first = inverse.First().ToList();
            Assert.Equal(edge1, first[0].edge);
            Assert.True(first[0].forward);
            Assert.Equal(edge3, first[1].edge);
            Assert.True(first[1].forward);
        }
        
        [Fact]
        public void Invert_OneForwardInvertPossibleShouldReturnSingle()
        {
            var routerDb = new RouterDb();
            
            using var mutable = routerDb.GetAsMutable();
            var vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex5 = mutable.AddVertex(4.797506332397461, 51.26674845584085);

            var edge1 = mutable.AddEdge(vertex1, vertex2);
            var edge2 = mutable.AddEdge(vertex2, vertex3);
            var edge3 = mutable.AddEdge(vertex4, vertex2);
            var edge4 = mutable.AddEdge(vertex2, vertex5);
            
            var sequence = new (EdgeId edge, bool forward)[] { (edge1, true), (edge2, true)};
            var inverse = sequence.Invert(mutable.GetEdgeEnumerator()).ToList();
            
            Assert.Equal(2, inverse.Count);
            var inverse1 = inverse[0].ToList();
            Assert.Equal(edge1, inverse1[0].edge);
            Assert.True(inverse1[0].forward);
            Assert.Equal(edge4, inverse1[1].edge);
            Assert.True(inverse1[1].forward);
            var inverse2 = inverse[1].ToList();
            Assert.Equal(edge1, inverse2[0].edge);
            Assert.True(inverse2[0].forward);
            Assert.Equal(edge3, inverse2[1].edge);
            Assert.False(inverse2[1].forward);
        }
    }
}