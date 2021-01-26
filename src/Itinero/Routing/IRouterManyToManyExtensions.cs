using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Routes;
using Itinero.Routes.Builders;
using Itinero.Routes.Paths;

namespace Itinero.Routing
{
    /// <summary>
    /// Many to many extensions.
    /// </summary>
    public static class IRouterManyToManyExtensions
    {
        /// <summary>
        /// Calculates the paths.
        /// </summary>
        /// <param name="manyToManyRouter">The router.</param>
        /// <returns>The paths.</returns>
        public static IReadOnlyList<IReadOnlyList<Result<Path>>> Paths(this IRouterManyToMany manyToManyRouter)
        {
            var sources = manyToManyRouter.Sources;
            var targets = manyToManyRouter.Targets;

            if (!sources.TryToUndirected(out var sourcesUndirected) ||
                !targets.TryToUndirected(out var targetsUndirected)) {
                return manyToManyRouter.Calculate(sources, targets);
            }
            else {
                return manyToManyRouter.Calculate(sourcesUndirected, targetsUndirected);
            }
        }

        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="manyToManyRouter">The router.</param>
        /// <returns>The paths.</returns>
        public static IReadOnlyList<IReadOnlyList<Result<Route>>> Calculate(this IRouterManyToMany manyToManyRouter)
        {
            var paths = manyToManyRouter.Paths();
            return paths.Select(x => {
                return x.Select(y =>
                        RouteBuilder.Default.Build(manyToManyRouter.Network, manyToManyRouter.Settings.Profile, y))
                    .ToArray();
            }).ToArray();
        }

        /// <summary>
        /// Calculates the weights.
        /// </summary>
        /// <param name="manyToManyWeightRouter">The router.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Result<IReadOnlyList<IReadOnlyList<double?>>> Calculate(
            this IRouterWeights<IRouterManyToMany> manyToManyWeightRouter)
        {
            return null;

            // var profileHandler = manyToManyWeightRouter.Router.Network.GetCostFunctionFor(
            //     manyToManyWeightRouter.Router.Settings.Profile);
            // var paths = manyToManyWeightRouter.Router.Paths();
            // return paths.Select(x =>
            // {
            //     return x.Select(y => y.Weight(profileHandler.GetForwardWeight)).ToArray();
            // }).ToArray();
        }
    }
}