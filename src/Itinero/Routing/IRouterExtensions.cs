using System;
using System.Collections.Generic;
using Itinero.Geo;
using Itinero.Geo.Directions;
using Itinero.Network;
using Itinero.Routes.Paths;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra;
using Itinero.Snapping;

namespace Itinero.Routing
{
    /// <summary>
    /// Contains extensions for the IRouter interface.
    /// </summary>
    public static class IRouterExtensions
    {
        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="snapPoint">The point to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSource From(this IRouter router, SnapPoint snapPoint)
        {
            return router.From((snapPoint, (bool?) null));
        }

        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="directedSnapPoint">The point to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSource From(this IRouter router, (SnapPoint snapPoint, bool? direction) directedSnapPoint)
        {
            return new Router(router.Network, router.Settings) {
                Source = directedSnapPoint
            };
        }

        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="directedSnapPoint">The point to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSource From(this IRouter router,
            (SnapPoint snapPoint, DirectionEnum? direction) directedSnapPoint)
        {
            return router.From(directedSnapPoint.ToDirected(router.Network));
        }

        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="snapPoints">The points to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSources From(this IRouter router, IReadOnlyList<SnapPoint> snapPoints)
        {
            return router.From(snapPoints.ToDirected());
        }

        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="directedSnapPoints">The points to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSources From(this IRouter router,
            IReadOnlyList<(SnapPoint snapPoint, DirectionEnum? direction)> directedSnapPoints)
        {
            return router.From(directedSnapPoints.ToDirected(router.Network));
        }

        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="directedSnapPoints">The points to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSources From(this IRouter router,
            IReadOnlyList<(SnapPoint snapPoint, bool? direction)> directedSnapPoints)
        {
            return new Router(router.Network, router.Settings) {
                Sources = directedSnapPoints
            };
        }

        internal static IReadOnlyList<IReadOnlyList<Result<Path>>> Calculate(this IRouter manyToManyRouter,
            IReadOnlyList<SnapPoint> sources, IReadOnlyList<SnapPoint> targets)
        {
            var settings = manyToManyRouter.Settings;
            var routerDb = manyToManyRouter.Network;

            var profile = settings.Profile;
            var costFunction = routerDb.GetCostFunctionFor(profile);

            var maxBox = settings.MaxBoxFor(routerDb, sources);

            bool checkMaxDistance(VertexId v)
            {
                if (maxBox == null) {
                    return false;
                }

                if (routerDb == null) {
                    throw new Exception("Router cannot be null here.");
                }

                var vertex = routerDb.GetVertex(v);
                if (!maxBox.Value.Overlaps(vertex)) {
                    return true;
                }

                return false;
            }

            var results = new IReadOnlyList<Result<Path>>[sources.Count];
            for (var s = 0; s < sources.Count; s++) {
                var source = sources[s];
                var paths = Dijkstra.Default.Run(routerDb, source, targets,
                    costFunction.GetDijkstraWeightFunc(),
                    (v) => {
                        routerDb.RouterDb.UsageNotifier.NotifyVertex(routerDb, v);
                        return checkMaxDistance(v);
                    });

                var sourceResults = new Result<Path>[paths.Length];
                for (var r = 0; r < sourceResults.Length; r++) {
                    var path = paths[r];
                    if (path == null) {
                        sourceResults[r] = new Result<Path>($"Path not found!");
                    }
                    else {
                        sourceResults[r] = path;
                    }
                }

                results[s] = sourceResults;
            }

            return results;
        }

        internal static IReadOnlyList<IReadOnlyList<Result<Path>>> Calculate(this IRouter manyToManyRouter,
            IReadOnlyList<(SnapPoint snapPoint, bool? direction)> sources,
            IReadOnlyList<(SnapPoint snapPoint, bool? direction)> targets)
        {
            var settings = manyToManyRouter.Settings;
            var routerDb = manyToManyRouter.Network;

            var profile = settings.Profile;
            var costFunction = routerDb.GetCostFunctionFor(profile);

            var maxBox = settings.MaxBoxFor(routerDb, sources);

            bool checkMaxDistance(VertexId v)
            {
                if (maxBox == null) {
                    return false;
                }

                if (routerDb == null) {
                    throw new Exception("Router cannot be null here.");
                }

                var vertex = routerDb.GetVertex(v);
                if (!maxBox.Value.Overlaps(vertex)) {
                    return true;
                }

                return false;
            }

            var results = new IReadOnlyList<Result<Path>>[sources.Count];
            for (var s = 0; s < sources.Count; s++) {
                var source = sources[s];
                var paths = Flavours.Dijkstra.EdgeBased.Dijkstra.Default.Run(routerDb, source, targets,
                    costFunction.GetDijkstraWeightFunc(),
                    e => {
                        routerDb.RouterDb.UsageNotifier.NotifyVertex(routerDb, e.vertexId);
                        return checkMaxDistance(e.vertexId);
                    });

                var sourceResults = new Result<Path>[paths.Length];
                for (var r = 0; r < sourceResults.Length; r++) {
                    var path = paths[r];
                    if (path == null) {
                        sourceResults[r] = new Result<Path>($"Routes not found!");
                    }
                    else {
                        sourceResults[r] = path;
                    }
                }

                results[s] = sourceResults;
            }

            return results;
        }
    }
}