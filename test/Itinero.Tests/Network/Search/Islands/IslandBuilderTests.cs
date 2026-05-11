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
}
