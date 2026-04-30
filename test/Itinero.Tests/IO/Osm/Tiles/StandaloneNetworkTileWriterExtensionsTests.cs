using System.Collections.Generic;
using System.Linq;
using Itinero.Data;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Itinero.Network.Tiles;
using Itinero.Network.Tiles.Standalone.Writer;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Tiles;

/// <summary>
/// Tests for the tile-creation pipeline in
/// <c>StandaloneNetworkTileWriterExtensions.AddTileData</c>: which restrictions
/// get fully resolved (and turned into turn costs) and which get stored as
/// <see cref="Itinero.Network.Tiles.Standalone.StandaloneNetworkTile" />
/// global restrictions for later retry.
/// </summary>
public class StandaloneNetworkTileWriterExtensionsTests
{
    [Fact]
    public void AddTileData_BollardOnSimpleInTileWay_ShouldStoreNoGlobalRestriction()
    {
        // Way [a, bollard, c] entirely inside one tile. The bollard splits the
        // way into two adjacent edges that are both stored with the exact
        // GlobalEdgeIds that the bollard's tail-hops produce → fully resolves
        // at tile creation, nothing deferred to runtime.

        const double lonA = 4.86638, latA = 51.269728;
        const double lonBollard = 4.86700, latBollard = 51.269400;
        const double lonC = 4.86737, latC = 51.269100;
        var (tx, ty) = TileStatic.WorldToTile(lonBollard, latBollard, 14);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonA, Latitude = latA },
            new Node
            {
                Id = 2, Longitude = lonBollard, Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonC, Latitude = latC },
            new Way
            {
                Id = 100, Nodes = new long[] { 1, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        var tile = BuildTile(tx, ty, osm);

        Assert.Empty(tile.GetGlobalRestrictions());
    }

    [Fact]
    public void AddTileData_BollardAtSharedNodeOfTwoInTileWays_ShouldStoreNoGlobalRestriction()
    {
        // Two ways meeting at the bollard node, both ways entirely inside one
        // tile. Both members of every restriction are stored as exact-match
        // GlobalEdgeIds → fully resolves at tile creation.

        const double lonA = 4.86638, latA = 51.269728;
        const double lonBollard = 4.86700, latBollard = 51.269400;
        const double lonC = 4.86737, latC = 51.269100;
        var (tx, ty) = TileStatic.WorldToTile(lonBollard, latBollard, 14);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonA, Latitude = latA },
            new Node
            {
                Id = 2, Longitude = lonBollard, Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonC, Latitude = latC },
            new Way { Id = 100, Nodes = new long[] { 1, 2 }, Tags = ResidentialTags() },
            new Way { Id = 200, Nodes = new long[] { 2, 3 }, Tags = ResidentialTags() }
        };

        var tile = BuildTile(tx, ty, osm);

