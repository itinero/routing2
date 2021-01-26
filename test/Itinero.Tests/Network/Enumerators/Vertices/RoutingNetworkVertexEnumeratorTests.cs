using System.Collections.Generic;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.Network.Enumerators.Vertices
{
    public class RoutingNetworkVertexEnumeratorTests
    {
        [Fact]
        public void RoutingNetwork_VertexEnumerator_Empty_ShouldNotReturnVertices()
        {
            var network = new RoutingNetwork(new RouterDb());

            var enumerator = network.GetVertexEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void RoutingNetwork_VertexEnumerator_OneVertex_ShouldReturnOneVertex()
        {
            var network = new RoutingNetwork(new RouterDb());

            var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[] {
                (4.7868, 51.2643, (float?) null)
            });

            var enumerator = network.GetVertexEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertices[0], enumerator.Current);
        }

        [Fact]
        public void RoutingNetwork_VertexEnumerator_TwoVertices_ShouldReturnTwoVertices()
        {
            var network = new RoutingNetwork(new RouterDb());

            var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[] {
                (4.800467491149902, 51.26896368721961, (float?) null),
                (4.800467491149902, 51.26896368721961, (float?) null)
            });

            var enumerator = network.GetVertexEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertices[0], enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertices[1], enumerator.Current);
        }

        [Fact]
        public void RoutingNetwork_VertexEnumerator_TwoVertices_DifferentTiles_ShouldReturnTwoVertices()
        {
            var network = new RoutingNetwork(new RouterDb());

            var (vertices, edges) = network.Write(new (double longitude, double latitude, float? e)[] {
                (4.800467491149902, 51.26896368721961, (float?) null),
                (5.801111221313477, 51.26676859478893, (float?) null)
            });

            var enumerator = network.GetVertexEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertices[0], enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertices[1], enumerator.Current);
        }
    }
}