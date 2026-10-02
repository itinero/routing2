using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Search.Reachability;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra;
using Xunit;

namespace Itinero.Tests.Network.Search.Reachability;

/// <summary>
/// Composing the local-access region around an endpoint: which edges are absorbed, which form the
/// perimeter, and when the region is too big to be a pocket.
/// </summary>
/// <remarks>
/// Both bounds err the same way — exceeding them means "not absorbed", so the strict rule applies
/// and the caller lands on the behaviour it would have had anyway.
/// </remarks>
public class LocalPocketTests
{
    private const int Allowance = 8;
    private const int Budget = 256;

    /// A chain of `count` edges along a line; edges[i] joins vertex i to i+1.
    private static (RouterDb db, EdgeId[] edges) Chain(int count, bool oneWay = false)
    {
        var routerDb = new RouterDb();
        var edges = new EdgeId[count];
        using (var writer = routerDb.GetMutableNetwork())
        {
            var verts = new VertexId[count + 1];
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i] = writer.AddVertex(4.79 + (i * 0.0005), 51.26);
            }

            for (var i = 0; i < count; i++) edges[i] = writer.AddEdge(verts[i], verts[i + 1]);
        }

        return (routerDb, edges);
    }

    /// A chain with a spur hanging off vertex `at`, returned as the last edge.
    private static (RouterDb db, EdgeId[] edges, EdgeId spur) ChainWithSpur(int count, int at)
    {
        var routerDb = new RouterDb();
        var edges = new EdgeId[count];
        EdgeId spur;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var verts = new VertexId[count + 1];
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i] = writer.AddVertex(4.79 + (i * 0.0005), 51.26);
            }

            for (var i = 0; i < count; i++) edges[i] = writer.AddEdge(verts[i], verts[i + 1]);

            var dead = writer.AddVertex(4.79 + (at * 0.0005), 51.2605);
            spur = writer.AddEdge(verts[at], dead);
        }

        return (routerDb, edges, spur);
    }

    private sealed class StubCostFunction : ICostFunction
    {
        private readonly HashSet<EdgeId> _local;
        private readonly HashSet<EdgeId> _oneWay;

        public StubCostFunction(HashSet<EdgeId>? local = null, HashSet<EdgeId>? oneWay = null)
        {
            _local = local ?? [];
            _oneWay = oneWay ?? [];
        }

        public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
            Itinero.Network.Enumerators.Edges.IEdgeEnumerator<RoutingNetwork> edgeEnumerator,
            bool tailToHead = true,
            PreviousEdgeEnumerable previousEdges = default)
        {
            // One-way edges are traversable only in their natural direction.
            var natural = edgeEnumerator.Forward == tailToHead;
            var canAccess = natural || !_oneWay.Contains(edgeEnumerator.EdgeId);

            return (canAccess, true, _local.Contains(edgeEnumerator.EdgeId), 1, 0);
        }
    }

    private static LocalPocket.Result Compose(RouterDb db, IEnumerable<EdgeId> seeds,
        HashSet<EdgeId>? local = null, bool asOrigin = true, int allowance = Allowance,
        int budget = Budget, HashSet<EdgeId>? oneWay = null) =>
        LocalPocket.Compose(db.Latest, new StubCostFunction(local, oneWay), seeds, asOrigin,
            allowance, budget);

    [Fact]
    public void ClosedAllLocalPocket_AbsorbsEverything_NoPerimeter()
    {
        var (db, e) = Chain(5);
        var all = new HashSet<EdgeId>(e);

        var pocket = Compose(db, [e[0]], local: all);

        Assert.False(pocket.Abandoned);
        Assert.Equal(5, pocket.Members.Count);
        Assert.Empty(pocket.Perimeter);
    }

    [Fact]
    public void LocalPocketWithOneExit_ExcludesTheExit()
    {
        // e0..e2 are the pocket; e3 onward is a long ordinary chain, i.e. main network.
        var (db, e) = Chain(40);
        var local = new HashSet<EdgeId> { e[0], e[1], e[2] };

        var pocket = Compose(db, [e[0]], local: local);

        Assert.False(pocket.Abandoned);
        Assert.Equal(local, pocket.Members);
        Assert.Contains(e[3], pocket.Perimeter);
    }

    [Fact]
    public void UntaggedSegmentThatClosesBackIntoThePocket_IsAbsorbed()
    {
        // e0 L, e1 N, e2 L, then main network. e1's non-local component is itself alone, because
        // local edges bound it on both sides, so it closes at once.
        var (db, e) = Chain(40);
        var local = new HashSet<EdgeId> { e[0], e[2] };

        var pocket = Compose(db, [e[0]], local: local);

        Assert.False(pocket.Abandoned);
        Assert.Contains(e[1], pocket.Members);
        Assert.Contains(e[2], pocket.Members);
    }

    [Fact]
    public void AlternatingChain_IsAbsorbedWhole()
    {
        // L N L N L, then main network. Each N component closes on its own, so chain length does
        // not matter and the fixpoint reaches the far end.
        var (db, e) = Chain(40);
        var local = new HashSet<EdgeId> { e[0], e[2], e[4] };

        var pocket = Compose(db, [e[0]], local: local);

        Assert.False(pocket.Abandoned);
        foreach (var i in new[] { 0, 1, 2, 3, 4 }) Assert.Contains(e[i], pocket.Members);
        Assert.Contains(e[5], pocket.Perimeter);
    }

    [Fact]
    public void UntaggedSegmentLeadingToMainNetwork_IsPerimeter_AndTheFarSideIsNotAMember()
    {
        // L1 at e0, then e1 is ordinary and continues into a long ordinary chain, and e30 is a
        // second local region. e1 escapes, so the pocket stops and L2 never joins.
        var (db, e) = Chain(40);
        var local = new HashSet<EdgeId> { e[0], e[30] };

        var pocket = Compose(db, [e[0]], local: local);

        Assert.False(pocket.Abandoned);
        Assert.Contains(e[1], pocket.Perimeter);
        Assert.DoesNotContain(e[1], pocket.Members);
        Assert.DoesNotContain(e[30], pocket.Members);
    }

    [Fact]
    public void DeadEndStub_IsAbsorbed()
    {
        // An untagged spur off the pocket that leads nowhere: its component closes with no local
        // boundary at all, which is the same test.
        var (db, e, spur) = ChainWithSpur(40, at: 1);
        var local = new HashSet<EdgeId> { e[0], e[1] };

        var pocket = Compose(db, [e[0]], local: local);

        Assert.False(pocket.Abandoned);
        Assert.Contains(spur, pocket.Members);
    }

    [Fact]
    public void SeedIsAnUntaggedEdgeEnclosedByLocalEdges()
    {
        // The 0109 and 0658 shape: the candidate itself is ordinary road whose only neighbours are
        // local-access, so it is seeded directly and the surrounding local edges join.
        var (db, e) = Chain(40);
        var local = new HashSet<EdgeId> { e[0], e[2], e[3] };

        var pocket = Compose(db, [e[1]], local: local);

        Assert.False(pocket.Abandoned);
        Assert.Contains(e[1], pocket.Members);
        Assert.Contains(e[0], pocket.Members);
        Assert.Contains(e[2], pocket.Members);
    }

    [Fact]
    public void UntaggedRunLongerThanTheAllowance_IsNotAbsorbed()
    {
        // The allowance is what separates an interior segment from main network, and a run longer
        // than it is treated as main — the conservative direction.
        var (db, e) = Chain(40);
        var local = new HashSet<EdgeId> { e[0] };

        var pocket = Compose(db, [e[0]], local: local, allowance: 3);

        Assert.False(pocket.Abandoned);
        Assert.Contains(e[1], pocket.Perimeter);
        Assert.Single(pocket.Members);
    }

    [Fact]
    public void RegionLargerThanTheBudget_IsAbandoned()
    {
        var (db, e) = Chain(40);
        var all = new HashSet<EdgeId>(e);

        var pocket = Compose(db, [e[0]], local: all, budget: 10);

        Assert.True(pocket.Abandoned);
    }

    [Fact]
    public void MainNetworkIsDiscoveredOnce_SoASecondSeedInItCostsNothing()
    {
        // Two separate exits from the same pocket into the same ordinary chain. The second is
        // answered from knownMain rather than by walking the allowance again.
        var (db, e, spur) = ChainWithSpur(40, at: 2);
        var local = new HashSet<EdgeId> { e[0], e[1], e[2] };

        var pocket = Compose(db, [e[0]], local: local);

        Assert.False(pocket.Abandoned);
        Assert.Contains(e[3], pocket.Perimeter);
        // The spur is a dead end off the same vertex, so it closes and is absorbed.
        Assert.Contains(spur, pocket.Members);
    }

    [Fact]
    public void PocketIsDirectional()
    {
        // A one-way ordinary chain beyond a local seed. As an origin the chain lies ahead and
        // escapes; as a destination nothing leads in, so there is no way out of the pocket at all.
        var (db, e) = Chain(40);
        var local = new HashSet<EdgeId> { e[0] };
        var oneWay = new HashSet<EdgeId>(e.Skip(1));

        var asOrigin = Compose(db, [e[0]], local: local, asOrigin: true, oneWay: oneWay);
        var asDestination = Compose(db, [e[0]], local: local, asOrigin: false, oneWay: oneWay);

        Assert.NotEqual(asOrigin.Perimeter.Count, asDestination.Perimeter.Count);
    }
}
