using System.Collections.Generic;
using Itinero.Data;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Itinero.Network.Tiles;
using Itinero.Network.Tiles.Standalone.Writer;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.Network.Tiles.Standalone.Writer;

/// <summary>
/// Integration tests for the build-then-merge restriction pipeline:
/// <c>StandaloneNetworkTileWriterExtensions.AddTileData</c> builds a
/// standalone tile from raw OSM, and
/// <c>RoutingNetworkWriterExtensions.AddStandaloneTile</c> merges it into a
/// routing network — resolving global restrictions in the process (in-tile
/// or deferred via <see cref="GlobalNetworkManager.PendingRestrictions"/>).
/// </summary>
public class RoutingNetworkWriterExtensionsTests
{
    [Fact]
    public void AddStandaloneTile_SingleTile_BollardOnWay_ShouldAddTurnCostAtBollardVertex()
    {
        // a single residential way with three nodes, all in the same tile,
        // with the middle node tagged as a bollard. After build + merge,
        // the routing network should carry a turn cost at the bollard's
        // vertex that blocks through-traversal.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        // three coords inside one zoom-14 tile.
        const double lonStart = 4.86638, latStart = 51.269728;
        const double lonBollard = 4.86700, latBollard = 51.269400;
        const double lonEnd = 4.86737, latEnd = 51.269100;
        var (tileX, tileY) = TileStatic.WorldToTile(lonBollard, latBollard, 14);

        // OSM input: 3 nodes + 1 way; bollard tag turns the middle node into
        // a vertex, splitting the way into two edges that meet at it.
        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonStart, Latitude = latStart },
            new Node
            {
                Id = 2,
                Longitude = lonBollard,
                Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonEnd, Latitude = latEnd },
            new Way
            {
                Id = 100,
                Nodes = new long[] { 1, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        // build the standalone tile.
        var tileWriter = routerDb.Latest.GetStandaloneTileWriter(tileX, tileY);
        tileWriter.AddTileData(osm);
        var standaloneTile = tileWriter.GetResultingTile();

        // merge into the routing network.
        var globalIdSet = new GlobalNetworkManager();
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(standaloneTile, globalIdSet);
        }

        // restriction is fully resolvable in-tile so nothing should be
        // left waiting in the deferred queue.
        Assert.Empty(globalIdSet.PendingRestrictions);

        // exactly three vertices were added: start, bollard (split point), end.
        // the bollard vertex is the only one with two incident edges.
        var bollardVertex = FindVertexWithDegree(routerDb.Latest, 2);
        Assert.True(bollardVertex.HasValue,
            "expected exactly one vertex with two incident edges (the bollard split point)");

        // assert at least one turn cost is recorded at the bollard vertex.
        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex blocking through-traversal");
    }

    [Fact]
    public void AddStandaloneTile_SingleTile_BollardOnSharedNodeBetweenTwoWays_ShouldAddTurnCostAtBollardVertex()
    {
        // a bollard sitting on the node SHARED by two distinct ways. Each
        // way is a single segment (start vertex -> bollard, bollard -> end
        // vertex), no shape points. Single tile.
        //
        // Unlike the in-way bollard, the resulting GlobalRestriction's two
        // edges have *different* EdgeIds (one per way), so ComputePivot's
        // shared-EdgeId heuristic returns null and the resolver has to fall
        // back to the fwd-direction default. With no shape points to muddy
        // the lookup, exact-match still works, so this should pass — it's
        // the canonical baseline for the cross-way restriction shape.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        const double lat = 51.269400;
        const double lonStart = 4.86638;   // start of way A (vertex)
        const double lonBollard = 4.86700; // bollard, shared between A and B (vertex)
        const double lonEnd = 4.86737;     // end of way B (vertex)
        var (tileX, tileY) = TileStatic.WorldToTile(lonBollard, lat, 14);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonStart, Latitude = lat },
            new Node
            {
                Id = 2,
                Longitude = lonBollard,
                Latitude = lat,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonEnd, Latitude = lat },
            new Way
            {
                Id = 100,
                Nodes = new long[] { 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 200,
                Nodes = new long[] { 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        var tile = BuildStandaloneTile(routerDb, tileX, tileY, osm);
        var globalIdSet = new GlobalNetworkManager();
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tile, globalIdSet);
        }

        Assert.Empty(globalIdSet.PendingRestrictions);

        var bollardVertex = FindVertexWithDegree(routerDb.Latest, 2);
        Assert.True(bollardVertex.HasValue,
            "expected exactly one vertex with two incident edges (the shared bollard node)");
        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the shared bollard vertex blocking through-traversal");
    }

