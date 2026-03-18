using System;
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
        //routerDb.PrepareFor(profile);
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
}
