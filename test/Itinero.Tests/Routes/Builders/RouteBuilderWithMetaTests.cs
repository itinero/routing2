using System.Collections.Generic;
using System.Linq;
using Itinero.Profiles;
using Itinero.Routes.Builders;
using Itinero.Tests.Profiles;
using Itinero.Tests.Routes.Paths;
using Xunit;

namespace Itinero.Tests.Routes.Builders
{
    public class RouteBuilderWithMetaTests
    {
        [Fact]
        public void RouteBuilder_OneEdgeWithMeta_MetaIsContained()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude)[] {
                    (4.801073670387268, 51.268064181900094),
                    (4.801771044731140, 51.268886491558250)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape, List<(string, string)>
                    attributes)[] {
                        (0, 1, new (double longitude, double latitude)[0],
                            new List<(string, string)> {("name", "Straatnaam")})
                    });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {(edges[0], true)});

            var result = RouteBuilder.Default.Build(network, new DefaultProfile(), path);
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotEmpty(route.ShapeMeta);
            Assert.Equal(route.TotalDistance, route.ShapeMeta.Sum(m => m.Distance));
            Assert.Single(route.ShapeMeta);
            var meta = route.ShapeMeta[0];
            Assert.Equal(1, meta.Shape);
            Assert.Equal(("name", "Straatnaam"), meta.Attributes.ToList()[0]);
        }

        [Fact]
        public void RouteBuilder_Build_TwoEdges_TwoMetas()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude)[] {
                    (4.801073670387268, 51.268064181900094),
                    (4.801771044731140, 51.268886491558250),
                    (4.802438914775848, 51.268097745847655)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape, List<(string, string)>
                    attributes)[] {
                        (0, 1, new (double longitude, double latitude)[] {
                                (4.800950288772583, 51.268426671236426),
                                (4.801242649555205, 51.268816008449830)
                            },
                            new List<(string, string)> {
                                ("name", "A")
                            }),
                        (1, 2, new (double longitude, double latitude)[] {
                                (4.802066087722777, 51.268582742153434),
                                (4.801921248435973, 51.268258852454680)
                            },
                            new List<(string, string)> {
                                ("name", "B")
                            })
                    });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {
                (edges[0], true),
                (edges[1], true)
            });

            var result = RouteBuilder.Default.Build(network, new DefaultProfile(), path);
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotEmpty(route.ShapeMeta);
            Assert.Equal(route.TotalDistance, route.ShapeMeta.Sum(m => m.Distance));

            Assert.Equal(2, route.ShapeMeta.Count);
            var meta0 = route.ShapeMeta[0];
            var meta1 = route.ShapeMeta[1];

            Assert.Equal(3, meta0.Shape); // This is the _end_ of the segment
            Assert.Equal(("name", "A"), meta0.Attributes.ToList()[0]);


            Assert.Equal(6, meta1.Shape);
            Assert.Equal(("name", "B"), meta1.Attributes.ToList()[0]);
        }

        [Fact]
        public void RouteBuilder_Build_TwoEdges_BackwardBackward_ShouldBuildTwoEdgeRoute()
        {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude)[] {
                    (4.801073670387268, 51.268064181900094),
                    (4.801771044731140, 51.268886491558250),
                    (4.802438914775848, 51.268097745847655)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[] {
                    (0, 1, new (double longitude, double latitude)[] {
                        (4.800950288772583, 51.268426671236426),
                        (4.801242649555205, 51.268816008449830)
                    }),
                    (1, 2, new (double longitude, double latitude)[] {
                        (4.802066087722777, 51.268582742153434),
                        (4.801921248435973, 51.268258852454680)
                    })
                });

            var network = routerDb.Latest;
            var path = network.BuildPath(new[] {
                (edges[1], false),
                (edges[0], false)
            });

            var result = RouteBuilder.Default.Build(network, new DefaultProfile(), path);
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotEmpty(route.ShapeMeta);
            Assert.Equal(route.TotalDistance, route.ShapeMeta.Sum(m => m.Distance));
        }
    }
}