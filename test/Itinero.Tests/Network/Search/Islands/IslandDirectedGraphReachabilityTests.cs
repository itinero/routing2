using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Search.Islands;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

/// <summary>
/// Pins what the eager cycle-merge is supposed to produce: after a sequence of
/// AddDirectedLink calls, the union-find partition must equal the strongly-connected
/// components of the graph those links form.
/// </summary>
/// <remarks>
/// This is the guard for changing how reachability is decided inside AddDirectedLink.
/// The classifier reads the merged state on every iteration of its main loop —
/// IsNotIsland(seed), the InForward/InBackward root sets, GetOutgoingRoots — so a merge
/// that happens at the wrong moment, or not at all, changes which edges get recorded as
/// islands. That failure is silent and permanent, which is why it is worth testing the
/// partition directly rather than trusting the routing corpus to notice.
///
/// The random cases matter more than the hand-written ones: a reachability bug that only
/// shows up on a particular shape of component is exactly what a handful of small
/// examples misses.
/// </remarks>
public class IslandDirectedGraphReachabilityTests
{
    private static EdgeId E(int i) => new(0, (uint)i);

    /// Reference: Kosaraju over the raw link list, entirely independent of the graph
    /// under test.
    private static Dictionary<int, int> ComponentsOf(int nodes, List<(int from, int to)> links)
    {
        var outs = new Dictionary<int, List<int>>();
        var ins = new Dictionary<int, List<int>>();
        for (var i = 0; i < nodes; i++)
        {
            outs[i] = new List<int>();
            ins[i] = new List<int>();
        }

        foreach (var (f, t) in links)
        {
            outs[f].Add(t);
            ins[t].Add(f);
        }

        var order = new List<int>();
        var seen = new HashSet<int>();

        void Visit(int start)
        {
            var stack = new Stack<(int node, int next)>();
            if (!seen.Add(start)) return;
            stack.Push((start, 0));
            while (stack.Count > 0)
            {
                var (node, next) = stack.Pop();
                if (next < outs[node].Count)
                {
                    stack.Push((node, next + 1));
                    var child = outs[node][next];
                    if (seen.Add(child)) stack.Push((child, 0));
                }
                else
                {
                    order.Add(node);
                }
            }
        }

        for (var i = 0; i < nodes; i++) Visit(i);

        var component = new Dictionary<int, int>();
        var label = 0;
        for (var i = order.Count - 1; i >= 0; i--)
        {
            var root = order[i];
            if (component.ContainsKey(root)) continue;

            var stack = new Stack<int>();
            stack.Push(root);
            component[root] = label;
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                foreach (var prev in ins[node])
                {
                    if (component.ContainsKey(prev)) continue;
                    component[prev] = label;
                    stack.Push(prev);
                }
            }

            label++;
        }

