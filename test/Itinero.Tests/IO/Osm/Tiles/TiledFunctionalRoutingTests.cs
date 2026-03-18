using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Data;
using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Itinero.Network.Tiles;
using Itinero.Network.Tiles.Standalone;
using Itinero.Network.Tiles.Standalone.Writer;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Routing;
using Itinero.Snapping;
using Itinero.Tests.Mocks.Indexes;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Tiles;

public class TiledFunctionalRoutingTests
{
    /// <summary>
    /// Loads OSM data through the tiled pipeline: OSM data → per-tile StandaloneNetworkTileWriter → AddStandaloneTile → RoutingNetwork.
    /// </summary>
    private static RouterDb LoadViaTiles(OsmGeo[] os, Profile profile, params (uint x, uint y)[] tiles)
    {
        var edgeTypeMap = new SimpleAttributesSetMapMock();

        // use a separate routerDb to create standalone tiles (simulates the tile source).
        var tileSourceDb = new RouterDb(new RouterDbConfiguration
        {
            MaxIslandSize = 0,
            EdgeTypeMap = edgeTypeMap
        });

        var standaloneTiles = new List<StandaloneNetworkTile>();
        foreach (var (x, y) in tiles)
        {
            var tileWriter = tileSourceDb.Latest.GetStandaloneTileWriter(x, y);
            tileWriter.AddTileData(os, s =>
            {
                s.TagsFilter.Filter = null;
                s.TagsFilter.CompleteFilter = null;
                s.TagsFilter.MemberFilter = null;
            });
            standaloneTiles.Add(tileWriter.GetResultingTile());
        }

        // load the standalone tiles into a fresh routerDb with the same edge type map.
        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            MaxIslandSize = 0,
            EdgeTypeMap = edgeTypeMap
        });
        routerDb.PrepareFor(profile);

        using var writer = routerDb.Latest.GetWriter();
        var globalManager = new GlobalNetworkManager();
        foreach (var tile in standaloneTiles)
        {
            writer.AddStandaloneTile(tile, globalManager);
        }

        return routerDb;
    }

    [Fact]
    public async Task SingleWay_SingleTile_ShouldRouteFromStartToEnd()
    {
        // both nodes in tile (8410, 5465).
        var profile = OsmProfiles.Car;
        var routerDb = LoadViaTiles(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile, (8410, 5465));

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
    public async Task SingleWay_SingleTile_DataIsLoaded()
    {
        var profile = OsmProfiles.Car;
        var routerDb = LoadViaTiles(new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, profile, (8410, 5465));

        var network = routerDb.Latest;

        // check vertices exist.
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
    public async Task TwoWays_SingleTile_ShouldRoute()
    {
        // three nodes all in tile (8410, 5465).
        var profile = OsmProfiles.Car;
        var routerDb = LoadViaTiles(new OsmGeo[]
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
        }, profile, (8410, 5465));

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
        Assert.True(route.Value.Shape.Count >= 3);
    }

    [Fact]
    public async Task TwoWays_CrossTileBoundary_ShouldRoute()
    {
        // node 1 at lon 4.810 → tile (8410, 5465)
        // node 2 at lon 4.813 → tile (8411, 5465) — crosses boundary at ~4.8120
        // node 3 at lon 4.816 → tile (8411, 5465)
        // Both tiles get the same OSM data; each processes only nodes within its bounds.
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
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
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465), (8411, 5465));

        var network = routerDb.Latest;

        // snap at each end — in different tiles.
        var snap1 = await network.Snap(profile).ToAsync(4.810, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap2 = await network.Snap(profile).ToAsync(4.816, 51.270);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        // route crosses the tile boundary.
        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap2.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);
        Assert.NotNull(route.Value);
        Assert.True(route.Value.Shape.Count >= 3);
    }

    [Fact]
    public async Task SingleWay_CrossTileBoundary_ShouldRoute()
    {
        // a single way where node 1 is in tile 8410 and node 2 is in tile 8411.
        // the boundary edge should be created when both tiles are loaded.
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.810, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.813, Latitude = 51.270 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465), (8411, 5465));

        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.810, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap2 = await network.Snap(profile).ToAsync(4.813, 51.270);
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
    public void GlobalEdgeId_SimpleWay_ShouldStoreCorrectIndices()
    {
        // way with 2 nodes → GlobalEdgeId should be (wayId=100, tail=0, head=1).
        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            MaxIslandSize = 0,
            EdgeTypeMap = new SimpleAttributesSetMapMock()
        });

        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 100, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        var tileWriter = routerDb.Latest.GetStandaloneTileWriter(8410, 5465);
        tileWriter.AddTileData(os, s =>
        {
            s.TagsFilter.Filter = null;
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });

        var tile = tileWriter.GetResultingTile();
        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(tile.NetworkTile);
        Assert.True(enumerator.MoveTo(new VertexId(tile.TileId, 0)));
        Assert.True(enumerator.MoveNext());

        var globalEdgeId = enumerator.GlobalEdgeId;
        Assert.NotNull(globalEdgeId);
        Assert.Equal(100, globalEdgeId.Value.EdgeId);
        Assert.Equal((ushort)0, globalEdgeId.Value.Tail);
        Assert.Equal((ushort)1, globalEdgeId.Value.Head);
    }

    [Fact]
    public void GlobalEdgeId_WayWithShapePoint_ShouldUseLastSegmentIndices()
    {
        // way with 3 nodes: [1, 2, 3]. Nodes 1 and 3 are endpoints (vertices),
        // node 2 is intermediate (shape point — only used once, not a vertex).
        // The GlobalEdgeId uses per-segment indices (n-1, n) because in a tiled
        // environment we can't know which nodes are vertices in other tiles.
        // So the edge gets GlobalEdgeId (wayId=100, tail=1, head=2).
        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            MaxIslandSize = 0,
            EdgeTypeMap = new SimpleAttributesSetMapMock()
        });

        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.8005, Latitude = 51.2695 },
            new Node { Id = 3, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 100, Nodes = new[] { 1L, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        var tileWriter = routerDb.Latest.GetStandaloneTileWriter(8410, 5465);
        tileWriter.AddTileData(os, s =>
        {
            s.TagsFilter.Filter = null;
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });

        var tile = tileWriter.GetResultingTile();
        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(tile.NetworkTile);
        Assert.True(enumerator.MoveTo(new VertexId(tile.TileId, 0)));
        Assert.True(enumerator.MoveNext());

        var globalEdgeId = enumerator.GlobalEdgeId;
        Assert.NotNull(globalEdgeId);
        Assert.Equal(100, globalEdgeId.Value.EdgeId);
        Assert.Equal((ushort)1, globalEdgeId.Value.Tail);
        Assert.Equal((ushort)2, globalEdgeId.Value.Head);
    }

    [Fact]
    public async Task Barrier_ShouldBlockRoute()
    {
        // direct path 1→2(barrier)→3 is short, detour 1→4→5→3 is long.
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.8005, Latitude = 51.2695,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.801, Latitude = 51.269 },
            new Node { Id = 4, Longitude = 4.795, Latitude = 51.265 },
            new Node { Id = 5, Longitude = 4.806, Latitude = 51.265 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 1L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 4, Nodes = new[] { 4L, 5 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 5, Nodes = new[] { 5L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465));
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

        // should detour, not pass through barrier at (4.8005, 51.2695).
        var passesBarrier = route.Value.Shape.Any(s =>
            Math.Abs(s.longitude - 4.8005) < 0.0001 &&
            Math.Abs(s.latitude - 51.2695) < 0.0001);
        Assert.False(passesBarrier, "Route should not pass through the barrier node");
    }

    [Fact]
    public async Task Barrier_NoAlternative_ShouldFail()
    {
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.8005, Latitude = 51.2695,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.801, Latitude = 51.269 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465));
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
    public async Task NoRightTurn_ShouldTakeDetour()
    {
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.270 },
            new Node { Id = 3, Longitude = 4.800, Latitude = 51.265 },
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.270 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 3L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 4, Nodes = new[] { 1L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 5, Nodes = new[] { 3L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
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
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465));
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

        var goesViaSouth = route.Value.Shape.Any(s => s.latitude < 51.268);
        Assert.True(goesViaSouth, "Route should detour south due to no_right_turn restriction");
    }

    [Fact]
    public async Task NoRightTurn_NoAlternative_ShouldFail()
    {
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.270 },
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.270 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
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
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465));
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
    public async Task OnlyRightTurn_ShouldAllowRight()
    {
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.802, Latitude = 51.270 },
            new Node { Id = 3, Longitude = 4.804, Latitude = 51.270 },
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.265 },
            new Node { Id = 5, Longitude = 4.802, Latitude = 51.275 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 4, Nodes = new[] { 2L, 5 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
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
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465));
        var network = routerDb.Latest;

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
    public async Task OnlyRightTurn_ShouldBlockOtherTurns()
    {
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.802, Latitude = 51.270 },
            new Node { Id = 3, Longitude = 4.804, Latitude = 51.270 },
            new Node { Id = 4, Longitude = 4.802, Latitude = 51.265 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
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
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465));
        var network = routerDb.Latest;

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
    public async Task Barrier_CrossTileBoundary_ShouldBlockRoute()
    {
        // way with 3 nodes: node1(tile 8410) → node2(tile 8411, barrier) → node3(tile 8411).
        // the way crosses the tile boundary between node1 and node2.
        // the barrier at node2 should block through-traffic.
        // a detour via node4 and node5 (both in tile 8411) provides a longer alternative.
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            // node1 in tile 8410, node2+node3 in tile 8411.
            new Node { Id = 1, Longitude = 4.810, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.813, Latitude = 51.270,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.816, Latitude = 51.270 },
            // detour nodes, both in tile 8411.
            new Node { Id = 4, Longitude = 4.813, Latitude = 51.265 },
            new Node { Id = 5, Longitude = 4.816, Latitude = 51.265 },
            // direct path through barrier: 1→2→3.
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            // detour: 1→4→5→3 (avoids barrier).
            new Way
            {
                Id = 2, Nodes = new[] { 1L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 3, Nodes = new[] { 4L, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 4, Nodes = new[] { 5L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465), (8411, 5465));
        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.810, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap3 = await network.Snap(profile).ToAsync(4.816, 51.270);
        Assert.False(snap3.IsError, snap3.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap3.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);

        // should detour south, not pass through barrier at (4.813, 51.270).
        var goesViaSouth = route.Value.Shape.Any(s => s.latitude < 51.268);
        Assert.True(goesViaSouth, "Route should detour south to avoid barrier at tile boundary");

        // reverse direction: node3 → node1 should also detour south.
        var reverseRoute = await network.Route(profile)
            .From(snap3.Value)
            .To(snap1.Value)
            .CalculateAsync();

        Assert.False(reverseRoute.IsError, reverseRoute.ErrorMessage);
        var reverseGoesViaSouth = reverseRoute.Value.Shape.Any(s => s.latitude < 51.268);
        Assert.True(reverseGoesViaSouth,
            "Reverse route should detour south to avoid barrier at tile boundary");
    }

    [Fact]
    public async Task Barrier_CrossTileBoundary_NoAlternative_ShouldFail()
    {
        // same setup but no detour — barrier blocks the only path.
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.810, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.813, Latitude = 51.270,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.816, Latitude = 51.270 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        };

        var edgeTypeMap = new SimpleAttributesSetMapMock();
        var tileSourceDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 0, EdgeTypeMap = edgeTypeMap });

        // create tile 8411 (contains the barrier).
        var tile8411Writer = tileSourceDb.Latest.GetStandaloneTileWriter(8411, 5465);
        tile8411Writer.AddTileData(os, s =>
        {
            s.TagsFilter.Filter = null;
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });
        var tile8411 = tile8411Writer.GetResultingTile();

        // verify tile 8411 has global restrictions from the barrier.
        var globalRestrictions = tile8411.GetGlobalRestrictions().ToList();
        Assert.True(globalRestrictions.Count > 0,
            "Tile 8411 should have global restrictions from the barrier");

        // create tile 8410.
        var tile8410Writer = tileSourceDb.Latest.GetStandaloneTileWriter(8410, 5465);
        tile8410Writer.AddTileData(os, s =>
        {
            s.TagsFilter.Filter = null;
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });
        var tile8410 = tile8410Writer.GetResultingTile();

        // load both tiles into a fresh routerDb.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 0, EdgeTypeMap = edgeTypeMap });
        routerDb.PrepareFor(profile);
        using var writer = routerDb.Latest.GetWriter();
        var globalManager = new GlobalNetworkManager();
        writer.AddStandaloneTile(tile8410, globalManager);
        writer.AddStandaloneTile(tile8411, globalManager);

        // verify all pending restrictions were resolved.
        Assert.Empty(globalManager.PendingRestrictions);

        // verify turn costs exist on the network.
        var network = routerDb.Latest;
        var edgeEnum = network.GetEdgeEnumerator();
        var vertEnum = network.GetVertexEnumerator();
        var hasTurnCost = false;
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext())
            {
                if (edgeEnum.TailOrder != null) hasTurnCost = true;
            }
        }
        Assert.True(hasTurnCost, "Expected turn costs at the barrier vertex");

        var snap1 = await network.Snap(profile).ToAsync(4.810, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap3 = await network.Snap(profile).ToAsync(4.816, 51.270);
        Assert.False(snap3.IsError, snap3.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap3.Value)
            .CalculateAsync();

        Assert.True(route.IsError, "Route through cross-tile barrier with no alternative should fail");

        // reverse direction: node3 → node1 should also fail.
        var reverseRoute = await network.Route(profile)
            .From(snap3.Value)
            .To(snap1.Value)
            .CalculateAsync();

        Assert.True(reverseRoute.IsError,
            "Reverse route through cross-tile barrier with no alternative should fail");
    }

    [Fact]
    public async Task Barrier_SameTileAsTail_CrossBoundaryAfter_ShouldBlockRoute()
    {
        // way with 3 nodes: node1(tile 8410) → node2(tile 8410, barrier) → node3(tile 8411).
        // the barrier is in the same tile as node1; the cross-tile edge is node2→node3.
        // this tests sync from the barrier vertex's tile outward.
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            // node1 and node2 in tile 8410, node3 in tile 8411.
            new Node { Id = 1, Longitude = 4.807, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.810, Latitude = 51.270,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.813, Latitude = 51.270 },
            // detour nodes.
            new Node { Id = 4, Longitude = 4.807, Latitude = 51.265 },
            new Node { Id = 5, Longitude = 4.813, Latitude = 51.265 },
            // direct path through barrier: 1→2→3.
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            // detour: 1→4→5→3 (avoids barrier).
            new Way
            {
                Id = 2, Nodes = new[] { 1L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 3, Nodes = new[] { 4L, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 4, Nodes = new[] { 5L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465), (8411, 5465));
        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.807, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap3 = await network.Snap(profile).ToAsync(4.813, 51.270);
        Assert.False(snap3.IsError, snap3.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap3.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);

        // should detour south, not pass through barrier at (4.810, 51.270).
        var goesViaSouth = route.Value.Shape.Any(s => s.latitude < 51.268);
        Assert.True(goesViaSouth,
            "Route should detour south to avoid barrier at tile boundary (barrier same tile as tail)");

        // reverse direction: node3 → node1 should also detour south.
        var reverseRoute = await network.Route(profile)
            .From(snap3.Value)
            .To(snap1.Value)
            .CalculateAsync();

        Assert.False(reverseRoute.IsError, reverseRoute.ErrorMessage);
        var reverseGoesViaSouth = reverseRoute.Value.Shape.Any(s => s.latitude < 51.268);
        Assert.True(reverseGoesViaSouth,
            "Reverse route should detour south to avoid barrier (barrier same tile as tail)");
    }

    [Fact]
    public async Task Barrier_SameTileAsTail_CrossBoundaryAfter_NoAlternative_ShouldFail()
    {
        // same layout but no detour — barrier blocks the only path.
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.807, Latitude = 51.270 },
            new Node
            {
                Id = 2, Longitude = 4.810, Latitude = 51.270,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = 4.813, Latitude = 51.270 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465), (8411, 5465));
        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.807, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap3 = await network.Snap(profile).ToAsync(4.813, 51.270);
        Assert.False(snap3.IsError, snap3.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap3.Value)
            .CalculateAsync();

        Assert.True(route.IsError,
            "Route through cross-tile barrier with no alternative should fail (barrier same tile as tail)");

        // reverse direction: node3 → node1 should also fail.
        var reverseRoute = await network.Route(profile)
            .From(snap3.Value)
            .To(snap1.Value)
            .CalculateAsync();

        Assert.True(reverseRoute.IsError,
            "Reverse route through cross-tile barrier with no alternative should fail (barrier same tile as tail)");
    }

    [Fact]
    public async Task NoRightTurn_FromEdgeCrossesTileBoundary_ShouldTakeDetour()
    {
        // node1(tile 8410) --way1--> node2(tile 8411, via) --way3--> node4(tile 8411)
        // no_right_turn from way1 via node2 to way3.
        // the "from" edge crosses the tile boundary.
        // detour: node1 → node3 → node4 (south).
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.810, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.813, Latitude = 51.270 },
            new Node { Id = 3, Longitude = 4.810, Latitude = 51.265 },
            new Node { Id = 4, Longitude = 4.816, Latitude = 51.270 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 3L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 4, Nodes = new[] { 1L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 5, Nodes = new[] { 3L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
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
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465), (8411, 5465));
        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.810, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap4 = await network.Snap(profile).ToAsync(4.816, 51.270);
        Assert.False(snap4.IsError, snap4.ErrorMessage);

        // forward: node1 → node4 should detour south.
        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap4.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);
        var goesViaSouth = route.Value.Shape.Any(s => s.latitude < 51.268);
        Assert.True(goesViaSouth,
            "Route should detour south due to no_right_turn (from-edge crosses tile boundary)");

        // reverse: node4 → node1 is not restricted (different turn direction), should go direct.
        var reverseRoute = await network.Route(profile)
            .From(snap4.Value)
            .To(snap1.Value)
            .CalculateAsync();

        Assert.False(reverseRoute.IsError, reverseRoute.ErrorMessage);
    }

    [Fact]
    public async Task NoRightTurn_FromEdgeCrossesTileBoundary_NoAlternative_ShouldFail()
    {
        // same layout but without the detour ways.
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.810, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.813, Latitude = 51.270 },
            new Node { Id = 4, Longitude = 4.816, Latitude = 51.270 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
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
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465), (8411, 5465));
        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.810, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap4 = await network.Snap(profile).ToAsync(4.816, 51.270);
        Assert.False(snap4.IsError, snap4.ErrorMessage);

        // forward: should fail due to restriction.
        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap4.Value)
            .CalculateAsync();

        Assert.True(route.IsError,
            "Route through no_right_turn with no alternative should fail (from-edge crosses tile boundary)");

        // reverse: node4 → node1 is not restricted, should succeed.
        var reverseRoute = await network.Route(profile)
            .From(snap4.Value)
            .To(snap1.Value)
            .CalculateAsync();

        Assert.False(reverseRoute.IsError,
            $"Reverse route should succeed (restriction is directional): {reverseRoute.ErrorMessage}");
    }

    [Fact]
    public async Task NoRightTurn_ToEdgeCrossesTileBoundary_ShouldTakeDetour()
    {
        // node1(tile 8410) --way1--> node2(tile 8410, via) --way3--> node4(tile 8411)
        // no_right_turn from way1 via node2 to way3.
        // the "to" edge crosses the tile boundary.
        // detour: node1 → node3 → node4 (south).
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.807, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.810, Latitude = 51.270 },
            new Node { Id = 3, Longitude = 4.807, Latitude = 51.265 },
            new Node { Id = 4, Longitude = 4.813, Latitude = 51.270 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Nodes = new[] { 3L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 4, Nodes = new[] { 1L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 5, Nodes = new[] { 3L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
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
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465), (8411, 5465));
        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.807, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap4 = await network.Snap(profile).ToAsync(4.813, 51.270);
        Assert.False(snap4.IsError, snap4.ErrorMessage);

        // forward: node1 → node4 should detour south.
        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap4.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);
        var goesViaSouth = route.Value.Shape.Any(s => s.latitude < 51.268);
        Assert.True(goesViaSouth,
            "Route should detour south due to no_right_turn (to-edge crosses tile boundary)");

        // reverse: node4 → node1 is not restricted, should go direct.
        var reverseRoute = await network.Route(profile)
            .From(snap4.Value)
            .To(snap1.Value)
            .CalculateAsync();

        Assert.False(reverseRoute.IsError, reverseRoute.ErrorMessage);
    }

    [Fact]
    public async Task NoRightTurn_ToEdgeCrossesTileBoundary_NoAlternative_ShouldFail()
    {
        // same layout but without the detour ways.
        var profile = OsmProfiles.Car;
        var os = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.807, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.810, Latitude = 51.270 },
            new Node { Id = 4, Longitude = 4.813, Latitude = 51.270 },
            new Way { Id = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Nodes = new[] { 2L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
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
        };

        var routerDb = LoadViaTiles(os, profile, (8410, 5465), (8411, 5465));
        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.807, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap4 = await network.Snap(profile).ToAsync(4.813, 51.270);
        Assert.False(snap4.IsError, snap4.ErrorMessage);

        // forward: should fail due to restriction.
        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap4.Value)
            .CalculateAsync();

        Assert.True(route.IsError,
            "Route through no_right_turn with no alternative should fail (to-edge crosses tile boundary)");

        // reverse: node4 → node1 is not restricted, should succeed.
        var reverseRoute = await network.Route(profile)
            .From(snap4.Value)
            .To(snap1.Value)
            .CalculateAsync();

        Assert.False(reverseRoute.IsError,
            $"Reverse route should succeed (restriction is directional): {reverseRoute.ErrorMessage}");
    }
}
