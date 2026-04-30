using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.IO.Osm;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Routing;
using Itinero.Snapping;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm;

public class FunctionalRoutingTests
{
    private static RouterDb LoadOsmData(OsmGeo[] os, Profile profile)
    {
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 0 });
        routerDb.PrepareFor(profile);
        routerDb.UseOsmData(new OsmEnumerableStreamSource(os), s =>
        {
            s.TagsFilter.Filter = null;
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });
        return routerDb;
    }

    [Fact]
    public async Task SingleWay_CarProfile_ShouldRouteFromStartToEnd()
    {
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile);

        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap2 = await network.Snap(profile).ToAsync(4.801, 51.269);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap2.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);
        Assert.NotNull(route.Value);
        Assert.True(route.Value.Shape.Count >= 2);
    }

    [Fact]
    public async Task SingleWay_CarProfile_DataIsLoaded()
    {
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile);

        var network = routerDb.Latest;

        // check vertices.
        var vertices = network.GetVertexEnumerator();
        Assert.True(vertices.MoveNext());
        var firstVertex = vertices.Current;
        Assert.True(vertices.MoveNext());
        Assert.False(vertices.MoveNext());

        // check edge from first vertex.
        var edges = network.GetEdgeEnumerator();
        Assert.True(edges.MoveTo(firstVertex));
        Assert.True(edges.MoveNext());
    }

    [Fact]
    public async Task SingleWay_CarProfile_SnapWorksAtBothEnds()
    {
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile);

        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);

        var snap2 = await network.Snap(profile).ToAsync(4.801, 51.269);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        Assert.Equal(snap1.Value.EdgeId, snap2.Value.EdgeId);
    }

    [Fact]
    public async Task TwoWays_CarProfile_ShouldLoadThreeVertices()
    {
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.269 },
            new Node { Id = 3, Longitude = 4.802, Latitude = 51.268 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile);

        var network = routerDb.Latest;

        var vertices = network.GetVertexEnumerator();
        var count = 0;
        while (vertices.MoveNext()) count++;
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task TwoWays_CrossTileBoundary_CarProfile_ShouldRoute()
    {
        // zoom-14 tile boundary at longitude ~4.8120
        // node 1 at lon 4.810 → tile x=8410
        // node 2 at lon 4.813 → tile x=8411
        // node 3 at lon 4.816 → tile x=8411
        // way 1 (nodes 1→2) crosses the tile boundary, way 2 (nodes 2→3) stays in tile 8411.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.810, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.813, Latitude = 51.270 },
            new Node { Id = 3, Longitude = 4.816, Latitude = 51.270 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile);

        var network = routerDb.Latest;

        // snap at each end — these are in different tiles.
        var snap1 = await network.Snap(profile).ToAsync(4.810, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap2 = await network.Snap(profile).ToAsync(4.816, 51.270);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        // route should cross the tile boundary.
        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap2.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);
        Assert.NotNull(route.Value);
        Assert.True(route.Value.Shape.Count >= 3);
    }

    [Fact]
    public async Task Barrier_CarProfile_ShouldBlockRoute()
    {
        // network: node1 —way1→ node2(barrier) —way2→ node3
        //            \—way3→ node4 —way4→ node5 —way5→ node3
        // direct path (1→2→3) is short but blocked by barrier on node2.
        // alternative path (1→4→5→3) is longer but passable.
        // without the barrier the router would pick the short direct path.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.8005, Latitude = 51.2695,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.801, Latitude = 51.269 },
            // detour nodes — placed far away to make the alternative path clearly longer.
            new Node { Id = 4, Longitude = 4.795, Latitude = 51.265 },
            new Node { Id = 5, Longitude = 4.806, Latitude = 51.265 },
            // direct short path through barrier.
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            // long alternative path around the barrier.
            new Way
            {
                Id = 3, Nodes = new[] { 1L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 4, Nodes = new[] { 4L, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 5, Nodes = new[] { 5L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        }, profile);

        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap2 = await network.Snap(profile).ToAsync(4.802, 51.268);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap2.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);
        Assert.NotNull(route.Value);

        // the route should go via node4 and node5 (the long detour), not through node2.
        Assert.True(route.Value.Shape.Count >= 4);

        // verify the route does NOT pass through the barrier node (4.8005, 51.2695).
        var passesBarrier = route.Value.Shape.Any(s =>
            Math.Abs(s.longitude - 4.8005) < 0.0001 &&
            Math.Abs(s.latitude - 51.2695) < 0.0001);
        Assert.False(passesBarrier, "Route should not pass through the barrier node");
    }

    [Fact]
    public async Task Barrier_CarProfile_NoAlternative_ShouldFailRoute()
    {
        // network: node1 —way1→ node2(barrier) —way2→ node3
        // no alternative path — the barrier blocks the only route.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.8005, Latitude = 51.2695,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        }, profile);

        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap2 = await network.Snap(profile).ToAsync(4.801, 51.269);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap2.Value)
            .CalculateAsync();

        Assert.True(route.IsError, "Route through barrier with no alternative should fail");
    }

    [Fact]
    public async Task Barrier_WithMotorcarYes_CarProfile_ShouldPassThrough()
    {
        // same as above but the barrier has motorcar=yes, so the car can pass.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.8005, Latitude = 51.2695,
                Tags = new TagsCollection(
                    new Tag("barrier", "bollard"),
                    new Tag("motorcar", "yes"))
            },
            new Node { Id = 3, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        }, profile);

        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap2 = await network.Snap(profile).ToAsync(4.801, 51.269);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap2.Value)
            .CalculateAsync();

        Assert.False(route.IsError, $"Route through barrier with motorcar=yes failed: {route.ErrorMessage}");
    }

    [Fact]
    public async Task ThreeNodes_TwoWays_CarProfile_ShouldRoute()
    {
        // minimal test: 3 nodes, 2 ways, no barrier, no restrictions.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.8005, Latitude = 51.2695 },
            new Node { Id = 3, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        }, profile);

        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap2 = await network.Snap(profile).ToAsync(4.801, 51.269);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap2.Value)
            .CalculateAsync();

        Assert.False(route.IsError, $"Simple 3-node route failed: {route.ErrorMessage}");
    }

    [Fact]
    public async Task NoRightTurn_CarProfile_ShouldTakeDetour()
    {
        // T-junction: way1(1→2) meets way2(3→2) and way3(2→4).
        // no_right_turn from way1 to way3 via node 2.
        // way4(1→3) and way5(3→4) provide a longer alternative.
        //
        //   node1 ——way1——→ node2 ——way3——→ node4
        //     \               ↑              ↑
        //    way4           way2            way5
        //       \             |              |
        //        → node3 ————+——————————————+
        //
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.270 },
            new Node { Id = 3, Longitude = 4.800, Latitude = 51.265 },
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.270 },
            // direct path: 1→2→4
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 3L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 3, Nodes = new[] { 2L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            // detour: 1→3→4
            new Way
            {
                Id = 4, Nodes = new[] { 1L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 5, Nodes = new[] { 3L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            // no_right_turn: from way1 via node2 to way3
            new Relation
            {
                Id = 1,
                Members = new[]
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Node),
                    new RelationMember(3, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "no_right_turn"))
            }
        }, profile);

        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap4 = await network.Snap(profile).ToAsync(4.802, 51.270);
        Assert.False(snap4.IsError, snap4.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap4.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);
        Assert.NotNull(route.Value);

        // the route should go via node3 (the detour), not directly through node2.
        // so it should pass through the detour area (lat ~51.265).
        var goesViaSouth = route.Value.Shape.Any(s => s.latitude < 51.268);
        Assert.True(goesViaSouth, "Route should detour via node3 (south) due to no_right_turn restriction");
    }

    [Fact]
    public async Task NoRightTurn_CarProfile_NoAlternative_ShouldFail()
    {
        // same T-junction but without the detour — no_right_turn blocks the only path.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.270 },
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.270 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 3, Nodes = new[] { 2L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Relation
            {
                Id = 1,
                Members = new[]
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Node),
                    new RelationMember(3, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "no_right_turn"))
            }
        }, profile);

        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap4 = await network.Snap(profile).ToAsync(4.802, 51.270);
        Assert.False(snap4.IsError, snap4.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap4.Value)
            .CalculateAsync();

        Assert.True(route.IsError, "Route through no_right_turn with no alternative should fail");
    }

    [Fact]
    public async Task OnlyRightTurn_CarProfile_ShouldAllowRight()
    {
        // Cross intersection: way1(1→2), way2(2→3 right), way3(2→4 left), way4(2→5 straight).
        // only_right_turn from way1 via node2 to way2 — only the right turn is allowed.
        // Route from 1→3 (right) should succeed via the direct path.
        //
        //                 node5
        //                  ↑
        //                way4
        //                  |
        //   node1 ——way1—→ node2 ——way2——→ node3
        //                  |
        //                way3
        //                  ↓
        //                 node4
        //
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.802, Latitude = 51.270 },
            new Node { Id = 3, Longitude = 4.804, Latitude = 51.270 },   // right
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.265 },   // left (south)
            new Node { Id = 5, Longitude = 4.802, Latitude = 51.275 },   // straight (north)
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 3, Nodes = new[] { 2L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 4, Nodes = new[] { 2L, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            // only_right_turn: from way1 via node2 to way2
            new Relation
            {
                Id = 1,
                Members = new[]
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Node),
                    new RelationMember(2, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "only_right_turn"))
            }
        }, profile);

        var network = routerDb.Latest;

        // route 1→3 (the allowed right turn) should succeed.
        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap3 = await network.Snap(profile).ToAsync(4.804, 51.270);
        Assert.False(snap3.IsError, snap3.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap3.Value)
            .CalculateAsync();

        Assert.False(route.IsError, $"Right turn should be allowed: {route.ErrorMessage}");
    }

    [Fact]
    public async Task OnlyRightTurn_CarProfile_ShouldBlockOtherTurns()
    {
        // same intersection as above, but route from 1→4 (left turn — blocked by only_right_turn).
        // no alternative path exists, so the route should fail.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.802, Latitude = 51.270 },
            new Node { Id = 3, Longitude = 4.804, Latitude = 51.270 },   // right (allowed)
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.265 },   // left (blocked)
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 3, Nodes = new[] { 2L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            // only_right_turn: from way1 via node2 to way2
            new Relation
            {
                Id = 1,
                Members = new[]
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Node),
                    new RelationMember(2, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "only_right_turn"))
            }
        }, profile);

        var network = routerDb.Latest;

        // route 1→4 (left turn — blocked) should fail.
        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap4 = await network.Snap(profile).ToAsync(4.802, 51.265);
        Assert.False(snap4.IsError, snap4.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap4.Value)
            .CalculateAsync();

        Assert.True(route.IsError, "Left turn should be blocked by only_right_turn restriction");
    }

    [Fact]
    public async Task OnlyRightTurn_TurnCostFactorEnabledProfile_ShouldBlockStraightOn()
    {
        // Mirrors the publish-api scenario: profiles in publish-api set
        // TurnCostFactorEnabled=true, which makes the router dispatch to
        // EdgeBased.Dijkstra. The existing OnlyRightTurn tests use
        // OsmProfiles.Car (Lua, TurnCostFactorEnabled=false → BidirectionalDijkstra).
        // This test confirms the engine + resolver pair handle only_right_turn
        // when going through the EdgeBased path.
        //
        // Setup: from way1, via node 2, only_right_turn to way2 (right).
        // way3 is the "straight on" alternative — must be blocked.
        // way4 is the "left" — also must be blocked.
        // Routing 1 → straight (node 5) must fail.
        var profile = new TurnCostFactorEnabledCarProfile();
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },          // origin
            new Node { Id = 2, Longitude = 4.802, Latitude = 51.270 },          // via
            new Node { Id = 3, Longitude = 4.802, Latitude = 51.272 },          // right (north of via)
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.265 },          // left (south of via)
            new Node { Id = 5, Longitude = 4.804, Latitude = 51.270 },          // straight (east of via)
            new Way { Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 4, Nodes = new[] { 2L, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Relation
            {
                Id = 1,
                Members = new[]
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Node),
                    new RelationMember(2, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "only_right_turn"))
            }
        }, profile);

        var network = routerDb.Latest;
        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snapStraight = await network.Snap(profile).ToAsync(4.804, 51.270);
        Assert.False(snapStraight.IsError, snapStraight.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snapStraight.Value)
            .CalculateAsync();

        Assert.True(route.IsError,
            "Going straight (way1 → way4) must fail under only_right_turn even with TurnCostFactorEnabled=true profile");
    }

    /// <summary>
    /// Same as <see cref="Itinero.Profiles.Lua.Osm.OsmProfiles.Car"/> but with
    /// TurnCostFactorEnabled=true to dispatch to EdgeBased.Dijkstra (matches
    /// publish-api's profile mode).
    /// </summary>
    private sealed class TurnCostFactorEnabledCarProfile : Profile
    {
        public override string Name => "test-car-tcf-enabled";
        public override bool TurnCostFactorEnabled => true;
        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
        {
            // accept "highway=residential" as a routable forward+backward edge with realistic speed.
            foreach (var (k, v) in attributes)
            {
                if (k == "highway" && v == "residential")
                {
                    return new EdgeFactor(1, 1, 5000, 5000); // 50 km/h ≈ 13.89 m/s × 100
                }
            }
            return EdgeFactor.NoFactor;
        }
        public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
        {
            // any restriction relation tagged with restriction=* is a hard block.
            foreach (var (k, _) in attributes)
            {
                if (k == "restriction") return Itinero.Profiles.TurnCostFactor.Binary;
            }
            return Itinero.Profiles.TurnCostFactor.Empty;
        }
    }

    [Fact]
    public async Task OnlyRightTurn_CarProfile_FromWaySplitByInteriorJunction_ShouldStillBlockLeftTurn()
    {
        // Same intersection as OnlyRightTurn_CarProfile_ShouldBlockOtherTurns,
        // but the FROM-way is split by an interior junction:
        //   way 1 = [a, j, via]   (a→via, but split at j by way 4)
        //   way 4 = [j, x]        (creates interior junction at j)
        //   way 2 = [via, right]  (the only allowed turn — right)
        //   way 3 = [via, left]   (must remain blocked)
        // Routing a→left must still fail despite the from-way being split.
        // Stresses the walk-from-anchor resolver under the mandatory branch.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },          // a
            new Node { Id = 5, Longitude = 4.801, Latitude = 51.270 },          // j (interior junction)
            new Node { Id = 2, Longitude = 4.802, Latitude = 51.270 },          // via
            new Node { Id = 3, Longitude = 4.804, Latitude = 51.270 },          // right (allowed)
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.265 },          // left (blocked)
            new Node { Id = 6, Longitude = 4.801, Latitude = 51.272 },          // x (north of j)
            new Way { Id = 1, Nodes = new[] { 1L, 5, 2 },                       // a → j → via (3 nodes)
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 4, Nodes = new[] { 5L, 6 },                           // j → x (makes j a junction)
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 2L, 3 },                           // via → right
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 },                           // via → left
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Relation
            {
                Id = 1,
                Members = new[]
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Node),
                    new RelationMember(2, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "only_right_turn"))
            }
        }, profile);

        var network = routerDb.Latest;
        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap4 = await network.Snap(profile).ToAsync(4.802, 51.265);
        Assert.False(snap4.IsError, snap4.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap4.Value)
            .CalculateAsync();

        Assert.True(route.IsError,
            "Left turn (way1 → way3) should remain blocked by only_right_turn even though way1 has an interior junction");
    }

    [Fact]
    public async Task OnlyRightTurn_CarProfile_ToWaySplitByInteriorJunction_ShouldStillBlockLeftTurn()
    {
        // Mandatory restriction with the TO-way (the allowed direction) split by an interior junction:
        //   way 1 = [a, via]
        //   way 2 = [via, j, right]   (the allowed turn, but split at j by way 5)
        //   way 5 = [j, x]            (interior junction at j)
        //   way 3 = [via, left]       (must remain blocked)
        // Routing a→left must still fail.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.802, Latitude = 51.270 },           // via
            new Node { Id = 5, Longitude = 4.803, Latitude = 51.270 },           // j
            new Node { Id = 3, Longitude = 4.804, Latitude = 51.270 },           // right
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.265 },           // left
            new Node { Id = 6, Longitude = 4.803, Latitude = 51.272 },           // x
            new Way { Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 2L, 5, 3 },                        // via → j → right
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 5, Nodes = new[] { 5L, 6 },                           // splits way 2 at j
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Relation
            {
                Id = 1,
                Members = new[]
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Node),
                    new RelationMember(2, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "only_right_turn"))
            }
        }, profile);

        var network = routerDb.Latest;
        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        var snap4 = await network.Snap(profile).ToAsync(4.802, 51.265);
        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap4.Value)
            .CalculateAsync();

        Assert.True(route.IsError,
            "Left turn should remain blocked even when the to-way (right turn target) is split by an interior junction");
    }

    [Fact]
    public async Task Barrier_CarProfile_BollardAtSharedNodeWithToWayInteriorJunction_ShouldBlockRoute()
    {
        // End-to-end variant of the resolver bug: bollard at the shared node
        // between way 1 (a→bollard) and way 2 (bollard→j2→c). Way 3 (j2→x)
        // forces an interior junction at j2 that splits way 2 into stored
        // sub-edges (way2, 0, 1) and (way2, 1, 2). The bollard's "going from a
        // toward c" restriction (chain [(way1, 0, 1), (way2, 0, 2)]) hits the
        // resolver bug: way2's tail-hop subsection search falls on the wrong
        // sub-edge (way2, 1, 2) instead of the bollard-adjacent (way2, 0, 1).
        // The misplaced turn cost lands on j2 with an unfireable edge pair,
        // so traversal from a through bollard to c is no longer blocked.
        //
        // Without the bug, the route from a→c is impossible (no alternative).
        // With the bug, the router happily routes straight through. This test
        // expects the route to fail; it currently succeeds and so the test
        // is RED until the resolver is fixed.

        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.269 },
            new Node
            {
                Id = 2, Longitude = 4.801, Latitude = 51.269,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.802, Latitude = 51.269 },
            new Node { Id = 4, Longitude = 4.803, Latitude = 51.269 },
            new Node { Id = 5, Longitude = 4.802, Latitude = 51.270 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 3, Nodes = new[] { 3L, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile);

        var network = routerDb.Latest;

        var snapA = await network.Snap(profile).ToAsync(4.800, 51.269);
        Assert.False(snapA.IsError, snapA.ErrorMessage);
        var snapC = await network.Snap(profile).ToAsync(4.803, 51.269);
        Assert.False(snapC.IsError, snapC.ErrorMessage);

        var route = await network.Route(profile)
            .From(snapA.Value)
            .To(snapC.Value)
            .CalculateAsync();

        Assert.True(route.IsError,
            "Route from a to c should fail — bollard blocks the only path through. " +
            "If this test passes, the bollard turn cost has been misplaced (resolver bug).");
    }

    [Fact(Skip = "Diagnostic-only — kept for reference, not a real test.")]
    public async Task DEBUG_DumpDiagForExistingPassingBollardTest()
    {
        // re-run the existing passing test pattern with the same diagnostic so
        // we can compare the turn-cost data structure to the failing case.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.8005, Latitude = 51.2695,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile);

        var network = routerDb.Latest;

        var diag = new System.Text.StringBuilder();
        var enumerator = network.GetEdgeEnumerator();
        foreach (var v in network.GetVertices())
        {
            if (!network.TryGetVertex(v, out var vLon, out var vLat, out _)) continue;
            enumerator.MoveTo(v);
            var anyOrder = false;
            var info = new System.Text.StringBuilder();
            while (enumerator.MoveNext())
            {
                if (enumerator.TailOrder != null || enumerator.HeadOrder != null)
                {
                    anyOrder = true;
                    info.Append($"e{enumerator.EdgeId} t={enumerator.TailOrder} h={enumerator.HeadOrder} fwd={enumerator.Forward}; ");
                    for (byte src = 0; src < 4; src++)
                    {
                        foreach (var tc in enumerator.GetTurnCostFromTail(src))
                            info.Append($"[fromTail src={src} cost={tc.cost}] ");
                        foreach (var tc in enumerator.GetTurnCostFromHead(src))
                            info.Append($"[fromHead src={src} cost={tc.cost}] ");
                    }
                }
            }
            if (anyOrder) diag.Append($"\nv({vLon:F5},{vLat:F5}): {info}");
        }

        // intentionally fail to dump diag.
        Assert.Fail($"DIAG of passing test: {diag}");
    }

    [Fact(Skip = "Diagnostic control — confirms simple bollard still blocks; kept for reference.")]
    public async Task DEBUG_Barrier_CarProfile_BollardSameStructureNoWay3_ShouldBlock()
    {
        // sanity: same node/way ids as the failing test but with way 3 removed
        // and j2 not present. bollard with simple way 2 → should block (existing
        // mechanism). this isolates whether the issue is in the resolver or in
        // some other behavior of the test's larger network.
        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.269 },
            new Node
            {
                Id = 2, Longitude = 4.801, Latitude = 51.269,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 4, Longitude = 4.803, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile);

        var network = routerDb.Latest;

        var snapA = await network.Snap(profile).ToAsync(4.800, 51.269);
        var snapC = await network.Snap(profile).ToAsync(4.803, 51.269);
        var route = await network.Route(profile)
            .From(snapA.Value)
            .To(snapC.Value)
            .CalculateAsync();

        Assert.True(route.IsError, "control: simple bollard with no interior junction should still block");
    }

    [Fact]
    public async Task TurnRestriction_CarProfile_NoStraightOn_WithToWayInteriorJunction_ShouldBlockRoute()
    {
        // End-to-end variant of the resolver bug for turn restrictions.
        // OSM relation: from=way1 [a, via], via=node 'via', to=way2 [via, j2, c],
        // restriction=no_straight_on. Way 3 [j2, x] makes j2 an interior
        // junction that splits way 2 into (way2, 0, 1) and (way2, 1, 2).
        //
        // Resolver chain: [(way1, 0, 1), (way2, 0, 2)]. Head-hop's subsection
        // search falls on the wrong sub-edge (way2, 1, 2) instead of the
        // via-adjacent (way2, 0, 1). The misplaced turn cost lands on j2,
        // not on via, so the no-straight-on restriction is unenforced and
        // a→c routes straight through.

        var profile = OsmProfiles.Car;
        var routerDb = LoadOsmData(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.269 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.269 },
            new Node { Id = 3, Longitude = 4.802, Latitude = 51.269 },
            new Node { Id = 4, Longitude = 4.803, Latitude = 51.269 },
            new Node { Id = 5, Longitude = 4.802, Latitude = 51.270 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 2, Nodes = new[] { 2L, 3, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 3, Nodes = new[] { 3L, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Relation
            {
                Id = 1,
                Members = new[]
                {
                    new RelationMember(1, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Node),
                    new RelationMember(2, "to", OsmGeoType.Way)
                },
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "no_straight_on"))
            }
        }, profile);

        var network = routerDb.Latest;

        var snapA = await network.Snap(profile).ToAsync(4.800, 51.269);
        Assert.False(snapA.IsError, snapA.ErrorMessage);
        var snapC = await network.Snap(profile).ToAsync(4.803, 51.269);
        Assert.False(snapC.IsError, snapC.ErrorMessage);

        var route = await network.Route(profile)
            .From(snapA.Value)
            .To(snapC.Value)
            .CalculateAsync();

        Assert.True(route.IsError,
            "Route from a to c should fail — no_straight_on blocks the only path through. " +
            "If this test passes, the turn-restriction cost has been misplaced (resolver bug).");
    }
}
