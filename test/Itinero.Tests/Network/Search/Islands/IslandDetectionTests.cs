using System.Linq;
using System.Threading.Tasks;
using Itinero.IO.Osm;
using Itinero.Network;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Routing;
using Itinero.Snapping;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

public class IslandDetectionTests
{
    private static RouterDb BuildNetwork(OsmGeo[] os, Profile profile, int maxIslandSize = 0)
    {
        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            MaxIslandSize = maxIslandSize
        });
        routerDb.PrepareFor(profile);
        routerDb.UseOsmData(new OsmEnumerableStreamSource(os), s =>
        {
            s.TagsFilter.Filter = null;
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });
        return routerDb;
    }

    /// <summary>
    /// Builds a grid network large enough to exceed MaxIslandSize.
    /// Two areas connected by a road, each area has a grid of the specified size.
    /// </summary>
    private static OsmGeo[] BuildTwoAreaNetwork(int gridSize)
    {
        var osm = new System.Collections.Generic.List<OsmGeo>();
        long nodeId = 1;
        long wayId = 1;

        // Area A: grid near (4.27, 50.88)
        var areaANodes = new long[gridSize, gridSize];
        for (var x = 0; x < gridSize; x++)
            for (var y = 0; y < gridSize; y++)
            {
                areaANodes[x, y] = nodeId;
                osm.Add(new Node
                {
                    Id = nodeId++,
                    Longitude = 4.270 + x * 0.001,
                    Latitude = 50.880 + y * 0.001
                });
            }

        // Area A horizontal ways
        for (var y = 0; y < gridSize; y++)
            for (var x = 0; x < gridSize - 1; x++)
            {
                osm.Add(new Way
                {
                    Id = wayId++,
                    Nodes = new[] { areaANodes[x, y], areaANodes[x + 1, y] },
                    Tags = new TagsCollection(new Tag("highway", "residential"))
                });
            }

        // Area A vertical ways
        for (var x = 0; x < gridSize; x++)
            for (var y = 0; y < gridSize - 1; y++)
            {
                osm.Add(new Way
                {
                    Id = wayId++,
                    Nodes = new[] { areaANodes[x, y], areaANodes[x, y + 1] },
                    Tags = new TagsCollection(new Tag("highway", "residential"))
                });
            }

        // Area B: grid near (4.80, 51.27) — different tile
        var areaBNodes = new long[gridSize, gridSize];
        for (var x = 0; x < gridSize; x++)
            for (var y = 0; y < gridSize; y++)
            {
                areaBNodes[x, y] = nodeId;
                osm.Add(new Node
                {
                    Id = nodeId++,
                    Longitude = 4.800 + x * 0.001,
                    Latitude = 51.267 + y * 0.001
                });
            }

        // Area B horizontal ways
        for (var y = 0; y < gridSize; y++)
            for (var x = 0; x < gridSize - 1; x++)
            {
                osm.Add(new Way
                {
                    Id = wayId++,
                    Nodes = new[] { areaBNodes[x, y], areaBNodes[x + 1, y] },
                    Tags = new TagsCollection(new Tag("highway", "residential"))
                });
            }

        // Area B vertical ways
        for (var x = 0; x < gridSize; x++)
            for (var y = 0; y < gridSize - 1; y++)
            {
                osm.Add(new Way
                {
                    Id = wayId++,
                    Nodes = new[] { areaBNodes[x, y], areaBNodes[x, y + 1] },
                    Tags = new TagsCollection(new Tag("highway", "residential"))
                });
            }

        // Connecting road between areas
        osm.Add(new Way
        {
            Id = wayId++,
            Nodes = new[] { areaANodes[gridSize - 1, 0], areaBNodes[0, 0] },
            Tags = new TagsCollection(new Tag("highway", "primary"))
        });

        return osm.ToArray();
    }

    [Fact]
    public async Task SnapShouldWorkAfterRoutingElsewhere_WithIslands()
    {
        // Build a network with two areas, each with enough edges to exceed MaxIslandSize.
        // Grid 20x20 = 400 nodes, ~760 edges per area — well above MaxIslandSize=256.
        var profile = OsmProfiles.Car;
        var os = BuildTwoAreaNetwork(20);

        var routerDb = BuildNetwork(os, profile, maxIslandSize: 256);
        var network = routerDb.Latest;

        // Snap in area B should work before any routing.
        var snapB1 = await network.Snap(profile).ToAsync(4.810, 51.277);
        Assert.False(snapB1.IsError,
            $"Snap in area B should work before routing: {snapB1.ErrorMessage}");

        // Route in area A.
        var snapA1 = await network.Snap(profile).ToAsync(4.270, 50.880);
        Assert.False(snapA1.IsError, $"Snap A1 failed: {snapA1.ErrorMessage}");
        var snapA2 = await network.Snap(profile).ToAsync(4.289, 50.899);
        Assert.False(snapA2.IsError, $"Snap A2 failed: {snapA2.ErrorMessage}");

        var route = await network.Route(profile)
            .From(snapA1.Value)
            .To(snapA2.Value)
            .CalculateAsync();
        Assert.False(route.IsError, $"Route in area A failed: {route.ErrorMessage}");

        // Snap in area B should STILL work after routing in area A.
        var snapB2 = await network.Snap(profile).ToAsync(4.810, 51.277);
        Assert.False(snapB2.IsError,
            $"Snap in area B should work after routing in area A: {snapB2.ErrorMessage}");
    }

    [Fact]
    public async Task SnapShouldWorkWithoutRouting_WithIslands()
    {
        // Same large network, just snap in area B without routing first.
        var profile = OsmProfiles.Car;
        var os = BuildTwoAreaNetwork(20);

        var routerDb = BuildNetwork(os, profile, maxIslandSize: 256);
        var network = routerDb.Latest;

        var snap = await network.Snap(profile).ToAsync(4.810, 51.277);
        Assert.False(snap.IsError,
            $"Snap on large connected network should work: {snap.ErrorMessage}");
    }

    /// <summary>
    /// Adds a 20×20 area like <see cref="BuildTwoAreaNetwork"/>'s area A/B but with the
    /// rightmost vertical column tagged one-way (north-bound only). Cold snap targeting
    /// a coordinate on that one-way column must still find a snap candidate: even though
    /// the one-way edges never bidirectionally merge with their neighbours, the wider
    /// connected component graduates to the main network and the one-way bridges to it
    /// from both sides (incoming via the top junction, outgoing via the bottom).
    /// </summary>
    private static OsmGeo[] BuildAreaWithOneWayColumn(int gridSize)
    {
        var osm = new System.Collections.Generic.List<OsmGeo>();
        long nodeId = 1;
        long wayId = 1;

        var nodes = new long[gridSize, gridSize];
        for (var x = 0; x < gridSize; x++)
            for (var y = 0; y < gridSize; y++)
            {
                nodes[x, y] = nodeId;
                osm.Add(new Node
                {
                    Id = nodeId++,
                    Longitude = 4.270 + x * 0.001,
                    Latitude = 50.880 + y * 0.001
                });
            }

        // Horizontal ways (bidirectional).
        for (var y = 0; y < gridSize; y++)
            for (var x = 0; x < gridSize - 1; x++)
            {
                osm.Add(new Way
                {
                    Id = wayId++,
                    Nodes = new[] { nodes[x, y], nodes[x + 1, y] },
                    Tags = new TagsCollection(new Tag("highway", "residential"))
                });
            }

        // Vertical ways: rightmost column is one-way (motorway-like), rest bidirectional.
        for (var x = 0; x < gridSize; x++)
            for (var y = 0; y < gridSize - 1; y++)
            {
                var tags = new TagsCollection(new Tag("highway", x == gridSize - 1 ? "motorway" : "residential"));
                if (x == gridSize - 1) tags.AddOrReplace(new Tag("oneway", "yes"));
                osm.Add(new Way
                {
                    Id = wayId++,
                    Nodes = new[] { nodes[x, y], nodes[x, y + 1] },
                    Tags = tags
                });
            }

        return osm.ToArray();
    }

    [Fact]
    public async Task SnapOnOneWayInLargeNetwork_NoBuildForTile_ShouldWork()
    {
        // publish-api#61 minimal reproducer at unit-test scale:
        //
        //   - 20×20 grid (~760 edges, well above MaxIslandSize=256).
        //   - Rightmost column is one-way (motorway, north-bound). 19 one-way edges.
        //   - The rest is bidirectional residential.
        //
        // Snap on the middle of the rightmost (one-way) column. The snap drives
        // ResolveEdgeAsync on a one-way seed against an un-classified network. Snap
        // must succeed: the seed is part of the main network via its connecting
        // bidirectional incoming/outgoing edges at the column's vertices.
        var profile = OsmProfiles.Car;
        var os = BuildAreaWithOneWayColumn(20);

        var routerDb = BuildNetwork(os, profile, maxIslandSize: 256);
        var network = routerDb.Latest;

        // Coordinate on the rightmost (one-way) column, mid-height.
        var lonOneWay = 4.270 + 19 * 0.001; // x=19
        var latOneWay = 50.880 + 10 * 0.001; // y=10

        var snap = await network.Snap(profile).ToAsync(lonOneWay, latOneWay);
        Assert.False(snap.IsError,
            $"Snap on a one-way edge in a large connected network should work cold: {snap.ErrorMessage}");
    }
}
