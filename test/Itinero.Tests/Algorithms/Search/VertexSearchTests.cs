using System.Linq;
using Itinero.Algorithms.Search;
using Itinero.Data.Graphs;
using Xunit;

namespace Itinero.Tests.Algorithms.Search
{
    public class VertexSearchTests
    {
        [Fact]
        public void VertexSearch_SearchVertexInBox_ShouldReturnOnlyVertexInBox()
        {
            var graph = new Graph();
            VertexId vertex1, vertex2;
            using (var writer = graph.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            }
            
            var vertices = graph.SearchVerticesInBox(((4.796, 51.267), (4.798, 51.265)));
            Assert.NotNull(vertices);

            var verticesList = vertices.ToList();
            Assert.Single(verticesList);
            Assert.Equal(vertex2, verticesList[0].vertex);
        }
    }
}