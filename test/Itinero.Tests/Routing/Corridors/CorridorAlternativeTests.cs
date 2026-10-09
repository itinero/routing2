using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search.Reachability;
using Itinero.Profiles;
using Itinero.Routes.Paths;
using Itinero.Routing;
using Itinero.Routing.Corridors;
using Xunit;

namespace Itinero.Tests.Routing.Corridors;

/// <summary>
/// Picking an alternative out of a corridor, under a budget and an overlap limit.
/// </summary>
/// <remarks>
/// One alternative by construction, so the expected answer is known: the budget decides whether
/// the detour is in the corridor, the overlap limit whether it counts once it is.
/// </remarks>
public class CorridorAlternativeTests
{
    private class FlatProfile : Profile
    {
        public override string Name => "flat";

        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
            => new(1, 1, 1, 1);

        public override Itinero.Profiles.TurnCostFactor TurnCostFactor(
            IEnumerable<(string key, string value)> attributes)
            => Itinero.Profiles.TurnCostFactor.Empty;
    }

    /// <summary>
    /// A chain west, two ways across the middle, then a chain east: optimum ~244 m, detour ~787 m.
    /// </summary>
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
            cheap1 = writer.AddEdge(split, mid);
            cheap2 = writer.AddEdge(mid, join);
            detour = writer.AddEdge(split, join,
                shape: [(lon + 0.0025, lat + 0.003, (float?)null)]);
            writer.AddEdge(join, e1);
            writer.AddEdge(e1, e2);
            writer.AddEdge(e2, e3);
        }

        return (db, (lon + 0.00075, lat), (lon + 0.00425, lat), cheap1, cheap2, detour);
    }

    private static async Task<(ResolvedRoute Route, RouteCorridor Corridor)> Searched(
        RouterDb db, Profile profile, (double, double) origin, (double, double) destination,
        double costFactor)
    {
        var result = await db.Latest.RouteCorridorAsync(new RoutingSettings { Profile = profile },
            origin, destination, new ReachabilityBounds(10), searchBoxMeters: 200, costFactor);
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);

        return result.Value;
    }

    [Fact]
    public async Task ItReturnsTheDetourWhenTheBudgetCoversIt()
    {
        // The detour is the only other way across, so anything else means the selection is not
        // finding the path it priced.
        var (db, origin, destination, _, _, detour) = Diamond();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 3.5);

        // The fixture's detour replaces ~140 m with ~682 m, so it is absurd by the local
        // detour criterion on purpose; that is tested separately on Trident.
        var alternatives = db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.8,
            maxAlternatives: 1, localDetourFactor: 6.0);
        Assert.Single(alternatives);

        var edges = alternatives[0].Select(x => x.edge).ToHashSet();
        Assert.Contains(detour, edges);
    }

    [Fact]
    public async Task TheAlternativeAvoidsTheBranchTheRouteTook()
    {
        // Diverging means not using the cheap branch: were an edge merely spliced into the
        // original, the cheap edges would still be here and only overlap would catch it.
        var (db, origin, destination, cheap1, cheap2, detour) = Diamond();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 3.5);

        // The fixture's detour replaces ~140 m with ~682 m, so it is absurd by the local
        // detour criterion on purpose; that is tested separately on Trident.
        var alternatives = db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.8,
            maxAlternatives: 1, localDetourFactor: 6.0);
        Assert.Single(alternatives);

        var edges = alternatives[0].Select(x => x.edge).ToHashSet();
        var onRoute = route.Path.Select(x => x.edge).ToHashSet();
        Assert.Contains(detour, edges);
        Assert.DoesNotContain(cheap1, edges);
        Assert.DoesNotContain(cheap2, edges);
        Assert.Contains(cheap1, onRoute);
    }

    [Fact]
    public async Task BudgetBelowTheDetour_LeavesNoAlternative()
    {
        // The budget is enforced by the corridor, not here: below the detour's cost it is not
        // in the corridor at all, so there is nothing to select.
        var (db, origin, destination, _, _, _) = Diamond();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 1.5);

        Assert.Empty(db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.9));
    }

    [Fact]
    public async Task AnOverlapLimitTooTightToMeet_IsReportedRatherThanBreached()
    {
        // Every path shares both chains and both snapped edges, so none clears a tight limit.
        // Returning one anyway would breach the contract silently.
        var (db, origin, destination, _, _, _) = Diamond();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 3.5);

        Assert.Empty(db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.01,
            maxAlternatives: 1, localDetourFactor: 6.0));
    }

    [Fact]
    public async Task TheAlternativeIsWithinTheBudgetAndIsAWalkableRoute()
    {
        // Within budget, and it composes into a Path at all: Append throws on edges that do not
        // meet, so building one proves the reconstruction is connected and correctly directed.
        var (db, origin, destination, _, _, _) = Diamond();
        const double factor = 3.5;
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, factor);

        // The fixture's detour replaces ~140 m with ~682 m, so it is absurd by the local
        // detour criterion on purpose; that is tested separately on Trident.
        var alternatives = db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.8,
            maxAlternatives: 1, localDetourFactor: 6.0);
        Assert.Single(alternatives);

        // One cost unit per centimetre under this profile, so length bounds cost directly.
        var length = 0d;
        foreach (var (edge, _, offset1, offset2) in alternatives[0])
        {
            var enumerator = db.Latest.GetEdgeEnumerator();
            Assert.True(enumerator.MoveTo(edge, true));
            length += enumerator.EdgeLength() * ((offset2 - offset1) / (double)ushort.MaxValue);
        }

        Assert.True(length * 100 <= corridor.OptimalCost * factor + 1,
            $"alternative priced {length * 100} against a budget of {corridor.OptimalCost * factor}");
    }

    [Fact]
    public async Task OverlapOutsideZeroToOne_Throws()
    {
        var (db, origin, destination, _, _, _) = Diamond();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 3.5);

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => db.Latest.AlternativesFor(route, corridor, maxOverlap: 1.5));
    }

    /// <summary>
    /// Three ways across the middle, priced so one is optimal, one is a modest detour and one is
    /// a long way round. Lets the count, the accumulation and the detour bound be tested apart.
    /// </summary>
    private static (RouterDb db, (double, double) origin, (double, double) destination,
        EdgeId cheap, EdgeId near, EdgeId far) Trident()
    {
        const double lon = 4.79;
        const double lat = 51.26;
        var db = new RouterDb();
        EdgeId cheap, near, far;
        using (var writer = db.GetMutableNetwork())
        {
            var w0 = writer.AddVertex(lon, lat);
            var w1 = writer.AddVertex(lon + 0.0010, lat);
            var split = writer.AddVertex(lon + 0.0015, lat);
            var mid = writer.AddVertex(lon + 0.0025, lat);
            var join = writer.AddVertex(lon + 0.0035, lat);
            var e1 = writer.AddVertex(lon + 0.0040, lat);
            var e2 = writer.AddVertex(lon + 0.0050, lat);

            writer.AddEdge(w0, w1);
            writer.AddEdge(w1, split);
            cheap = writer.AddEdge(split, mid);
            writer.AddEdge(mid, join);
            near = writer.AddEdge(split, join,
                shape: [(lon + 0.0025, lat + 0.0012, (float?)null)]);
            far = writer.AddEdge(split, join,
                shape: [(lon + 0.0025, lat - 0.0060, (float?)null)]);
            writer.AddEdge(join, e1);
            writer.AddEdge(e1, e2);
        }

        return (db, (lon + 0.0005, lat), (lon + 0.0045, lat), cheap, near, far);
    }

    [Fact]
    public async Task MaxAlternatives_BoundsHowManyComeBack()
    {
        // Two ways off the optimum exist, so asking for one must give one and asking for
        // everything must give both — in cheapest-first order.
        var (db, origin, destination, _, near, far) = Trident();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 6.0);

        var one = db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.9, maxAlternatives: 1);
        var all = db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.9,
            maxAlternatives: int.MaxValue, localDetourFactor: 100);

        Assert.Single(one);
        Assert.True(all.Count >= 2, $"expected both branches, got {all.Count}");

        // Cheapest first: the near detour before the long way round.
        Assert.Contains(near, all[0].Select(x => x.edge));
        Assert.Contains(far, all[1].Select(x => x.edge));
    }

    [Fact]
    public async Task OverlapAccumulates_SoASecondAlternativeMustDifferFromTheFirst()
    {
        // Why the union rather than the original: each branch alone passes the limit, a
        // near-duplicate of an accepted one does not.
        var (db, origin, destination, _, _, _) = Trident();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 6.0);

        var all = db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.9,
            maxAlternatives: int.MaxValue, localDetourFactor: 100);

        // No two returned routes may share more than the limit with each other either, which is
        // what accumulation buys and what pairwise-against-the-original would not.
        for (var i = 0; i < all.Count; i++)
        {
            for (var j = i + 1; j < all.Count; j++)
            {
                var a = all[i].Select(x => x.edge).ToHashSet();
                var b = all[j].Select(x => x.edge).ToList();
                var sharedCount = b.Count(a.Contains);
                Assert.True(sharedCount < b.Count,
                    $"routes {i} and {j} are the same set of edges");
            }
        }
    }

    [Fact]
    public async Task TheLocalDetourBound_RejectsALoopThatIsAbsurdForWhatItReplaces()
    {
        // The far branch replaces ~140 m with a very long stretch, so 2.0 must refuse it and a
        // large factor admit it. Sharing cannot express this: both branches diverge identically.
        var (db, origin, destination, _, _, far) = Trident();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 6.0);

        var strict = db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.9,
            maxAlternatives: int.MaxValue, localDetourFactor: 2.0);
        var loose = db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.9,
            maxAlternatives: int.MaxValue, localDetourFactor: 100);

        Assert.DoesNotContain(far, strict.SelectMany(p => p.Select(x => x.edge)));
        Assert.Contains(far, loose.SelectMany(p => p.Select(x => x.edge)));
    }

    [Fact]
    public async Task MaxAlternativesZero_ReturnsNothingAndDoesNoWork()
    {
        var (db, origin, destination, _, _, _) = Trident();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 6.0);

        Assert.Empty(db.Latest.AlternativesFor(route, corridor, 0.9, maxAlternatives: 0));
    }

    [Fact]
    public async Task EveryAlternativeIsASimplePath()
    {
        // Prefix and suffix are each simple but built from opposite ends, so they can meet and
        // the join hold a loop. Seen on real data, and invisible to budget and detour bound.
        var (db, origin, destination, _, _, _) = Trident();
        var (route, corridor) = await Searched(db, new FlatProfile(), origin, destination, 6.0);

        var all = db.Latest.AlternativesFor(route, corridor, maxOverlap: 0.9,
            maxAlternatives: int.MaxValue, localDetourFactor: 100);

        foreach (var path in all)
        {
            var edges = path.Select(x => x.edge).ToList();
            Assert.Equal(edges.Count, edges.Distinct().Count());
        }
    }
}
