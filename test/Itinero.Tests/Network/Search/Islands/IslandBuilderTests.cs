using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

public class IslandBuilderTests
{
    private static async Task BuildIslands(RouterDb routerDb, Profile profile, IEnumerable<EdgeId> edges)
    {
        var network = routerDb.Latest;

        // build islands for all tiles that contain edges
        var tiles = new HashSet<uint>();
        foreach (var edge in edges)
        {
            tiles.Add(edge.TileId);
        }

        foreach (var tileId in tiles)
        {
            await IslandBuilder.BuildForTileAsync(network, profile, tileId, CancellationToken.None);
        }
    }

    private static bool IsEdgeOnIsland(RouterDb routerDb, Profile profile, EdgeId edgeId)
    {
        var result = IslandBuilder.ResolveEdgeAsync(routerDb.Latest, profile, edgeId, CancellationToken.None).Result;
        return result == true; // true = island, false/null = not island
    }

    /// <summary>
    /// Cold-path check: do NOT pre-warm with BuildForTileAsync. Exercises
    /// <see cref="IslandBuilder.ResolveEdgeAsync"/> against an un-classified network,
    /// which is the runtime scenario hit by <see cref="Itinero.Snapping.Snapper"/> on
    /// a cold tile cache. The existing pre-warmed tests don't catch failures unique
    /// to this path because they always run <c>BuildForTileAsync</c> first.
    /// </summary>
    private static bool IsEdgeOnIsland_Cold(RouterDb routerDb, Profile profile, EdgeId edgeId)
    {
        var result = IslandBuilder.ResolveEdgeAsync(routerDb.Latest, profile, edgeId, CancellationToken.None).Result;
        return result == true;
    }

