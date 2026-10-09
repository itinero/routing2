using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Search.Reachability;
using Itinero.Profiles;
using Itinero.Routing;
using Itinero.Routing.Corridors;
using Xunit;

namespace Itinero.Tests.Routing.Corridors;

/// <summary>
/// The cost ellipse around a route, extracted from the router's own bidirectional search.
/// </summary>
/// <remarks>
/// Hand-built networks with one unambiguous optimum and one priced detour, so the corridor is
/// known in advance. A misprice has to change which edges survive, not merely what they cost.
/// </remarks>
public class RouteCorridorSearchTests
{
    /// <summary>Everything passable at a constant cost per centimetre, no turn costs.</summary>
    private class FlatProfile : Profile
    {
        public override string Name => "flat";

        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
            => new(1, 1, 1, 1);

        public override Itinero.Profiles.TurnCostFactor TurnCostFactor(
            IEnumerable<(string key, string value)> attributes)
            => Itinero.Profiles.TurnCostFactor.Empty;
    }

    /// <summary>The same, with a goal-direction bound that is exact here.</summary>
    private sealed class FlatProfileWithBound : FlatProfile
    {
        public override string Name => "flat-bound";

        public override uint MinFactor => 1;
    }

    /// <summary>
    /// A chain west, two ways across the middle, then a chain east: optimum ~244 m, detour ~787 m.
    /// </summary>
    /// <remarks>
    /// One shaped edge rather than a pair through a northern vertex: that vertex lands in another
    /// tile, and a cross-tile edge hands back no id to assert on.
    /// </remarks>
    private static (RouterDb db, (double, double) origin, (double, double) destination,
        EdgeId cheap1, EdgeId cheap2, EdgeId detour) Diamond()
    {
        const double lon = 4.79;
        const double lat = 51.26;
        var db = new RouterDb();
        EdgeId cheap1, cheap2, detour;
        using (var writer = db.GetMutableNetwork())
        {
            var w0 = writer.AddVertex(lon, lat);
            var w1 = writer.AddVertex(lon + 0.0005, lat);
            var w2 = writer.AddVertex(lon + 0.0010, lat);
            var split = writer.AddVertex(lon + 0.0015, lat);
            var mid = writer.AddVertex(lon + 0.0025, lat);
            var join = writer.AddVertex(lon + 0.0035, lat);
            var e1 = writer.AddVertex(lon + 0.0040, lat);
            var e2 = writer.AddVertex(lon + 0.0045, lat);
            var e3 = writer.AddVertex(lon + 0.0050, lat);

            writer.AddEdge(w0, w1);
            writer.AddEdge(w1, w2);
            writer.AddEdge(w2, split);
            cheap1 = writer.AddEdge(split, mid);     // ~70 m
            cheap2 = writer.AddEdge(mid, join);      // ~70 m
            detour = writer.AddEdge(split, join,     // ~682 m the long way round
                shape: [(lon + 0.0025, lat + 0.003, (float?)null)]);
            writer.AddEdge(join, e1);
            writer.AddEdge(e1, e2);
            writer.AddEdge(e2, e3);
        }

        return (db, (lon + 0.00075, lat), (lon + 0.00425, lat), cheap1, cheap2, detour);
    }

    private static Task<Result<(ResolvedRoute Route, RouteCorridor Corridor)>> Search(
        RouterDb db, Profile profile, (double, double) origin, (double, double) destination,
        double costFactor)
        => db.Latest.RouteCorridorAsync(new RoutingSettings { Profile = profile },
            origin, destination, new ReachabilityBounds(10), searchBoxMeters: 200, costFactor);

    private static async Task<RouteCorridor> Corridor(
        RouterDb db, Profile profile, (double, double) origin, (double, double) destination,
        double costFactor)
    {
        var result = await Search(db, profile, origin, destination, costFactor);
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);

