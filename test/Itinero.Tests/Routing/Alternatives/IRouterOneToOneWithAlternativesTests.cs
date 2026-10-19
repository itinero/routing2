using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routing;
using Itinero.Routing.Alternatives;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Routing.Alternatives;

/// <summary>
/// Asking the fluent router for alternatives.
/// </summary>
/// <remarks>
/// The selection itself is covered by CorridorAlternativeTests; what is claimed here is that the
/// chain reaches it and that each setting arrives where it belongs.
/// </remarks>
public class IRouterOneToOneWithAlternativesTests
{
    private sealed class FlatProfile : Profile
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
    private static (RouterDb db, (double, double, float?) origin, (double, double, float?) destination,
        EdgeId detour) Diamond()
    {
        const double lon = 4.79;
        const double lat = 51.26;
        var provider = TestProvider.New();
        var db = provider.RouterDb;
        EdgeId detour;
        using (var writer = provider.BeginWrite())
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
            writer.AddEdge(split, mid);
            writer.AddEdge(mid, join);
            detour = writer.AddEdge(split, join,
                shape: [(lon + 0.0025, lat + 0.003, (float?)null)]);
            writer.AddEdge(join, e1);
            writer.AddEdge(e1, e2);
            writer.AddEdge(e2, e3);
        }

        return (db, (lon + 0.00075, lat, null), (lon + 0.00425, lat, null), detour);
    }

    /// <summary>
    /// The detour replaces ~140 m with ~682 m, so it only survives a deliberately loose bound.
    /// </summary>
    private static AlternativeRouteSettings Loose() => new()
    {
        MaxCostFactor = 3.5,
        MaxOverlap = 0.8,
        LocalDetourFactor = 6.0
    };

    private static async Task<(IRouterOneToOne Router, EdgeId Detour)> ChainAsync(
        AlternativeRouteSettings? _ = null)
    {
        var (db, origin, destination, detour) = Diamond();
        var network = db.Latest;
        var source = await network.Snap().ToAsync(origin);
        var target = await network.Snap().ToAsync(destination);

        return (network.Route(new RoutingSettings { Profile = new FlatProfile() })
            .From(source).To(target), detour);
    }

    [Fact]
    public async Task TheChainReturnsTheFastestRouteAndTheDetour()
    {
        var (chain, detour) = await ChainAsync();

        var result = await chain.WithAlternatives(Loose()).PathsAsync();
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);

        // The detour is the only other way across, so the count is known.
        Assert.Equal(2, result.Value.Count);
        Assert.DoesNotContain(detour, result.Value[0].Select(x => x.edge));
        Assert.Contains(detour, result.Value[1].Select(x => x.edge));
    }

    [Fact]
    public async Task TheCountIncludesTheFastestRoute()
    {
        var (chain, _) = await ChainAsync();

        var settings = Loose();
        settings.MaxNumberOfAlternativeRoutes = 1;

        var result = await chain.WithAlternatives(settings).PathsAsync();
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task TheDefaultDetourBound_RejectsADetourThisAbsurd()
    {
        var (chain, _) = await ChainAsync();

        var settings = Loose();
        settings.LocalDetourFactor = new AlternativeRouteSettings().LocalDetourFactor;

        var result = await chain.WithAlternatives(settings).PathsAsync();
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task ABudgetBelowTheDetour_LeavesNothingToChooseFrom()
    {
        var (chain, _) = await ChainAsync();

        var settings = Loose();
        settings.MaxCostFactor = 1.5;

        var result = await chain.WithAlternatives(settings).PathsAsync();
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task AnOverlapLimitTooTightToMeet_LeavesOnlyTheFastestRoute()
    {
        var (chain, _) = await ChainAsync();

        var settings = Loose();
        settings.MaxOverlap = 0.01;

        var result = await chain.WithAlternatives(settings).PathsAsync();
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task ADirectedEndpoint_IsReportedRatherThanIgnored()
    {
        var (db, origin, destination, _) = Diamond();
        var network = db.Latest;
        var source = await network.Snap().ToAsync(origin);
        var target = await network.Snap().ToAsync(destination);

        var result = await network.Route(new RoutingSettings { Profile = new FlatProfile() })
            .From((source.Value, (bool?)true)).To(target)
            .WithAlternatives(Loose()).PathsAsync();

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task EveryPathComesBackAsARoute()
    {
        var (chain, _) = await ChainAsync();

        var result = await chain.WithAlternatives(Loose()).CalculateAsync();
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, route => Assert.NotNull(route.Shape));
    }

    [Fact]
    public async Task TheFastestRouteIsTheOneTheRouterReturnsOnItsOwn()
    {
        var (chain, _) = await ChainAsync();

        var plain = await chain.PathAsync();
        var withAlternatives = await chain.WithAlternatives(Loose()).PathsAsync();

        Assert.False(plain.IsError, plain.IsError ? plain.ErrorMessage : null);
        Assert.False(withAlternatives.IsError);
        Assert.Equal(plain.Value.Select(x => x.edge), withAlternatives.Value[0].Select(x => x.edge));
    }
}
