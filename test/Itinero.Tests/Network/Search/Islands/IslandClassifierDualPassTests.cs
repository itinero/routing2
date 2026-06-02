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
/// Tests for the kind-aware <see cref="IslandClassifier.ClassifyAsync"/> and
/// the dual-pass <see cref="IslandClassifier.BuildForTileAsync"/> coordinator.
/// Covers NonLocal classification, the pre-population trick, and the
/// Locals = NonLocal-Island ∩ Full-NotIsland ∩ non-L gap computation.
/// </summary>
public class IslandClassifierDualPassTests
{
    private static Profile LocalAccessProfile()
        => new DefaultProfile(getEdgeFactor: a =>
            a.Any(x => x.key == "access" && x.value == "destination")
                ? new EdgeFactor(1, 1, 1, 1, isLocalAccess: true)
                : new EdgeFactor(1, 1, 1, 1));

    [Fact]
    public async Task Classify_NonLocal_LTaggedSeed_ReturnsUnknown()
    {
        // NonLocalCostFunction masks L-edges. The seed's GetIslandBuilderCost
        // returns false in both directions → classifier returns Unknown
        // without writing any state.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId lEdge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.2500, 51.000);
            var v2 = writer.AddVertex(4.2501, 51.000);
            lEdge = writer.AddEdge(v1, v2, attributes: new[] { ("access", "destination") });
        }

        var profile = LocalAccessProfile();
        var result = await IslandClassifier.ClassifyAsync(
            routerDb.Latest, profile, lEdge, CancellationToken.None, IslandKind.NonLocal);

