using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Routing;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Routing
{
    public class IRouterOneToOneExtensionsTests
    {
        [Fact]
        public void IRouterOneToOneExtensions_Calculate_OneEdge_ShouldMatchEdge()
        {
            var (routerDb, _, _) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801771044731140, 51.268886491558250),
                    (4.801073670387268, 51.268064181900094)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, null)
                });

            var network = routerDb.Latest;

            var snap1 = network.Snap().To((4.801771044731140, 51.268886491558250));
            var snap2 = network.Snap().To((4.801073670387268, 51.268064181900094));
            
            var result = routerDb.Latest.Route(ProfileScaffolding.Any)
                .From(snap1).To(snap2).Calculate();
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotNull(route.Shape);
            Assert.Equal(2, route.Shape.Count);
            Assert.True((4.801771044731140, 51.268886491558250).DistanceEstimateInMeter(route.Shape[0]) < 1);
            Assert.True((4.801073670387268, 51.268064181900094).DistanceEstimateInMeter(route.Shape[1]) < 1);
        }
        
        [Fact]
        public void IRouterOneToOneExtensions_Calculate_OneEdge_Reverse_ShouldMatchEdge()
        {
            var (routerDb, _, _) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801771044731140, 51.268886491558250),
                    (4.801073670387268, 51.268064181900094)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, null)
                });

            var network = routerDb.Latest;

            var snap1 = network.Snap().To((4.801073670387268, 51.268064181900094));
            var snap2 = network.Snap().To((4.801771044731140, 51.268886491558250));
            
            var result = routerDb.Latest.Route(ProfileScaffolding.Any)
                .From(snap1).To(snap2).Calculate();
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotNull(route.Shape);
            Assert.Equal(2, route.Shape.Count);
            Assert.True((4.801073670387268, 51.268064181900094).DistanceEstimateInMeter(route.Shape[0]) < 1);
            Assert.True((4.801771044731140, 51.268886491558250).DistanceEstimateInMeter(route.Shape[1]) < 1);
        }
        
        [Fact]
        public void IRouterOneToOneExtensions_Calculate_OneEdge_WithShapePoint_ShouldMatchEdge()
        {
            var (routerDb, _, _) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801771044731140, 51.268886491558250),
                    (4.801073670387268, 51.268064181900094)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude)[]
                    {
                        (4.800735712051392, 51.26872203080377)
                    })
                });

            var network = routerDb.Latest;

            var snap1 = network.Snap().To((4.801771044731140, 51.268886491558250));
            var snap2 = network.Snap().To((4.801073670387268, 51.268064181900094));
            
            var result = routerDb.Latest.Route(ProfileScaffolding.Any)
                .From(snap1).To(snap2).Calculate();
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotNull(route.Shape);
            Assert.Equal(3, route.Shape.Count);
            Assert.True((4.801771044731140, 51.268886491558250).DistanceEstimateInMeter(route.Shape[0]) < 1);
            Assert.True((4.800735712051392, 51.268722030803770).DistanceEstimateInMeter(route.Shape[1]) < 1);
            Assert.True((4.801073670387268, 51.268064181900094).DistanceEstimateInMeter(route.Shape[2]) < 1);
        }
        
        [Fact]
        public void IRouterOneToOneExtensions_Calculate_OneEdge_Reverse_WithShapePoint_ShouldMatchEdge()
        {
            var (routerDb, _, _) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801771044731140, 51.268886491558250),
                    (4.801073670387268, 51.268064181900094)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude)[]
                    {
                        (4.800735712051392, 51.26872203080377)
                    })
                });

            var network = routerDb.Latest;

            var snap1 = network.Snap().To((4.801073670387268, 51.268064181900094));
            var snap2 = network.Snap().To((4.801771044731140, 51.268886491558250));
            
            var result = routerDb.Latest.Route(ProfileScaffolding.Any)
                .From(snap1).To(snap2).Calculate();
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotNull(route.Shape);
            Assert.Equal(3, route.Shape.Count);
            Assert.True((4.801073670387268, 51.268064181900094).DistanceEstimateInMeter(route.Shape[0]) < 1);
            Assert.True((4.800735712051392, 51.268722030803770).DistanceEstimateInMeter(route.Shape[1]) < 1);
            Assert.True((4.801771044731140, 51.268886491558250).DistanceEstimateInMeter(route.Shape[2]) < 1);
        }
        
        [Fact]
        public void IRouterOneToOneExtensions_Calculate_OneEdge_With2ShapePoints_ShouldMatchEdge()
        {
            var (routerDb, _, _) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801771044731140, 51.268886491558250),
                    (4.801073670387268, 51.268064181900094)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude)[]
                    {
                        (4.801242649555205, 51.268816008449830),
                        (4.800950288772583, 51.268426671236426)
                    })
                });

            var network = routerDb.Latest;

            var snap1 = network.Snap().To((4.801771044731140, 51.268886491558250));
            var snap2 = network.Snap().To((4.801073670387268, 51.268064181900094));
            
            var result = routerDb.Latest.Route(ProfileScaffolding.Any)
                .From(snap1).To(snap2).Calculate();
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotNull(route.Shape);
            Assert.Equal(4, route.Shape.Count);
            Assert.True((4.801771044731140, 51.268886491558250).DistanceEstimateInMeter(route.Shape[0]) < 1);
            Assert.True((4.801242649555205, 51.268816008449830).DistanceEstimateInMeter(route.Shape[1]) < 1);
            Assert.True((4.800950288772583, 51.268426671236426).DistanceEstimateInMeter(route.Shape[2]) < 1);
            Assert.True((4.801073670387268, 51.268064181900094).DistanceEstimateInMeter(route.Shape[3]) < 1);
        }
        
        [Fact]
        public void IRouterOneToOneExtensions_Calculate_OneEdge_Reverse_With2ShapePoints_ShouldMatchEdge()
        {
            var (routerDb, _, _) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
                {
                    (4.801771044731140, 51.268886491558250),
                    (4.801073670387268, 51.268064181900094)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude)>? shape)[]
                {
                    (0, 1, new (double longitude, double latitude)[]
                    {
                        (4.801242649555205, 51.268816008449830),
                        (4.800950288772583, 51.268426671236426)
                    })
                });

            var network = routerDb.Latest;

            var snap1 = network.Snap().To((4.801073670387268, 51.268064181900094));
            var snap2 = network.Snap().To((4.801771044731140, 51.268886491558250));
            
            var result = routerDb.Latest.Route(ProfileScaffolding.Any)
                .From(snap1).To(snap2).Calculate();
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotNull(route.Shape);
            Assert.Equal(4, route.Shape.Count);
            Assert.True((4.801073670387268, 51.268064181900094).DistanceEstimateInMeter(route.Shape[0]) < 1);
            Assert.True((4.800950288772583, 51.268426671236426).DistanceEstimateInMeter(route.Shape[1]) < 1);
            Assert.True((4.801242649555205, 51.268816008449830).DistanceEstimateInMeter(route.Shape[2]) < 1);
            Assert.True((4.801771044731140, 51.268886491558250).DistanceEstimateInMeter(route.Shape[3]) < 1);
        }
        
        [Fact]
        public void IRouterOneToOneExtensions_Calculate_TwoEdge_With2ShapePoints_ShouldMatchEdges()
        {
            var (routerDb, _, _) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude)[]
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

            var snap1 = network.Snap().To((4.801073670387268, 51.268064181900094));
            var snap2 = network.Snap().To((4.802438914775848, 51.268097745847650));
            
            var result = routerDb.Latest.Route(ProfileScaffolding.Any)
                .From(snap1).To(snap2).Calculate();
            Assert.False(result.IsError);
            var route = result.Value;
            Assert.NotNull(route.Shape);
            Assert.Equal(7, route.Shape.Count);
            Assert.True((4.801073670387268, 51.268064181900094).DistanceEstimateInMeter(route.Shape[0]) < 1);
            Assert.True((4.800950288772583, 51.268426671236426).DistanceEstimateInMeter(route.Shape[1]) < 1);
            Assert.True((4.801242649555205, 51.268816008449830).DistanceEstimateInMeter(route.Shape[2]) < 1);
            Assert.True((4.801771044731140, 51.268886491558250).DistanceEstimateInMeter(route.Shape[3]) < 1);
            Assert.True((4.802066087722777, 51.268582742153434).DistanceEstimateInMeter(route.Shape[4]) < 1);
            Assert.True((4.801921248435973, 51.268258852454680).DistanceEstimateInMeter(route.Shape[5]) < 1);
            Assert.True((4.802438914775848, 51.268097745847650).DistanceEstimateInMeter(route.Shape[6]) < 1);
        }
    }
}