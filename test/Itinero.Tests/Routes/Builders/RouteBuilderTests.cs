using System.Collections.Generic;
using Itinero.Profiles;
using Itinero.Routes.Builders;
using Itinero.Tests.Profiles;
using Itinero.Tests.Routes.Paths;
using Xunit;

namespace Itinero.Tests.Routes.Builders
{
    public class RouteBuilderTests
    {
        [Fact]
        public void RouteBuilder_Build_OneEdge_Forward_ShouldBuildOneEdgeRoute()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0])
                });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {(edges[0], true)});

            var result = RouteBuilder.Default.Build(network, new DefaultProfile(), path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude, float? e)[] {
                (4.801073670387268, 51.268064181900094, (float?) null),
                (4.801771044731140, 51.268886491558250, (float?) null)
            }, result.Value);
        }

        [Fact]
        public void RouteBuilder_Build_OneEdge_Backward_ShouldBuildOneEdgeRoute()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0])
                });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {(edges[0], false)});

            var result = RouteBuilder.Default.Build(network, new DefaultProfile(), path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude, float? e)[] {
                (4.801771044731140, 51.268886491558250, (float?) null),
                (4.801073670387268, 51.268064181900094, (float?) null)
            }, result.Value);
        }

        [Fact]
        public void RouteBuilder_Build_OneEdge_WithShape_Forward_ShouldBuildOneEdgeRoute_WithShape()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, (float?) null),
                        (4.801242649555205, 51.268816008449830, (float?) null)
                    })
                });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {(edges[0], true)});

            var result = RouteBuilder.Default.Build(network, new DefaultProfile(), path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude, float? e)[] {
                (4.801073670387268, 51.268064181900094, (float?) null),
                (4.800950288772583, 51.268426671236426, (float?) null),
                (4.801242649555205, 51.268816008449830, (float?) null),
                (4.801771044731140, 51.268886491558250, (float?) null)
            }, result.Value);
        }

        [Fact]
        public void RouteBuilder_Build_OneEdge_WithShape_Backward_ShouldBuildOneEdgeRoute_WithShape()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, (float?) null),
                        (4.801242649555205, 51.268816008449830, (float?) null)
                    })
                });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {(edges[0], false)});

            var result = RouteBuilder.Default.Build(network, new DefaultProfile(), path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude, float? e)[] {
                (4.801771044731140, 51.268886491558250, (float?) null),
                (4.801242649555205, 51.268816008449830, (float?) null),
                (4.800950288772583, 51.268426671236426, (float?) null),
                (4.801073670387268, 51.268064181900094, (float?) null)
            }, result.Value);

            var route = result.Value;
            Assert.Equal(4, route.Shape.Count);
            ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250, (float?) null), route.Shape[0]);
            ItineroAsserts.SameLocations((4.801242649555205, 51.268816008449830, (float?) null), route.Shape[1]);
            ItineroAsserts.SameLocations((4.800950288772583, 51.268426671236426, (float?) null), route.Shape[2]);
            ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094, (float?) null), route.Shape[3]);
        }

        [Fact]
        public void RouteBuilder_Build_TwoEdges_ForwardForward_ShouldBuildTwoEdgeRoute()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null),
                    (4.802438914775848, 51.268097745847655, (float?) null)
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, (float?) null),
                        (4.801242649555205, 51.268816008449830, (float?) null)
                    }),
                    (1, 2, new (double longitude, double latitude, float? e)[] {
                        (4.802066087722777, 51.268582742153434, (float?) null),
                        (4.801921248435973, 51.268258852454680, (float?) null)
                    })
                });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {
                (edges[0], true),
                (edges[1], true)
            });

            var result = RouteBuilder.Default.Build(network, new DefaultProfile(), path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude, float? e)[] {
                (4.801073670387268, 51.268064181900094, (float?) null),
                (4.800950288772583, 51.268426671236426, (float?) null),
                (4.801242649555205, 51.268816008449830, (float?) null),
                (4.801771044731140, 51.268886491558250, (float?) null),
                (4.802066087722777, 51.268582742153434, (float?) null),
                (4.801921248435973, 51.268258852454680, (float?) null),
                (4.802438914775848, 51.268097745847655, (float?) null)
            }, result.Value);
        }

        [Fact]
        public void RouteBuilder_Build_TwoEdges_BackwardBackward_ShouldBuildTwoEdgeRoute()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null),
                    (4.802438914775848, 51.268097745847655, (float?) null)
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, (float?) null),
                        (4.801242649555205, 51.268816008449830, (float?) null)
                    }),
                    (1, 2, new (double longitude, double latitude, float? e)[] {
                        (4.802066087722777, 51.268582742153434, (float?) null),
                        (4.801921248435973, 51.268258852454680, (float?) null)
                    })
                });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {
                (edges[1], false),
                (edges[0], false)
            });

            var result = RouteBuilder.Default.Build(network, new DefaultProfile(), path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude, float? e)[] {
                (4.802438914775848, 51.268097745847655, (float?) null),
                (4.801921248435973, 51.268258852454680, (float?) null),
                (4.802066087722777, 51.268582742153434, (float?) null),
                (4.801771044731140, 51.268886491558250, (float?) null),
                (4.801242649555205, 51.268816008449830, (float?) null),
                (4.800950288772583, 51.268426671236426, (float?) null),
                (4.801073670387268, 51.268064181900094, (float?) null)
            }, result.Value);
        }

        [Fact]
        public void RouteBuilder_Build_OneEdge_Backward_ShouldUseBackwardSpeed()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
                },
                new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, (float?) null),
                        (4.801242649555205, 51.268816008449830, (float?) null)
                    })
                });
            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {
                (edges[0], false)
            });
        
            // This edgeFactor kan move only backward over the edge, e.g. a way with 'oneway=-1'
            var edgeFactor = new EdgeFactor(0, 1, 0, 10);
        
            var result = RouteBuilder.Default.Build(network, new DefaultProfile(getEdgeFactor: (_) => edgeFactor), path);
            Assert.NotNull(result);
            Assert.False(result.IsError);
            var route = result.Value;
            var speed = 100 * route.TotalDistance / route.TotalTime;
            Assert.Equal(10, speed);
        }
    }
}