        return result.Value.Corridor;
    }

    [Fact]
    public async Task BudgetCoveringTheDetour_AdmitsBothBranches()
    {
        var (db, origin, destination, cheap1, cheap2, detour) = Diamond();

        var corridor = await Corridor(db, new FlatProfile(), origin, destination, 3.5);
        var edges = corridor.Edges.Select(x => x.Edge).ToHashSet();
        Assert.Contains(cheap1, edges);
        Assert.Contains(cheap2, edges);
        Assert.Contains(detour, edges);
    }

    [Fact]
    public async Task BudgetBelowTheDetour_ExcludesIt()
    {
        // The detour adds ~543 m to a ~244 m route, so 1.5x cannot reach it. Keeping it would
        // mean the filter bounds nothing.
        var (db, origin, destination, cheap1, cheap2, detour) = Diamond();

        var corridor = await Corridor(db, new FlatProfile(), origin, destination, 1.5);
        var edges = corridor.Edges.Select(x => x.Edge).ToHashSet();
        Assert.Contains(cheap1, edges);
        Assert.Contains(cheap2, edges);
        Assert.DoesNotContain(detour, edges);
    }

    [Fact]
    public async Task EveryTransitionIsPricedTheSameWayRoundAsThePath()
    {
        // A transition costs the edge entered plus the turn onto it. Taking the backward half's
        // roles as they come charges the edge being left: 70 m where it should be 682 m.
        var (db, origin, destination, cheap1, cheap2, detour) = Diamond();
        var profile = new FlatProfile();

        var merged = await Search(db, profile, origin, destination, 3.5);
        Assert.False(merged.IsError, merged.IsError ? merged.ErrorMessage : null);
        var corridor = merged.Value.Corridor;

        // Cost is one unit per centimetre under this profile and Length is reported in metres, so
        // a transition entering an edge costs a hundred times that edge's length and nothing else.
        foreach (var edge in corridor.Edges)
        {
            if (edge.Half != CorridorHalf.None) continue;

            Assert.Equal(edge.Length * 100, edge.Cost, 3);
        }

        var byEdge = corridor.Edges.Where(x => x.Half == CorridorHalf.None)
            .GroupBy(x => x.Edge).ToDictionary(g => g.Key, g => g.First().Cost);
        Assert.True(byEdge[detour] > 5 * byEdge[cheap1],
            $"the detour priced at {byEdge[detour]} against {byEdge[cheap1]} for a tenth of its length");
        Assert.Contains(cheap2, byEdge.Keys);
    }

    [Fact]
    public async Task AtTheOptimum_CorridorIsExactlyTheRoute()
    {
        // At 1.0 only optimal paths are admitted, so the corridor must collapse onto the route.
        // Anything mispriced below its true cost lands inside the budget and shows up here.
        var (db, origin, destination, _, _, _) = Diamond();
        var profile = new FlatProfile();

        var searched = await Search(db, profile, origin, destination, 1.0);
        Assert.False(searched.IsError, searched.IsError ? searched.ErrorMessage : null);

        Assert.Equal(
            searched.Value.Route.Path.Select(x => x.edge).ToHashSet(),
            searched.Value.Corridor.Edges.Select(x => x.Edge).ToHashSet());
    }

    [Fact]
    public async Task EveryEdgeLiesOnAnOriginToDestinationPath()
    {
        // Each half may only enter the pocket it started in, so their union can price an edge
        // inside the budget with no way onward. The trim removes those; nothing else would.
        var (db, origin, destination, _, _, _) = Diamond();

        var merged = await Search(db, new FlatProfile(), origin, destination, 3.5);
        Assert.False(merged.IsError, merged.IsError ? merged.ErrorMessage : null);
        var corridor = merged.Value.Corridor;

        var fromOrigin = Reachable(corridor, RouteCorridor.SourceNode, forwards: true);
        var toDestination = Reachable(corridor, RouteCorridor.TargetNode, forwards: false);
        foreach (var edge in corridor.Edges)
        {
            Assert.Contains(edge.Tail, fromOrigin);
            Assert.Contains(edge.Head, toDestination);
        }
    }

    [Fact]
    public async Task GoalDirection_DoesNotChangeTheCorridor()
    {
        // The stopping rule compares the sum of both frontier keys against the budget. Were
        // that unsound for a lifted ceiling, the corridor would quietly shrink.
        var (db, origin, destination, _, _, _) = Diamond();

        var without = await Search(db, new FlatProfile(), origin, destination, 3.5);
        var with = await Search(db, new FlatProfileWithBound(), origin, destination, 3.5);
        Assert.False(without.IsError, without.IsError ? without.ErrorMessage : null);
        Assert.False(with.IsError, with.IsError ? with.ErrorMessage : null);

        Assert.Equal(
            without.Value.Corridor.Edges.Select(x => x.Edge).ToHashSet(),
            with.Value.Corridor.Edges.Select(x => x.Edge).ToHashSet());
    }

    [Fact]
    public async Task SameEdgeEndpoints_YieldAnEmptyCorridor()
    {
        // Both endpoints on one edge: every join is a U-turn. Empty with stage counts, not an
        // error, so the caller can tell it from a destination it could not reach.
        const double lon = 4.79;
        const double lat = 51.26;
        var db = new RouterDb();
        using (var writer = db.GetMutableNetwork())
        {
            var a = writer.AddVertex(lon, lat);
            var b = writer.AddVertex(lon + 0.002, lat);
            var c = writer.AddVertex(lon + 0.004, lat);
            writer.AddEdge(a, b);
            writer.AddEdge(b, c);
        }

        var merged = await Search(db, new FlatProfile(),
            (lon + 0.0005, lat), (lon + 0.0015, lat), 1.5);
        Assert.False(merged.IsError, merged.IsError ? merged.ErrorMessage : null);
        Assert.Empty(merged.Value.Corridor.Edges);
        Assert.Equal(0, merged.Value.Corridor.ToDestination);
    }

    [Fact]
    public async Task CostFactorBelowOne_Throws()
    {
        var (db, origin, destination, _, _, _) = Diamond();

        await Assert.ThrowsAsync<System.ArgumentOutOfRangeException>(() =>
            Search(db, new FlatProfile(), origin, destination, 0.9));
    }

    private static HashSet<int> Reachable(RouteCorridor corridor, int start, bool forwards)
    {
        var seen = new HashSet<int> { start };
        var stack = new Stack<int>([start]);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            foreach (var edge in corridor.Edges)
            {
                var (from, to) = forwards ? (edge.Tail, edge.Head) : (edge.Head, edge.Tail);
                if (from != node) continue;
                if (seen.Add(to)) stack.Push(to);
            }
        }

        return seen;
    }

    [Fact]
    public async Task ALocalDetourBound_KeepsAnAbsurdSpurOutOfTheCorridorItself()
    {
        // ~682 m replacing ~140 m: the trip barely moves, so a 3.5x budget passes it while
        // locally it is nonsense. Per edge, so a flow model with no single path can use it.
        var (db, origin, destination, cheap1, cheap2, detour) = Diamond();

        var unbounded = await Corridor(db, new FlatProfile(), origin, destination, 3.5);
        var bounded = await CorridorWithDetourBound(db, new FlatProfile(), origin, destination,
            3.5, maxLocalDetour: 2.0);

        Assert.Contains(detour, unbounded.Edges.Select(x => x.Edge));
        Assert.DoesNotContain(detour, bounded.Edges.Select(x => x.Edge));

        // The route itself has to survive, or the bound has eaten the answer along with the spur.
        Assert.Contains(cheap1, bounded.Edges.Select(x => x.Edge));
        Assert.Contains(cheap2, bounded.Edges.Select(x => x.Edge));
    }

    [Fact]
    public async Task TheDetourBound_LeavesTheDistanceFieldsExact()
    {
        // Pruning invalidates both fields, which are published as exact, so they are recomputed.
        // Every surviving edge must still satisfy the filter's identity.
        var (db, origin, destination, _, _, _) = Diamond();

        var bounded = await CorridorWithDetourBound(db, new FlatProfile(), origin, destination,
            3.5, maxLocalDetour: 2.0);

        foreach (var edge in bounded.Edges)
        {
            var before = bounded.CostFromOrigin[edge.Tail];
            var after = bounded.CostToDestination[edge.Head];
            Assert.False(double.IsPositiveInfinity(before), "a kept edge is unreachable forwards");
            Assert.False(double.IsPositiveInfinity(after), "a kept edge reaches nothing");
            Assert.True(before + edge.Cost + after <= bounded.OptimalCost * 3.5 + 1,
                $"kept edge priced {before + edge.Cost + after} over a budget of {bounded.OptimalCost * 3.5}");
        }
    }

    [Fact]
    public async Task ADetourBoundBelowOne_Throws()
    {
        var (db, origin, destination, _, _, _) = Diamond();

        await Assert.ThrowsAsync<System.ArgumentOutOfRangeException>(() =>
            db.Latest.RouteCorridorAsync(new RoutingSettings { Profile = new FlatProfile() },
                origin, destination, new ReachabilityBounds(10), 200, 3.5, default,
                maxLocalDetour: 0.5));
    }

    private static async Task<RouteCorridor> CorridorWithDetourBound(
        RouterDb db, Profile profile, (double, double) origin, (double, double) destination,
        double costFactor, double maxLocalDetour)
    {
        var result = await db.Latest.RouteCorridorAsync(new RoutingSettings { Profile = profile },
            origin, destination, new ReachabilityBounds(10), searchBoxMeters: 200, costFactor,
            default, maxLocalDetour);
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);

        return result.Value.Corridor;
    }
}
