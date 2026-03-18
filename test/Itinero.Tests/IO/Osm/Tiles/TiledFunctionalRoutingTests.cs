using System.Linq;
using System.Threading.Tasks;
using Itinero.Data;
using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Itinero.Network.Tiles;
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
        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            MaxIslandSize = 0,
            EdgeTypeMap = new SimpleAttributesSetMapMock()
        });
        routerDb.PrepareFor(profile);

        var network = routerDb.Latest;
        using var writer = network.GetWriter();
        var globalManager = new GlobalNetworkManager();

        foreach (var (x, y) in tiles)
        {
            var tileWriter = network.GetStandaloneTileWriter(x, y);
            tileWriter.AddTileData(os, s =>
            {
                s.TagsFilter.Filter = null;
                s.TagsFilter.CompleteFilter = null;
                s.TagsFilter.MemberFilter = null;
            });
            var tile = tileWriter.GetResultingTile();
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
}
