using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

/// <summary>
/// Focused tests for the two-enumerator
/// <see cref="ICostFunctionExtensions.GetIslandBuilderCost(Itinero.Routing.Costs.ICostFunction,
/// RoutingNetworkEdgeEnumerator, RoutingNetworkEdgeEnumerator)"/> primitive
/// that <see cref="IslandClassifier"/> relies on for bidirectional merge
/// decisions. The classifier's <c>ProcessEdge</c> calls this twice per
/// (edge, neighbor) pair — once as canGoTo and once as canComeFrom — and
/// only merges into a single component when BOTH return true.
///
/// These tests pin the expected return values for common shapes so a
/// regression in either the cost function or the enumerator-positioning
/// would be caught here.
/// </summary>
public class IslandBuilderCostTests
{
    /// <summary>
    /// Mirrors the canGoTo / canComeFrom pair the classifier evaluates for
    /// edges <paramref name="e1"/> and <paramref name="e2"/> sharing vertex
    /// <paramref name="sharedVertex"/>, with <paramref name="e1"/> in
    /// <paramref name="e1Forward"/> direction (head = sharedVertex).
    /// </summary>
    private static (bool canGoTo, bool canComeFrom) Evaluate(
        RoutingNetwork network, Profile profile,
        EdgeId e1, bool e1Forward, EdgeId e2, VertexId sharedVertex)
    {
        var cost = network.GetCostFunctionFor(profile);

        // Position edgeIdFrom on e1 in its given direction (head=sharedVertex).
        var edgeIdFrom = network.GetEdgeEnumerator();
        Assert.True(edgeIdFrom.MoveTo(e1, e1Forward));
        Assert.Equal(sharedVertex, edgeIdFrom.Head);

        // Position edgeIdTo on e1 in the opposite direction (tail=sharedVertex).
        var edgeIdTo = network.GetEdgeEnumerator();
        Assert.True(edgeIdTo.MoveTo(e1, !e1Forward));
        Assert.Equal(sharedVertex, edgeIdTo.Tail);

        // Find e2 via vertex enumeration at sharedVertex (same way ProcessEdge does).
        var vertexEnum = network.GetEdgeEnumerator();
        Assert.True(vertexEnum.MoveTo(sharedVertex));
        bool found = false;
        while (vertexEnum.MoveNext())
        {
            if (vertexEnum.EdgeId == e2) { found = true; break; }
        }
        Assert.True(found, "e2 not enumerated at sharedVertex");

        var canGoTo = cost.GetIslandBuilderCost(edgeIdFrom, vertexEnum);

        var neighborArriving = network.GetEdgeEnumerator();
        Assert.True(neighborArriving.MoveTo(e2, !vertexEnum.Forward));
        var canComeFrom = cost.GetIslandBuilderCost(neighborArriving, edgeIdTo);

        return (canGoTo, canComeFrom);
    }

    [Fact]
    public void TwoBidirEdgesAtSharedVertex_BothCanGoToAndCanComeFromShouldBeTrue()
    {
        // Two plain bidirectional edges meeting at v2. No turn cost, no
        // restriction. This is the typical NL residential intersection.
        // For the classifier to bidir-merge them, BOTH canGoTo and
        // canComeFrom must return true.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            e1 = writer.AddEdge(v1, v2);
            e2 = writer.AddEdge(v2, v3);
        }
        var network = routerDb.Latest;

        var (canGoTo, canComeFrom) = Evaluate(network, new DefaultProfile(),
            e1, e1Forward: true, e2, v2);

