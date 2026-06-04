using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routing;
using Itinero.Snapping;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.Routing;

/// <summary>
/// Direct unit tests of the routing layer with turn-cost-based blocking
/// (bollards, no-turn restrictions). Builds a tiny network programmatically
/// (no OSM stream / no tile pipeline involved) and asserts that the routing
/// engine actually honors the turn cost at the right vertex.
/// These tests close the gap between "the resolver places turn costs at
/// the right vertex" (which our restriction-resolver tests already cover)
/// and "the router refuses to traverse a forbidden turn" (which until now
/// only end-to-end tests could observe).
/// </summary>
public class TurnCostBlockingTests
{
    /// <summary>
    /// Profile that returns binary turn-cost factor whenever any attribute is
    /// present (so any AddTurnCosts call with non-empty attributes acts as a
    /// hard block when the cost > 0). TurnCostFactorEnabled inherits the
    /// Profile.cs default of <c>false</c>, matching <see cref="Itinero.Profiles.Lua.Osm.OsmProfiles.Car"/>.
    /// </summary>
    private sealed class TurnCostBlockingProfile : Profile
    {
        private readonly bool _enabled;

        public TurnCostBlockingProfile(bool turnCostFactorEnabled = false)
        {
            _enabled = turnCostFactorEnabled;
        }

        public override string Name => $"test-blocking-{_enabled}";
        public override bool TurnCostFactorEnabled => _enabled;
        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
            => new EdgeFactor(1, 1, 100, 100);
        public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
            => Itinero.Profiles.TurnCostFactor.Binary;
    }

    private static Profile BlockingProfile(bool turnCostFactorEnabled = false)
        => new TurnCostBlockingProfile(turnCostFactorEnabled);

    [Fact]
    public async Task Routing_TwoEdgesAtVertex_TurnCostForbidsAtoB_ShouldBlockRoute()
    {
        // baseline scenario: 3 vertices in a line, 2 edges. A turn cost at the
        // middle vertex forbids edge1→edge2. Routing from start of edge1 to
        // end of edge2 must fail (no alternative).
        var routerDb = new RouterDb();
        routerDb.PrepareFor(BlockingProfile());
        VertexId a, mid, b;
        EdgeId edge1, edge2;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.270);
            mid = mutable.AddVertex(4.801, 51.270);
            b = mutable.AddVertex(4.802, 51.270);

            edge1 = mutable.AddEdge(a, mid);
            edge2 = mutable.AddEdge(mid, b);

            mutable.AddTurnCosts(mid,
                new[] { ("barrier", "bollard") },
                new[] { edge1, edge2 },
                new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile();
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapB = await network.Snap().ToAsync(b).FirstAsync();

        var route = await network.Route(profile)
            .From(snapA)
            .To(snapB)
            .CalculateAsync();

        Assert.True(route.IsError,
            "route from a to b should fail — turn cost at mid forbids edge1→edge2 and there is no other path");
    }

    [Fact]
    public async Task Routing_BollardSplitToWay_TurnCostBetweenWay1AndWay2FirstSubedge_ShouldBlockRoute()
    {
        // mirror of the failing end-to-end test, but built directly:
        //   way 1   = edge_w1   (a → bollard)
        //   way 2   = edge_w2a  (bollard → j2)   AND   edge_w2b  (j2 → c)
        //   way 3   = edge_w3   (j2 → x)         — only there to make j2 a junction;
        //                                           irrelevant to the turn cost itself.
        // turn cost at the bollard vertex forbids [edge_w1, edge_w2a] in both
        // directions. Routing from a to c must fail (no alternative).
        var routerDb = new RouterDb();
        routerDb.PrepareFor(BlockingProfile());
        VertexId a, bollard, j2, c, x;
        EdgeId w1, w2a, w2b, w3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.269);
            bollard = mutable.AddVertex(4.801, 51.269);
            j2 = mutable.AddVertex(4.802, 51.269);
            c = mutable.AddVertex(4.803, 51.269);
            x = mutable.AddVertex(4.802, 51.270);

            w1 = mutable.AddEdge(a, bollard);
            w2a = mutable.AddEdge(bollard, j2);
            w2b = mutable.AddEdge(j2, c);
            w3 = mutable.AddEdge(j2, x);

            // forbid through-traversal at the bollard (matching what the
            // resolver should be producing for this scenario).
            mutable.AddTurnCosts(bollard,
                new[] { ("barrier", "bollard") },
                new[] { w1, w2a },
                new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile();
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapC = await network.Snap().ToAsync(c).FirstAsync();

        var route = await network.Route(profile)
            .From(snapA)
            .To(snapC)
            .CalculateAsync();

        Assert.True(route.IsError,
            "route from a to c should fail — turn cost at bollard forbids way1→way2a and there is no alternative");
    }

