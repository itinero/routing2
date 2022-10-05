using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Routes;
using Itinero.Routes.Builders;
using Itinero.Routes.Paths;
using Itinero.Snapping;

namespace Itinero.Routing
{
    /// <summary>
    /// Contains extensions methods for the one to many router.
    /// </summary>
    public static class IRouterOneToManyExtensions
    {
        /// <summary>
        /// Calculates the paths.
        /// </summary>
        /// <param name="routerOneToMany">The router.</param>
        /// <returns>The paths.</returns>
        public static async Task<IReadOnlyList<Result<Path>>> Paths(this IRouterOneToMany routerOneToMany, CancellationToken cancellationToken)
        {
            var source = routerOneToMany.Source;
            var targets = routerOneToMany.Targets;

            if (source.direction.HasValue ||
                !targets.TryToUndirected(out var targetsUndirected))
            {
                var paths = await routerOneToMany.CalculateAsync(
                    new (SnapPoint snapPoint, bool? direction)[] { source }, targets);

                if (paths == null)
                {
                    throw new Exception("Could not calculate routes.");
                }

                if (paths.Count < 1)
                {
                    throw new Exception("Could not calculate routes.");
                }

                return paths[0];
            }
            else
            {
                var paths = await routerOneToMany.CalculateAsync(new[] { source.sp }, targetsUndirected, cancellationToken);
                if (paths == null)
                {
                    throw new Exception("Could not calculate routes.");
                }

                if (paths.Count < 1)
                {
                    throw new Exception("Could not calculate routes.");
                }

                return paths[0];
            }
        }

        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="routerOneToMany">The router.</param>
        /// <returns>The routes.</returns>
        public static async Task<IReadOnlyList<Result<Route>>> Calculate(this IRouterOneToMany routerOneToMany, CancellationToken cancellationToken = default)
        {
            return (await routerOneToMany.Paths(cancellationToken)).Select(x => routerOneToMany.Settings.RouteBuilder.Build(routerOneToMany.Network,
                routerOneToMany.Settings.Profile, x)).ToArray();
        }

        /// <summary>
        /// Calculates the weights.
        /// </summary>
        /// <param name="routerOneToMany">The router.</param>
        /// <returns>The weights.</returns>
        public static Task<Result<IReadOnlyList<double?>>> Calculate(this IRouterWeights<IRouterOneToMany> routerOneToMany)
        {
            return Task.FromResult(null);

            // var profileHandler = routerOneToMany.Router.Network.GetCostFunctionFor(
            //     routerOneToMany.Router.Settings.Profile);
            // return routerOneToMany.Router.Paths().Select(x => x.Weight(profileHandler.GetForwardWeight)).ToArray();
        }
    }
}