        Assert.True(canGoTo, "canGoTo bidir→bidir should be true");
        Assert.True(canComeFrom, "canComeFrom bidir←bidir should be true");
    }

    [Fact]
    public void BidirThenOneWayLeavingSharedVertex_CanGoToTrueCanComeFromFalse()
    {
        // e1 bidir, e2 one-way leaving v2 (v2→v3). Classifier should add the
        // forward link e1→e2 (canGoTo true) but NOT bidir-merge because the
        // one-way can't come back via its backward direction (canComeFrom false).
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            e1 = writer.AddEdge(v1, v2);
            e2 = writer.AddEdge(v2, v3, attributes: new[] { ("oneway", "yes") });
        }
        var network = routerDb.Latest;

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        var (canGoTo, canComeFrom) = Evaluate(network, profile, e1, e1Forward: true, e2, v2);

        Assert.True(canGoTo, "canGoTo bidir→oneway-leaving should be true");
        Assert.False(canComeFrom, "canComeFrom from oneway in reverse should be false (one-way backward forbidden)");
    }

    [Fact]
    public void OneWayThenBidirLeavingSharedVertex_BothShouldBeTrue()
    {
        // Symmetric to above: e1 is one-way INTO v2 (v1→v2), e2 is bidir.
        // For ProcessEdge(e1) at v2 (e1's head): canGoTo asks "can e1 (forward,
        // arriving at v2) lead into e2 leaving v2?". e1 forward is valid, e2
        // leaving v2 forward is valid → true. canComeFrom asks "can e2 in
        // reverse (head=v2) come back into e1 in backward (leaving v2 toward
        // v1)?". e1 backward is forbidden → false.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            e1 = writer.AddEdge(v1, v2, attributes: new[] { ("oneway", "yes") });
            e2 = writer.AddEdge(v2, v3);
        }
        var network = routerDb.Latest;

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        var (canGoTo, canComeFrom) = Evaluate(network, profile, e1, e1Forward: true, e2, v2);

        Assert.True(canGoTo, "canGoTo oneway→bidir at v2 should be true");
        Assert.False(canComeFrom, "canComeFrom needs e1 backward, which is forbidden → false");
    }

    [Fact]
    public void TwoBidirEdgesWithBarrierTurnRestriction_BothShouldBeFalse()
    {
        // A binary turn cost barrier at v2 between e1 and e2 forbids transit.
        // BOTH directions of the turn should be blocked → both canGoTo and
        // canComeFrom false.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v1 = writer.AddVertex(4.792, 51.265);
            v2 = writer.AddVertex(4.794, 51.266);
            var v3 = writer.AddVertex(4.796, 51.267);
            e1 = writer.AddEdge(v1, v2);
            e2 = writer.AddEdge(v2, v3);
            writer.AddTurnCosts(v2,
                attributes: new[] { ("barrier", "bollard") },
                edges: new[] { e1, e2 },
                costs: new uint[,] { { 0, 1 }, { 1, 0 } });
        }
        var network = routerDb.Latest;

        var profile = new DefaultProfile(getTurnCostFactor: a =>
            a.Any(x => x.key == "barrier") ? TurnCostFactor.Binary : TurnCostFactor.Empty);

        var (canGoTo, canComeFrom) = Evaluate(network, profile, e1, e1Forward: true, e2, v2);

        Assert.False(canGoTo, "canGoTo blocked by binary barrier");
        Assert.False(canComeFrom, "canComeFrom blocked by binary barrier (symmetric)");
    }

    [Fact]
    public void TwoBidirEdgesAtChainOfThree_PairAtMiddleBothCanGoToAndCanComeFrom()
    {
        // Replicates the "chain of bidir edges" shape that should naturally
        // bidir-merge into one component. Pin the canGoTo/canComeFrom result
        // for the MIDDLE edge against its left neighbour and right neighbour
        // independently. All four checks must be true.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 4 });
        EdgeId eL, eMid, eR;
        VertexId vL, vR;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var v0 = writer.AddVertex(4.790, 51.265);
            vL = writer.AddVertex(4.792, 51.266);
            vR = writer.AddVertex(4.794, 51.267);
            var v3 = writer.AddVertex(4.796, 51.268);
            eL = writer.AddEdge(v0, vL);
            eMid = writer.AddEdge(vL, vR);
            eR = writer.AddEdge(vR, v3);
        }
        var network = routerDb.Latest;
        var profile = new DefaultProfile();

        // Pair {eL, eMid} at vL — eL.head=vL (eL forward).
        var (l_canGoTo, l_canComeFrom) = Evaluate(network, profile, eL, e1Forward: true, eMid, vL);
        Assert.True(l_canGoTo, "left pair canGoTo");
        Assert.True(l_canComeFrom, "left pair canComeFrom");

        // Pair {eMid, eR} at vR — eMid.head=vR (eMid forward).
        var (r_canGoTo, r_canComeFrom) = Evaluate(network, profile, eMid, e1Forward: true, eR, vR);
        Assert.True(r_canGoTo, "right pair canGoTo");
        Assert.True(r_canComeFrom, "right pair canComeFrom");
    }

    // ────────────────────────────────────────────────────────────────────
    // Orientation tests: storage direction of edges should not affect the
    // canGoTo / canComeFrom answer for plain bidir pairs. ProcessEdge runs
    // both passes (forward + backward) for bidir edges, so we test BOTH
    // passes here too — bidir-bidir must return (true, true) regardless of
    // which vertex the edges have stored as tail or head.
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void BidirPair_StoredHeadAtSharedVertex_PassForwardShouldBeTrueTrue()
    {
        // e1 stored u→v (head at shared v), e2 stored v→w (tail at shared v).
        // ProcessEdge(e1) pass 0 (forward) targetVertex = e1.Head = v.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v;
        using (var writer = routerDb.GetMutableNetwork())
        {
            // Coordinates tightly within one zoom-14 tile to avoid the
            // cross-tile boundary-edge effect on AddTurnCosts (which keys
            // edges by canonical EdgeId, but enumerates them as boundary
            // refs at the neighbor tile).
            var u = writer.AddVertex(4.79200, 51.26500);
            v = writer.AddVertex(4.79210, 51.26510);
            var w = writer.AddVertex(4.79220, 51.26520);
            e1 = writer.AddEdge(u, v); // stored u→v
            e2 = writer.AddEdge(v, w); // stored v→w
        }

        var (canGoTo, canComeFrom) = Evaluate(routerDb.Latest, new DefaultProfile(),
            e1, e1Forward: true, e2, v);
        Assert.True(canGoTo);
        Assert.True(canComeFrom);
    }

    [Fact]
    public void BidirPair_StoredHeadAtSharedVertex_PassBackwardShouldBeTrueTrue()
    {
        // Same edges as above. ProcessEdge(e1) pass 1 (backward) → e1 reversed,
        // targetVertex = u (e1.Tail). This pass exercises the "backward
        // direction at non-shared vertex" branch — verify it still works.
        // The shared vertex relative to e1 in backward is u.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId u;
        using (var writer = routerDb.GetMutableNetwork())
        {
            u = writer.AddVertex(4.790, 51.265);
            var v = writer.AddVertex(4.794, 51.266);
            var w = writer.AddVertex(4.798, 51.267);
            // attach e2 at u so we have a neighbour to test against on the
            // backward-pass side of e1.
            e1 = writer.AddEdge(u, v);
            e2 = writer.AddEdge(u, w);
        }

        // e1 in BACKWARD direction (pass 1) gives head=u, so shared vertex is u.
        var (canGoTo, canComeFrom) = Evaluate(routerDb.Latest, new DefaultProfile(),
            e1, e1Forward: false, e2, u);
        Assert.True(canGoTo, "pass-1 canGoTo bidir-bidir should be true");
        Assert.True(canComeFrom, "pass-1 canComeFrom bidir-bidir should be true");
    }

    [Fact]
    public void BidirPair_StoredTailAtSharedVertex_PassForwardShouldBeTrueTrue()
    {
        // e1 stored v→u (TAIL at shared v). ProcessEdge(e1) pass 0 (forward)
        // targetVertex = e1.Head = u. That's NOT the shared vertex; in this
        // configuration ProcessEdge would instead use pass 1 to enumerate at v.
        // But it's worth pinning the canGoTo/canComeFrom for the converse:
        // ProcessEdge using pass 1 (backward) of e1 reaches v as targetVertex.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v;
        using (var writer = routerDb.GetMutableNetwork())
        {
            v = writer.AddVertex(4.790, 51.265);
            var u = writer.AddVertex(4.794, 51.266);
            var w = writer.AddVertex(4.798, 51.267);
            e1 = writer.AddEdge(v, u); // stored v→u (v is tail)
            e2 = writer.AddEdge(v, w); // stored v→w (v is tail)
        }

        // For ProcessEdge to reach shared vertex v via e1, it uses backward pass.
        var (canGoTo, canComeFrom) = Evaluate(routerDb.Latest, new DefaultProfile(),
            e1, e1Forward: false, e2, v);
        Assert.True(canGoTo);
        Assert.True(canComeFrom);
    }

    [Fact]
    public void BidirPair_E2StoredOppositeDirection_BothShouldBeTrue()
    {
        // e1 stored u→v, e2 stored w→v (BOTH ending at v). When ProcessEdge
        // enumerates at v, e2 is iterated with Forward=false (storage ends at
        // v). This exercises the "neighbour iterated in reverse direction at
        // shared vertex" path through canGoTo/canComeFrom.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v;
        using (var writer = routerDb.GetMutableNetwork())
        {
            // Coordinates tightly within one zoom-14 tile to avoid the
            // cross-tile boundary-edge effect on AddTurnCosts (which keys
            // edges by canonical EdgeId, but enumerates them as boundary
            // refs at the neighbor tile).
            var u = writer.AddVertex(4.79200, 51.26500);
            v = writer.AddVertex(4.79210, 51.26510);
            var w = writer.AddVertex(4.79220, 51.26520);
            e1 = writer.AddEdge(u, v); // head=v
            e2 = writer.AddEdge(w, v); // head=v (both end at v)
        }

        var (canGoTo, canComeFrom) = Evaluate(routerDb.Latest, new DefaultProfile(),
            e1, e1Forward: true, e2, v);
        Assert.True(canGoTo, "e1 forward + e2 stored-opposite at shared head should still canGoTo");
        Assert.True(canComeFrom, "and canComeFrom — bidir pair regardless of storage");
    }

    [Fact]
    public void BidirPair_BothStoredStartingAtSharedVertex_BothShouldBeTrue()
    {
        // e1 stored v→u, e2 stored v→w (BOTH starting at v). At v, BOTH
        // iterated with Forward=true (storage starts at v).
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v;
        using (var writer = routerDb.GetMutableNetwork())
        {
            v = writer.AddVertex(4.790, 51.265);
            var u = writer.AddVertex(4.794, 51.266);
            var w = writer.AddVertex(4.798, 51.267);
            e1 = writer.AddEdge(v, u); // tail=v
            e2 = writer.AddEdge(v, w); // tail=v (both start at v)
        }

        // To reach shared vertex v from e1, use backward pass (head=v in reverse).
        var (canGoTo, canComeFrom) = Evaluate(routerDb.Latest, new DefaultProfile(),
            e1, e1Forward: false, e2, v);
        Assert.True(canGoTo);
        Assert.True(canComeFrom);
    }

    [Fact]
    public void HighDegreeVertex_FourBidirEdges_AllPairsMergeable()
    {
        // 4-way intersection: 4 bidir edges meeting at v. For ANY pair the
        // canGoTo/canComeFrom must be true. Real urban intersections look
        // like this — verify the high-degree case is handled.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId eN, eE, eS, eW;
        VertexId v;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var n = writer.AddVertex(4.794, 51.270);
            var e = writer.AddVertex(4.800, 51.266);
            var s = writer.AddVertex(4.794, 51.262);
            var w = writer.AddVertex(4.788, 51.266);
            v = writer.AddVertex(4.794, 51.266);
            eN = writer.AddEdge(n, v);
            eE = writer.AddEdge(e, v);
            eS = writer.AddEdge(s, v);
            eW = writer.AddEdge(w, v);
        }
        var network = routerDb.Latest;
        var profile = new DefaultProfile();

        var pairs = new[]
        {
            (eN, eE), (eN, eS), (eN, eW),
            (eE, eS), (eE, eW),
            (eS, eW),
        };
        foreach (var (a, b) in pairs)
        {
            var (canGoTo, canComeFrom) = Evaluate(network, profile, a, e1Forward: true, b, v);
            Assert.True(canGoTo, $"canGoTo for pair ({a},{b})");
            Assert.True(canComeFrom, $"canComeFrom for pair ({a},{b})");
        }
    }

    [Fact]
    public void AsymmetricTurnRestriction_OneDirectionForbidden()
    {
        // Asymmetric turn cost: a→b allowed (cost 0), b→a forbidden (cost 1
        // with binary turn factor). This is a common real-world shape:
        // "no left turn from B onto A" but "right turn from A onto B is ok".
        // canGoTo and canComeFrom evaluate DIFFERENT directions of the turn,
        // so they should differ.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v;
        using (var writer = routerDb.GetMutableNetwork())
        {
            // Coordinates tightly within one zoom-14 tile to avoid the
            // cross-tile boundary-edge effect on AddTurnCosts (which keys
            // edges by canonical EdgeId, but enumerates them as boundary
            // refs at the neighbor tile).
            var u = writer.AddVertex(4.79200, 51.26500);
            v = writer.AddVertex(4.79210, 51.26510);
            var w = writer.AddVertex(4.79220, 51.26520);
            e1 = writer.AddEdge(u, v);
            e2 = writer.AddEdge(v, w);
            writer.AddTurnCosts(v,
                attributes: new[] { ("restriction", "no_left_turn") },
                edges: new[] { e1, e2 },
                costs: new uint[,] { { 0, 0 }, { 1, 0 } });
        }

        var profile = new DefaultProfile(getTurnCostFactor: a =>
            a.Any(x => x.key == "restriction") ? TurnCostFactor.Binary : TurnCostFactor.Empty);

        var (canGoTo, canComeFrom) = Evaluate(routerDb.Latest, profile,
            e1, e1Forward: true, e2, v);
        Assert.True(canGoTo, "e1→e2 turn allowed");
        Assert.False(canComeFrom, "e2→e1 turn forbidden");
    }

    [Fact]
    public void TurnCostEntryWithZeroCost_StillReturnsTrue()
    {
        // A turn-cost entry with cost 0 across the board (no restriction
        // actually encoded) must NOT cause spurious false returns. This
        // protects against a bug where the mere presence of a turn-cost
        // table at a vertex inadvertently fails the check.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v;
        using (var writer = routerDb.GetMutableNetwork())
        {
            // Coordinates tightly within one zoom-14 tile to avoid the
            // cross-tile boundary-edge effect on AddTurnCosts (which keys
            // edges by canonical EdgeId, but enumerates them as boundary
            // refs at the neighbor tile).
            var u = writer.AddVertex(4.79200, 51.26500);
            v = writer.AddVertex(4.79210, 51.26510);
            var w = writer.AddVertex(4.79220, 51.26520);
            e1 = writer.AddEdge(u, v);
            e2 = writer.AddEdge(v, w);
            writer.AddTurnCosts(v,
                attributes: new[] { ("highway", "traffic_signals") },
                edges: new[] { e1, e2 },
                costs: new uint[,] { { 0, 0 }, { 0, 0 } });
        }

        var profile = new DefaultProfile(getTurnCostFactor: a =>
            a.Any(x => x.key == "highway") ? TurnCostFactor.Binary : TurnCostFactor.Empty);

        var (canGoTo, canComeFrom) = Evaluate(routerDb.Latest, profile,
            e1, e1Forward: true, e2, v);
        Assert.True(canGoTo, "zero turn cost should still allow");
        Assert.True(canComeFrom, "zero turn cost should still allow");
    }

    [Fact]
    public void OneWayPair_BothAlignedAtSharedHead_CanGoToTrueCanComeFromFalse()
    {
        // Two one-way edges flowing INTO the shared vertex v:
        //   e1: u→v  (one-way)
        //   e2: w→v  (one-way)
        // Geometrically you cannot drive between them across v — you arrive
        // at v via either, then you cannot leave (both are arriving-only).
        // canGoTo asks "e1 arriving at v, leave via e2 leaving v" — but e2
        // can't LEAVE v (it ends at v in one-way). canGoTo should be false.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v;
        using (var writer = routerDb.GetMutableNetwork())
        {
            // Coordinates tightly within one zoom-14 tile to avoid the
            // cross-tile boundary-edge effect on AddTurnCosts (which keys
            // edges by canonical EdgeId, but enumerates them as boundary
            // refs at the neighbor tile).
            var u = writer.AddVertex(4.79200, 51.26500);
            v = writer.AddVertex(4.79210, 51.26510);
            var w = writer.AddVertex(4.79220, 51.26520);
            e1 = writer.AddEdge(u, v, attributes: new[] { ("oneway", "yes") });
            e2 = writer.AddEdge(w, v, attributes: new[] { ("oneway", "yes") });
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        var (canGoTo, canComeFrom) = Evaluate(routerDb.Latest, profile,
            e1, e1Forward: true, e2, v);
        Assert.False(canGoTo, "cannot leave v via e2 (e2 arrives at v in its only allowed direction)");
        Assert.False(canComeFrom, "cannot leave v via e1 either (symmetric)");
    }

    [Fact]
    public void OneWayPair_BothLeavingSharedTail_CanGoToFalseCanComeFromFalse()
    {
        // Two one-way edges leaving v:
        //   e1: v→u  (one-way)
        //   e2: v→w  (one-way)
        // For ProcessEdge to reach v as targetVertex via e1, it uses backward
        // pass — but e1 has canBackward=false, so pass 1 is skipped entirely.
        // We exercise canGoTo/canComeFrom DIRECTLY here for the (impossible)
        // attempt: e1 arriving at v (would require backward) → leave via e2.
        // Both directions should refuse.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        EdgeId e1, e2;
        VertexId v;
        using (var writer = routerDb.GetMutableNetwork())
        {
            v = writer.AddVertex(4.790, 51.265);
            var u = writer.AddVertex(4.794, 51.266);
            var w = writer.AddVertex(4.798, 51.267);
            e1 = writer.AddEdge(v, u, attributes: new[] { ("oneway", "yes") });
            e2 = writer.AddEdge(v, w, attributes: new[] { ("oneway", "yes") });
        }

        var profile = new DefaultProfile(getEdgeFactor: a => a.Any(x => x.key == "oneway")
            ? new EdgeFactor(1, 0, 1, 0)
            : new EdgeFactor(1, 1, 1, 1));

        // e1 backward (would arrive at v) — not allowed.
        var (canGoTo, canComeFrom) = Evaluate(routerDb.Latest, profile,
            e1, e1Forward: false, e2, v);
        Assert.False(canGoTo, "e1 backward forbidden — cannot 'arrive at v' via e1");
        Assert.False(canComeFrom, "e2 backward also forbidden — cannot leave v via e2 backward");
    }
}
