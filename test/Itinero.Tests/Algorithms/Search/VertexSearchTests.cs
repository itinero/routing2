using System.Linq;
using Itinero.Algorithms.Search;
using Itinero.Data.Graphs;
using NUnit.Framework;

namespace Itinero.Tests.Algorithms.Search
{
    public class VertexSearchTests
    {
        [Test]
        public void VertexSearch_SearchVertexInBox_ShouldReturnOnlyVertexInBox()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var vertices = graph.SearchVerticesInBox((4.796, 51.265, 4.798, 51.267));
            Assert.IsNotNull(vertices);

            var verticesList = vertices.ToList();
            Assert.AreEqual(1, verticesList.Count);
            Assert.AreEqual(vertex2, verticesList[0].vertex);
        }
    }
}