    [Fact]
    public async Task Routing_TurnCostAtMid_ButQueryFromAToTipOfBranchPastMid_ShouldBlockRoute()
    {
        // even more minimal case — 4 vertices, 3 edges in a line:
        //   a — edge1 — mid — edge2 — j2 — edge3 — c
        // turn cost at mid forbids edge1→edge2.
        // routing a → c must fail.
        var routerDb = new RouterDb();
        routerDb.PrepareFor(BlockingProfile());
        VertexId a, mid, j2, c;
        EdgeId edge1, edge2, edge3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.269);
            mid = mutable.AddVertex(4.801, 51.269);
            j2 = mutable.AddVertex(4.802, 51.269);
            c = mutable.AddVertex(4.803, 51.269);

            edge1 = mutable.AddEdge(a, mid);
            edge2 = mutable.AddEdge(mid, j2);
            edge3 = mutable.AddEdge(j2, c);

            mutable.AddTurnCosts(mid,
                new[] { ("barrier", "bollard") },
                new[] { edge1, edge2 },
                new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile();
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapC = await network.Snap().ToAsync(c).FirstAsync();

        var route = await network.Route(profile)
            .From(snapA)
            .To(snapC)
            .CalculateAsync();

        Assert.True(route.IsError,
            "route from a to c should fail — turn cost at mid forbids edge1→edge2; the only path runs through that turn");
    }

    [Fact]
    public async Task Routing_TwoEdgesAtVertex_TurnCostForbidsAtoB_TurnCostFactorEnabled_ShouldBlockRoute()
    {
        // SAME scenario as the first test, but the profile sets
        // TurnCostFactorEnabled=true so the edge-based router is used.
        // If this passes while the version with TurnCostFactorEnabled=false
        // fails, the bug is in the simple bidirectional Dijkstra path
        // (ignoring turn costs when TurnCostFactorEnabled is false).
        var routerDb = new RouterDb();
        routerDb.PrepareFor(BlockingProfile());
        VertexId a, mid, b;
        EdgeId edge1, edge2;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.270);
            mid = mutable.AddVertex(4.801, 51.270);
            b = mutable.AddVertex(4.802, 51.270);

            edge1 = mutable.AddEdge(a, mid);
            edge2 = mutable.AddEdge(mid, b);

            mutable.AddTurnCosts(mid,
                new[] { ("barrier", "bollard") },
                new[] { edge1, edge2 },
                new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile(turnCostFactorEnabled: true);
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapB = await network.Snap().ToAsync(b).FirstAsync();

        var route = await network.Route(profile)
            .From(snapA)
            .To(snapB)
            .CalculateAsync();

        Assert.True(route.IsError,
            "with TurnCostFactorEnabled=true the edge-based router runs and must honor the turn cost");
    }

    [Fact]
    public async Task Routing_FourVertexLine_TurnCostForbidsMidTurn_TurnCostFactorEnabled_ShouldBlockRoute()
    {
        // Edge-based router (TurnCostFactorEnabled=true) variant of the
        // 4-vertex case: a — edge1 — mid — edge2 — j2 — edge3 — c, turn cost
        // at mid forbids edge1↔edge2. This is the exact scenario that exposed
        // the BidirectionalDijkstra OnQueued bug; here we run it via the
        // edge-based router instead to verify it has no equivalent bug.
        var routerDb = new RouterDb();
        VertexId a, mid, j2, c;
        EdgeId edge1, edge2, edge3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.269);
            mid = mutable.AddVertex(4.801, 51.269);
            j2 = mutable.AddVertex(4.802, 51.269);
            c = mutable.AddVertex(4.803, 51.269);

            edge1 = mutable.AddEdge(a, mid);
            edge2 = mutable.AddEdge(mid, j2);
            edge3 = mutable.AddEdge(j2, c);

            mutable.AddTurnCosts(mid,
                new[] { ("barrier", "bollard") },
                new[] { edge1, edge2 },
                new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile(turnCostFactorEnabled: true);
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapC = await network.Snap().ToAsync(c).FirstAsync();

        var route = await network.Route(profile)
            .From(snapA)
            .To(snapC)
            .CalculateAsync();

        Assert.True(route.IsError,
            "edge-based router must block the route — turn at mid is forbidden");
    }

