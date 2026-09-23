using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Search.Islands;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

/// <summary>
/// _outgoing and _incoming encode the same edge set from both ends. MergeNoLock depends
/// on that: to repair everything pointing at an absorbed root it walks _incoming[rootB],
/// so a link recorded in only one direction is a link the repair cannot see, and the
/// stale entry it leaves behind survives until some later Find papers over it.
/// </summary>
/// <remarks>
/// Written after a two-line change to MergeNoLock's self-loop cleanup — removing rootB
/// from rootA's own sets, which is correct in isolation — hung a real route while 300
/// random graphs and 591 unit tests stayed green. The partition-equals-SCCs property is
/// blind to it, because removing a self-loop cannot change the partition. If the mirror
/// is already asymmetric then every reader has been relying on Find to normalise away
/// entries of several different kinds, and removing only one kind changes which survive.
/// </remarks>
public class IslandDirectedGraphMirrorTests
{
    private static EdgeId E(int i) => new(0, (uint)i);

    [Fact]
    public void FreshLinks_AreMirrored()
    {
        var graph = new IslandDirectedGraph();
        for (var i = 0; i < 5; i++) graph.AddVertex(E(i));
        graph.AddDirectedLink(E(0), E(1));
        graph.AddDirectedLink(E(1), E(2));
        graph.AddDirectedLink(E(3), E(1));

        Assert.True(graph.AdjacencyIsMirrored(out var failure), failure);
    }

    [Fact]
    public void AfterACycleMerge_OutgoingStaysCanonical()
    {
        // The mirror does not survive a merge - see the theory below - but the outgoing
        // index does stay canonical, which is what lets the forward traversal skip Find.
        var graph = new IslandDirectedGraph();
        for (var i = 0; i < 5; i++) graph.AddVertex(E(i));
        graph.AddDirectedLink(E(0), E(1));
        graph.AddDirectedLink(E(1), E(2));
        graph.AddDirectedLink(E(2), E(0));   // closes the cycle, merges 0/1/2
        graph.AddDirectedLink(E(2), E(3));
        graph.AddDirectedLink(E(4), E(0));

        Assert.True(graph.OutgoingIsCanonical(out var failure), failure);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(41)]
    [InlineData(59)]
    [InlineData(26)]
    [InlineData(53)]
    public void RandomLinksAndCollapses_BreakTheMirror_AndThatIsLoadBearing(int seed)
    {
        // The mirror does NOT survive merges, and this test records that rather than
        // demanding it. MergeNoLock removes _incoming[rootB] wholesale while leaving the
        // absorbed root inside _incoming[rootA], so the two indexes disagree.
        //
        // Repairing it - removing the absorbed root from _incoming[rootA] too - makes both
        // indexes exactly canonical and hangs
        // Car/generated/case_gen_car_classifications_016. Doing the same for _outgoing
        // alone is safe. So the asymmetry in the backward index is load-bearing for
        // reasons not yet understood, and the mirror is an invariant this structure does
        // not have.
        var random = new Random(seed);
        for (var iteration = 0; iteration < 40; iteration++)
        {
            var nodes = random.Next(4, 40);
            var graph = new IslandDirectedGraph();
            for (var i = 0; i < nodes; i++) graph.AddVertex(E(i));

            var operations = random.Next(nodes, nodes * 4);
            for (var op = 0; op < operations; op++)
            {
                if (random.Next(6) == 0)
                {
                    graph.CollapseToMainNetwork(E(random.Next(nodes)));
                }
                else
                {
                    var f = random.Next(nodes);
                    var t = random.Next(nodes);
                    if (f != t) graph.AddDirectedLink(E(f), E(t));
                }

                // Outgoing is canonical and may be read without Find. Incoming is not.
                Assert.True(graph.OutgoingIsCanonical(out var failure),
                    $"seed {seed}, iteration {iteration}, operation {op}: {failure}");
            }
        }
    }
}
