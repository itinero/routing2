using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Routes;
using Itinero.Routes.Paths;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra;

namespace Itinero.Routing.Alternatives
{
    public static class IRouterOneToOneWithAlternativesExtensions
    {

        internal static Result<IReadOnlyList<Path>> CalculatePaths(this IRouterOneToOneWithAlternatives alternativeRouter)
        {
             var settings = alternativeRouter.Settings;
            var altSettings = alternativeRouter.AlternativeRouteSettings;
            var routingNetwork = alternativeRouter.Network;

            if (routingNetwork == null) {
                throw new NullReferenceException(
                    "RoutingNetwork is null, cannot do routeplanning without a routing network");
            }

            var profile = settings.Profile;
            var costFunction = routingNetwork.GetCostFunctionFor(profile);


            var maxBox = settings.MaxBoxFor(routingNetwork, alternativeRouter.Source.sp);

            bool checkMaxDistance(VertexId v)
            {
                if (maxBox == null) {
                    return false;
                }

                if (routingNetwork == null) {
                    throw new Exception("Router cannot be null here.");
                }

                var vertex = routingNetwork.GetVertex(v);
                if (!maxBox.Value.Overlaps(vertex)) {
                    return true;
                }

                return false;
            }

            Path? runDijkstra(ICostFunction costFunction)
            {
                var source = alternativeRouter.Source;
                var target = alternativeRouter.Target;

                if (source.direction == null && target.direction == null) {
                    // Run the undirected dijkstra
                    return Dijkstra.Default.Run(routingNetwork, source.sp, target.sp,
                        costFunction.GetDijkstraWeightFunc(),
                        v => {
                            routingNetwork.RouterDb.UsageNotifier.NotifyVertex(routingNetwork, v);
                            return checkMaxDistance(v);
                        });
                }

                // Run directed dijkstra
                return Flavours.Dijkstra.EdgeBased.Dijkstra.Default.Run(routingNetwork, source, target,
                    costFunction.GetDijkstraWeightFunc(),
                    v => {
                        routingNetwork.RouterDb.UsageNotifier.NotifyVertex(routingNetwork, v.vertexId);
                        return checkMaxDistance(v.vertexId);
                    });
            }


            var initialPath = runDijkstra(costFunction);
            if (initialPath == null) {
                return new Result<IReadOnlyList<Path>>("Not a single path found!");
            }

            var results = new List<Path>(altSettings.MaxNumberOfAlternativeRoutes) {
                initialPath
            };

            var seenEdges = new HashSet<EdgeId>();
            foreach (var (edge, _, _, _) in initialPath) {
                seenEdges.Add(edge);
            }

            var maxTries = altSettings.MaxNumberOfAlternativeRoutes * 5;
            while (results.Count < altSettings.MaxNumberOfAlternativeRoutes && maxTries > 0) {
                maxTries--;
                var altCostFunction = new AlternativeRouteCostFunction(costFunction, seenEdges);
                var altPath = runDijkstra(altCostFunction);

                if (altPath == null) {
                    // No more alternative routes can be found
                    break;
                }

                var totalEdges = 0;
                var alreadyKnownEdges = 0;

                foreach (var (edge, _, _, _) in altPath) {
                    totalEdges++;
                    if (!seenEdges.Add(edge)) {
                        // Already seen!
                        alreadyKnownEdges++;
                    }
                }


                var overlapPercentage = (double) alreadyKnownEdges / totalEdges;

                if (overlapPercentage > altSettings.MaxPercentageOfEqualEdges) {
                    continue;
                }

                results.Add(altPath);
            }

            return results;
        }

        public static Result<IReadOnlyList<Route>> Calculate(this IRouterOneToOneWithAlternatives withAlternatives)
        {

            var paths = withAlternatives.CalculatePaths();

            if (paths.IsError) {
                return new Result<IReadOnlyList<Route>>(paths.ErrorMessage);
            }


            var routes = paths.Value.Select(path =>
                withAlternatives.Settings.RouteBuilder.Build(withAlternatives.Network,
                    withAlternatives.Settings.Profile, path).Value
            ).ToList();

            return new Result<IReadOnlyList<Route>>(routes);
            
         
        }
        
    }
}