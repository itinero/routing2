using Itinero.Network;
using Itinero.Network.Search.Islands;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

/// <summary>
/// Unit tests for <see cref="IslandDirectedGraph"/> focused on the eager
/// cycle-merge behaviour of <see cref="IslandDirectedGraph.AddDirectedLink"/>.
/// When a new directed link closes a cycle in the existing graph, every
/// component on the SCC must be merged into one node — no separate Tarjan
/// pass required.
/// </summary>
public class IslandDirectedGraphTests
{
    private static EdgeId Edge(uint local) => new(0u, local);

    [Fact]
    public void AddDirectedLink_NoCycle_ReturnsFalse_KeepsSeparateComponents()
    {
        var dg = new IslandDirectedGraph();
        var a = Edge(1);
        var b = Edge(2);
        dg.AddVertex(a);
        dg.AddVertex(b);

        var merged = dg.AddDirectedLink(a, b);

        Assert.False(merged);
        Assert.NotEqual(dg.Find(a), dg.Find(b));
    }

    [Fact]
    public void AddDirectedLink_TwoEdge_DirectBidir_MergesIntoOneComponent()
    {
        // a → b, then b → a closes a 2-cycle.
        var dg = new IslandDirectedGraph();
        var a = Edge(1);
        var b = Edge(2);
        dg.AddVertex(a);
        dg.AddVertex(b);

        Assert.False(dg.AddDirectedLink(a, b));
        var merged = dg.AddDirectedLink(b, a);

        Assert.True(merged);
        Assert.Equal(dg.Find(a), dg.Find(b));
        Assert.Equal(2, dg.GetSize(dg.Find(a)));
    }

    [Fact]
    public void AddDirectedLink_ThreeEdgeOneWayCycle_ClosingEdgeMergesAllThree()
    {
        // Pure one-way 3-cycle: a → b → c → a. None of the pairs are directly
        // bidirectional. Without eager SCC, this stays as three components.
        // With eager SCC on insert, closing the cycle (c → a) detects path
        // a ↝ b ↝ c and merges {a, b, c}.
        var dg = new IslandDirectedGraph();
        var a = Edge(1);
        var b = Edge(2);
        var c = Edge(3);
        dg.AddVertex(a);
        dg.AddVertex(b);
        dg.AddVertex(c);

        Assert.False(dg.AddDirectedLink(a, b));
        Assert.False(dg.AddDirectedLink(b, c));
        var merged = dg.AddDirectedLink(c, a);

        Assert.True(merged);
        Assert.Equal(dg.Find(a), dg.Find(b));
        Assert.Equal(dg.Find(b), dg.Find(c));
        Assert.Equal(3, dg.GetSize(dg.Find(a)));
    }

    [Fact]
    public void AddDirectedLink_CycleClosesAcrossLongChain_MergesEntirePath()
    {
        // Chain a → b → c → d → e → a. Closing edge e → a should merge {a..e}.
        var dg = new IslandDirectedGraph();
        var ids = new[] { Edge(1), Edge(2), Edge(3), Edge(4), Edge(5) };
        foreach (var id in ids) dg.AddVertex(id);

        for (var i = 0; i < ids.Length - 1; i++)
            Assert.False(dg.AddDirectedLink(ids[i], ids[i + 1]));

        var merged = dg.AddDirectedLink(ids[^1], ids[0]);

        Assert.True(merged);
        var root = dg.Find(ids[0]);
        for (var i = 1; i < ids.Length; i++)
            Assert.Equal(root, dg.Find(ids[i]));
        Assert.Equal(ids.Length, dg.GetSize(root));
    }

    [Fact]
    public void AddDirectedLink_DanglingSide_DoesNotMergeIntoCycle()
    {
        // Cycle a→b→c→a plus a dangling d that has only b → d (no return).
        // Adding c→a merges {a, b, c}. d should NOT merge in — there's no
        // path from d back to anything in the cycle.
        var dg = new IslandDirectedGraph();
        var a = Edge(1);
        var b = Edge(2);
        var c = Edge(3);
        var d = Edge(4);
        dg.AddVertex(a);
        dg.AddVertex(b);
        dg.AddVertex(c);
        dg.AddVertex(d);

        dg.AddDirectedLink(a, b);
        dg.AddDirectedLink(b, c);
        dg.AddDirectedLink(b, d);
        dg.AddDirectedLink(c, a);

        Assert.Equal(dg.Find(a), dg.Find(b));
        Assert.Equal(dg.Find(b), dg.Find(c));
        Assert.NotEqual(dg.Find(d), dg.Find(a));
    }

    [Fact]
    public void AddDirectedLink_LargeOneWayCycleAtMaxIslandSize_AllInOneComponent()
    {
        // 300-edge one-way cycle: closing edge causes a single merge of 300
        // components into one. The caller can then graduate to MainNet if
        // size ≥ MaxIslandSize. This pins the semantic shift away from the
        // legacy "global Tarjan in TryResolve" model.
        var dg = new IslandDirectedGraph();
        var ids = new EdgeId[300];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = Edge((uint)i + 1);
            dg.AddVertex(ids[i]);
        }

        for (var i = 0; i < ids.Length - 1; i++)
            Assert.False(dg.AddDirectedLink(ids[i], ids[i + 1]));

        var merged = dg.AddDirectedLink(ids[^1], ids[0]);

        Assert.True(merged);
        var root = dg.Find(ids[0]);
        for (var i = 1; i < ids.Length; i++)
            Assert.Equal(root, dg.Find(ids[i]));
        Assert.Equal(ids.Length, dg.GetSize(root));
    }
}
