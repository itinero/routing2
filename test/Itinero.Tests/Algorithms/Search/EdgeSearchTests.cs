using Itinero.Algorithms.Search;
using Itinero.Data;
using NUnit.Framework;

namespace Itinero.Tests.Algorithms.Search
{
    public class EdgeSearchTests
    {
        [Test]
        public void EdgeSearch_SearchEdgesInBox_ShouldReturnNothingWhenNoEdges()
        {
            var network = new Network();
            network.AddVertex(4.792613983154297, 51.26535213392538);
            network.AddVertex(4.797506332397461, 51.26674845584085);

            var edges = network.Graph.SearchEdgesInBox((4.796, 51.265, 4.798, 51.267));
            Assert.IsNotNull(edges);
            Assert.IsFalse(edges.MoveNext());
        }
        
        [Test]
        public void EdgeSearch_SearchEdgesInBox_ShouldReturnEdgeWhenOneVertexInBox()
        {
            var network = new Network();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);
            var edge = network.AddEdge(vertex1, vertex2);
            
            var edges = network.Graph.SearchEdgesInBox((4.796, 51.265, 4.798, 51.267));
            Assert.IsNotNull(edges);
            Assert.IsTrue(edges.MoveNext());
            Assert.AreEqual(edge, edges.GraphEnumerator.Id);
            Assert.IsFalse(edges.MoveNext());
        }
    }
}