        Assert.Empty(tile.GetGlobalRestrictions());
    }

    [Fact]
    public void AddTileData_BollardOnWayCrossingTileBoundary_ShouldDeferToGlobalRestriction()
    {
        // Way crosses a tile boundary; bollard is inside this tile but the
        // far end of the way is in another tile. The boundary half is a
        // "boundary edge" with an unknown EdgeId at tile creation, so the
        // restriction can't be turned into a turn cost yet — it must be
        // stored as a global restriction with that boundary member's
        // EdgeId set to null.

        const double lonInTile = 4.96682, latInTile = 51.269;     // bollard side
        const double lonOutOfTile = 4.96082, latOutOfTile = 51.269; // other end
        const double lonOther = 4.96782, latOther = 51.269;       // second leg, in tile
        var (tx, ty) = TileStatic.WorldToTile(lonInTile, latInTile, 14);
        var (otherTx, _) = TileStatic.WorldToTile(lonOutOfTile, latOutOfTile, 14);
        Assert.NotEqual(tx, otherTx);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonOutOfTile, Latitude = latOutOfTile },
            new Node
            {
                Id = 2, Longitude = lonInTile, Latitude = latInTile,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonOther, Latitude = latOther },
            new Way { Id = 100, Nodes = new long[] { 1, 2 }, Tags = ResidentialTags() },
            new Way { Id = 200, Nodes = new long[] { 2, 3 }, Tags = ResidentialTags() }
        };

        var tile = BuildTile(tx, ty, osm);

        var stored = tile.GetGlobalRestrictions().ToList();
        Assert.NotEmpty(stored);

        // every stored restriction must still satisfy the chain-connectivity
        // invariant on its GlobalEdgeIds.
        AssertChainConnectivity(stored);

        // at least one stored restriction has a member with EdgeId == null —
        // that is the boundary edge for way 100.
        Assert.Contains(stored, r => r.edges.Any(e => e.edgeId == null));
    }

    [Fact]
    public void AddTileData_BollardOnInTileWayWithInteriorJunction_ShouldFullyResolveAtCreation()
    {
        // Way 1 = [a, j, bollard, c] all in one tile, with a second way (way 2)
        // creating an interior junction at j. The network splits way 1 at j,
        // so the actual stored sub-edge at the bollard is (way1, 1, 2) — and
        // the bollard's tail-hop emits the way-spanning (way1, 0, 2).
        //
        // After the resolver refactor (walk-from-anchor), the tile-creation
        // lookup walks from the via-anchor end and finds the bollard-adjacent
        // sub-edge, so the restriction is fully resolved at tile creation
        // and nothing is deferred.

        const double lonA = 4.86620, latA = 51.269700;
        const double lonJ = 4.86660, latJ = 51.269500;
        const double lonBollard = 4.86700, latBollard = 51.269300;
        const double lonC = 4.86740, latC = 51.269100;
        const double lonX = 4.86660, latX = 51.269700; // makes j a junction with way 2
        var (tx, ty) = TileStatic.WorldToTile(lonBollard, latBollard, 14);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonA, Latitude = latA },
            new Node { Id = 2, Longitude = lonJ, Latitude = latJ },
            new Node
            {
                Id = 3, Longitude = lonBollard, Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 4, Longitude = lonC, Latitude = latC },
            new Node { Id = 5, Longitude = lonX, Latitude = latX },
            new Way { Id = 100, Nodes = new long[] { 1, 2, 3, 4 }, Tags = ResidentialTags() },
            new Way { Id = 200, Nodes = new long[] { 2, 5 }, Tags = ResidentialTags() }
        };

        var tile = BuildTile(tx, ty, osm);

        Assert.Empty(tile.GetGlobalRestrictions());
    }

    private static Itinero.Network.Tiles.Standalone.StandaloneNetworkTile BuildTile(uint x, uint y, OsmGeo[] osm)
    {
        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });
        var w = routerDb.Latest.GetStandaloneTileWriter(x, y);
        w.AddTileData(osm);
        return w.GetResultingTile();
    }

    private static TagsCollection ResidentialTags() =>
        new(new Tag("highway", "residential"));

    private static void AssertChainConnectivity(
        IEnumerable<(IReadOnlyList<(Itinero.Network.Tiles.Standalone.Global.GlobalEdgeId globalEdgeId,
            EdgeId? edgeId)> edges, bool isProhibitory, uint turnCostTypeId,
            IEnumerable<(string key, string value)> attributes)> restrictions)
    {
        foreach (var r in restrictions)
        {
            for (var i = 1; i < r.edges.Count; i++)
            {
                var prev = r.edges[i - 1].globalEdgeId;
                var cur = r.edges[i].globalEdgeId;
                // shared OSM node = previous edge's Head index on its way must equal
                // current edge's Tail index on its way (and both ways share that node).
                // Without access to the way data here, we at least check the indices
                // stay consistent within a single way.
                if (prev.EdgeId == cur.EdgeId)
                {
                    Assert.Equal(prev.Head, cur.Tail);
                }
            }
        }
    }
}
