using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Routes;
using Itinero.Routes.Paths;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra;

namespace Itinero.Routing.Alternatives;

public static class IRouterOneToOneWithAlternativesExtensions
{
    internal static async Task<Result<IReadOnlyList<Path>>> CalculatePathsAsync(
        this IRouterOneToOneWithAlternatives alternativeRouter, CancellationToken cancellationToken)
    {
        const double penaltyFactor = 2.0;

        var settings = alternativeRouter.Settings;
        var altSettings = alternativeRouter.AlternativeRouteSettings;
        var routingNetwork = alternativeRouter.Network;

        if (routingNetwork == null)
        {
            throw new NullReferenceException(
                "RoutingNetwork is null, cannot do route planning without a routing network");
        }

        var profile = settings.Profile;
        var costFunction = routingNetwork.GetCostFunctionFor(profile);


        var maxBox = settings.MaxBoxFor(routingNetwork, alternativeRouter.Source.sp);

        bool CheckMaxDistance(VertexId v)
        {
            if (maxBox == null)
            {
                return false;
            }

            if (routingNetwork == null)
            {
                throw new Exception("Router cannot be null here.");
            }

            var vertex = routingNetwork.GetVertex(v);
            if (!maxBox.Value.Overlaps(vertex))
            {
                return true;
            }

            return false;
        }

        async Task<(Path? path, double cost)> RunDijkstraAsync(ICostFunction costFunction, CancellationToken cancellationToken)
        {
            var source = alternativeRouter.Source;
            var target = alternativeRouter.Target;

            if (source.direction == null && target.direction == null)
            {
                // Run the undirected dijkstra
                return await Dijkstra.Default.RunAsync(routingNetwork, source.sp, target.sp,
                    costFunction.GetDijkstraWeightFunc(),
                    async v =>
                    {
                        await routingNetwork.RouterDb.UsageNotifier.NotifyVertex(routingNetwork, v, cancellationToken);
                        if (cancellationToken.IsCancellationRequested) return false;
                        return CheckMaxDistance(v);
                    });
            }

            // Run directed dijkstra
            return await Flavours.Dijkstra.EdgeBased.Dijkstra.Default.RunAsync(routingNetwork, source, target,
                costFunction.GetDijkstraWeightFunc(),
                async v =>
                {
                    await routingNetwork.RouterDb.UsageNotifier.NotifyVertex(routingNetwork, v.vertexId, cancellationToken);
                    if (cancellationToken.IsCancellationRequested) return false;
                    return CheckMaxDistance(v.vertexId);
                });
        }

        var (initialPath, initialCost) = await RunDijkstraAsync(costFunction, cancellationToken);
        if (initialPath == null)
        {
            return new Result<IReadOnlyList<Path>>("Not a single path found!");
        }

        var results = new List<Path>(altSettings.MaxNumberOfAlternativeRoutes) {
                initialPath
            };

        if (altSettings.MaxNumberOfAlternativeRoutes == 1) return results;

        var costThreshold = initialCost * altSettings.MaxWeightIncreasePercentage;

        var seenEdges = new HashSet<EdgeId>();
        foreach (var (edge, _, _, _) in initialPath)
        {
            seenEdges.Add(edge);
        }

        var maxTries = altSettings.MaxNumberOfAlternativeRoutes * 5;
        while (results.Count < altSettings.MaxNumberOfAlternativeRoutes && maxTries > 0)
        {
            if (cancellationToken.IsCancellationRequested) break;

            maxTries--;
            var altCostFunction = new AlternativeRouteCostFunction(costFunction, seenEdges,
                penaltyFactor);
            var (altPath, altCost) = await RunDijkstraAsync(altCostFunction, cancellationToken);

            if (altCost > costThreshold)
            {
                // No more alternative routes can be found
                break;
            }

            if (altPath == null)
            {
                // No more alternative routes can be found
                break;
            }

            var totalEdges = 0;
            var alreadyKnownEdges = 0;

            foreach (var (edge, _, _, _) in altPath)
            {
                totalEdges++;
                if (!seenEdges.Add(edge))
                {
                    // Already seen!
                    alreadyKnownEdges++;
                }
            }

            var overlapPercentage = (double)alreadyKnownEdges / totalEdges;

            if (overlapPercentage > altSettings.MaxPercentageOfEqualEdges)
            {
                continue;
            }

            results.Add(altPath);
        }

        return results;
    }

    public static async Task<Result<IReadOnlyList<Route>>> CalculateAsync(this IRouterOneToOneWithAlternatives withAlternatives, CancellationToken cancellationToken = default)
    {
        var paths = await withAlternatives.CalculatePathsAsync(cancellationToken);

        if (paths.IsError)
        {
            return new Result<IReadOnlyList<Route>>(paths.ErrorMessage);
        }


        var routes = paths.Value.Select(path =>
            withAlternatives.Settings.RouteBuilder.Build(withAlternatives.Network,
                withAlternatives.Settings.Profile, path).Value
        ).ToList();

        return new Result<IReadOnlyList<Route>>(routes);
    }
}