    [Fact]
    public void AddStandaloneTile_TwoTiles_BollardOnSharedNodeAtBoundary_ShouldResolveAfterBothTilesLoaded()
    {
        // a bollard on the node SHARED between two ways, with the bollard
        // sitting in tile B. Way A goes start (tile A) -> bollard (tile B)
        // crossing the boundary; way B goes bollard (tile B) -> end (tile B).
        // No shape points, single segment per way.
        //
        // Tile B alone produces the bollard restriction but the way-A side
        // is a half-loaded boundary crossing — it cannot resolve and is
        // deferred. After tile A is added, the boundary crossing matches,
        // the (way_A, 0, 1) edge gets registered globally, and the retry
        // loop resolves the restriction. Turn cost lands at the bollard.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        const double lat = 51.269;
        const double lonStart = 4.96082;   // tile A
        const double lonBollard = 4.96682; // tile B (just past 8417->8418 boundary)
        const double lonEnd = 4.97082;     // tile B
        var (tileAx, tileAy) = TileStatic.WorldToTile(lonStart, lat, 14);
        var (tileBx, tileBy) = TileStatic.WorldToTile(lonBollard, lat, 14);
        Assert.NotEqual((tileAx, tileAy), (tileBx, tileBy));

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonStart, Latitude = lat },
            new Node
            {
                Id = 2,
                Longitude = lonBollard,
                Latitude = lat,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonEnd, Latitude = lat },
            new Way
            {
                Id = 100,
                Nodes = new long[] { 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way
            {
                Id = 200,
                Nodes = new long[] { 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        var tileBStandalone = BuildStandaloneTile(routerDb, tileBx, tileBy, osm);
        var tileAStandalone = BuildStandaloneTile(routerDb, tileAx, tileAy, osm);

        var globalIdSet = new GlobalNetworkManager();

        // tile B alone — bollard's cross-way restriction can't find way A's
        // edge yet (boundary-crossing half is unmatched), so it defers.
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileBStandalone, globalIdSet);
        }
        Assert.NotEmpty(globalIdSet.PendingRestrictions);

        // tile A — boundary crossing matched, retry resolves.
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileAStandalone, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);
        Assert.Empty(globalIdSet.PendingBoundaryCrossings);

        var bollardVertex = FindVertexWithDegree(routerDb.Latest, 2);
        Assert.True(bollardVertex.HasValue,
            "expected exactly one vertex with two incident edges (the shared bollard node)");
        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the shared bollard vertex blocking through-traversal");
    }

    [Fact]
    public void AddStandaloneTile_TwoTiles_BollardWithFromEdgeAcrossBoundary_ShouldResolveAfterBothTilesLoaded()
    {
        // a residential way crossing a tile boundary, with the bollard on the
        // tile-B side. The "from" half of the bollard restriction lives on the
        // boundary-crossing edge — it can only be resolved after both tiles
        // have been merged into the routing network.
        //
        // sequence:
        //  - merge tile B (where the bollard vertex lives)  -> restriction
        //    cannot resolve (missing edge), goes into PendingRestrictions
        //  - merge tile A (where the from-side of the way starts) -> boundary
        //    crossing edge is created, retry resolves the restriction, turn
        //    cost is added at the bollard vertex

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        // tile boundary between zoom-14 tiles (8417,5465) and (8418,5465)
        // sits at lon 4.9658203125. Pick coords spanning it.
        const double lonStart = 4.96082, latStart = 51.269;     // tile (8417, 5465)
        const double lonBollard = 4.97082, latBollard = 51.269; // tile (8418, 5465)
        const double lonEnd = 4.97582, latEnd = 51.269;         // tile (8418, 5465)
        var (tileAx, tileAy) = TileStatic.WorldToTile(lonStart, latStart, 14);
        var (tileBx, tileBy) = TileStatic.WorldToTile(lonBollard, latBollard, 14);
        Assert.NotEqual((tileAx, tileAy), (tileBx, tileBy));

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonStart, Latitude = latStart },
            new Node
            {
                Id = 2,
                Longitude = lonBollard,
                Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonEnd, Latitude = latEnd },
            new Way
            {
                Id = 100,
                Nodes = new long[] { 1, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        // build standalone tiles for both tiles independently from the same
        // OSM input — the writer's IsInTile filter selects the right subset.
        var tileBStandalone = BuildStandaloneTile(routerDb, tileBx, tileBy, osm);
        var tileAStandalone = BuildStandaloneTile(routerDb, tileAx, tileAy, osm);

        var globalIdSet = new GlobalNetworkManager();

        // step 1: add tile B alone. The bollard vertex exists, the
        // bollard→end edge exists, but the from-side of the way is on a
        // boundary crossing whose other half hasn't been loaded yet. The
        // bollard restriction cannot resolve and must be deferred.
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileBStandalone, globalIdSet);
        }
        Assert.NotEmpty(globalIdSet.PendingRestrictions);