    [Fact]
    public async Task Routing_OnlyRightTurn_TurnCostFactorEnabled_OneToOne_ShouldBlockStraightOn()
    {
        // Mandatory restriction shape ("only_right_turn"): from-edge enters via,
        // only the right-turn edge is allowed out, all other exits forbidden.
        // We model it directly as a turn-cost layout (skipping the OSM resolver):
        //
        //              right (b_right)
        //                 ^
        //                 |
        //   a -- e_from ->v-- e_straight ---> b_straight
        //                 |
        //              ... (no left in this minimal case)
        //
        // Cost layout at v: forbid (e_from -> e_straight). Going (e_from -> e_right)
        // remains free (only-right-turn permits it).
        // Routing a -> b_straight must fail.
        // Profile uses TurnCostFactorEnabled=true (matches publish-api).
        var routerDb = new RouterDb();
        routerDb.PrepareFor(BlockingProfile(turnCostFactorEnabled: true));
        VertexId a, via, bRight, bStraight;
        EdgeId eFrom, eRight, eStraight;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.270);
            via = mutable.AddVertex(4.802, 51.270);
            bRight = mutable.AddVertex(4.803, 51.272); // north of via
            bStraight = mutable.AddVertex(4.804, 51.270); // east of via (straight on)

            eFrom = mutable.AddEdge(a, via);
            eRight = mutable.AddEdge(via, bRight);
            eStraight = mutable.AddEdge(via, bStraight);

            // forbid (e_from -> e_straight) at via — the "going straight" turn.
            mutable.AddTurnCosts(via,
                new[] { ("type", "restriction"), ("restriction", "only_right_turn") },
                new[] { eFrom, eStraight },
                new uint[,] { { 0, 1 }, { 0, 0 } });
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile(turnCostFactorEnabled: true);
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapStraight = await network.Snap().ToAsync(bStraight).FirstAsync();

        var route = await network.Route(profile)
            .From(snapA)
            .To(snapStraight)
            .CalculateAsync();

