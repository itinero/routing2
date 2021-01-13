using System.Collections.Generic;
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
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801073670387268, 51.268064181900094),
                    (4.801771044731140, 51.268886491558250)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude)[0])
                });

            var network = routerDb.Latest;
            var path = network.BuildPath((new[] {(edges[0], true)}));
            
            var result = RouteBuilder.Default.Build(network, ProfileScaffolding.Any, path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude)[]
            {
                (4.801073670387268, 51.268064181900094),
                (4.801771044731140, 51.268886491558250)
            }, result.Value);
        }
        
        [Fact]
        public void RouteBuilder_Build_OneEdge_Backward_ShouldBuildOneEdgeRoute()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801073670387268, 51.268064181900094),
                    (4.801771044731140, 51.268886491558250)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude)[0])
                });

            var network = routerDb.Latest;
            var path = network.BuildPath((new[] {(edges[0], false)}));
            
            var result = RouteBuilder.Default.Build(network, ProfileScaffolding.Any, path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude)[]
            {
                (4.801771044731140, 51.268886491558250),
                (4.801073670387268, 51.268064181900094)
            }, result.Value);
        }
        
        [Fact]
        public void RouteBuilder_Build_OneEdge_WithShape_Forward_ShouldBuildOneEdgeRoute_WithShape()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801073670387268, 51.268064181900094),
                    (4.801771044731140, 51.268886491558250)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude)[]
                    {
                        (4.800950288772583, 51.268426671236426),
                        (4.801242649555205, 51.268816008449830)
                    }),
                });

            var network = routerDb.Latest;
            var path = network.BuildPath((new[] {(edges[0], true)}));
            
            var result = RouteBuilder.Default.Build(network, ProfileScaffolding.Any, path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude)[]
            {
                (4.801073670387268, 51.268064181900094),
                (4.800950288772583, 51.268426671236426),
                (4.801242649555205, 51.268816008449830),
                (4.801771044731140, 51.268886491558250)
            }, result.Value);
        }
        
        [Fact]
        public void RouteBuilder_Build_OneEdge_WithShape_Backward_ShouldBuildOneEdgeRoute_WithShape()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801073670387268, 51.268064181900094),
                    (4.801771044731140, 51.268886491558250)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude)[]
                    {
                        (4.800950288772583, 51.268426671236426),
                        (4.801242649555205, 51.268816008449830)
                    }),
                });

            var network = routerDb.Latest;
            var path = network.BuildPath((new[] {(edges[0], false)}));
            
            var result = RouteBuilder.Default.Build(network, ProfileScaffolding.Any, path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude)[]
            {
                (4.801771044731140, 51.268886491558250),
                (4.801242649555205, 51.268816008449830),
                (4.800950288772583, 51.268426671236426),
                (4.801073670387268, 51.268064181900094)
            }, result.Value);
            
            var route = result.Value;
            Assert.Equal(4, route.Shape.Count);
            ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250), route.Shape[0]);
            ItineroAsserts.SameLocations((4.801242649555205, 51.268816008449830), route.Shape[1]);
            ItineroAsserts.SameLocations((4.800950288772583, 51.268426671236426), route.Shape[2]);
            ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094), route.Shape[3]);
        }
        
        [Fact]
        public void RouteBuilder_Build_TwoEdges_ForwardForward_ShouldBuildTwoEdgeRoute()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
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
            var path = network.BuildPath((new[]
            {
                (edges[0], true),
                (edges[1], true)
            }));
            
            var result = RouteBuilder.Default.Build(network, ProfileScaffolding.Any, path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude)[]
            {
                (4.801073670387268, 51.268064181900094),
                (4.800950288772583, 51.268426671236426),
                (4.801242649555205, 51.268816008449830),
                (4.801771044731140, 51.268886491558250),
                (4.802066087722777, 51.268582742153434),
                (4.801921248435973, 51.268258852454680),
                (4.802438914775848, 51.268097745847655)
            }, result.Value);
        }
        
        [Fact]
        public void RouteBuilder_Build_TwoEdges_BackwardBackward_ShouldBuildTwoEdgeRoute()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
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
            var path = network.BuildPath((new[]
            {
                (edges[1], false),
                (edges[0], false),
            }));
            
            var result = RouteBuilder.Default.Build(network, ProfileScaffolding.Any, path);
            Assert.False(result.IsError);
            ItineroAsserts.RouteMatches(new (double longitude, double latitude)[]
            {
                (4.802438914775848, 51.268097745847655),
                (4.801921248435973, 51.268258852454680),
                (4.802066087722777, 51.268582742153434),
                (4.801771044731140, 51.268886491558250),
                (4.801242649555205, 51.268816008449830),
                (4.800950288772583, 51.268426671236426),
                (4.801073670387268, 51.268064181900094),
            }, result.Value);
        }
    }
}