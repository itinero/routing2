using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Geo.Directions;
using Itinero.Network;
using Itinero.Network.Mutation;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Snapping {
    public class SnapPointExtensionsTests {
        [Fact]
        public void SnapPointExtensions_LocationOnNetwork_Offset0_ShouldReturnVertex1() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (3.1074142456054688, 51.31012070202407, null),
                    (3.1095707416534424, 51.31076453560284, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, null)
                });

            var routerDbLatest = routerDb.Latest;
            var snapPoint = new SnapPoint(edges[0], 0);
            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDbLatest);
            Assert.True(locationOnNetwork.DistanceEstimateInMeter(routerDbLatest.GetVertex(vertices[0])) < .1);
        }

        [Fact]
        public void SnapPointExtensions_LocationOnNetwork_OffsetMax_ShouldReturnVertex2() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (3.1074142456054688, 51.31012070202407, null),
                    (3.1095707416534424, 51.31076453560284, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, null)
                });

            var routerDbLatest = routerDb.Latest;
            var snapPoint = new SnapPoint(edges[0], ushort.MaxValue);
            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDbLatest);
            Assert.True(locationOnNetwork.DistanceEstimateInMeter(routerDbLatest.GetVertex(vertices[1])) < .1);
        }

        [Fact]
        public void SnapPointExtensions_LocationOnNetwork_OffsetHalf_ShouldReturnMiddle() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (3.1074142456054688, 51.31012070202407, null),
                    (3.1095707416534424, 51.31076453560284, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, null)
                });

            var routerDbLatest = routerDb.Latest;
            var snapPoint = new SnapPoint(edges[0], ushort.MaxValue / 2);
            var locationOnNetwork = snapPoint.LocationOnNetwork(routerDbLatest);
            Assert.True(locationOnNetwork.DistanceEstimateInMeter((routerDbLatest.GetVertex(vertices[0]),
                routerDbLatest.GetVertex(vertices[1])).Center()) < .1);
        }

        [Fact]
        public void Snap_Empty_ShouldFail() {
            var routerDb = new RouterDb();

            var result = routerDb.Latest.Snap().To(new VertexId(0, 1));
            Assert.True(result.IsError);
        }

        [Fact]
        public void Snap_OneVertex_ShouldFail() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (3.146638870239258, 51.31060357805506, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[0]);

            var result = routerDb.Latest.Snap().To(vertices[0]);
            Assert.True(result.IsError);
        }

        [Fact]
        public void Snap_OneEdge_Vertex1_ShouldReturnOffset0() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (3.1074142456054688, 51.31012070202407, null),
                    (3.1466388702392580, 51.31060357805506, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, null)
                });

            var result = routerDb.Latest.Snap().To(vertices[0]);
            Assert.False(result.IsError);
            Assert.Equal(0, result.Value.Offset);
            Assert.Equal(edges[0], result.Value.EdgeId);
        }

        [Fact]
        public void Snap_OneEdge_Vertex2_ShouldReturnOffsetMax() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (3.1074142456054688, 51.31012070202407, null),
                    (3.1466388702392580, 51.31060357805506, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, null)
                });

            var result = routerDb.Latest.Snap().To(vertices[1]);
            Assert.False(result.IsError);
            Assert.Equal(ushort.MaxValue, result.Value.Offset);
            Assert.Equal(edges[0], result.Value.EdgeId);
        }

        [Fact]
        public void Snap_OneEdge_Middle_ShouldReturnMiddle() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (3.1074142456054688, 51.31012070202407, null),
                    (3.1095707416534424, 51.31076453560284, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, null)
                });

            var location = ((3.1074142456054688 + 3.1095707416534424) / 2,
                (51.31012070202407 + 51.31076453560284) / 2, (float?) null);
            var routerDbLatest = routerDb.Latest;
            var result = routerDbLatest.Snap().To(location);
            Assert.False(result.IsError);
            Assert.True(result.Value.LocationOnNetwork(routerDbLatest).DistanceEstimateInMeter(location) < 1);
            Assert.Equal(edges[0], result.Value.EdgeId);
        }

        [Fact]
        public void Snap_OneEdgeWithShape_Vertex1_ShouldReturnOffset0() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.800467491149902, 51.26896368721961, null),
                    (4.801111221313477, 51.26676859478893, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800703525543213, 51.26832598004091, null),
                        (4.801368713378906, 51.26782252075405, null)
                    })
                });

            var routerDbLatest = routerDb.Latest;
            var result = routerDbLatest.Snap().To(routerDbLatest.GetVertex(vertices[0]));
            Assert.False(result.IsError);
            Assert.Equal(0, result.Value.Offset);
            Assert.Equal(edges[0], result.Value.EdgeId);
        }

        [Fact]
        public void Snap_OneEdgeWithShape_Vertex2_ShouldReturnOffsetMax() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.800467491149902, 51.26896368721961, null),
                    (4.801111221313477, 51.26676859478893, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800703525543213, 51.26832598004091, null),
                        (4.801368713378906, 51.26782252075405, null)
                    })
                });

            var routerDbLatest = routerDb.Latest;
            var result = routerDbLatest.Snap().To(routerDbLatest.GetVertex(vertices[1]));
            Assert.False(result.IsError);
            Assert.Equal(ushort.MaxValue, result.Value.Offset);
            Assert.Equal(edges[0], result.Value.EdgeId);
        }

        [Fact]
        public void Snap_OneEdgeWithShape_Middle_ShouldReturnMiddle() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.800467491149902, 51.26896368721961, null),
                    (4.801111221313477, 51.26676859478893, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800703525543213, 51.26832598004091, null),
                        (4.801368713378906, 51.26782252075405, null)
                    })
                });

            var routerDbLatest = routerDb.Latest;
            var location = new SnapPoint(edges[0], ushort.MaxValue / 2).LocationOnNetwork(routerDbLatest);
            var result = routerDbLatest.Snap().To(location);
            Assert.False(result.IsError);
            Assert.True(result.Value.LocationOnNetwork(routerDbLatest).DistanceEstimateInMeter(location) < 1);
            Assert.Equal(edges[0], result.Value.EdgeId);
        }

        [Fact]
        public void Snap_TwoEdgeWithShapes_VertexLocation0_ShouldReturnVertex0() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, null),
                    (4.801771044731140, 51.268886491558250, null),
                    (4.802438914775848, 51.268097745847655, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, null),
                        (4.801242649555205, 51.268816008449830, null)
                    }),
                    (1, 2, new (double longitude, double latitude, float? e)[] {
                        (4.802066087722777, 51.268582742153434, null),
                        (4.801921248435973, 51.268258852454680, null)
                    })
                });

            var network = routerDb.Latest;

            var result = network.Snap().To((4.801073670387268, 51.268064181900094, null));
            Assert.False(result.IsError);
            Assert.Equal(edges[0], result.Value.EdgeId);
            Assert.True(
                result.Value.LocationOnNetwork(network).DistanceEstimateInMeter(network.GetVertex(vertices[0])) < 1);
        }

        [Fact]
        public void Snap_TwoEdgeWithShapes_VertexLocation1_ShouldReturnVertex1() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, null),
                    (4.801771044731140, 51.268886491558250, null),
                    (4.802438914775848, 51.268097745847655, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, null),
                        (4.801242649555205, 51.268816008449830, null)
                    }),
                    (1, 2, new (double longitude, double latitude, float? e)[] {
                        (4.802066087722777, 51.268582742153434, (float?) null),
                        (4.801921248435973, 51.268258852454680, (float?) null)
                    })
                });

            var network = routerDb.Latest;

            var result = network.Snap().To((4.801771044731140, 51.268886491558250, null));
            Assert.False(result.IsError);
            Assert.True(
                result.Value.LocationOnNetwork(network).DistanceEstimateInMeter(network.GetVertex(vertices[1])) < 1);
        }

        [Fact]
        public void Snap_TwoEdgeWithShapes_VertexLocation2_ShouldReturnVertex2() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, null),
                    (4.801771044731140, 51.268886491558250, null),
                    (4.802438914775848, 51.268097745847655, null)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, null),
                        (4.801242649555205, 51.268816008449830, null)
                    }),
                    (1, 2, new (double longitude, double latitude, float? e)[] {
                        (4.802066087722777, 51.268582742153434, null),
                        (4.801921248435973, 51.268258852454680, null)
                    })
                });

            var network = routerDb.Latest;

            var result = network.Snap().To((4.802438914775848, 51.268097745847655, null));
            Assert.False(result.IsError);
            Assert.Equal(edges[1], result.Value.EdgeId);
            Assert.True(
                result.Value.LocationOnNetwork(network).DistanceEstimateInMeter(network.GetVertex(vertices[2])) < 1);
        }

        [Fact]
        public void Snap_Direction_0Degrees_PerfectForward_ShouldReturnForward() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.800467491149902, 51.26896368721961, (float?) null),
                    (4.800467491149902, 51.26896368721961, (float?) null).OffsetWithDistanceX(100)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0])
                });

            // calculate direction.
            var routerDbLatest = routerDb.Latest;
            var direction = new SnapPoint(edges[0], ushort.MaxValue / 2).Direction(routerDbLatest, DirectionEnum.East);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
        }

        [Fact]
        public void Snap_Direction_0Degrees_45OffsetForward_ShouldReturnForward() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.800467491149902, 51.26896368721961, (float?) null),
                    (4.800467491149902, 51.26896368721961, (float?) null).OffsetWithDistanceX(100)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0])
                });

            // calculate direction.
            var routerDbLatest = routerDb.Latest;
            var direction =
                new SnapPoint(edges[0], ushort.MaxValue / 2).Direction(routerDbLatest, DirectionEnum.NorthEast);
            Assert.NotNull(direction);
            Assert.True(direction.Value);

            // calculate direction.
            direction = new SnapPoint(edges[0], ushort.MaxValue / 2).Direction(routerDbLatest, DirectionEnum.SouthEast);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
        }

        [Fact]
        public void Snap_Direction_0Degrees_180OffsetForward_ShouldReturnBackward() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.800467491149902, 51.26896368721961, (float?) null),
                    (4.800467491149902, 51.26896368721961, (float?) null).OffsetWithDistanceX(100)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0])
                });

            // calculate direction.
            var routerDbLatest = routerDb.Latest;
            var direction = new SnapPoint(edges[0], ushort.MaxValue / 2).Direction(routerDbLatest, DirectionEnum.West);
            Assert.NotNull(direction);
            Assert.False(direction.Value);
        }

        [Fact]
        public void Snap_Direction_180Degrees_PerfectForward_ShouldReturnForward() {
            var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
                new (double longitude, double latitude, float? e)[] {
                    (4.800467491149902, 51.26896368721961, (float?) null),
                    (4.800467491149902, 51.26896368721961, (float?) null).OffsetWithDistanceX(100)
                },
                new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0])
                });

            // calculate direction.
            var routerDbLatest = routerDb.Latest;
            var direction = new SnapPoint(edges[0], ushort.MaxValue / 2).Direction(routerDbLatest, DirectionEnum.East);
            Assert.NotNull(direction);
            Assert.True(direction.Value);
        }
    }
}