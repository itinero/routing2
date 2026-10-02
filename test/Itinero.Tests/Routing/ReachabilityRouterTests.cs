using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero;
using Itinero.Network;
using Itinero.Network.Search.Reachability;
using Itinero.Profiles;
using Itinero.Routing;
using Xunit;

namespace Itinero.Tests.Routing;

/// <summary>
/// The driver that snaps and routes as one operation, reusing each endpoint's expansion as the
/// search half that continues from it.
/// </summary>
/// <remarks>
/// Hand-built networks, so these pin the wiring only. Whether bounded expansion agrees with
/// island detection on real data is a corpus question.
/// </remarks>
public class ReachabilityRouterTests
{
    /// A profile that can use everything at a constant speed, with no turn costs.
    private static Profile AnythingGoes() => new DefaultProfile();

    private sealed class DefaultProfile : Profile
    {
        public override string Name => "test";

        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
            => new(1, 1, 1, 1);

        public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
            => Itinero.Profiles.TurnCostFactor.Empty;
    }

    private static RoutingSettings SettingsFor(Profile profile) =>
        new() { Profile = profile };

    [Fact]
    public async Task ConnectedNetwork_RoutesBetweenTwoLocations()
    {
        // A chain long enough that an expansion from either end clears a small ceiling, so both
        // endpoints are accepted and the halves meet in the middle.
        var routerDb = new RouterDb();
        var verts = new VertexId[40];
        using (var writer = routerDb.GetMutableNetwork())
        {
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i] = writer.AddVertex(4.79 + (i * 0.0005), 51.26);
            }

            for (var i = 0; i < verts.Length - 1; i++) writer.AddEdge(verts[i], verts[i + 1]);
        }

        var profile = AnythingGoes();
        var result = await routerDb.Latest.PathAsync(SettingsFor(profile),
            (4.79, 51.26), (4.79 + (39 * 0.0005), 51.26),
            new ReachabilityBounds(10), searchBoxMeters: 200);

        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);
        Assert.NotEmpty(result.Value.Path.Select(x => x.edge).ToList());
    }

    [Fact]
    public async Task BothEndpointsInsideEachOthersExpansion_StillMeet()
    {
        // Why the reconciliation pass exists: each expansion settles the other endpoint on its
        // way, with no partner to check against, so adoption must reconcile the settled sets.
        var routerDb = new RouterDb();
        var verts = new VertexId[40];
        using (var writer = routerDb.GetMutableNetwork())
        {
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i] = writer.AddVertex(4.79 + (i * 0.0005), 51.26);
            }

            for (var i = 0; i < verts.Length - 1; i++) writer.AddEdge(verts[i], verts[i + 1]);
        }

        var profile = AnythingGoes();
        var result = await routerDb.Latest.PathAsync(SettingsFor(profile),
            (4.79, 51.26), (4.79 + (3 * 0.0005), 51.26),
            new ReachabilityBounds(10), searchBoxMeters: 200);

        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);
    }

    [Fact]
    public async Task IslandCandidate_IsPassedOverForTheRoadBehindIt()
    {
        // A dead-end fragment is nearest; the expansion from it runs out below the ceiling and
        // the next candidate is tried. Same outcome as classification, without classifying.
        var routerDb = new RouterDb();
        using (var writer = routerDb.GetMutableNetwork())
        {
            // The fragment: two edges going nowhere, right next to the query point.
            var f0 = writer.AddVertex(4.7900, 51.2600);
            var f1 = writer.AddVertex(4.7901, 51.2600);
            var f2 = writer.AddVertex(4.7902, 51.2600);
            writer.AddEdge(f0, f1);
            writer.AddEdge(f1, f2);

            // The through road, slightly further south and long enough to clear the ceiling.
            var previous = writer.AddVertex(4.7890, 51.2590);
            for (var i = 1; i < 40; i++)
            {
                var next = writer.AddVertex(4.7890 + (i * 0.0005), 51.2590);
                writer.AddEdge(previous, next);
                previous = next;
            }
        }

        var profile = AnythingGoes();
        var result = await routerDb.Latest.PathAsync(SettingsFor(profile),
            (4.7901, 51.2600), (4.7890 + (39 * 0.0005), 51.2590),
            new ReachabilityBounds(10), searchBoxMeters: 500);

        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);
    }

    [Fact]
    public async Task EverythingReachableIsAnIsland_ReportsNoSnap()
    {
        // Nothing within reach leads anywhere. Reported at the endpoint that caused it, from a
        // bounded walk, rather than after the router exhausts its tile budget.
        var routerDb = new RouterDb();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var a = writer.AddVertex(4.7900, 51.2600);
            var b = writer.AddVertex(4.7901, 51.2600);
            writer.AddEdge(a, b);

            var c = writer.AddVertex(4.8900, 51.3600);
            var d = writer.AddVertex(4.8901, 51.3600);
            writer.AddEdge(c, d);
        }

        var profile = AnythingGoes();
        var result = await routerDb.Latest.PathAsync(SettingsFor(profile),
            (4.7900, 51.2600), (4.8900, 51.3600),
            new ReachabilityBounds(50), searchBoxMeters: 200);

        Assert.True(result.IsError);
        Assert.Contains("connected network", result.ErrorMessage);
    }

    /// A profile where every edge is access=destination, i.e. one big local-access pocket.
    private sealed class AllLocalProfile : Profile
    {
        public override string Name => "test-local";

        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
            => new(1, 1, 1, 1, canStop: true, isLocalAccess: true);

        public override TurnCostFactor TurnCostFactor(IEnumerable<(string key, string value)> attributes)
            => Itinero.Profiles.TurnCostFactor.Empty;
    }

    [Fact]
    public async Task RouteEntirelyInsideOneLocalPocket_IsFound()
    {
        // Both endpoints in the same access=destination pocket, and the pocket is closed — far
        // smaller than the threshold. It routes because the halves meet before anything asks
        // whether the pocket is connected to anything; L to L is never restricted.
        var routerDb = new RouterDb();
        var verts = new VertexId[12];
        using (var writer = routerDb.GetMutableNetwork())
        {
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i] = writer.AddVertex(4.79 + (i * 0.0005), 51.26);
            }

            for (var i = 0; i < verts.Length - 1; i++) writer.AddEdge(verts[i], verts[i + 1]);
        }

        var profile = new AllLocalProfile();
        var result = await routerDb.Latest.PathAsync(SettingsFor(profile),
            (4.7901, 51.2600), (4.79 + (10 * 0.0005), 51.2600),
            new ReachabilityBounds(8192), searchBoxMeters: 500);

        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);
    }
}