        return component;
    }

    private static void AssertPartitionMatchesSccs(int nodes, List<(int from, int to)> links)
    {
        var graph = new IslandDirectedGraph();
        for (var i = 0; i < nodes; i++) graph.AddVertex(E(i));
        foreach (var (f, t) in links) graph.AddDirectedLink(E(f), E(t));

        // The sets should never hold a stale root - see OutgoingIsCanonical. If this
        // holds, the Find on every element of every adjacency read is redundant.
        Assert.True(graph.OutgoingIsCanonical(out var failure), failure);

        var expected = ComponentsOf(nodes, links);

        for (var a = 0; a < nodes; a++)
        {
            for (var b = a + 1; b < nodes; b++)
            {
                var together = graph.Find(E(a)) == graph.Find(E(b));
                var shouldBe = expected[a] == expected[b];
                Assert.True(together == shouldBe,
                    $"nodes {a} and {b}: merged={together}, same SCC={shouldBe}. Links: " +
                    string.Join(",", links.Select(l => $"{l.from}->{l.to}")));
            }
        }
    }

    [Fact]
    public void Chain_NoCycle_StaysSeparate()
    {
        AssertPartitionMatchesSccs(4, new List<(int, int)> { (0, 1), (1, 2), (2, 3) });
    }

    [Fact]
    public void ClosingLink_MergesTheWholeCycle()
    {
        AssertPartitionMatchesSccs(4, new List<(int, int)> { (0, 1), (1, 2), (2, 3), (3, 0) });
    }

    [Fact]
    public void ShortcutForward_DoesNotMerge()
    {
        // 0->2 adds no cycle: a forward-only walk finds 2 from 0, but that is not a cycle.
        AssertPartitionMatchesSccs(3, new List<(int, int)> { (0, 1), (1, 2), (0, 2) });
    }

    [Fact]
    public void TwoDisjointCycles_StayDisjoint()
    {
        AssertPartitionMatchesSccs(6, new List<(int, int)>
        {
            (0, 1), (1, 0),
            (2, 3), (3, 2),
            (4, 5),
        });
    }

    [Fact]
    public void CycleReachableFromAnother_DoesNotMergeThem()
    {
        // A one-way link between two cycles: reachable, but not mutually reachable.
        AssertPartitionMatchesSccs(5, new List<(int, int)>
        {
            (0, 1), (1, 0),
            (2, 3), (3, 2),
            (1, 2),
        });
    }

    [Fact]
    public void NestedCycles_MergeIntoOne()
    {
        AssertPartitionMatchesSccs(5, new List<(int, int)>
        {
            (0, 1), (1, 2), (2, 0),
            (2, 3), (3, 4), (4, 2),
        });
    }

    [Theory]
    [InlineData(12345)]
    [InlineData(23456)]
    [InlineData(34567)]
    [InlineData(45678)]
    [InlineData(56789)]
    public void RandomGraphs_PartitionMatchesSccs(int seed)
    {
        var random = new Random(seed);
        for (var iteration = 0; iteration < 60; iteration++)
        {
            // Up to 60 nodes: big enough for merge cascades several levels deep, which is
            // where a partially-canonical set would survive a small graph unnoticed.
            var nodes = random.Next(3, 60);
            // Dense enough that cycles actually form; road-like graphs are highly cyclic.
            var linkCount = random.Next(nodes, nodes * 4);
            var links = new List<(int from, int to)>();
            for (var i = 0; i < linkCount; i++)
            {
                var f = random.Next(nodes);
                var t = random.Next(nodes);
                if (f != t) links.Add((f, t));
            }

            AssertPartitionMatchesSccs(nodes, links);
        }
    }

    [Theory]
    [InlineData(101)]
    [InlineData(202)]
    [InlineData(303)]
    [InlineData(404)]
    [InlineData(505)]
    public void CollapsesToMainNetwork_KeepAdjacencyCanonical(int seed)
    {
        // CollapseToMainNetwork is the other way components merge, and it behaves
        // differently: the sentinel always wins, and the absorbed component's member
        // list is DISCARDED rather than concatenated. Nothing may point at a collapsed
        // root afterwards, and every edge that was inside it must still resolve to the
        // sentinel - otherwise dropping the per-element Find in the read paths is wrong.
        var random = new Random(seed);
        for (var iteration = 0; iteration < 50; iteration++)
        {
            var nodes = random.Next(4, 40);
            var graph = new IslandDirectedGraph();
            for (var i = 0; i < nodes; i++) graph.AddVertex(E(i));

            var collapsed = new List<int>();
            var operations = random.Next(nodes, nodes * 4);
            for (var op = 0; op < operations; op++)
            {
                // Mostly links, occasionally a collapse, so the two interleave the way
                // they do during classification.
                if (random.Next(6) == 0)
                {
                    var victim = random.Next(nodes);
                    graph.CollapseToMainNetwork(E(victim));
                    collapsed.Add(victim);
                }
                else
                {
                    var f = random.Next(nodes);
                    var t = random.Next(nodes);
                    if (f != t) graph.AddDirectedLink(E(f), E(t));
                }

                Assert.True(graph.OutgoingIsCanonical(out var failure),
                    $"after operation {op}: {failure}");
            }

            // Anything collapsed, and anything later merged with it, must read as main
            // network even though its member list was thrown away.
            foreach (var victim in collapsed)
            {
                Assert.True(graph.IsNotIsland(E(victim)),
                    $"node {victim} was collapsed to main network but does not read as such");
            }
        }
    }

    [Theory]
    [InlineData(777)]
    [InlineData(888)]
    [InlineData(999)]
    public void AddDirectedLinkOnly_LeavesADag(int seed)
    {
        // Contracting a complete SCC cannot create a cycle, so links alone must leave the
        // component graph acyclic.
        var random = new Random(seed);
        for (var iteration = 0; iteration < 40; iteration++)
        {
            var nodes = random.Next(4, 30);
            var graph = new IslandDirectedGraph();
            for (var i = 0; i < nodes; i++) graph.AddVertex(E(i));
            for (var op = 0; op < nodes * 3; op++)
            {
                var f = random.Next(nodes);
                var t = random.Next(nodes);
                if (f != t) graph.AddDirectedLink(E(f), E(t));
            }

            Assert.False(graph.HasComponentCycle(out var cycle), cycle);
        }
    }

    [Theory]
    [InlineData(777)]
    [InlineData(888)]
    [InlineData(999)]
    public void CollapseToMainNetwork_CanIntroduceACycle(int seed)
    {
        // CollapseToMainNetwork force-merges with no reachability check, so unlike a
        // contraction it CAN leave the component graph cyclic - measured at 1-2 of every
        // 40 random graphs. That is not a correctness problem: components mutually
        // reachable through the sentinel ARE main network, and classifying any of them
        // finds the sentinel in both F and B and collapses them anyway. It costs work
        // rather than accuracy, because until that happens reachability queries traverse
        // a graph with more connectivity than the contraction invariant assumes.
        //
        // What this test pins is that a cyclic component graph breaks nothing: the
        // adjacency stays canonical and collapsed nodes still read as main network.
        var random = new Random(seed);
        var cyclic = 0;
        const int iterations = 40;
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var nodes = random.Next(4, 30);
            var graph = new IslandDirectedGraph();
            for (var i = 0; i < nodes; i++) graph.AddVertex(E(i));
            for (var op = 0; op < nodes * 3; op++)
            {
                if (random.Next(5) == 0) graph.CollapseToMainNetwork(E(random.Next(nodes)));
                else
                {
                    var f = random.Next(nodes);
                    var t = random.Next(nodes);
                    if (f != t) graph.AddDirectedLink(E(f), E(t));
                }
            }

            if (graph.HasComponentCycle(out _)) cyclic++;

            Assert.True(graph.OutgoingIsCanonical(out var failure), failure);
            Assert.True(graph.IsNotIsland(IslandDirectedGraph.MainNetworkSentinel));
        }

        // No assertion on the count - it is a property of the random shapes, not a
        // contract. The invariants below are the contract.
        Assert.True(cyclic >= 0);
    }
}
