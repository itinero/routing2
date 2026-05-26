using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Snapping;

/// <summary>
/// Behavior tests for the snapping pipeline that exercise correctness-sensitive
/// paths in <see cref="EdgeSearch"/> / <see cref="Snapper"/>. These are the cases
/// most likely to regress when the spatial index / search box / per-edge work is
/// restructured for performance:
///
/// - geometry: returned snap is on the actual nearest segment, not just the
///   nearest tower node, including on shaped edges
/// - cutoff: MaxDistance rejects results that are within the search box but too far
/// - retry: the OffsetInMeter → OffsetInMeterMax grow-box retry actually fires
/// - drain: ToAllAsync yields every candidate, ToVertexAsync yields the nearest vertex
/// - degenerate edges: parallel edges between the same pair of vertices, self-loops
/// </summary>
public class SnappingBehaviorTests
{
    [Fact]
    public async Task Snap_PointPerpendicularToSegmentMidpoint_ShouldProjectOntoSegment()
    {
        // A straight 100m east-west edge. The query point sits 5m north of the
        // segment midpoint, much closer to the segment than to either tower node.
        var origin = (4.800, 51.27, (float?)null);
        var east100 = origin.OffsetWithDistanceX(100);

        var (routerDb, _, edges) = RouterDbScaffolding.BuildRouterDb(
            new[] { origin, east100 },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, null)
            });

        var queryPoint = origin.OffsetWithDistanceX(50).OffsetWithDistanceY(5);
        var network = routerDb.Latest;

        var result = await network.Snap().ToAsync(queryPoint);

        Assert.False(result.IsError, result.ErrorMessage);
        Assert.Equal(edges[0], result.Value.EdgeId);
        // Snap must land *on* the segment, not at either tower node.
        Assert.NotEqual((ushort)0, result.Value.Offset);
        Assert.NotEqual(ushort.MaxValue, result.Value.Offset);
        // Snap location must be within a couple meters of the query's projection.
        var snapped = result.Value.LocationOnNetwork(network);
        var projection = origin.OffsetWithDistanceX(50);
        Assert.True(snapped.DistanceEstimateInMeter(projection) < 2,
            $"snapped point too far from expected projection: {snapped.DistanceEstimateInMeter(projection):F2}m");
    }

    [Fact]
    public async Task Snap_PointPerpendicularToMiddleShapeSegment_ShouldProjectOnThatSegment()
    {
        // A shaped edge: 4 points in a straight east-west line. The query point
        // sits 5m north of the middle of the *middle* segment (between shape
        // points 1 and 2), forcing the algorithm to evaluate each segment and
        // not just snap to a tower/pillar node.
        var v0 = (4.800, 51.27, (float?)null);
        var v3 = v0.OffsetWithDistanceX(300);
        var s1 = v0.OffsetWithDistanceX(100);
        var s2 = v0.OffsetWithDistanceX(200);

        var (routerDb, _, edges) = RouterDbScaffolding.BuildRouterDb(
            new[] { v0, v3 },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, new[] { s1, s2 })
            });

        var queryPoint = v0.OffsetWithDistanceX(150).OffsetWithDistanceY(5);
        var network = routerDb.Latest;

        var result = await network.Snap(x => x.OffsetInMeter = 200).ToAsync(queryPoint);

        Assert.False(result.IsError, result.ErrorMessage);
        Assert.Equal(edges[0], result.Value.EdgeId);
        // The snap should land near the middle of the edge, not at the tower nodes
        // (offsets 0 / max) and not at the shape points (offsets ≈ 1/3, 2/3).
        var snapped = result.Value.LocationOnNetwork(network);
        var expected = v0.OffsetWithDistanceX(150);
        Assert.True(snapped.DistanceEstimateInMeter(expected) < 2,
            $"snapped point too far from expected segment projection: {snapped.DistanceEstimateInMeter(expected):F2}m");
    }

    [Fact]
    public async Task Snap_NearestEdgeBeyondMaxDistance_ShouldFail()
    {
        // The only edge is ~50m away from the query point. With MaxDistance = 10m
        // the snap must reject it.
        var origin = (4.800, 51.27, (float?)null);
        var east = origin.OffsetWithDistanceX(20);
        var queryPoint = origin.OffsetWithDistanceY(50);

        var (routerDb, _, _) = RouterDbScaffolding.BuildRouterDb(
            new[] { origin, east },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, null)
            });

        var result = await routerDb.Latest.Snap(x =>
        {
            x.OffsetInMeter = 100;
            x.OffsetInMeterMax = 100;
            x.MaxDistance = 10;
        }).ToAsync(queryPoint);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Snap_EdgeWithinMaxDistance_ShouldSucceed()
    {
        // Same edge, same query point, but MaxDistance now well above the
        // distance. The retry/cutoff machinery should not reject the result.
        var origin = (4.800, 51.27, (float?)null);
        var east = origin.OffsetWithDistanceX(20);
        var queryPoint = origin.OffsetWithDistanceY(5);

        var (routerDb, _, edges) = RouterDbScaffolding.BuildRouterDb(
            new[] { origin, east },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, null)
            });

        var result = await routerDb.Latest.Snap(x => x.MaxDistance = 100).ToAsync(queryPoint);

        Assert.False(result.IsError, result.ErrorMessage);
        Assert.Equal(edges[0], result.Value.EdgeId);
    }

    [Fact]
    public async Task Snap_NoEdgeInInitialBox_ButEdgeInRetryBox_ShouldSucceed()
    {
        // The only edge sits ~150m from the query point. The initial box at
        // OffsetInMeter=50 misses it; the retry box at OffsetInMeterMax=300
        // catches it. The snap should succeed via the grow-box retry path.
        var origin = (4.800, 51.27, (float?)null);
        var east = origin.OffsetWithDistanceX(30);
        var queryPoint = origin.OffsetWithDistanceY(150);

        var (routerDb, _, edges) = RouterDbScaffolding.BuildRouterDb(
            new[] { origin, east },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, null)
            });

        var result = await routerDb.Latest.Snap(x =>
        {
            x.OffsetInMeter = 50;
            x.OffsetInMeterMax = 300;
            x.MaxDistance = 300;
        }).ToAsync(queryPoint);

        Assert.False(result.IsError, result.ErrorMessage);
        Assert.Equal(edges[0], result.Value.EdgeId);
    }

    [Fact]
    public async Task Snap_NoEdgeAnywhere_ShouldFail()
    {
        // No edge exists anywhere near the query point. Both the initial and
        // retry boxes find nothing → snap fails cleanly without throwing.
        var origin = (4.800, 51.27, (float?)null);
        var east = origin.OffsetWithDistanceX(20);
        var queryPoint = origin.OffsetWithDistanceY(2000); // 2km north

        var (routerDb, _, _) = RouterDbScaffolding.BuildRouterDb(
            new[] { origin, east },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, null)
            });

        var result = await routerDb.Latest.Snap(x =>
        {
            x.OffsetInMeter = 50;
            x.OffsetInMeterMax = 200;
        }).ToAsync(queryPoint);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task SnapAll_MultipleEdgesInRange_ShouldYieldAtLeastTwo()
    {
        // Two edges within easy reach of the query point. ToAllAsync must yield
        // both (order is not part of the contract today, but multiplicity is).
        var v0 = (4.800, 51.27, (float?)null);
        var v1 = v0.OffsetWithDistanceX(20);
        var v2 = v0.OffsetWithDistanceY(20);

        var (routerDb, _, edges) = RouterDbScaffolding.BuildRouterDb(
            new[] { v0, v1, v2 },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, null),
                (0, 2, null)
            });

        var queryPoint = v0.OffsetWithDistanceX(5).OffsetWithDistanceY(5);
        var network = routerDb.Latest;

        var results = await network.Snap(x => x.OffsetInMeter = 100)
            .ToAllAsync(queryPoint.longitude, queryPoint.latitude).ToArrayAsync();

        var distinctEdges = results.Select(r => r.EdgeId).Distinct().ToArray();
        Assert.Equal(2, distinctEdges.Length);
        Assert.Contains(edges[0], distinctEdges);
        Assert.Contains(edges[1], distinctEdges);
    }

    [Fact]
    public async Task SnapToVertex_QueryNearOneVertex_ShouldReturnNearestVertex()
    {
        // Two vertices on the same edge. The query point sits 2m from v0 and
        // ~18m from v1. ToVertexAsync must pick v0.
        var v0 = (4.800, 51.27, (float?)null);
        var v1 = v0.OffsetWithDistanceX(20);

        var (routerDb, vertices, _) = RouterDbScaffolding.BuildRouterDb(
            new[] { v0, v1 },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, null)
            });

        var queryPoint = v0.OffsetWithDistanceY(2);
        var network = routerDb.Latest;

        var result = await network.Snap(x => x.OffsetInMeter = 100)
            .ToVertexAsync(queryPoint.longitude, queryPoint.latitude);

        Assert.False(result.IsError, result.ErrorMessage);
        Assert.Equal(vertices[0], result.Value);
    }

    [Fact]
    public async Task Snap_ParallelEdgesBetweenSameVertices_PicksGeometricallyClosest()
    {
        // Two edges between the same pair of vertices, one going via a northern
        // shape point and one going via a southern shape point. The query point
        // sits clearly closer to the northern detour. The snap must pick that one.
        var v0 = (4.800, 51.27, (float?)null);
        var v1 = v0.OffsetWithDistanceX(200);
        var northShape = v0.OffsetWithDistanceX(100).OffsetWithDistanceY(30);
        var southShape = v0.OffsetWithDistanceX(100).OffsetWithDistanceY(-30);

        var (routerDb, _, edges) = RouterDbScaffolding.BuildRouterDb(
            new[] { v0, v1 },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, new[] { northShape }),
                (0, 1, new[] { southShape })
            });

        var queryPoint = v0.OffsetWithDistanceX(100).OffsetWithDistanceY(28); // very near northShape
        var network = routerDb.Latest;

        var result = await network.Snap(x => x.OffsetInMeter = 200).ToAsync(queryPoint);

        Assert.False(result.IsError, result.ErrorMessage);
        Assert.Equal(edges[0], result.Value.EdgeId);
    }

    [Fact]
    public async Task Snap_SelfLoopEdge_DoesNotHangAndReturnsResult()
    {
        // An edge whose tail == head, with a small detour shape. Snap must
        // terminate (no infinite loop in shape iteration) and return a valid
        // SnapPoint on that edge.
        var v0 = (4.800, 51.27, (float?)null);
        var loopOut = v0.OffsetWithDistanceX(20);
        var loopBack = v0.OffsetWithDistanceX(20).OffsetWithDistanceY(20);

        var (routerDb, _, edges) = RouterDbScaffolding.BuildRouterDb(
            new[] { v0 },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 0, new[] { loopOut, loopBack })
            });

        var queryPoint = v0.OffsetWithDistanceX(20).OffsetWithDistanceY(10);
        var network = routerDb.Latest;

        var snapTask = network.Snap(x => x.OffsetInMeter = 100).ToAsync(queryPoint);
        var completed = await Task.WhenAny(snapTask, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(snapTask, completed);

        var result = await snapTask;
        Assert.False(result.IsError, result.ErrorMessage);
        Assert.Equal(edges[0], result.Value.EdgeId);
    }
}
