using Itinero.Data.Graphs;
using Xunit;

namespace Itinero.Tests.Data
{
    public class NetworkTests
    {
        [Fact]
        public void NetworkGraphEnumerator_ShouldEnumerateEdgesInGraph()
        {
            var network = new Graph();
            
            VertexId vertex1, vertex2;
            using (var writer = network.GetWriter())
            {
                vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
                vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

                writer.AddEdge(vertex1, vertex2);
            }

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.From);
            Assert.Equal(vertex2, enumerator.To);
            Assert.True(enumerator.Forward);
        }
    }
}