        // step 2: add tile A. Matching boundary crossing edge is created;
        // the retry loop should resolve every pending bollard restriction.
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileAStandalone, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);

        // bollard vertex (degree 2: one boundary-crossing edge from tile A,
        // one regular edge inside tile B) should now carry a turn cost.
        var bollardVertex = FindVertexWithDegree(routerDb.Latest, 2);
        Assert.True(bollardVertex.HasValue,
            "expected exactly one vertex with two incident edges (the bollard split point)");
        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex blocking through-traversal");
    }

    [Fact]
    public void AddStandaloneTile_TwoTiles_BollardWithFromEdgeAcrossBoundary_ShapePointsOnBothEdges_ShouldResolve()
    {
        // same shape as the previous test (the from-edge crosses the tile
        // boundary, the bollard sits in tile B), but each edge of the way
        // is given 4 intermediate shape-point nodes so the GlobalEdgeIds
        // span multiple way-node indices. The boundary is crossed in the
        // first segment of the from-edge (between way nodes 0 and 1).
        //
        // exercises the subsection lookup in the cross-tile resolver: the
        // bollard restriction asks for `way_0->5` and `way_5->10`, neither
        // of which exists as a registered edge — only `way_0->1` (boundary
        // crossing), `way_1->5` (in tile B) and `way_5->10` (in tile B) do.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        // way nodes 0..10. Node 0 in tile A, nodes 1..10 in tile B (boundary
        // at lon 4.965820 between zoom-14 tiles 8417 and 8418). Bollard at
        // node 5; way nodes 1..4 and 6..9 are pure shape points (no tags,
        // no other ways referencing them).
        const double lat = 51.269;
        var lons = new[]
        {
            4.96082, // node 0 — start, tile A
            4.96682, // node 1 — shape (becomes vertex via boundary entry)
            4.96782, // node 2 — shape
            4.96882, // node 3 — shape
            4.96982, // node 4 — shape
            4.97082, // node 5 — bollard
            4.97182, // node 6 — shape
            4.97282, // node 7 — shape
            4.97382, // node 8 — shape
            4.97482, // node 9 — shape
            4.97582  // node 10 — end
        };
        var (tileAx, tileAy) = TileStatic.WorldToTile(lons[0], lat, 14);
        var (tileBx, tileBy) = TileStatic.WorldToTile(lons[5], lat, 14);
        Assert.NotEqual((tileAx, tileAy), (tileBx, tileBy));

        var nodes = new OsmGeo[lons.Length];
        for (var i = 0; i < lons.Length; i++)
        {
            nodes[i] = new Node { Id = i + 1, Longitude = lons[i], Latitude = lat };
        }
        ((Node)nodes[5]).Tags = new TagsCollection(new Tag("barrier", "bollard"));
        var way = new Way
        {
            Id = 100,
            Nodes = new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },
            Tags = new TagsCollection(new Tag("highway", "residential"))
        };
        var osm = new List<OsmGeo>(nodes) { way }.ToArray();

        var tileBStandalone = BuildStandaloneTile(routerDb, tileBx, tileBy, osm);
        var tileAStandalone = BuildStandaloneTile(routerDb, tileAx, tileAy, osm);

        var globalIdSet = new GlobalNetworkManager();

        // load both tiles; the cross-tile resolver's subsection lookup
        // should find `way_1->5` as the substitute for the bollard
        // restriction's `way_0->5` (since vertex-1 is the boundary entry
        // vertex inside tile B, with shape points 2..4 in between).
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileBStandalone, globalIdSet);
            writer.AddStandaloneTile(tileAStandalone, globalIdSet);
        }

        Assert.Empty(globalIdSet.PendingRestrictions);
        Assert.Empty(globalIdSet.PendingBoundaryCrossings);

        // the bollard vertex is at lons[5] — locate by coordinate (degree-2
        // alone is ambiguous: the boundary-entry vertex is also degree 2).
        var bollardVertex = FindVertexAtLocation(routerDb.Latest, lons[5], lat);
        Assert.True(bollardVertex.HasValue, "expected to find the bollard vertex by location");
        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex blocking through-traversal");
    }

    [Fact]
    public void AddStandaloneTile_TwoTiles_BollardWithFromEdgeAcrossBoundary_ShapePointsOnBothEdges_ReverseLoadOrder_ShouldResolve()
    {
        // identical geometry to the previous test (way crosses boundary in
        // first segment, bollard at way-node 5 in tile B, 4 shape points on
        // each side of the bollard) — but tiles are merged in reverse: tile
        // A first (no bollard, just the start vertex + outgoing boundary
        // crossing), then tile B. The bollard restriction must still resolve
        // when tile B arrives, regardless of merge order.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        const double lat = 51.269;
        var lons = new[]
        {
            4.96082, 4.96682, 4.96782, 4.96882, 4.96982,
            4.97082, // node 5 — bollard
            4.97182, 4.97282, 4.97382, 4.97482, 4.97582
        };
        var (tileAx, tileAy) = TileStatic.WorldToTile(lons[0], lat, 14);
        var (tileBx, tileBy) = TileStatic.WorldToTile(lons[5], lat, 14);
        Assert.NotEqual((tileAx, tileAy), (tileBx, tileBy));

        var nodes = new OsmGeo[lons.Length];
        for (var i = 0; i < lons.Length; i++)
        {
            nodes[i] = new Node { Id = i + 1, Longitude = lons[i], Latitude = lat };
        }
        ((Node)nodes[5]).Tags = new TagsCollection(new Tag("barrier", "bollard"));
        var way = new Way
        {
            Id = 100,
            Nodes = new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },
            Tags = new TagsCollection(new Tag("highway", "residential"))
        };
        var osm = new List<OsmGeo>(nodes) { way }.ToArray();

        var tileAStandalone = BuildStandaloneTile(routerDb, tileAx, tileAy, osm);
        var tileBStandalone = BuildStandaloneTile(routerDb, tileBx, tileBy, osm);

        var globalIdSet = new GlobalNetworkManager();

        // tile A first — only the start vertex and an outgoing boundary
        // crossing. No bollard restriction yet (it lives in tile B).
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileAStandalone, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);
        Assert.NotEmpty(globalIdSet.PendingBoundaryCrossings);

        // tile B second — boundary crossing matched, bollard's cross-tile
        // restriction resolves on first try via the subsection lookup.
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileBStandalone, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);
        Assert.Empty(globalIdSet.PendingBoundaryCrossings);

        var bollardVertex = FindVertexAtLocation(routerDb.Latest, lons[5], lat);
        Assert.True(bollardVertex.HasValue, "expected to find the bollard vertex by location");
        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex blocking through-traversal");
    }

    [Fact]
    public void AddStandaloneTile_TwoTiles_BollardWithBoundaryOnLastFromSegment_ShouldResolve()
    {
        // 11-node way with bollard at idx 5; the boundary is now crossed in
        // the LAST segment of the from-edge (between way nodes 4 and 5).
        // The from-side has 4 shape points all inside tile A; only the
        // bollard itself sits in tile B. The to-side stays entirely in
        // tile B with its own 4 shape points.
        //
        // Tile A registers a real edge `way_0->4` (start through 4 shape
        // points to the last in-tile node) and an outgoing boundary
        // crossing `way_4->5`. Tile B registers `way_5->10` plus the
        // matching incoming boundary crossing.
        //
        // The bollard restriction asks for `way_0->5`, which doesn't exist
        // exactly; the cross-tile resolver's subsection lookup must pick
        // up the boundary-crossing edge `way_4->5` to apply the turn cost
        // at the bollard vertex.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        const double lat = 51.269;
        var lons = new[]
        {
            4.945, 4.950, 4.955, 4.960, 4.964,
            4.968, // node 5 — bollard, just past boundary in tile B
            4.970, 4.972, 4.974, 4.976, 4.978
        };
        var (tileAx, tileAy) = TileStatic.WorldToTile(lons[0], lat, 14);
        var (tileBx, tileBy) = TileStatic.WorldToTile(lons[5], lat, 14);
        Assert.NotEqual((tileAx, tileAy), (tileBx, tileBy));

        var nodes = new OsmGeo[lons.Length];
        for (var i = 0; i < lons.Length; i++)
        {
            nodes[i] = new Node { Id = i + 1, Longitude = lons[i], Latitude = lat };
        }
        ((Node)nodes[5]).Tags = new TagsCollection(new Tag("barrier", "bollard"));
        var way = new Way
        {
            Id = 100,
            Nodes = new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },
            Tags = new TagsCollection(new Tag("highway", "residential"))
        };
        var osm = new List<OsmGeo>(nodes) { way }.ToArray();

        var tileAStandalone = BuildStandaloneTile(routerDb, tileAx, tileAy, osm);
        var tileBStandalone = BuildStandaloneTile(routerDb, tileBx, tileBy, osm);

        var globalIdSet = new GlobalNetworkManager();
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileAStandalone, globalIdSet);
            writer.AddStandaloneTile(tileBStandalone, globalIdSet);
        }

        Assert.Empty(globalIdSet.PendingRestrictions);
        Assert.Empty(globalIdSet.PendingBoundaryCrossings);

        var bollardVertex = FindVertexAtLocation(routerDb.Latest, lons[5], lat);
        Assert.True(bollardVertex.HasValue, "expected to find the bollard vertex by location");
        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex blocking through-traversal");
    }

    [Fact]
    public void AddStandaloneTile_TwoTiles_BollardWithToEdgeAcrossBoundary_ShouldResolveAfterBothTilesLoaded()
    {
        // mirror of the previous test: the bollard sits in tile A together
        // with the from-side of the way; only the to-side crosses into tile
        // B. Until tile B loads, the to-edge is a half-loaded boundary
        // crossing and the bollard restriction can't resolve.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        const double lonStart = 4.95582, latStart = 51.269;     // tile (8417, 5465)
        const double lonBollard = 4.96082, latBollard = 51.269; // tile (8417, 5465)
        const double lonEnd = 4.97082, latEnd = 51.269;         // tile (8418, 5465)
        var (tileAx, tileAy) = TileStatic.WorldToTile(lonBollard, latBollard, 14);
        var (tileBx, tileBy) = TileStatic.WorldToTile(lonEnd, latEnd, 14);
        Assert.NotEqual((tileAx, tileAy), (tileBx, tileBy));

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonStart, Latitude = latStart },
            new Node
            {
                Id = 2,
                Longitude = lonBollard,
                Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonEnd, Latitude = latEnd },
            new Way
            {
                Id = 100,
                Nodes = new long[] { 1, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };

        var tileAStandalone = BuildStandaloneTile(routerDb, tileAx, tileAy, osm);
        var tileBStandalone = BuildStandaloneTile(routerDb, tileBx, tileBy, osm);

        var globalIdSet = new GlobalNetworkManager();

        // step 1: add tile A. Bollard vertex and start→bollard edge exist,
        // but bollard→end is a boundary crossing waiting for tile B.
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileAStandalone, globalIdSet);
        }
        Assert.NotEmpty(globalIdSet.PendingRestrictions);

        // step 2: add tile B → boundary crossing matched, retry resolves.
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileBStandalone, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);

        var bollardVertex = FindVertexWithDegree(routerDb.Latest, 2);
        Assert.True(bollardVertex.HasValue,
            "expected exactly one vertex with two incident edges (the bollard split point)");
        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex blocking through-traversal");
    }

    [Fact]
    public void AddStandaloneTile_BollardAtSharedNodeWithThirdWayCreatingInteriorJunction_TurnCostShouldBeAtBollardOnly()
    {
        // Way 100 = [a, j, bollard]. Way 200 = [bollard, b]. Way 300 = [j, x].
        // Way 300 makes 'j' a junction, splitting way 100 into two stored
        // sub-edges (100, 0, 1) and (100, 1, 2). The bollard's tail-hop emits
        // the way-spanning (100, 0, 2) which can't exact-match → restriction
        // is deferred to runtime.
        //
        // At runtime, restriction [(100, 0, 2), (200, 0, 1)] resolves correctly
        // (TryHi finds (100, 1, 2) — bollard-adjacent). But the symmetric
        // restriction [(200, 1, 0), (100, 2, 0)] hits the fwd-default bug:
        // pivot is null (different EdgeIds), fwd = (Tail < Head) = (2 < 0) =
        // false, TryLo searches from lo=0 and finds (100, 0, 1) — the WRONG
        // sub-edge (a→j, not j→bollard). The turn cost gets attached to vertex
        // 'j' instead of the bollard vertex.
        //
        // This test asserts the CORRECT behavior: turn costs should land on
        // the bollard vertex, NOT on 'j'. It is expected to FAIL on the
        // current resolver, demonstrating the bug.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });
        const double lonA = 4.86620, latA = 51.269700;
        const double lonJ = 4.86660, latJ = 51.269500;
        const double lonBollard = 4.86700, latBollard = 51.269300;
        const double lonB = 4.86740, latB = 51.269100;
        const double lonX = 4.86660, latX = 51.269700;
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
            new Node { Id = 4, Longitude = lonB, Latitude = latB },
            new Node { Id = 5, Longitude = lonX, Latitude = latX },
            new Way { Id = 100, Nodes = new long[] { 1, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 200, Nodes = new long[] { 3, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 300, Nodes = new long[] { 2, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential")) }
        };

        var tile = BuildStandaloneTile(routerDb, tx, ty, osm);

        var globalIdSet = new GlobalNetworkManager();
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tile, globalIdSet);
        }

        Assert.Empty(globalIdSet.PendingRestrictions);

        var bollardVertex = FindVertexAtLocation(routerDb.Latest, lonBollard, latBollard);
        var jVertex = FindVertexAtLocation(routerDb.Latest, lonJ, latJ);
        Assert.True(bollardVertex.HasValue, "expected to find a vertex at the bollard location");
        Assert.True(jVertex.HasValue, "expected to find a vertex at the junction location");

        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex");
        Assert.False(HasAnyTurnCost(routerDb.Latest, jVertex.Value),
            "expected NO turn cost at the junction vertex 'j' — the bollard is not there");
    }

    [Fact]
    public void AddStandaloneTile_TwoTiles_BollardAtBoundary_WithInteriorJunctionOnToWaySide_TileBFirst_TurnCostShouldBeAtBollardOnly()
    {
        // Cross-tile bollard with an interior junction on the in-tile (to-way)
        // side:
        //   Way 100 = [a, bollard]               — a in tile A, bollard in tile B (boundary crossing)
        //   Way 200 = [bollard, j2, b]           — entirely in tile B, split at j2 by way 300
        //   Way 300 = [j2, x]                    — in tile B
        //
        // Tile B loads first → bollard restriction is deferred (the way-100
        // boundary half hasn't materialized yet AND way-200's tail-hop
        // (200, 2, 0) doesn't exact-match against the stored sub-edges).
        // Tile A loads → boundary edge is created → retry runs.
        //
        // Both restrictions in the retry hit the resolver bugs:
        //   r1 [(100,0,1), (200,0,2)]: head-hop subsection TryHi → (200, 1, 2)
        //     which is the b-side, NOT bollard-adjacent. Last-edge.Tail = j2.
        //   r2 [(200,2,0), (100,1,0)]: tail-hop subsection TryLo → (200, 0, 1)
        //     correctly. Head-hop exact-match for inverted (100, 0, 1).
        //     Last-edge.Tail = vertex(a).
        // Net effect: turn cost at j2 and a, none at the bollard.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        const double lonA = 4.95582, latA = 51.269;        // tile A
        const double lonBollard = 4.96682, latBollard = 51.269;  // tile B
        const double lonJ2 = 4.97082, latJ2 = 51.269;       // tile B
        const double lonB = 4.97582, latB = 51.269;         // tile B
        const double lonX = 4.97082, latX = 51.270;         // tile B (different lat)
        var (tileAx, tileAy) = TileStatic.WorldToTile(lonA, latA, 14);
        var (tileBx, tileBy) = TileStatic.WorldToTile(lonBollard, latBollard, 14);
        Assert.NotEqual((tileAx, tileAy), (tileBx, tileBy));

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonA, Latitude = latA },
            new Node
            {
                Id = 2, Longitude = lonBollard, Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonJ2, Latitude = latJ2 },
            new Node { Id = 4, Longitude = lonB, Latitude = latB },
            new Node { Id = 5, Longitude = lonX, Latitude = latX },
            new Way { Id = 100, Nodes = new long[] { 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 200, Nodes = new long[] { 2, 3, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 300, Nodes = new long[] { 3, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential")) }
        };

        var tileAStandalone = BuildStandaloneTile(routerDb, tileAx, tileAy, osm);
        var tileBStandalone = BuildStandaloneTile(routerDb, tileBx, tileBy, osm);

        var globalIdSet = new GlobalNetworkManager();

        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileBStandalone, globalIdSet);
        }
        Assert.NotEmpty(globalIdSet.PendingRestrictions);

        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileAStandalone, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);

        var bollardVertex = FindVertexAtLocation(routerDb.Latest, lonBollard, latBollard);
        var j2Vertex = FindVertexAtLocation(routerDb.Latest, lonJ2, latJ2);
        var aVertex = FindVertexAtLocation(routerDb.Latest, lonA, latA);
        Assert.True(bollardVertex.HasValue);
        Assert.True(j2Vertex.HasValue);
        Assert.True(aVertex.HasValue);

        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex");
        Assert.False(HasAnyTurnCost(routerDb.Latest, j2Vertex.Value),
            "expected NO turn cost at the to-way junction vertex 'j2'");
        Assert.False(HasAnyTurnCost(routerDb.Latest, aVertex.Value),
            "expected NO turn cost at the from-way far-end vertex 'a'");
    }

    [Fact]
    public void AddStandaloneTile_TwoTiles_BollardAtBoundary_WithInteriorJunctionOnToWaySide_TileAFirst_TurnCostShouldBeAtBollardOnly()
    {
        // Same scenario as the previous test but with tile A loaded first
        // (the from-side, far from the bollard). Bollard restriction goes
        // pending after tile A, retries after tile B loads. The bug is
        // load-order-independent — the resolver mis-picks regardless.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        const double lonA = 4.95582, latA = 51.269;
        const double lonBollard = 4.96682, latBollard = 51.269;
        const double lonJ2 = 4.97082, latJ2 = 51.269;
        const double lonB = 4.97582, latB = 51.269;
        const double lonX = 4.97082, latX = 51.270;
        var (tileAx, tileAy) = TileStatic.WorldToTile(lonA, latA, 14);
        var (tileBx, tileBy) = TileStatic.WorldToTile(lonBollard, latBollard, 14);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonA, Latitude = latA },
            new Node
            {
                Id = 2, Longitude = lonBollard, Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonJ2, Latitude = latJ2 },
            new Node { Id = 4, Longitude = lonB, Latitude = latB },
            new Node { Id = 5, Longitude = lonX, Latitude = latX },
            new Way { Id = 100, Nodes = new long[] { 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 200, Nodes = new long[] { 2, 3, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 300, Nodes = new long[] { 3, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential")) }
        };

        var tileAStandalone = BuildStandaloneTile(routerDb, tileAx, tileAy, osm);
        var tileBStandalone = BuildStandaloneTile(routerDb, tileBx, tileBy, osm);

        var globalIdSet = new GlobalNetworkManager();

        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileAStandalone, globalIdSet);
        }
        // Tile A only contains the far-end of way 100 (vertex a). The bollard
        // and the rest of the restriction live in tile B, so nothing is
        // resolvable yet — but no restriction has even been emitted yet
        // (tile A doesn't see way 200 / way 300 ways).

        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tileBStandalone, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);

        var bollardVertex = FindVertexAtLocation(routerDb.Latest, lonBollard, latBollard);
        var j2Vertex = FindVertexAtLocation(routerDb.Latest, lonJ2, latJ2);
        var aVertex = FindVertexAtLocation(routerDb.Latest, lonA, latA);
        Assert.True(bollardVertex.HasValue);
        Assert.True(j2Vertex.HasValue);
        Assert.True(aVertex.HasValue);

        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex");
        Assert.False(HasAnyTurnCost(routerDb.Latest, j2Vertex.Value),
            "expected NO turn cost at the to-way junction vertex 'j2'");
        Assert.False(HasAnyTurnCost(routerDb.Latest, aVertex.Value),
            "expected NO turn cost at the from-way far-end vertex 'a'");
    }

    [Fact]
    public void AddStandaloneTile_BollardAtSharedNodeWithJunctionOnToWaySide_TurnCostShouldBeAtBollardOnly()
    {
        // Mirror of the previous test, but the interior junction is on the
        // other side of the bollard:
        //   Way 100 = [a, bollard]                (simple)
        //   Way 200 = [bollard, j2, b]            (split by way 300)
        //   Way 300 = [j2, x]                     (creates junction at j2)
        //
        // The bollard's tail-hop on way 200 emits (200, 2, 0) (way-spanning
        // toward the b-end). Stored sub-edges are (200, 0, 1) and (200, 1, 2).
        // Restriction [(100, 0, 1), (200, 0, 2)] head-hop hits TryHi which
        // finds (200, 1, 2) — the b-side sub-edge, NOT the bollard-adjacent
        // (200, 0, 1). Turn cost lands on j2 instead of bollard.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });
        const double lonA = 4.86620, latA = 51.269700;
        const double lonBollard = 4.86660, latBollard = 51.269500;
        const double lonJ2 = 4.86700, latJ2 = 51.269300;
        const double lonB = 4.86740, latB = 51.269100;
        const double lonX = 4.86700, latX = 51.269700;
        var (tx, ty) = TileStatic.WorldToTile(lonBollard, latBollard, 14);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonA, Latitude = latA },
            new Node
            {
                Id = 2, Longitude = lonBollard, Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 3, Longitude = lonJ2, Latitude = latJ2 },
            new Node { Id = 4, Longitude = lonB, Latitude = latB },
            new Node { Id = 5, Longitude = lonX, Latitude = latX },
            new Way { Id = 100, Nodes = new long[] { 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 200, Nodes = new long[] { 2, 3, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 300, Nodes = new long[] { 3, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential")) }
        };

        var tile = BuildStandaloneTile(routerDb, tx, ty, osm);
        var globalIdSet = new GlobalNetworkManager();
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tile, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);

        var bollardVertex = FindVertexAtLocation(routerDb.Latest, lonBollard, latBollard);
        var j2Vertex = FindVertexAtLocation(routerDb.Latest, lonJ2, latJ2);
        Assert.True(bollardVertex.HasValue);
        Assert.True(j2Vertex.HasValue);

        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex");
        Assert.False(HasAnyTurnCost(routerDb.Latest, j2Vertex.Value),
            "expected NO turn cost at the to-way junction vertex 'j2'");
    }

    [Fact]
    public void AddStandaloneTile_BollardAtSharedNodeWithJunctionsOnBothSides_TurnCostShouldBeAtBollardOnly()
    {
        // Both member ways are split by interior junctions:
        //   Way 100 = [a, j1, bollard]   (split by way 300 at j1)
        //   Way 200 = [bollard, j2, b]   (split by way 400 at j2)
        // Now BOTH tail-hops need subsection search and BOTH hit the
        // wrong-end fwd-default. Turn costs should still land only on the
        // bollard vertex; this test asserts NO spurious turn cost at j1 or j2.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });
        const double lonA = 4.86600, latA = 51.269800;
        const double lonJ1 = 4.86640, latJ1 = 51.269600;
        const double lonBollard = 4.86680, latBollard = 51.269400;
        const double lonJ2 = 4.86720, latJ2 = 51.269200;
        const double lonB = 4.86760, latB = 51.269000;
        const double lonX1 = 4.86640, latX1 = 51.269800;
        const double lonX2 = 4.86720, latX2 = 51.269000;
        var (tx, ty) = TileStatic.WorldToTile(lonBollard, latBollard, 14);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonA, Latitude = latA },
            new Node { Id = 2, Longitude = lonJ1, Latitude = latJ1 },
            new Node
            {
                Id = 3, Longitude = lonBollard, Latitude = latBollard,
                Tags = new TagsCollection(new Tag("barrier", "bollard"))
            },
            new Node { Id = 4, Longitude = lonJ2, Latitude = latJ2 },
            new Node { Id = 5, Longitude = lonB, Latitude = latB },
            new Node { Id = 6, Longitude = lonX1, Latitude = latX1 },
            new Node { Id = 7, Longitude = lonX2, Latitude = latX2 },
            new Way { Id = 100, Nodes = new long[] { 1, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 200, Nodes = new long[] { 3, 4, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 300, Nodes = new long[] { 2, 6 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 400, Nodes = new long[] { 4, 7 },
                Tags = new TagsCollection(new Tag("highway", "residential")) }
        };

        var tile = BuildStandaloneTile(routerDb, tx, ty, osm);
        var globalIdSet = new GlobalNetworkManager();
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tile, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);

        var bollardVertex = FindVertexAtLocation(routerDb.Latest, lonBollard, latBollard);
        var j1Vertex = FindVertexAtLocation(routerDb.Latest, lonJ1, latJ1);
        var j2Vertex = FindVertexAtLocation(routerDb.Latest, lonJ2, latJ2);
        Assert.True(bollardVertex.HasValue);
        Assert.True(j1Vertex.HasValue);
        Assert.True(j2Vertex.HasValue);

        Assert.True(HasAnyTurnCost(routerDb.Latest, bollardVertex.Value),
            "expected a turn cost at the bollard vertex");
        Assert.False(HasAnyTurnCost(routerDb.Latest, j1Vertex.Value),
            "expected NO turn cost at the from-way junction vertex 'j1'");
        Assert.False(HasAnyTurnCost(routerDb.Latest, j2Vertex.Value),
            "expected NO turn cost at the to-way junction vertex 'j2'");
    }

    [Fact]
    public void AddStandaloneTile_ViaNodeTurnRestriction_ToWaySplitByInteriorJunction_TurnCostShouldBeAtViaOnly()
    {
        // OSM turn restriction: from way 100, via node 2, to way 200.
        // Way 200 = [via, j2, b] is split at j2 by way 300.
        //
        // Restriction emitted as chain [(100, 0, 1), (200, 0, 2)].
        //   tail-hop (100, 0, 1) — exact match.
        //   head-hop (200, 0, 2) — exact-match fail. Subsection: lo=0, hi=2,
        //     fwd=true, pivot=null (different EdgeIds), pivotIsHi=true, TryHi.
        //     d=1: (200, 1, 2). STORED → returned. But this is the b-side
        //     sub-edge, NOT the via-adjacent (200, 0, 1).
        // last-edge (200, 1, 2) forward=true → Tail = j2. Turn cost at j2,
        // not at via.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });
        const double lonA = 4.86620, latA = 51.269700;
        const double lonVia = 4.86660, latVia = 51.269500;
        const double lonJ2 = 4.86700, latJ2 = 51.269300;
        const double lonB = 4.86740, latB = 51.269100;
        const double lonX = 4.86700, latX = 51.269700;
        var (tx, ty) = TileStatic.WorldToTile(lonVia, latVia, 14);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonA, Latitude = latA },
            new Node { Id = 2, Longitude = lonVia, Latitude = latVia },
            new Node { Id = 3, Longitude = lonJ2, Latitude = latJ2 },
            new Node { Id = 4, Longitude = lonB, Latitude = latB },
            new Node { Id = 5, Longitude = lonX, Latitude = latX },
            new Way { Id = 100, Nodes = new long[] { 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 200, Nodes = new long[] { 2, 3, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 300, Nodes = new long[] { 3, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Relation
            {
                Id = 1,
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "no_straight_on")),
                Members = new[]
                {
                    new RelationMember(100, "from", OsmGeoType.Way),
                    new RelationMember(2, "via", OsmGeoType.Node),
                    new RelationMember(200, "to", OsmGeoType.Way)
                }
            }
        };

        var tile = BuildStandaloneTile(routerDb, tx, ty, osm);
        var globalIdSet = new GlobalNetworkManager();
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tile, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);

        var viaVertex = FindVertexAtLocation(routerDb.Latest, lonVia, latVia);
        var j2Vertex = FindVertexAtLocation(routerDb.Latest, lonJ2, latJ2);
        Assert.True(viaVertex.HasValue);
        Assert.True(j2Vertex.HasValue);

        Assert.True(HasAnyTurnCost(routerDb.Latest, viaVertex.Value),
            "expected a turn cost at the via vertex");
        Assert.False(HasAnyTurnCost(routerDb.Latest, j2Vertex.Value),
            "expected NO turn cost at the to-way junction vertex 'j2'");
    }

    [Fact]
    public void AddStandaloneTile_ViaNodeTurnRestriction_FromWaySplitByInteriorJunction_TurnCostShouldBeAtViaOnly()
    {
        // OSM turn restriction: from way 100 = [a, j, via], via node 3 (the via
        // index in way 100 is 2), to way 200 = [via, b]. Way 100 is split at
        // j by way 300.
        //
        // Chain emitted: [(100, 0, 2), (200, 0, 1)].
        //   tail-hop (100, 0, 2): subsection TryHi finds (100, 1, 2) — the
        //     via-adjacent sub-edge. ✓
        //   head-hop (200, 0, 1): exact match. ✓
        // last-edge (200, 0, 1) forward=true → Tail = via. Turn cost at via. ✓
        //
        // For one-direction turn restrictions, the from-way-split case
        // currently works correctly because the LAST edge in the chain is
        // the simple to-way (exact match), so the wrong-end fwd-default
        // bug never affects the turn-cost vertex selection. This test is
        // a control / sanity check — it should already pass.

        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });
        const double lonA = 4.86620, latA = 51.269700;
        const double lonJ = 4.86660, latJ = 51.269500;
        const double lonVia = 4.86700, latVia = 51.269300;
        const double lonB = 4.86740, latB = 51.269100;
        const double lonX = 4.86660, latX = 51.269700;
        var (tx, ty) = TileStatic.WorldToTile(lonVia, latVia, 14);

        var osm = new OsmGeo[]
        {
            new Node { Id = 1, Longitude = lonA, Latitude = latA },
            new Node { Id = 2, Longitude = lonJ, Latitude = latJ },
            new Node { Id = 3, Longitude = lonVia, Latitude = latVia },
            new Node { Id = 4, Longitude = lonB, Latitude = latB },
            new Node { Id = 5, Longitude = lonX, Latitude = latX },
            new Way { Id = 100, Nodes = new long[] { 1, 2, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 200, Nodes = new long[] { 3, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 300, Nodes = new long[] { 2, 5 },
                Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Relation
            {
                Id = 1,
                Tags = new TagsCollection(
                    new Tag("type", "restriction"),
                    new Tag("restriction", "no_straight_on")),
                Members = new[]
                {
                    new RelationMember(100, "from", OsmGeoType.Way),
                    new RelationMember(3, "via", OsmGeoType.Node),
                    new RelationMember(200, "to", OsmGeoType.Way)
                }
            }
        };

        var tile = BuildStandaloneTile(routerDb, tx, ty, osm);
        var globalIdSet = new GlobalNetworkManager();
        using (var writer = routerDb.Latest.GetWriter())
        {
            writer.AddStandaloneTile(tile, globalIdSet);
        }
        Assert.Empty(globalIdSet.PendingRestrictions);

        var viaVertex = FindVertexAtLocation(routerDb.Latest, lonVia, latVia);
        var jVertex = FindVertexAtLocation(routerDb.Latest, lonJ, latJ);
        Assert.True(viaVertex.HasValue);
        Assert.True(jVertex.HasValue);

        Assert.True(HasAnyTurnCost(routerDb.Latest, viaVertex.Value),
            "expected a turn cost at the via vertex");
        Assert.False(HasAnyTurnCost(routerDb.Latest, jVertex.Value),
            "expected NO turn cost at the from-way junction vertex 'j'");
    }

    private static Itinero.Network.Tiles.Standalone.StandaloneNetworkTile BuildStandaloneTile(
        RouterDb routerDb, uint x, uint y, OsmGeo[] osm)
    {
        var w = routerDb.Latest.GetStandaloneTileWriter(x, y);
        w.AddTileData(osm);
        return w.GetResultingTile();
    }

    private static VertexId? FindVertexAtLocation(RoutingNetwork network, double longitude, double latitude)
    {
        const double tolerance = 1e-4;
        foreach (var v in network.GetVertices())
        {
            if (!network.TryGetVertex(v, out var vLon, out var vLat, out _)) continue;
            if (System.Math.Abs(vLon - longitude) < tolerance &&
                System.Math.Abs(vLat - latitude) < tolerance)
            {
                return v;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the first vertex in the network whose incident-edge count equals the given degree.
    /// </summary>
    private static VertexId? FindVertexWithDegree(RoutingNetwork network, int degree)
    {
        var enumerator = network.GetEdgeEnumerator();
        foreach (var v in network.GetVertices())
        {
            enumerator.MoveTo(v);
            var d = 0;
            while (enumerator.MoveNext()) d++;
            if (d == degree) return v;
        }
        return null;
    }

    /// <summary>
    /// Returns true if any incident edge of the given vertex carries a turn-cost
    /// reference at this vertex. After MoveTo(vertex), the enumerator's
    /// TailOrder is the turn-cost order at the iteration vertex (tail in the
    /// enumerator view); HeadOrder is the order at the OTHER endpoint and
    /// must not be considered.
    /// </summary>
    private static bool HasAnyTurnCost(RoutingNetwork network, VertexId vertex)
    {
        var enumerator = network.GetEdgeEnumerator();
        enumerator.MoveTo(vertex);
        while (enumerator.MoveNext())
        {
            if (enumerator.TailOrder != null) return true;
        }
        return false;
    }
}
