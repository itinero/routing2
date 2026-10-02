using System.Collections.Generic;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Search.Reachability;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra;
using Itinero.Routing.Flavours.Dijkstra.Bidirectional;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Routing.Flavours.Dijkstra.Bidirectional;

/// <summary>
/// Getting out of a local-access enclave: the search that starts on an access=destination edge
/// and runs until it is properly clear of the area.
/// </summary>
/// <remarks>
/// L → L and L → N are always allowed; N → L only when the N is itself stuck behind local edges,
/// which is the one transition tags cannot decide and the only one consulting a detector.
/// </remarks>
public class EscapeLocalTests
{
    private static (RouterDb db, EdgeId[] edges) BuildChain(int edgeCount)
    {
        var routerDb = new RouterDb();
        var edges = new EdgeId[edgeCount];
        using (var writer = routerDb.GetMutableNetwork())
        {
            var verts = new VertexId[edgeCount + 1];
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i] = writer.AddVertex(4.79 + (i * 0.0005), 51.26);
            }

            for (var i = 0; i < edgeCount; i++)
            {
                edges[i] = writer.AddEdge(verts[i], verts[i + 1]);
            }
        }

        return (routerDb, edges);
    }

    private sealed class StubCostFunction : ICostFunction
    {
        private readonly HashSet<EdgeId> _local;

        public StubCostFunction(HashSet<EdgeId> local)
        {
            _local = local;
        }

        public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
            Itinero.Network.Enumerators.Edges.IEdgeEnumerator<RoutingNetwork> edgeEnumerator,
            bool tailToHead = true,
            PreviousEdgeEnumerable previousEdges = default)
            => (true, true, _local.Contains(edgeEnumerator.EdgeId), 1, 0);
    }

    private static async Task<(ReachabilityVerdict verdict, SearchHalf half)> EscapeAsync(
        RouterDb db, EdgeId from, HashSet<EdgeId> local, int threshold, int ceiling = 1000,
        bool forward = true)
    {
        var cost = new StubCostFunction(local);
        var half = new SearchHalf(isForwardHalf: forward);
        half.PushTerminal(db.Latest.GetEdgeEnumerator(),
            new SnapPoint(from, forward ? (ushort)0 : ushort.MaxValue), cost, potential: null);

        var verdict = await half.EscapeLocalAsync(db.Latest, cost, threshold, ceiling);
        return (verdict, half);
    }

    [Fact]
    public async Task EnclaveWithAWayOut_Escapes()
    {
        // A few local streets, then ordinary road. The search leaves and keeps going until it
        // has seen enough of the road to have learned something.
        var (db, e) = BuildChain(40);
        var local = new HashSet<EdgeId> { e[0], e[1], e[2] };

        var (verdict, half) = await EscapeAsync(db, e[0], local, threshold: 10);

        Assert.Equal(ReachabilityVerdict.BeyondBound, verdict);
        Assert.Equal(0, half.LocalOnHeap);
    }

    [Fact]
    public async Task ClosedEnclave_IsAnIsland()
    {
        // A private estate with no exit at all. Nothing to escape to, and running out of
        // network is the answer.
        var routerDb = new RouterDb();
        EdgeId first;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var a = writer.AddVertex(4.790, 51.260);
            var b = writer.AddVertex(4.791, 51.260);
            var c = writer.AddVertex(4.792, 51.260);
            first = writer.AddEdge(a, b);
            writer.AddEdge(b, c);
        }

        var local = new HashSet<EdgeId>();
        foreach (var edge in new[] { first }) local.Add(edge);

        var (verdict, _) = await EscapeAsync(routerDb, first, local, threshold: 10);

        Assert.Equal(ReachabilityVerdict.Island, verdict);
    }

    [Fact]
    public async Task ThresholdAloneDoesNotStopIt_WhileLocalEdgesAreStillQueued()
    {
        // Half the conjunction. The enclave is bigger than the threshold, so a search that
        // stopped on the count alone would still be inside it — with local edges waiting on
        // the frontier and no idea whether any of this leads anywhere.
        var (db, e) = BuildChain(40);
        var local = new HashSet<EdgeId>();
        for (var i = 0; i < 20; i++) local.Add(e[i]);

        var (verdict, half) = await EscapeAsync(db, e[0], local, threshold: 5);

        Assert.Equal(ReachabilityVerdict.BeyondBound, verdict);
        Assert.Equal(0, half.LocalOnHeap);
    }

    [Fact]
    public async Task FrontierGoingNonLocalAloneDoesNotStopIt_BelowTheThreshold()
    {
        // The other half. One local edge, then road: the frontier goes non-local almost at
        // once, and stopping there would have established nothing about where the road goes.
        // The threshold forces it to keep looking.
        var (db, e) = BuildChain(40);
        var local = new HashSet<EdgeId> { e[0] };

        var (verdict, half) = await EscapeAsync(db, e[0], local, threshold: 20);

        Assert.Equal(ReachabilityVerdict.BeyondBound, verdict);

        // The point of the test: it kept going well past where the frontier first went
        // non-local, which happens after the single local edge. Settling at least the
        // threshold is what distinguishes that from stopping at the first road.
        Assert.True(half.Settled.Count >= 20,
            $"expected at least 20 settled states, got {half.Settled.Count}");
    }

    [Fact]
    public async Task CeilingStopsAnEnormousEnclave()
    {
        // A pedestrianised centre: everything local, nothing to escape to, but far too big to
        // walk. The ceiling ends it and reports the area as network in its own right rather
        // than as somewhere to get out of.
        var (db, e) = BuildChain(200);
        var local = new HashSet<EdgeId>(e);

        var (verdict, _) = await EscapeAsync(db, e[0], local, threshold: 10, ceiling: 20);

        Assert.Equal(ReachabilityVerdict.BeyondBound, verdict);
    }

    [Fact]
    public async Task RefusesToCrossAnOrdinaryRoadIntoAnotherEnclave()
    {
        // L1 → N → L2. N to L is refused outright, so a half leaves its own pocket and never
        // enters another — including when e[2] is itself stuck behind local edges, which used to
        // be an exception and is deliberately no longer one.
        var (db, e) = BuildChain(40);
        var local = new HashSet<EdgeId> { e[0], e[1], e[3], e[4], e[5] };

        var (_, half) = await EscapeAsync(db, e[0], local, threshold: 10);

        Assert.DoesNotContain(e[4], SettledEdges(half));
    }

    [Fact]
    public async Task LeavingAnEnclaveIsAllowedInBothHalves()
    {
        // L → N unconditionally, whichever way the half grows, because each half works in its
        // own search direction — easy to misread as N → L if you think in path order.
        var (db, e) = BuildChain(40);
        var local = new HashSet<EdgeId> { e[0], e[1] };

        var (forwardVerdict, _) = await EscapeAsync(db, e[0], local, threshold: 10, forward: true);
        var (backwardVerdict, _) = await EscapeAsync(db, e[1], local, threshold: 10, forward: false);

        Assert.Equal(ReachabilityVerdict.BeyondBound, forwardVerdict);
        Assert.Equal(ReachabilityVerdict.BeyondBound, backwardVerdict);
    }

    private static HashSet<EdgeId> SettledEdges(SearchHalf half)
    {
        var edges = new HashSet<EdgeId>();
        foreach (var (edge, _) in half.Settled) edges.Add(edge);
        return edges;
    }
}
