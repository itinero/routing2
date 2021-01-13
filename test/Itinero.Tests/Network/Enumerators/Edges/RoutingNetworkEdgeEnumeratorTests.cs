using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.Network.Enumerators.Edges
{
    public class RoutingNetworkEdgeEnumeratorTests
    {
        [Fact]
        public void RoutingNetworkEdgeEnumerator_NoEdges_ShouldEnumerateNoEdges()
        {
            var network = new RoutingNetwork(new RouterDb());
             
            var (vertices, edges) = network.Write(new (double longitude, double latitude)[]
            {
                (4.7868, 51.2643)
            });
             
            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveTo(vertices[0]);
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void RoutingNetworkEdgeEnumerator_OneEdge_ShouldEnumerateOneEdge()
        {
            var network = new RoutingNetwork(new RouterDb());
            var (vertices, edges) = network.Write(new (double longitude, double latitude)[]
            {
                (4.792613983154297, 51.26535213392538),
                (4.797506332397461, 51.26674845584085)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
            {
                (0, 1, null)
            });

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveTo(vertices[0]);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertices[0], enumerator.From);
            Assert.Equal(vertices[1], enumerator.To);
            Assert.True(enumerator.Forward);
        }

        [Fact]
        public void RoutingEdgeNetworkEnumerator_OneEdge_WithShape_ShouldEnumerateShapeForward()
        {
            var network = new RoutingNetwork(new RouterDb());
            var (vertices, edges) = network.Write(new (double longitude, double latitude)[]
            {
                (4.800467491149902, 51.26896368721961),
                (4.801111221313477, 51.26676859478893)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
            {
                (0, 1, new (double longitude, double latitude)[]
                {
                    (4.800703525543213, 51.26832598004091),
                    (4.801368713378906, 51.26782252075405)
                })
            });

            var enumerator = network.GetEdgeEnumerator();
            Assert.True(enumerator.MoveTo(vertices[0]));
            Assert.True(enumerator.MoveNext());

            var shape = enumerator.Shape.ToArray();
            Assert.Equal(2, shape.Length);
            Assert.Equal(4.800703525543213, shape[0].longitude, 4);
            Assert.Equal(51.26832598004091, shape[0].latitude, 4);
            Assert.Equal(4.801368713378906, shape[1].longitude, 4);
            Assert.Equal(51.26782252075405, shape[1].latitude, 4);
        }

        [Fact]
        public void RoutingEdgeNetworkEnumerator_OneEdge_WithShape_ShouldEnumerateShapeBackward()
        {
            var network = new RoutingNetwork(new RouterDb());
            var (vertices, edges) = network.Write(new (double longitude, double latitude)[]
            {
                (4.800467491149902, 51.26896368721961),
                (4.801111221313477, 51.26676859478893)
            }, new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
            {
                (0, 1, new (double longitude, double latitude)[]
                {
                    (4.800703525543213, 51.26832598004091),
                    (4.801368713378906, 51.26782252075405)
                })
            });

            var enumerator = network.GetEdgeEnumerator();
            Assert.True(enumerator.MoveTo(vertices[1]));
            Assert.True(enumerator.MoveNext());

            var shape = enumerator.Shape.ToArray();
            Assert.Equal(2, shape.Length);
            Assert.Equal(4.800703525543213, shape[1].longitude, 4);
            Assert.Equal(51.26832598004091, shape[1].latitude, 4);
            Assert.Equal(4.801368713378906, shape[0].longitude, 4);
            Assert.Equal(51.26782252075405, shape[0].latitude, 4);
        }

        [Fact]
        public void RoutingEdgeNetworkEnumerator_TwoEdges_WithShape_ShouldEnumerateShapes()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude)[]
                {
                    (4.801073670387268, 51.268064181900094),
                    (4.801771044731140, 51.268886491558250),
                    (4.802438914775848, 51.268097745847655)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude)[]
                    {
                        (4.800950288772583, 51.268426671236426),
                        (4.801242649555205, 51.268816008449830)
                    }),
                    (1, 2, new (double longitude, double latitude)[]
                    {
                        (4.802066087722777, 51.268582742153434),
                        (4.801921248435973, 51.268258852454680)
                    })
                });

            var network = routerDb.Latest;

            var enumerator = network.GetEdgeEnumerator();
            
            // go for vertex1.
            Assert.True(enumerator.MoveTo(vertices[0]));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edges[0], enumerator.Id);
            var shape = enumerator.Shape.ToArray();
            Assert.Equal(2, shape.Length);
            ItineroAsserts.SameLocations((4.800950288772583, 51.268426671236426), shape[0]);
            ItineroAsserts.SameLocations((4.801242649555205, 51.268816008449830), shape[1]);
            
            // got for vertex2.
            Assert.True(enumerator.MoveTo(vertices[1]));
            while (enumerator.MoveNext())
            {
                if (enumerator.Id == edges[0])
                {
                    // edge1 in reverse.
                    shape = enumerator.Shape.ToArray();
                    Assert.Equal(2, shape.Length);
                    ItineroAsserts.SameLocations((4.801242649555205, 51.268816008449830), shape[0]);
                    ItineroAsserts.SameLocations((4.800950288772583, 51.268426671236426), shape[1]);
                }
                else if (enumerator.Id == edges[1])
                {
                    // edge2 forward.
                    shape = enumerator.Shape.ToArray();
                    Assert.Equal(2, shape.Length);
                }
            }
            
            // go for vertex3.
            Assert.True(enumerator.MoveTo(vertices[2]));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(edges[1], enumerator.Id);
            shape = enumerator.Shape.ToArray();
            Assert.Equal(2, shape.Length);
            ItineroAsserts.SameLocations((4.801921248435973, 51.268258852454680), shape[0]);
            ItineroAsserts.SameLocations((4.802066087722777, 51.268582742153434), shape[1]);
        }
    }
}