        Assert.Equal(IslandStatus.Unknown, result);
        var islands = routerDb.Latest.IslandManager.GetIslandsFor(profile);
        Assert.False(islands.IsEdgeOnIsland(lEdge, IslandKind.NonLocal));
        Assert.False(islands.IsEdgeOnIsland(lEdge, IslandKind.Full));
    }

    [Fact]
    public async Task Classify_NonLocal_IsolatedNonLChain_BehindLEdge_IsIsland()
    {
        // A 2-edge bidir chain reachable from a graduating mainland only via
        // an L-edge. With L masked in NonLocal mode the chain is isolated
        // and below MaxIslandSize → NonLocal-Island.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        var pocketEdges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.2500, 51.000);
            var v2 = writer.AddVertex(4.2501, 51.000);
            var v3 = writer.AddVertex(4.2502, 51.000);
            var v4 = writer.AddVertex(4.2503, 51.000);
            var v5 = writer.AddVertex(4.2504, 51.000);
            var v6 = writer.AddVertex(4.2505, 51.000);
            var v7 = writer.AddVertex(4.2506, 51.000);
            var v8 = writer.AddVertex(4.2507, 51.000);
            // Mainland chain of 4 bidir edges → graduates with MaxIslandSize=4.
            writer.AddEdge(v1, v2);
            writer.AddEdge(v2, v3);
            writer.AddEdge(v3, v4);
            writer.AddEdge(v4, v5);
            // L-edge to pocket.
            writer.AddEdge(v5, v6, attributes: new[] { ("access", "destination") });
            // Pocket triangle (cycle) — bidir, 3 edges.
            pocketEdges.Add(writer.AddEdge(v6, v7));
            pocketEdges.Add(writer.AddEdge(v7, v8));
            pocketEdges.Add(writer.AddEdge(v8, v6));
        }

        var profile = LocalAccessProfile();
        var network = routerDb.Latest;

        foreach (var e in pocketEdges)
        {
            var status = await IslandClassifier.ClassifyAsync(
                network, profile, e, CancellationToken.None, IslandKind.NonLocal);
            Assert.Equal(IslandStatus.Island, status);
        }

        var islands = network.IslandManager.GetIslandsFor(profile);
        foreach (var e in pocketEdges)
        {
            Assert.True(islands.IsEdgeOnIsland(e, IslandKind.NonLocal));
            Assert.False(islands.IsEdgeOnIsland(e, IslandKind.Full));
        }
    }

    [Fact]
    public async Task Classify_Full_PrePopulatedFromNonLocalSentinel_ShortCircuits()
    {
        // Same chain as TwoEdgesBidirectional, but the NonLocal DG is pre-
        // seeded with the seed in MainNet. Full classification must immediately
        // return NotIsland via the pre-population trick — confirmed by the
        // result being NotIsland even at MaxIslandSize=10 where the chain alone
        // would normally classify as Island.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 10 });
        EdgeId seed;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.2500, 51.000);
            var v2 = writer.AddVertex(4.2501, 51.000);
            seed = writer.AddEdge(v1, v2);
        }

        var profile = LocalAccessProfile();
        var network = routerDb.Latest;

        // Sanity: cold Full classification on a 1-edge chain would normally
        // be Island (size 1 < MaxIslandSize=10, no graduation).
        // Pre-seed NonLocal DG: pretend NonLocal classification already placed
        // seed in main-N (the sentinel).
        var nonLocalDg = network.IslandManager.GetOrCreateDirectedGraph(profile, IslandKind.NonLocal);
        nonLocalDg.AddVertex(seed);
        nonLocalDg.CollapseToMainNetwork(seed);

        var result = await IslandClassifier.ClassifyAsync(
            network, profile, seed, CancellationToken.None, IslandKind.Full);
        Assert.Equal(IslandStatus.NotIsland, result);

        // The Full DG should also have seed collapsed into its own sentinel.
        var fullDg = network.IslandManager.GetOrCreateDirectedGraph(profile, IslandKind.Full);
        Assert.True(fullDg.IsNotIsland(seed));
    }

    [Fact]
    public async Task BuildForTile_PocketBehindLEdge_PocketEdgesMarkedLocal()
    {
        // End-to-end: 4-edge mainland chain (size 4, graduates with
        // MaxIslandSize=4) connected to a 3-edge pocket triangle via a
        // single L-tagged edge. After both passes:
        //  - mainland edges: Full-NotIsland, NonLocal-NotIsland, not Local.
        //  - L-edge itself: not Local (excluded by L-tag).
        //  - pocket edges: Full-NotIsland, NonLocal-Island → marked Local.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        var mainlandEdges = new List<EdgeId>();
        var pocketEdges = new List<EdgeId>();
        EdgeId lEdge;
        uint tileId;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.2500, 51.000);
            var v2 = writer.AddVertex(4.2501, 51.000);
            var v3 = writer.AddVertex(4.2502, 51.000);
            var v4 = writer.AddVertex(4.2503, 51.000);
            var v5 = writer.AddVertex(4.2504, 51.000);
            var v6 = writer.AddVertex(4.2505, 51.000);
            var v7 = writer.AddVertex(4.2506, 51.000);
            var v8 = writer.AddVertex(4.2507, 51.000);
            mainlandEdges.Add(writer.AddEdge(v1, v2));
            mainlandEdges.Add(writer.AddEdge(v2, v3));
            mainlandEdges.Add(writer.AddEdge(v3, v4));
            mainlandEdges.Add(writer.AddEdge(v4, v5));
            lEdge = writer.AddEdge(v5, v6, attributes: new[] { ("access", "destination") });
            pocketEdges.Add(writer.AddEdge(v6, v7));
            pocketEdges.Add(writer.AddEdge(v7, v8));
            pocketEdges.Add(writer.AddEdge(v8, v6));
            tileId = lEdge.TileId;
        }

        // Sanity: scenario relies on all edges being in one tile.
        foreach (var e in mainlandEdges) Assert.Equal(tileId, e.TileId);
        foreach (var e in pocketEdges) Assert.Equal(tileId, e.TileId);

        var profile = LocalAccessProfile();
        await IslandClassifier.BuildForTileAsync(
            routerDb.Latest, profile, tileId, CancellationToken.None);

        var islands = routerDb.Latest.IslandManager.GetIslandsFor(profile);
        Assert.True(islands.GetTileDone(tileId), "tile should be marked done");

        foreach (var e in mainlandEdges)
        {
            Assert.False(islands.IsEdgeLocal(e), $"mainland edge {e} should not be Local");
            Assert.False(islands.IsEdgeOnIsland(e), $"mainland edge {e} should not be Full-Island");
        }

        Assert.False(islands.IsEdgeLocal(lEdge), "L-edge itself should not be marked Local");
        Assert.False(islands.IsEdgeOnIsland(lEdge), "L-edge should not be Full-Island (joins mainland)");

        foreach (var e in pocketEdges)
        {
            Assert.True(islands.IsEdgeLocal(e), $"pocket edge {e} should be marked Local");
            Assert.False(islands.IsEdgeOnIsland(e), $"pocket edge {e} should not be Full-Island");
        }
    }

    [Fact]
    public async Task BuildForTile_ClearsTransientNonLocalIslandsAfterBuild()
    {
        // After BuildForTileAsync completes, the transient _nonLocalIslandEdges
        // set must be empty — its only contents (pocket edges flagged during
        // the NonLocal pass) have been folded into _localEdges.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        var pocketEdges = new List<EdgeId>();
        uint tileId;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.2500, 51.000);
            var v2 = writer.AddVertex(4.2501, 51.000);
            var v3 = writer.AddVertex(4.2502, 51.000);
            var v4 = writer.AddVertex(4.2503, 51.000);
            var v5 = writer.AddVertex(4.2504, 51.000);
            var v6 = writer.AddVertex(4.2505, 51.000);
            var v7 = writer.AddVertex(4.2506, 51.000);
            var v8 = writer.AddVertex(4.2507, 51.000);
            writer.AddEdge(v1, v2);
            writer.AddEdge(v2, v3);
            writer.AddEdge(v3, v4);
            var lastMainland = writer.AddEdge(v4, v5);
            writer.AddEdge(v5, v6, attributes: new[] { ("access", "destination") });
            pocketEdges.Add(writer.AddEdge(v6, v7));
            pocketEdges.Add(writer.AddEdge(v7, v8));
            pocketEdges.Add(writer.AddEdge(v8, v6));
            tileId = lastMainland.TileId;
        }

        var profile = LocalAccessProfile();
        await IslandClassifier.BuildForTileAsync(
            routerDb.Latest, profile, tileId, CancellationToken.None);

        var islands = routerDb.Latest.IslandManager.GetIslandsFor(profile);
        foreach (var e in pocketEdges)
        {
            Assert.False(islands.IsEdgeOnIsland(e, IslandKind.NonLocal),
                $"transient NonLocal-island flag should be cleared for {e}");
            Assert.True(islands.IsEdgeLocal(e));
        }
    }

    [Fact]
    public async Task BuildForTile_NoLEdges_NoLocalsCreated()
    {
        // Sanity: tile with no L-tagged edges has nothing to classify as
        // Local. Mainland edges graduate via both passes; no Locals computed.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        var edges = new List<EdgeId>();
        uint tileId;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.2500, 51.000);
            var v2 = writer.AddVertex(4.2501, 51.000);
            var v3 = writer.AddVertex(4.2502, 51.000);
            var v4 = writer.AddVertex(4.2503, 51.000);
            var v5 = writer.AddVertex(4.2504, 51.000);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
            edges.Add(writer.AddEdge(v3, v4));
            edges.Add(writer.AddEdge(v4, v5));
            tileId = edges[0].TileId;
        }

        var profile = LocalAccessProfile();
        await IslandClassifier.BuildForTileAsync(
            routerDb.Latest, profile, tileId, CancellationToken.None);

        var islands = routerDb.Latest.IslandManager.GetIslandsFor(profile);
        Assert.True(islands.GetTileDone(tileId));
        foreach (var e in edges)
        {
            Assert.False(islands.IsEdgeLocal(e));
            Assert.False(islands.IsEdgeOnIsland(e));
        }
    }
}
