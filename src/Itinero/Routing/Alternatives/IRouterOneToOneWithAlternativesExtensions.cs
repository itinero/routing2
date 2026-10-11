using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Routes;
using Itinero.Routes.Paths;
using Itinero.Routing.Corridors;

namespace Itinero.Routing.Alternatives;

/// <summary>
/// One to one with alternatives extensions.
/// </summary>
public static class IRouterOneToOneWithAlternativesExtensions
{
    /// <summary>
    /// Calculates the fastest path and its alternatives.
    /// </summary>
    /// <returns>
    /// The fastest path first, then the alternatives cheapest-first. Never empty unless the
    /// endpoints are not connected, in which case an error comes back.
    /// </returns>
    /// <remarks>
    /// One search: the alternatives come out of the cost ellipse around the fastest route, which
    /// the route's own search maps on the way past.
    /// </remarks>
    public static async Task<Result<IReadOnlyList<Path>>> PathsAsync(
        this IRouterOneToOneWithAlternatives withAlternatives,
        CancellationToken cancellationToken = default)
    {
        if (withAlternatives.Source.direction != null || withAlternatives.Target.direction != null)
        {
            return new Result<IReadOnlyList<Path>>(
                "Alternatives are not supported for directed endpoints");
        }

        var settings = withAlternatives.AlternativeRouteSettings;
        var network = withAlternatives.Network;
        var source = withAlternatives.Source.sp;
        var target = withAlternatives.Target.sp;

        // The budget IS the cost constraint, so every path in the corridor satisfies it by
        // construction and only overlap and the detour bound are left to apply.
        var searched = await network.RouteCorridorAsync(withAlternatives.Settings, source, target,
            settings.MaxCostFactor, cancellationToken);
        if (searched.IsError) return new Result<IReadOnlyList<Path>>(searched.ErrorMessage);

        var (route, corridor) = searched.Value;
        var paths = new List<Path>(settings.MaxNumberOfAlternativeRoutes) { route.Path };
        if (settings.MaxNumberOfAlternativeRoutes <= 1) return paths;

        paths.AddRange(network.AlternativesFor(route, corridor, settings.MaxOverlap,
            settings.MaxNumberOfAlternativeRoutes - 1, settings.LocalDetourFactor,
            cancellationToken));

        return paths;
    }

    /// <summary>
    /// Calculates the fastest route and its alternatives.
    /// </summary>
    /// <returns>The fastest route first, then the alternatives cheapest-first.</returns>
    public static async Task<Result<IReadOnlyList<Route>>> CalculateAsync(
        this IRouterOneToOneWithAlternatives withAlternatives,
        CancellationToken cancellationToken = default)
    {
        var paths = await withAlternatives.PathsAsync(cancellationToken);
        if (paths.IsError) return new Result<IReadOnlyList<Route>>(paths.ErrorMessage);

        var routes = paths.Value.Select(path => withAlternatives.Settings.RouteBuilder.Build(
            withAlternatives.Network, withAlternatives.Settings.Profile, path).Value).ToList();

        return new Result<IReadOnlyList<Route>>(routes);
    }
}