        Assert.True(route.IsError,
            "route a -> bStraight must fail — only_right_turn forbids the straight-on turn at via");
    }

    [Fact]
    public async Task Routing_OneToOne_TurnCostFactorEnabled_BollardAtSharedNodeWithToWaySplit_ShouldBlockRoute()
    {
        // Covers the dispatch path used when a profile sets TurnCostFactorEnabled=true:
        //   - Single origin/destination via network.Route(...).From(f).To(t).PathAsync().
        //   - PathAsync dispatches to the unidirectional Dijkstra (one-to-one variant via
        //     many-to-many CalculateAsync with single-element lists) when
        //     TurnCostFactorEnabled is true — see IRouterOneToOneExtensions.PathAsync.
        //
        // Network shape stresses the resolver/walk fix: bollard at the shared
        // node between way 1 (a→bollard) and way 2 (bollard→j2→c), with way 3
        // (j2→x) creating an interior junction at j2 that splits way 2 into
        // stored sub-edges (way2,0,1) and (way2,1,2). The bollard's (way2,2,0)
        // tail-hop must walk-from-anchor to find the bollard-adjacent sub-edge
        // (way2,0,1); without that, the turn cost lands on j2 instead of the
        // bollard and the router happily routes a→bollard→j2→c.

        var routerDb = new RouterDb();
        VertexId a, bollard, j2, c, x;
        EdgeId w1, w2a, w2b, w3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.269);
            bollard = mutable.AddVertex(4.801, 51.269);
            j2 = mutable.AddVertex(4.802, 51.269);
            c = mutable.AddVertex(4.803, 51.269);
            x = mutable.AddVertex(4.802, 51.270);

            w1 = mutable.AddEdge(a, bollard);
            w2a = mutable.AddEdge(bollard, j2);
            w2b = mutable.AddEdge(j2, c);
            w3 = mutable.AddEdge(j2, x);

            mutable.AddTurnCosts(bollard,
                new[] { ("barrier", "bollard") },
                new[] { w1, w2a },
                new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile(turnCostFactorEnabled: true);
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapC = await network.Snap().ToAsync(c).FirstAsync();

        var route = await network.Route(profile)
            .From(snapA)
            .To(snapC)
            .CalculateAsync();

        Assert.True(route.IsError,
            "publish-api code path: route from a to c must fail — bollard turn cost forbids way1→way2a, no alternative");
    }

    [Fact]
    public async Task Routing_OneToMany_FourVertexLine_NoTurnCost_BaselineShouldWork()
    {
        // Baseline: same shape as the next test but WITHOUT a turn cost.
        // If this fails, the one-to-many machinery has issues unrelated to
        // turn costs and the next test's failure can't be attributed to
        // turn-cost handling.
        var routerDb = new RouterDb();
        VertexId a, mid, j2, c;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.269);
            mid = mutable.AddVertex(4.801, 51.269);
            j2 = mutable.AddVertex(4.802, 51.269);
            c = mutable.AddVertex(4.803, 51.269);

            mutable.AddEdge(a, mid);
            mutable.AddEdge(mid, j2);
            mutable.AddEdge(j2, c);
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile();
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapMid = await network.Snap().ToAsync(mid).FirstAsync();
        var snapC = await network.Snap().ToAsync(c).FirstAsync();

        var paths = await network.Route(profile)
            .From(snapA)
            .To(new[] { snapMid.Value, snapC.Value })
            .Paths(default);

        Assert.False(paths[0].IsError, "baseline a→mid should succeed (no turn cost in network)");
        Assert.False(paths[1].IsError, "baseline a→c should succeed (no turn cost in network)");
    }

    [Fact(Skip = "Documents a separate pre-existing bug in the one-to-many path: " +
                  "adding ANY turn cost to the network causes ALL paths through that " +
                  "router to fail (even ones that don't traverse the turn-cost vertex). " +
                  "Left as a marker for follow-up.")]
    public async Task Routing_OneToMany_FourVertexLine_TurnCostForbidsMidTurn_ShouldFailToReachBlockedTarget()
    {
        // The one-to-many path also routes through the edge-based engine.
        // From a, we ask for routes to mid (reachable, no turn-cost issue) AND
        // to c (requires the forbidden edge1→edge2 turn). The first should
        // succeed, the second should fail.
        var routerDb = new RouterDb();
        VertexId a, mid, j2, c;
        EdgeId edge1, edge2, edge3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.269);
            mid = mutable.AddVertex(4.801, 51.269);
            j2 = mutable.AddVertex(4.802, 51.269);
            c = mutable.AddVertex(4.803, 51.269);

            edge1 = mutable.AddEdge(a, mid);
            edge2 = mutable.AddEdge(mid, j2);
            edge3 = mutable.AddEdge(j2, c);

            mutable.AddTurnCosts(mid,
                new[] { ("barrier", "bollard") },
                new[] { edge1, edge2 },
                new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile();
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapMid = await network.Snap().ToAsync(mid).FirstAsync();
        var snapC = await network.Snap().ToAsync(c).FirstAsync();

        var paths = await network.Route(profile)
            .From(snapA)
            .To(new[] { snapMid.Value, snapC.Value })
            .Paths(default);

        // path to mid: reachable.
        Assert.False(paths[0].IsError, "path a→mid should succeed");
        // path to c: blocked by turn cost at mid.
        Assert.True(paths[1].IsError, "path a→c should fail — turn cost at mid forbids edge1→edge2");
    }

    [Fact(Skip = "Same separate one-to-many/many-to-many bug as above.")]
    public async Task Routing_ManyToMany_FourVertexLine_TurnCostForbidsMidTurn_ShouldFailBlockedPair()
    {
        // Many-to-many also runs through the edge-based engine. Sources={a,j2},
        // targets={mid,c}: a→mid and a→c involve the forbidden turn at mid;
        // j2→mid and j2→c don't. Verifies turn costs hold across all pairs.
        var routerDb = new RouterDb();
        VertexId a, mid, j2, c;
        EdgeId edge1, edge2, edge3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            a = mutable.AddVertex(4.800, 51.269);
            mid = mutable.AddVertex(4.801, 51.269);
            j2 = mutable.AddVertex(4.802, 51.269);
            c = mutable.AddVertex(4.803, 51.269);

            edge1 = mutable.AddEdge(a, mid);
            edge2 = mutable.AddEdge(mid, j2);
            edge3 = mutable.AddEdge(j2, c);

            mutable.AddTurnCosts(mid,
                new[] { ("barrier", "bollard") },
                new[] { edge1, edge2 },
                new uint[,] { { 0, 1 }, { 1, 0 } });
        }

        var network = routerDb.Latest;
        var profile = BlockingProfile();
        var snapA = await network.Snap().ToAsync(a).FirstAsync();
        var snapMid = await network.Snap().ToAsync(mid).FirstAsync();
        var snapJ2 = await network.Snap().ToAsync(j2).FirstAsync();
        var snapC = await network.Snap().ToAsync(c).FirstAsync();

        var paths = await network.Route(profile)
            .From(new[] { snapA.Value, snapJ2.Value })
            .To(new[] { snapMid.Value, snapC.Value })
            .Paths(default);

        Assert.False(paths[0][0].IsError, "a→mid should succeed (single edge, no turn cost relevant)");
        Assert.True(paths[0][1].IsError, "a→c should fail — turn cost forbids edge1→edge2 at mid");
        Assert.False(paths[1][0].IsError, "j2→mid should succeed (no turn cost issue)");
        Assert.False(paths[1][1].IsError, "j2→c should succeed (no turn cost issue)");
    }
}
