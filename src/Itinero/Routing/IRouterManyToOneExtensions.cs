using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Routes;
using Itinero.Routes.Builders;
using Itinero.Routes.Paths;

namespace Itinero.Routing
{
    /// <summary>
    /// Many to one extensions.
    /// </summary>
    public static class IRouterManyToOneExtensions
    {
        /// <summary>
        /// Calculates the paths.
        /// </summary>
        /// <param name="routerOneToMany">The router.</param>
        /// <returns>The paths.</returns>
        public static async Task<IReadOnlyList<Result<Path>>> Paths(this IRouterManyToOne routerOneToMany)
        {
            var sources = routerOneToMany.Sources;
            var target = routerOneToMany.Target;

            if (target.direction.HasValue ||
                !sources.TryToUndirected(out var sourcesUndirected)) {
                var routes = await routerOneToMany.CalculateAsync(
                    sources, new[] {target});
                if (routes == null) {
                    throw new Exception("Could not calculate routes.");
                }

                var manyToOne = new Result<Path>[sources.Count];
                for (var s = 0; s < manyToOne.Length; s++) {
                    manyToOne[s] = routes[s][0];
                }

                return manyToOne;
            }
            else {
                var routes = await routerOneToMany.CalculateAsync(sourcesUndirected, new[] {target.sp});
                if (routes == null) {
                    throw new Exception("Could not calculate routes.");
                }

                var manyToOne = new Result<Path>[sources.Count];
                for (var s = 0; s < manyToOne.Length; s++) {
                    manyToOne[s] = routes[s][0];
                }

                return manyToOne;
            }
        }

        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="routerManyToOne">The router.</param>
        /// <returns>The routes.</returns>
        public static async Task<IReadOnlyList<Result<Route>>> Calculate(this IRouterManyToOne routerManyToOne)
        {
            return (await routerManyToOne.Paths()).Select(x => routerManyToOne.Settings.RouteBuilder.Build(routerManyToOne.Network,
                routerManyToOne.Settings.Profile, x)).ToArray();
        }

        /// <summary>
        /// Calculates the weights.
        /// </summary>
        /// <param name="routerManyToOne">The router.</param>
        /// <returns>The weights.</returns>
        public static async Task<Result<IReadOnlyList<double?>>> Calculate(this IRouterWeights<IRouterManyToOne> routerManyToOne)
        {
            return null;
            //
            // var profileHandler = routerManyToOne.Router.Network.GetCostFunctionFor(
            //     routerManyToOne.Router.Settings.Profile);
            // return routerManyToOne.Router.Paths().Select(x => x.Weight(profileHandler.GetForwardWeight)).ToArray();
        }
    }
}