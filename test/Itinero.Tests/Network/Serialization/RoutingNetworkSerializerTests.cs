using System.IO;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Serialization;
using Xunit;

namespace Itinero.Tests.Network.Serialization
{
    public class RoutingNetworkSerializerTests
    {
        [Fact]
        public void RoutingNetworkSerializer_Serialize_Empty_ShouldDeserialize_Empty()
        {
            var expected = new RoutingNetwork(new RouterDb());

            var stream = new MemoryStream();
            expected.GetAsMutable().WriteTo(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var routingNetwork = stream.ReadFrom(new RouterDb());
            Assert.Empty(routingNetwork.GetVertices());
        }

        [Fact]
        public void RoutingNetworkSerializer_Serialize_OneVertex_ShouldDeserialize_OneVertex()
        {
            var expected = new RoutingNetwork(new RouterDb());
            VertexId vertex;
            using (var writer = expected.GetWriter()) {
                vertex = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            }

            var stream = new MemoryStream();
            expected.GetAsMutable().WriteTo(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var graph = stream.ReadFrom(new RouterDb());
            var vertices = graph.GetVertices().ToList();
            Assert.Single(vertices);
            Assert.Equal(vertex, vertices[0]);
        }

        [Fact]
        public void RoutingNetworkSerializer_Serialize_OneEdge_ShouldDeserialize_OneEdge()
        {
            var expected = new RoutingNetwork(new RouterDb());
            VertexId vertex1, vertex2;
            EdgeId edge;
            using (var writer = expected.GetWriter()) {
                vertex1 = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
                vertex2 = writer.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

                edge = writer.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[] {
                    ("highway", "residential"),
                    ("maxspeed", "50")
                });
            }

            var stream = new MemoryStream();
            expected.GetAsMutable().WriteTo(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var graph = stream.ReadFrom(new RouterDb());
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