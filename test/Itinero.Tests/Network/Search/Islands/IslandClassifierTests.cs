using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

/// <summary>
/// Tests for the new pure-graph <see cref="IslandClassifier"/>. Each test seeds
/// a small in-memory network and runs <see cref="IslandClassifier.ClassifyAsync"/>
/// cold (fresh RouterDb means fresh IslandManager → fresh shared dg + Islands),
/// then asserts the seed's classification. These mirror the cold tests in
/// <see cref="IslandBuilderTests"/> but exercise the new API directly so the
/// legacy <see cref="IslandBuilder"/> code path is not involved.
/// </summary>
public class IslandClassifierTests
{
    private static async Task<IslandStatus> ClassifyCold(RouterDb routerDb, Profile profile, EdgeId edgeId)
    {
        // Each test creates its own RouterDb so its IslandManager (and the
        // shared per-profile Islands + IslandDirectedGraph it owns) starts
        // empty — that's how "cold" is achieved now that the classifier
        // reuses state from previous calls inside a single network.
        return await IslandClassifier.ClassifyAsync(
            routerDb.Latest, profile, edgeId, CancellationToken.None);
    }

    [Fact]
    public async Task SingleEdge_ShouldBeIsland()
    {
        // Component size 1 < MaxIslandSize=2. The lone edge can't graduate.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(v1, v2);
        }

        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, new DefaultProfile(), edge));
    }

    [Fact]
    public async Task TwoEdges_MaxSizeThree_ShouldBeIsland()
    {
        // Component size 2 < MaxIslandSize=3 → both edges island.
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

        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, new DefaultProfile(), edges[0]));
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, new DefaultProfile(), edges[1]));
    }

    [Fact]
    public async Task TwoEdgesOneWay_ShouldBeIsland()
    {
        // Two one-way edges in a chain. They never bidirectionally merge
        // (canComeFrom fails on each transition), so they stay singletons.
        // Neither cycle nor main-network reachability rescues them → island.
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

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[0]));
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[1]));
    }

    [Fact]
    public async Task OneWayLeadingIntoCulDeSac_ShouldBothBeIsland()
    {
        // Bidirectional main → one-way → bidirectional cul-de-sac. Under
        // symmetric reachability the one-way edge AND the cul-de-sac are
        // traps. MaxIslandSize=2 so the main side (size 2) graduates; the
        // one-way and cul-de-sac do not.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            var v4 = writer.AddVertex(4.793, 51.264);
            var v5 = writer.AddVertex(4.798, 51.268);
            edges.Add(writer.AddEdge(v4, v1));
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3, attributes: new[] { ("oneway", "yes") }));
            edges.Add(writer.AddEdge(v3, v5));
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, edges[0]));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, edges[1]));
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[2]));
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[3]));
    }

    [Fact]
    public async Task TwoEdgesWithBarrier_ShouldBothBeIsland()
    {
        // Binary turn restriction at v2 between the two edges forbids
        // transit. For routing, each edge is its own connected component
        // → both island.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var v3 = writer.AddVertex(4.797506332397461, 51.26874845584085);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));

            writer.AddTurnCosts(v2,
                attributes: new[] { ("barrier", "bollard") },
                edges: edges.ToArray(),
                costs: new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var profile = new DefaultProfile(getTurnCostFactor: a =>
            a.Any(x => x.key == "barrier") ? TurnCostFactor.Binary : TurnCostFactor.Empty);

        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[0]));
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[1]));
    }

    [Fact]
    public async Task OneWayEdgeWithBarrier_ShouldBothBeIsland()
    {
        // Like TwoEdgesWithBarrier but edge 0 is one-way (v1→v2). The
        // barrier at v2 must still isolate both. Exercises directional
        // handling combined with turn restrictions.
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

        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[0]));
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[1]));
    }

    [Fact]
    public async Task TwoEdgesBidirectional_MaxSizeTwo_ShouldNotBeIsland()
    {
        // Simplest "not island": two bidirectional edges, component size 2.
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

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, new DefaultProfile(), edges[0]));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, new DefaultProfile(), edges[1]));
    }

    [Fact]
    public async Task DisconnectedComponent_ShouldBeIsland()
    {
        // Two disconnected groups, MaxIslandSize=3. The small group is an island.
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

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, new DefaultProfile(), edges[0]));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, new DefaultProfile(), edges[1]));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, new DefaultProfile(), edges[2]));
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, new DefaultProfile(), edges[3]));
    }

    [Fact]
    public async Task OneWayBetweenTwoMainNetworks_ShouldNotBeIsland()
    {
        // publish-api#61 minimal reproducer: a one-way edge bridges two
        // bidirectional main networks. Each end has its own bidir component
        // that graduates past MaxIslandSize and the one-way must inherit
        // main-network status via CanReachMainNetwork.
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

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, edges[0]));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, edges[1]));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, edges[2]));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, edges[3]));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, edges[4]));
    }

    [Fact]
    public async Task OneWayLoopSpansTwoTiles_ShouldNotBeIsland()
    {
        // A directed 4-edge cycle a1→a2→b1→b2→a1 spanning two tiles.
        // With MaxIslandSize=4 the cycle, when detected as an SCC, merges into
        // one component of size 4 → not island. Requires the classifier to
        // process both tiles before deciding.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        EdgeId seed;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var a1 = writer.AddVertex(4.255, 51.000);
            var a2 = writer.AddVertex(4.260, 51.000);
            var b1 = writer.AddVertex(4.270, 51.000);
            var b2 = writer.AddVertex(4.275, 51.000);

            Assert.NotEqual(a1.TileId, b1.TileId);

            var oneway = new[] { ("oneway", "yes") };
            seed = writer.AddEdge(a1, a2, attributes: oneway);
            writer.AddEdge(a2, b1, attributes: oneway);
            writer.AddEdge(b1, b2, attributes: oneway);
            writer.AddEdge(b2, a1, attributes: oneway);
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, seed));
    }

    [Fact]
    public async Task OneWayInsideTileA_BidirNetworkSpansTileAandB_ShouldNotBeIsland()
    {
        // publish-api#61 production shape at unit-test scale. A bidirectional
        // chain spans two tiles; total size 9 vs MaxIslandSize=8. A one-way
        // seed inside tile A only reaches main-network status once both tiles
        // contribute to the combined bidir component.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 8 });
        EdgeId seed;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var a1 = writer.AddVertex(4.250, 51.000);
            var a2 = writer.AddVertex(4.253, 51.000);
            var a3 = writer.AddVertex(4.256, 51.000);
            var a4 = writer.AddVertex(4.259, 51.000);
            var a5 = writer.AddVertex(4.261, 51.000);
            var b1 = writer.AddVertex(4.265, 51.000);
            var b2 = writer.AddVertex(4.270, 51.000);
            var b3 = writer.AddVertex(4.275, 51.000);
            var b4 = writer.AddVertex(4.280, 51.000);
            var b5 = writer.AddVertex(4.282, 51.000);

            Assert.NotEqual(a5.TileId, b1.TileId);

            writer.AddEdge(a1, a2);
            writer.AddEdge(a2, a3);
            writer.AddEdge(a3, a4);
            writer.AddEdge(a4, a5);
            writer.AddEdge(a5, b1);
            writer.AddEdge(b1, b2);
            writer.AddEdge(b2, b3);
            writer.AddEdge(b3, b4);
            writer.AddEdge(b4, b5);
            seed = writer.AddEdge(a1, a2, attributes: new[] { ("oneway", "yes") });
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, seed));
    }

    [Fact]
    public async Task OneWayDeadEnd_ShouldBeIsland()
    {
        // A genuine one-way trap: the head end has no return path. The fix for
        // OneWayBetweenTwoMainNetworks must NOT generalise to "all one-way
        // edges are not-island".
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

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, edges[0]));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, edges[1]));
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[2]));
    }

    // ────────────────────────────────────────────────────────────────────
    // Shared-state tests. Verify the contract:
    //   - The IslandManager on a RoutingNetwork holds a single shared dg +
    //     Islands per profile. All ClassifyAsync calls on that network share
    //     and accumulate this state.
    //   - A fresh RouterDb has a fresh IslandManager (no sharing across them).
    //   - Subsequent calls on a previously-classified edge short-circuit on
    //     the cached state.
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SharedDg_SameSeedTwice_BothReturnSameAndCacheIsHit()
    {
        // Classify the same edge twice on the same network. Verify:
        //  - both calls return NotIsland.
        //  - between calls, the shared dg already has the edge in MainNet
        //    (so the second call hits the cached NotIsland fast-path).
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            edge = writer.AddEdge(v1, v2);
            writer.AddEdge(v2, v3);
        }
        var profile = new DefaultProfile();
        var network = routerDb.Latest;

        var first = await IslandClassifier.ClassifyAsync(network, profile, edge, CancellationToken.None);
        Assert.Equal(IslandStatus.NotIsland, first);

        // The shared dg should now know this edge is on the main network.
        var dg = network.IslandManager.GetOrCreateDirectedGraph(profile);
        Assert.True(dg.IsNotIsland(edge),
            "after first ClassifyAsync, the shared dg must mark the edge as on the main network");

        var second = await IslandClassifier.ClassifyAsync(network, profile, edge, CancellationToken.None);
        Assert.Equal(IslandStatus.NotIsland, second);
    }

    [Fact]
    public async Task SharedDg_ClassifyingOneEdgeMarksItsMergedPartners()
    {
        // A bidir chain v1-v2-v3-v4. Classifying edges[0] graduates the
        // (edges[0], edges[1]) bidir merge to MainNet immediately. The BFS
        // exits as soon as seed is NotIsland — edges[2] is NOT visited yet.
        //
        // What the shared dg DOES guarantee: edges[0] AND edges[1] are now
        // in MainNet without edges[1] having been explicitly classified —
        // because they merged into the seed's component during ProcessEdge.
        //
        // What it does NOT guarantee: every edge in the same physical bidir
        // cluster is automatically classified. That happens incrementally
        // as more edges get explicitly classified — but each subsequent
        // classify can short-circuit on partial state.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
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
        }
        var profile = new DefaultProfile();
        var network = routerDb.Latest;
        var dg = network.IslandManager.GetOrCreateDirectedGraph(profile);

        // Before any classification: none of the edges are in dg.
        Assert.False(dg.IsNotIsland(edges[0]));
        Assert.False(dg.IsNotIsland(edges[1]));
        Assert.False(dg.IsNotIsland(edges[2]));

        // Classify edges[0]. The (edges[0], edges[1]) merge reaches MaxIslandSize=2
        // and graduates; the BFS exits early.
        var result = await IslandClassifier.ClassifyAsync(network, profile, edges[0], CancellationToken.None);
        Assert.Equal(IslandStatus.NotIsland, result);

        // edges[0] and its merged partner edges[1] are both in MainNet in the
        // SHARED dg — even though only edges[0] was explicitly classified.
        Assert.True(dg.IsNotIsland(edges[0]), "seed in MainNet after classify");
        Assert.True(dg.IsNotIsland(edges[1]), "seed's merged bidir partner also in MainNet");

        // edges[2] hasn't been visited at all — not in dg yet.
        Assert.False(dg.IsNotIsland(edges[2]),
            "edges[2] was never visited so it's not in MainNet — shared state only covers what the BFS touched");

        // Classifying edges[2] now should immediately discover edges[1] (which
        // it shares vertex v3 with) is already in MainNet → merge into MainNet
        // on the very first ProcessEdge call.
        var second = await IslandClassifier.ClassifyAsync(network, profile, edges[2], CancellationToken.None);
        Assert.Equal(IslandStatus.NotIsland, second);
        Assert.True(dg.IsNotIsland(edges[2]), "edges[2] now also in MainNet");
    }

    [Fact]
    public async Task SharedDg_IslandClassificationPersists()
    {
        // True one-way dead end → island. After classifying it, the shared
        // Islands set should contain it; subsequent calls re-use the answer.
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
        var network = routerDb.Latest;
        var islands = network.IslandManager.GetIslandsFor(profile);

        Assert.False(islands.IsEdgeOnIsland(edges[2]));

        var first = await IslandClassifier.ClassifyAsync(network, profile, edges[2], CancellationToken.None);
        Assert.Equal(IslandStatus.Island, first);

        Assert.True(islands.IsEdgeOnIsland(edges[2]),
            "after first ClassifyAsync, the shared Islands must mark the seed as island");

        var second = await IslandClassifier.ClassifyAsync(network, profile, edges[2], CancellationToken.None);
        Assert.Equal(IslandStatus.Island, second);
    }

    [Fact]
    public async Task IsolatedRouterDbs_DoNotShareIslandState()
    {
        // Two RouterDbs built identically have INDEPENDENT IslandManagers.
        // Classifying on one must not affect the other's shared dg / Islands.
        var (rdb1, e1) = BuildSimpleBidirPair();
        var (rdb2, e2) = BuildSimpleBidirPair();

        var profile = new DefaultProfile();

        var result1 = await IslandClassifier.ClassifyAsync(rdb1.Latest, profile, e1, CancellationToken.None);
        Assert.Equal(IslandStatus.NotIsland, result1);

        var dg1 = rdb1.Latest.IslandManager.GetOrCreateDirectedGraph(profile);
        var dg2 = rdb2.Latest.IslandManager.GetOrCreateDirectedGraph(profile);
        Assert.True(dg1.IsNotIsland(e1), "rdb1's classification was persisted");
        Assert.False(dg2.IsNotIsland(e2),
            "rdb2's dg must be unaffected by classifications on rdb1 — separate RouterDbs are isolated");

        // Sanity: rdb2 can still classify independently.
        var result2 = await IslandClassifier.ClassifyAsync(rdb2.Latest, profile, e2, CancellationToken.None);
        Assert.Equal(IslandStatus.NotIsland, result2);
    }

    private static (RouterDb routerDb, EdgeId firstEdge) BuildSimpleBidirPair()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId first;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            first = writer.AddEdge(v1, v2);
            writer.AddEdge(v2, v3);
        }
        return (routerDb, first);
    }

    // ────────────────────────────────────────────────────────────────────
    // Real-world OSM-shaped topologies. These exercise the full algorithm
    // (oracle + dg eager cycle-merge + per-side termination) on shapes that
    // mirror what we encounter in production data.
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DividedMotorway_BothCarriageways_NotIsland()
    {
        // Divided motorway shape: two opposite one-ways m1→m2 and m2→m1
        // (eastbound/westbound carriageways), connected at each end to bidir
        // main networks via bidir on/off ramps. The cycle through both
        // carriageways and the ramps is what gives each one-way edge its
        // bidirectional reach to MainNet.
        //
        //   a1↔a2↔a3↔m1 ==(east oneway m1→m2)==> m2 ↔b1↔b2↔b3
        //                <==(west oneway m2→m1)==
        //
        // MaxIslandSize=8. The full cycle component (4 bidir A + 2 oneways
        // + 4 bidir B = 10) graduates via size-threshold once SCC merges
        // bring everything together.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 8 });
        EdgeId eastbound;
        EdgeId westbound;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var a1 = writer.AddVertex(4.250, 51.000);
            var a2 = writer.AddVertex(4.252, 51.000);
            var a3 = writer.AddVertex(4.254, 51.000);
            var m1 = writer.AddVertex(4.256, 51.000);
            var m2 = writer.AddVertex(4.270, 51.000);
            var b1 = writer.AddVertex(4.272, 51.000);
            var b2 = writer.AddVertex(4.274, 51.000);
            var b3 = writer.AddVertex(4.276, 51.000);

            writer.AddEdge(a1, a2);
            writer.AddEdge(a2, a3);
            writer.AddEdge(a3, m1);
            eastbound = writer.AddEdge(m1, m2, attributes: new[] { ("oneway", "yes") });
            westbound = writer.AddEdge(m2, m1, attributes: new[] { ("oneway", "yes") });
            writer.AddEdge(m2, b1);
            writer.AddEdge(b1, b2);
            writer.AddEdge(b2, b3);
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, eastbound));
        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, westbound));
    }

    [Fact]
    public async Task Roundabout_OneWayCycleWithBidirSpoke_NotIsland()
    {
        // Roundabout shape: 4-edge directed cycle (a→b→c→d→a) with one
        // bidir spoke connecting to a bidir main network. MaxIslandSize=8.
        // The roundabout alone is size 4 — below threshold. The spoke +
        // its attached main network must merge in for the merged
        // component to graduate via size-threshold.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 8 });
        EdgeId roundaboutEdge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var a = writer.AddVertex(4.250, 51.000);
            var b = writer.AddVertex(4.252, 51.000);
            var c = writer.AddVertex(4.254, 51.001);
            var d = writer.AddVertex(4.252, 51.002);
            // Bidir main network attached at vertex a via a 4-edge chain.
            var ext1 = writer.AddVertex(4.245, 51.000);
            var ext2 = writer.AddVertex(4.243, 51.000);
            var ext3 = writer.AddVertex(4.241, 51.000);
            var ext4 = writer.AddVertex(4.239, 51.000);

            var oneway = new[] { ("oneway", "yes") };
            roundaboutEdge = writer.AddEdge(a, b, attributes: oneway);
            writer.AddEdge(b, c, attributes: oneway);
            writer.AddEdge(c, d, attributes: oneway);
            writer.AddEdge(d, a, attributes: oneway);

            writer.AddEdge(a, ext1);
            writer.AddEdge(ext1, ext2);
            writer.AddEdge(ext2, ext3);
            writer.AddEdge(ext3, ext4);
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, roundaboutEdge));
    }

    [Fact]
    public async Task BidirSeedConnectedByOneWayOut_ShouldBeIsland()
    {
        // Mirror of OneWayLeadingIntoCulDeSac's cul-de-sac case. A bidir
        // seed v_h↔v_t with v_h dead-end. v_t has a single one-way going
        // OUT (v_t→v_x) into a bidir main network. From the seed you can
        // route OUT to MainNet, but MainNet cannot route back IN — the
        // one-way is OUT only. So Island.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId seed;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v_h = writer.AddVertex(4.250, 51.000); // dead-end
            var v_t = writer.AddVertex(4.252, 51.000);
            var v_x = writer.AddVertex(4.254, 51.000);
            var v_y = writer.AddVertex(4.256, 51.000);
            var v_z = writer.AddVertex(4.258, 51.000);

            seed = writer.AddEdge(v_h, v_t);
            writer.AddEdge(v_t, v_x, attributes: new[] { ("oneway", "yes") });
            writer.AddEdge(v_x, v_y);
            writer.AddEdge(v_y, v_z);
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, seed));
    }

    [Fact]
    public async Task OneWayBridgeBetweenTwoLargeMainNets_NotIsland()
    {
        // publish-api#61 production shape: two distinct bidir main networks,
        // each large enough to graduate on its own, connected by a single
        // one-way bridge. The bridge must classify NotIsland — its incoming
        // chain reaches MainNet A and its outgoing chain reaches MainNet B,
        // and the cycle through bridge + bridging-edges closes.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        EdgeId bridge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            // Main A: 5 bidir edges (≥ 4 = MaxIslandSize)
            var a = new VertexId[6];
            for (var i = 0; i < a.Length; i++) a[i] = writer.AddVertex(4.250 + i * 0.001, 51.000);
            for (var i = 0; i < a.Length - 1; i++) writer.AddEdge(a[i], a[i + 1]);

            // Bridge: oneway A's last → B's first
            var bStart = writer.AddVertex(4.270, 51.000);
            bridge = writer.AddEdge(a[^1], bStart, attributes: new[] { ("oneway", "yes") });

            // Main B: 5 bidir edges chained from bStart
            var prev = bStart;
            for (var i = 1; i < 6; i++)
            {
                var v = writer.AddVertex(4.270 + i * 0.001, 51.000);
                writer.AddEdge(prev, v);
                prev = v;
            }
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.Equal(IslandStatus.NotIsland, await ClassifyCold(routerDb, profile, bridge));
    }

    [Fact]
    public async Task OneWayTrapAcrossTiles_TailDeadEnd_ShouldBeIsland()
    {
        // One-way trap shape spanning two tiles: seed = oneway a1→a2 in
        // tile A. Tail at a1 is a dead-end. Head side leads into tile B
        // via a bidir chain to a small main net in tile B. Per-side
        // fast-path should fire Island (tail empty + no backward reach)
        // without draining the cross-tile head closure.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        EdgeId seed;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var a1 = writer.AddVertex(4.255, 51.000);
            var a2 = writer.AddVertex(4.260, 51.000);
            var b1 = writer.AddVertex(4.270, 51.000);
            var b2 = writer.AddVertex(4.275, 51.000);
            var b3 = writer.AddVertex(4.280, 51.000);
            var b4 = writer.AddVertex(4.285, 51.000);

            Assert.NotEqual(a1.TileId, b1.TileId);

            seed = writer.AddEdge(a1, a2, attributes: new[] { ("oneway", "yes") });
            writer.AddEdge(a2, b1);
            writer.AddEdge(b1, b2);
            writer.AddEdge(b2, b3);
            writer.AddEdge(b3, b4);
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, seed));
    }

    // ────────────────────────────────────────────────────────────────────
    // Oracle tests. These verify that the classifier respects pre-populated
    // per-tile state: the Islands set (known-island edges) and the
    // tile-done flag (a tile in which every traversable edge that is not
    // in the Islands set is by definition NotIsland).
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Oracle_KnownIslandNeighbour_DoesNotExpandThrough()
    {
        // 3-edge bidir chain v1↔v2↔v3↔v4 with MaxIslandSize=4. The full
        // chain — if explored — would NOT graduate (size 3 < 4) but at
        // least it would be one merged component. Pre-mark edges[1] as
        // Island. When classifying edges[0], the algorithm sees edges[1]
        // via the oracle and does NOT expand through it: edges[2] is
        // never visited. Seed stays in its own component → Island.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.250, 51.000);
            var v2 = writer.AddVertex(4.252, 51.000);
            var v3 = writer.AddVertex(4.254, 51.000);
            var v4 = writer.AddVertex(4.256, 51.000);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
            edges.Add(writer.AddEdge(v3, v4));
        }

        var profile = new DefaultProfile();
        var network = routerDb.Latest;
        var islands = network.IslandManager.GetIslandsFor(profile);
        var dg = network.IslandManager.GetOrCreateDirectedGraph(profile);

        islands.SetEdgeOnIsland(edges[1]);

        var result = await IslandClassifier.ClassifyAsync(
            network, profile, edges[0], CancellationToken.None);
        Assert.Equal(IslandStatus.Island, result);

        // edges[2] was NEVER touched — it shouldn't be in the dg.
        Assert.False(dg.IsInGraph(edges[2]),
            "edges[2] should not be in the dg — the oracle's island short-circuit on edges[1] prevented expansion through it");
    }

    [Fact]
    public async Task Oracle_TileDone_NeighbourTreatedAsNotIsland()
    {
        // 2-edge bidir chain with MaxIslandSize=10. The chain alone is too
        // small to graduate (size 2 < 10), so without the oracle this would
        // classify as Island. Pre-mark the tile as done — the oracle now
        // treats the neighbour as NotIsland, links it into the sentinel,
        // and the seed's bidir cycle through it absorbs the seed too.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 10 });
        EdgeId seed;
        EdgeId neighbour;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.250, 51.000);
            var v2 = writer.AddVertex(4.252, 51.000);
            var v3 = writer.AddVertex(4.254, 51.000);
            seed = writer.AddEdge(v1, v2);
            neighbour = writer.AddEdge(v2, v3);
        }

        var profile = new DefaultProfile();
        var network = routerDb.Latest;
        var islands = network.IslandManager.GetIslandsFor(profile);

        // Sanity: without the oracle, the chain is Island.
        Assert.Equal(IslandStatus.Island,
            await IslandClassifier.ClassifyAsync(network, profile, seed, CancellationToken.None));

        // Now mark the neighbour's tile as done. Re-classify a fresh seed
        // edge (we make a second RouterDb so dg state isn't reused).
        var (rdb2, seed2, neighbour2) = BuildSimpleBidirChain(maxIslandSize: 10);
        var islands2 = rdb2.Latest.IslandManager.GetIslandsFor(profile);
        islands2.SetTileDone(neighbour2.TileId);

        var result = await IslandClassifier.ClassifyAsync(
            rdb2.Latest, profile, seed2, CancellationToken.None);
        Assert.Equal(IslandStatus.NotIsland, result);
    }

    [Fact]
    public async Task Oracle_TileDone_IslandEdgeStaysIsland()
    {
        // Even when a tile is marked done, the Islands set takes
        // precedence: an edge that's in the Islands set is treated as
        // Island regardless of tile-done state. This is the layering
        // contract — the Islands set is "the islands inside a done tile",
        // and the tile-done flag means "everything else here is NotIsland".
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.250, 51.000);
            var v2 = writer.AddVertex(4.252, 51.000);
            edge = writer.AddEdge(v1, v2);
        }

        var profile = new DefaultProfile();
        var network = routerDb.Latest;
        var islands = network.IslandManager.GetIslandsFor(profile);

        islands.SetTileDone(edge.TileId);
        islands.SetEdgeOnIsland(edge);

        var result = await IslandClassifier.ClassifyAsync(
            network, profile, edge, CancellationToken.None);
        Assert.Equal(IslandStatus.Island, result);
    }

    private static (RouterDb routerDb, EdgeId seed, EdgeId neighbour) BuildSimpleBidirChain(int maxIslandSize)
    {
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = maxIslandSize });
        EdgeId seed;
        EdgeId neighbour;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.250, 51.000);
            var v2 = writer.AddVertex(4.252, 51.000);
            var v3 = writer.AddVertex(4.254, 51.000);
            seed = writer.AddEdge(v1, v2);
            neighbour = writer.AddEdge(v2, v3);
        }
        return (routerDb, seed, neighbour);
    }

    [Fact]
    public async Task LongOneWayChainNoCycle_ShouldBeIsland()
    {
        // 10-edge one-way chain v0→v1→…→v10 with MaxIslandSize=8 and NO
        // bidirectional reach to MainNet. Pins two things at once:
        // (1) size alone does not graduate a non-bidir chain — without a
        //     closing cycle through the sentinel, the chain stays in its
        //     own non-sentinel component however long it gets.
        // (2) The one-way per-side fast-path kicks in: tail queue empty
        //     with no backward reach in dg → Island, without having to
        //     drain the long forward chain on the head side.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 8 });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var verts = new VertexId[11];
            for (var i = 0; i < verts.Length; i++)
                verts[i] = writer.AddVertex(4.250 + i * 0.001, 51.000);
            var oneway = new[] { ("oneway", "yes") };
            for (var i = 0; i < verts.Length - 1; i++)
                edges.Add(writer.AddEdge(verts[i], verts[i + 1], attributes: oneway));
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        // The first edge has no backward reach (v0 is a dead-end). All
        // edges on this chain are Island.
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[0]));
        // The last edge has no forward reach (v10 is a dead-end). Also Island.
        Assert.Equal(IslandStatus.Island, await ClassifyCold(routerDb, profile, edges[^1]));
    }
}