    [Fact]
    public async Task IslandBuilder_SingleEdge_ShouldBeIsland()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(v1, v2);
        }

        await BuildIslands(routerDb, new DefaultProfile(), new[] { edge });
        Assert.True(IsEdgeOnIsland(routerDb, new DefaultProfile(), edge));
    }

    [Fact]
    public async Task IslandBuilder_TwoEdges_MaxSizeTwo_ShouldNotBeIsland()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var v3 = writer.AddVertex(4.797506332397461, 51.26874845584085);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
        }

        await BuildIslands(routerDb, new DefaultProfile(), edges);
        Assert.False(IsEdgeOnIsland(routerDb, new DefaultProfile(), edges[0]));
        Assert.False(IsEdgeOnIsland(routerDb, new DefaultProfile(), edges[1]));
    }

    [Fact]
    public async Task IslandBuilder_TwoEdges_MaxSizeThree_ShouldBeIsland()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 3 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var v3 = writer.AddVertex(4.797506332397461, 51.26874845584085);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
        }

        await BuildIslands(routerDb, new DefaultProfile(), edges);
        Assert.True(IsEdgeOnIsland(routerDb, new DefaultProfile(), edges[0]));
        Assert.True(IsEdgeOnIsland(routerDb, new DefaultProfile(), edges[1]));
    }

    [Fact]
    public async Task IslandBuilder_TwoEdgesOneWay_ShouldBeIsland()
    {
        // Two one-way edges: they don't connect bidirectionally,
        // and they don't form a loop, so they're islands.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var v3 = writer.AddVertex(4.797506332397461, 51.26874845584085);
            edges.Add(writer.AddEdge(v1, v2, attributes: new[] { ("oneway", "yes") }));
            edges.Add(writer.AddEdge(v2, v3, attributes: new[] { ("oneway", "yes") }));
        }

        var profile = new DefaultProfile(getEdgeFactor: a =>
        {
            if (a.Any(x => x.key == "oneway")) return new EdgeFactor(1, 0, 1, 0);
            return new EdgeFactor(1, 1, 1, 1);
        });

        await BuildIslands(routerDb, profile, edges);
        Assert.True(IsEdgeOnIsland(routerDb, profile, edges[0]));
        Assert.True(IsEdgeOnIsland(routerDb, profile, edges[1]));
    }

    [Fact]
    public async Task IslandBuilder_OneWayLeadingIntoCulDeSac_ShouldBothBeIsland()
    {
        // A bidirectional main network connected to a bidirectional cul-de-sac via a
        // one-way edge. Vehicles can drive INTO the cul-de-sac (main → v2→v3 → cul-de-sac)
        // but cannot return: the one-way edge is only traversable away from main, and
        // the cul-de-sac dead-ends at v5. Under Itinero's symmetric-reachability
        // definition (CanReachMainNetwork requires both directions) the one-way edge
        // AND the cul-de-sac are routing traps → both island.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            var v4 = writer.AddVertex(4.793, 51.264);
            var v5 = writer.AddVertex(4.798, 51.268);
            // bidirectional main network at the tail of the one-way
            edges.Add(writer.AddEdge(v4, v1));
            edges.Add(writer.AddEdge(v1, v2));
            // one-way edge feeding into the cul-de-sac
            edges.Add(writer.AddEdge(v2, v3, attributes: new[] { ("oneway", "yes") }));
            // bidirectional cul-de-sac (no further edges at v5)
            edges.Add(writer.AddEdge(v3, v5));
        }

        var profile = new DefaultProfile(getEdgeFactor: a =>
        {
            if (a.Any(x => x.key == "oneway")) return new EdgeFactor(1, 0, 1, 0);
            return new EdgeFactor(1, 1, 1, 1);
        });

        await BuildIslands(routerDb, profile, edges);
        Assert.False(IsEdgeOnIsland(routerDb, profile, edges[0]), "main-network edge at tail should not be island");
        Assert.False(IsEdgeOnIsland(routerDb, profile, edges[1]), "main-network edge should not be island");
        Assert.True(IsEdgeOnIsland(routerDb, profile, edges[2]), "one-way edge feeding into cul-de-sac is a routing trap, so it IS an island");
        Assert.True(IsEdgeOnIsland(routerDb, profile, edges[3]), "bidirectional cul-de-sac behind a one-way edge is a routing trap, so it IS an island");
    }

    [Fact]
    public async Task IslandBuilder_OneWayBetweenTwoMainNetworks_ShouldNotBeIsland()
    {
        // A one-way edge bridging two real main-network components. Each side has
        // enough bidirectional edges (≥ MaxIslandSize) to graduate to the sentinel,
        // and the one-way edge is traversable in its allowed direction with main
        // network reachable from both ends — so it is genuinely part of the routable
        // network. None of the edges should be islands.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            var v4 = writer.AddVertex(4.793, 51.264);
            var v5 = writer.AddVertex(4.798, 51.268);
            var v6 = writer.AddVertex(4.799, 51.269);
            // main network A (tail side of the one-way)
            edges.Add(writer.AddEdge(v4, v1));
            edges.Add(writer.AddEdge(v1, v2));
            // one-way edge (the bridge under test)
            edges.Add(writer.AddEdge(v2, v3, attributes: new[] { ("oneway", "yes") }));
            // main network B (head side of the one-way)
            edges.Add(writer.AddEdge(v3, v5));
            edges.Add(writer.AddEdge(v5, v6));
        }

        var profile = new DefaultProfile(getEdgeFactor: a =>
        {
            if (a.Any(x => x.key == "oneway")) return new EdgeFactor(1, 0, 1, 0);
            return new EdgeFactor(1, 1, 1, 1);
        });

        await BuildIslands(routerDb, profile, edges);
        Assert.False(IsEdgeOnIsland(routerDb, profile, edges[0]), "main-network A edge should not be island");
        Assert.False(IsEdgeOnIsland(routerDb, profile, edges[1]), "main-network A edge should not be island");
        Assert.False(IsEdgeOnIsland(routerDb, profile, edges[2]), "one-way edge bridging two main networks is routable and not an island");
        Assert.False(IsEdgeOnIsland(routerDb, profile, edges[3]), "main-network B edge should not be island");
        Assert.False(IsEdgeOnIsland(routerDb, profile, edges[4]), "main-network B edge should not be island");
    }

    [Fact]
    public async Task IslandBuilder_OneWayDeadEnd_ShouldBeIsland()
    {
        // One-way edge where the head end has no bidirectional connection
        // → dead end, should be an island.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            // bidirectional edges
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
            // one-way edge — head (v4) is a dead end
            var v4 = writer.AddVertex(4.798, 51.268);
            edges.Add(writer.AddEdge(v3, v4, attributes: new[] { ("oneway", "yes") }));
        }

        var profile = new DefaultProfile(getEdgeFactor: a =>
        {
            if (a.Any(x => x.key == "oneway")) return new EdgeFactor(1, 0, 1, 0);
            return new EdgeFactor(1, 1, 1, 1);
        });

        await BuildIslands(routerDb, profile, edges);
        Assert.False(IsEdgeOnIsland(routerDb, profile, edges[0]), "bidirectional edge 0 should not be island");
        Assert.False(IsEdgeOnIsland(routerDb, profile, edges[1]), "bidirectional edge 1 should not be island");
        Assert.True(IsEdgeOnIsland(routerDb, profile, edges[2]), "one-way dead end should be island");
    }

    [Fact]
    public async Task IslandBuilder_TwoEdgesWithBarrier_ShouldBothBeIsland()
    {
        // Two edges meeting at v2 with a binary turn-restriction at v2 (a barrier
        // that forbids transit between the two edges). For routing, each edge is
        // its own connected component → both should be detected as islands.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var v3 = writer.AddVertex(4.797506332397461, 51.26874845584085);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));

            // barrier at v2: turning between the two edges is forbidden.
            writer.AddTurnCosts(v2,
                attributes: new[] { ("barrier", "bollard") },
                edges: edges.ToArray(),
                costs: new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var profile = new DefaultProfile(getTurnCostFactor: a =>
            a.Any(x => x.key == "barrier") ? TurnCostFactor.Binary : TurnCostFactor.Empty);

        await BuildIslands(routerDb, profile, edges);
        Assert.True(IsEdgeOnIsland(routerDb, profile, edges[0]), "edge 0 isolated by barrier at v2");
        Assert.True(IsEdgeOnIsland(routerDb, profile, edges[1]), "edge 1 isolated by barrier at v2");
    }

    [Fact]
    public async Task IslandBuilder_OneWayEdgeWithBarrier_ShouldBothBeIsland()
    {
        // Like IslandBuilder_TwoEdgesWithBarrier_ShouldBothBeIsland, but edge 0 is
        // one-way (v1 → v2 only). The barrier at v2 must still island both edges.
        // The asymmetry between the two edges' directions also exercises the
        // directional handling in the two-enumerator GetIslandBuilderCost primitive.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var v3 = writer.AddVertex(4.797506332397461, 51.26874845584085);
            edges.Add(writer.AddEdge(v1, v2, attributes: new[] { ("oneway", "yes") }));
            edges.Add(writer.AddEdge(v2, v3));

            writer.AddTurnCosts(v2,
                attributes: new[] { ("barrier", "bollard") },
                edges: edges.ToArray(),
                costs: new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var profile = new DefaultProfile(
            getEdgeFactor: a => a.Any(x => x.key == "oneway")
                ? new EdgeFactor(1, 0, 1, 0)
                : new EdgeFactor(1, 1, 1, 1),
            getTurnCostFactor: a => a.Any(x => x.key == "barrier")
                ? TurnCostFactor.Binary
                : TurnCostFactor.Empty);

        await BuildIslands(routerDb, profile, edges);
        Assert.True(IsEdgeOnIsland(routerDb, profile, edges[0]),
            "one-way edge 0 isolated by barrier at v2");
        Assert.True(IsEdgeOnIsland(routerDb, profile, edges[1]),
            "edge 1 isolated by barrier at v2");
    }

    [Fact]
    public async Task IslandBuilder_DisconnectedComponent_ShouldBeIsland()
    {
        // Two separate groups of edges — the small group is an island.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 3 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            // large group (3 edges ≥ MaxIslandSize)
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            var v4 = writer.AddVertex(4.798, 51.268);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
            edges.Add(writer.AddEdge(v3, v4));

            // small disconnected group (1 edge < MaxIslandSize)
            var v5 = writer.AddVertex(4.780, 51.260);
            var v6 = writer.AddVertex(4.782, 51.261);
            edges.Add(writer.AddEdge(v5, v6));
        }

        await BuildIslands(routerDb, new DefaultProfile(), edges);
        Assert.False(IsEdgeOnIsland(routerDb, new DefaultProfile(), edges[0]), "large group edge 0");
        Assert.False(IsEdgeOnIsland(routerDb, new DefaultProfile(), edges[1]), "large group edge 1");
        Assert.False(IsEdgeOnIsland(routerDb, new DefaultProfile(), edges[2]), "large group edge 2");
        Assert.True(IsEdgeOnIsland(routerDb, new DefaultProfile(), edges[3]), "small disconnected group should be island");
    }

    // ────────────────────────────────────────────────────────────────────
    // Cold-path tests: exercise ResolveEdgeAsync against an un-classified
    // network (no BuildForTileAsync pre-warm). These tests cover the
    // runtime scenario that publish-api hits when its tile cache is cold —
    // the snapper sees an unresolved edge, IsAcceptable returns null,
    // RunCheckAsync → ResolveEdgeAsync. No DONE-tile fast path available.
    // See publish-api#60 / #61.
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void IslandBuilder_Cold_TwoEdgesBidirectional_MaxSizeTwo_ShouldNotBeIsland()
    {
        // Cold variant of IslandBuilder_TwoEdges_MaxSizeTwo_ShouldNotBeIsland.
        // The simplest possible "not island" case: two bidirectional edges, component
        // size 2 graduates immediately via the bidirectional merge inside ProcessEdge.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var v3 = writer.AddVertex(4.797506332397461, 51.26874845584085);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
        }

        Assert.False(IsEdgeOnIsland_Cold(routerDb, new DefaultProfile(), edges[0]),
            "cold ResolveEdgeAsync on a bidirectional pair should not be island");
        Assert.False(IsEdgeOnIsland_Cold(routerDb, new DefaultProfile(), edges[1]),
            "cold ResolveEdgeAsync on a bidirectional pair should not be island");
    }

    [Fact]
    public void IslandBuilder_Cold_DisconnectedComponent_ShouldBeIsland()
    {
        // Cold variant of IslandBuilder_DisconnectedComponent_ShouldBeIsland.
        // Two disconnected groups, MaxIslandSize=3. Without pre-warming, ResolveEdgeAsync
        // must still classify the small disconnected group as island and the large group
        // as not-island.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 3 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            var v4 = writer.AddVertex(4.798, 51.268);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
            edges.Add(writer.AddEdge(v3, v4));

            var v5 = writer.AddVertex(4.780, 51.260);
            var v6 = writer.AddVertex(4.782, 51.261);
            edges.Add(writer.AddEdge(v5, v6));
        }

        Assert.False(IsEdgeOnIsland_Cold(routerDb, new DefaultProfile(), edges[0]), "cold large group edge 0");
        Assert.False(IsEdgeOnIsland_Cold(routerDb, new DefaultProfile(), edges[1]), "cold large group edge 1");
        Assert.False(IsEdgeOnIsland_Cold(routerDb, new DefaultProfile(), edges[2]), "cold large group edge 2");
        Assert.True(IsEdgeOnIsland_Cold(routerDb, new DefaultProfile(), edges[3]), "cold small disconnected group should be island");
    }

    [Fact]
    public void IslandBuilder_Cold_OneWayBetweenTwoMainNetworks_ShouldNotBeIsland()
    {
        // Cold variant of IslandBuilder_OneWayBetweenTwoMainNetworks_ShouldNotBeIsland.
        // This is the case that publish-api#61 motorways trigger. A one-way edge bridges
        // two bidirectional main networks. Pre-warmed BuildForTileAsync graduates the
        // one-way bridge via TryResolve / CanReachMainNetwork. Cold ResolveEdgeAsync must
        // do the same on its own.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            var v4 = writer.AddVertex(4.793, 51.264);
            var v5 = writer.AddVertex(4.798, 51.268);
            var v6 = writer.AddVertex(4.799, 51.269);
            edges.Add(writer.AddEdge(v4, v1));
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3, attributes: new[] { ("oneway", "yes") }));
            edges.Add(writer.AddEdge(v3, v5));
            edges.Add(writer.AddEdge(v5, v6));
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.False(IsEdgeOnIsland_Cold(routerDb, profile, edges[0]), "cold main-network A edge should not be island");
        Assert.False(IsEdgeOnIsland_Cold(routerDb, profile, edges[1]), "cold main-network A edge should not be island");
        Assert.False(IsEdgeOnIsland_Cold(routerDb, profile, edges[2]),
            "cold one-way bridging two main networks should not be island — this is the publish-api#61 case");
        Assert.False(IsEdgeOnIsland_Cold(routerDb, profile, edges[3]), "cold main-network B edge should not be island");
        Assert.False(IsEdgeOnIsland_Cold(routerDb, profile, edges[4]), "cold main-network B edge should not be island");
    }

    [Fact]
    public void IslandBuilder_Cold_OneWayLoopSpansTwoTiles_ShouldNotBeIsland()
    {
        // A one-way 4-edge loop that physically spans two tiles. Topology:
        //
        //       a1 → a2          (e0 inside tile A)
        //       ↑     ↓
        //      e3    e1           (e1 cross-tile A→B, e3 cross-tile B→A)
        //       ↑     ↓
        //       b2 ← b1          (e2 inside tile B)
        //
        // All four edges one-way; together they form a directed cycle a1→a2→b1→b2→a1.
        // With MaxIslandSize=4 the cycle, when detected as a Tarjan SCC, merges into one
        // component of size 4 → CollapseToMainNetwork → not island.
        //
        // Cold ResolveEdgeAsync(e0): the tile-BFS loop seeds tilesToProcess with the
        // seed's tail and head tile ids — both are tile A. EnqueueNeighborTiles then
        // walks members of the seed's component (e0 alone, since one-way edges don't
        // bidirectionally merge with anyone), so only tile A is processed. e2 and e3 live
        // in tile B and are never ProcessEdge'd. The dg only sees half the cycle: it has
        // e0→e1, e1→e2, e3→e0 but is missing e2→e3. Tarjan finds no SCC. Component stays
        // singletons. CanReachMainNetwork(e0) returns false → e0 declared island.
        //
        // This pins the multi-tile expansion bug at unit-test scale. publish-api#61
        // production case_037 is the same shape but on a sprawling motorway chain.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        var edges = new List<EdgeId>();
        EdgeId seed;
        using (var writer = routerDb.GetMutableNetwork())
        {
            // Tile A vertices (x=8385).
            var a1 = writer.AddVertex(4.255, 51.000);
            var a2 = writer.AddVertex(4.260, 51.000);
            // Tile B vertices (x=8386).
            var b1 = writer.AddVertex(4.270, 51.000);
            var b2 = writer.AddVertex(4.275, 51.000);

            Assert.NotEqual(a1.TileId, b1.TileId); // sanity: tiles must differ

            var oneway = new[] { ("oneway", "yes") };
            seed = writer.AddEdge(a1, a2, attributes: oneway); // e0 in tile A
            edges.Add(seed);
            edges.Add(writer.AddEdge(a2, b1, attributes: oneway)); // e1 cross-tile A→B
            edges.Add(writer.AddEdge(b1, b2, attributes: oneway)); // e2 in tile B
            edges.Add(writer.AddEdge(b2, a1, attributes: oneway)); // e3 cross-tile B→A
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.False(IsEdgeOnIsland_Cold(routerDb, profile, seed),
            "cold ResolveEdgeAsync on a one-way edge that's part of a closed loop spanning " +
            "two tiles should return not-island — Tarjan SCC needs to see the full cycle, " +
            "which requires processing both tiles. publish-api#61 multi-tile shape.");
    }

    [Fact]
    public void IslandBuilder_Cold_OneWayInsideTileA_BidirNetworkSpansTileAandB_ShouldNotBeIsland()
    {
        // Reproduces the publish-api#61 production failure shape at unit-test scale.
        //
        // Topology:
        //   - Tile A (x=8385): 5 vertices a1..a5 chained by 4 bidirectional residentials.
        //   - Cross-tile bidirectional bridge from a5 (tile A) to b1 (tile B, x=8386).
        //   - Tile B (x=8386): 5 vertices b1..b5 chained by 4 bidirectional residentials.
        //   - One-way seed e_seed: a1 → a2, INSIDE tile A only.
        //
        // Sizes:
        //   - Combined bidirectional component (A chain + bridge + B chain) = 4+1+4 = 9.
        //   - MaxIslandSize = 8.
        //   - Tile A alone yields a bidir component of at most 6 (A's 4 + bridge + e4 once
        //     ProcessEdge(bridge) walks b1 and merges with e4 in tile B's data).
        //     6 < 8 → no CollapseToMainNetwork from tile A's view alone.
        //
        // Cold ResolveEdgeAsync(e_seed) must classify the seed as not-island. To do so,
        // the algorithm has to process tile B as well — only then does the combined
        // bidirectional component reach MaxIslandSize and graduate to MainNet, which is
        // what gives the seed its CanReachMainNetwork path.
        //
        // On develop and on the partial fix that just removes the ProcessEdge skip for
        // IsNotIsland edges, this test FAILS: the tile-BFS loop in ResolveEdgeAsync only
        // processes the seed's tile (tile A), so the combined component never grows past
        // MaxIslandSize, no edge graduates, and CanReachMainNetwork(e_seed) returns false.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 8 });
        var edges = new List<EdgeId>();
        EdgeId seed;
        using (var writer = routerDb.GetMutableNetwork())
        {
            // Tile A vertices (x=8385).
            var a1 = writer.AddVertex(4.250, 51.000);
            var a2 = writer.AddVertex(4.253, 51.000);
            var a3 = writer.AddVertex(4.256, 51.000);
            var a4 = writer.AddVertex(4.259, 51.000);
            var a5 = writer.AddVertex(4.261, 51.000);
            // Tile B vertices (x=8386).
            var b1 = writer.AddVertex(4.265, 51.000);
            var b2 = writer.AddVertex(4.270, 51.000);
            var b3 = writer.AddVertex(4.275, 51.000);
            var b4 = writer.AddVertex(4.280, 51.000);
            var b5 = writer.AddVertex(4.282, 51.000);

            Assert.NotEqual(a5.TileId, b1.TileId); // sanity: tiles must differ

            // Bidir chain in tile A (4 edges).
            edges.Add(writer.AddEdge(a1, a2));
            edges.Add(writer.AddEdge(a2, a3));
            edges.Add(writer.AddEdge(a3, a4));
            edges.Add(writer.AddEdge(a4, a5));
            // Cross-tile bidir bridge.
            edges.Add(writer.AddEdge(a5, b1));
            // Bidir chain in tile B (4 edges).
            edges.Add(writer.AddEdge(b1, b2));
            edges.Add(writer.AddEdge(b2, b3));
            edges.Add(writer.AddEdge(b3, b4));
            edges.Add(writer.AddEdge(b4, b5));
            // One-way seed: a parallel one-way edge from a1 to a2.
            seed = writer.AddEdge(a1, a2, attributes: new[] { ("oneway", "yes") });
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.False(IsEdgeOnIsland_Cold(routerDb, profile, seed),
            "cold ResolveEdgeAsync on a one-way seed should classify it not-island when the " +
            "wider bidirectional network spans into another tile and only collectively exceeds " +
            "MaxIslandSize — publish-api#61 production shape at unit-test scale");
    }

    [Fact]
    public void IslandBuilder_Cold_OneWayDeadEnd_ShouldBeIsland()
    {
        // Cold variant of IslandBuilder_OneWayDeadEnd_ShouldBeIsland.
        // A genuine one-way trap: the head end has no return path. Cold ResolveEdgeAsync
        // must still classify this as island (so the fix for the bridge case doesn't
        // overgeneralise into "all one-way edges are not-island").
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
            var v4 = writer.AddVertex(4.798, 51.268);
            edges.Add(writer.AddEdge(v3, v4, attributes: new[] { ("oneway", "yes") }));
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.False(IsEdgeOnIsland_Cold(routerDb, profile, edges[0]), "cold bidirectional edge 0 should not be island");
        Assert.False(IsEdgeOnIsland_Cold(routerDb, profile, edges[1]), "cold bidirectional edge 1 should not be island");
        Assert.True(IsEdgeOnIsland_Cold(routerDb, profile, edges[2]),
            "cold one-way dead end should be island");
    }
}
