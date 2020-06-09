using System.IO;
using System.Linq;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Serialization;
using Xunit;

namespace Itinero.Tests.Data.Graphs.Serialization
{
    public class GraphSerializerTests
    {
        [Fact]
        public void GraphSerializer_Serialize_Empty_ShouldDeserialize_Empty()
        {
            var expected = new Graph();
            
            var stream = new MemoryStream();
            stream.WriteGraph(expected.GetAsMutable());
            stream.Seek(0, SeekOrigin.Begin);

            var graph = stream.ReadGraph();
            Assert.Empty(graph.GetVertices());
        }
        
        [Fact]
        public void GraphSerializer_Serialize_OneVertex_ShouldDeserialize_OneVertex()
        {
            var expected = new Graph();
            VertexId vertex;
            using (var writer = expected.GetWriter())
            {
                vertex = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            }
            
            var stream = new MemoryStream();
            stream.WriteGraph(expected.GetAsMutable());
            stream.Seek(0, SeekOrigin.Begin);

            var graph = stream.ReadGraph();
            var vertices = graph.GetVertices().ToList();
            Assert.Single(vertices);
            Assert.Equal(vertex, vertices[0]);
        }
        
        [Fact]
        public void GraphSerializer_Serialize_OneEdge_ShouldDeserialize_OneEdge()
        {
            var expected = new Graph();
            VertexId vertex1, vertex2;
            EdgeId edge;
            using (var writer = expected.GetWriter())
            {
                vertex1 = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
                vertex2 = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

                edge = writer.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
                {
                    ("highway", "residential"),
                    ("maxspeed", "50")
                });
            }
            
            var stream = new MemoryStream();
            stream.WriteGraph(expected.GetAsMutable());
            stream.Seek(0, SeekOrigin.Begin);

            var graph = stream.ReadGraph();
            var vertices = graph.GetVertices().ToList();
            Assert.Equal(2, vertices.Count);
            Assert.Equal(vertex1, vertices[0]);
            Assert.Equal(vertex2, vertices[1]);

            var edges = graph.GetEdges().ToList();
            Assert.Single(edges);
            Assert.Equal(edge, edges[0]);
        }
    }
}