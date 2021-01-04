using System;
using System.Collections.Generic;
using System.Linq;
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
        public static IReadOnlyList<Result<Path>> Paths(this IRouterManyToOne routerOneToMany)
        {
            var sources = routerOneToMany.Sources;
            var target = routerOneToMany.Target;

            if (target.direction.HasValue ||
                !sources.TryToUndirected(out var sourcesUndirected))
            {
                var routes = routerOneToMany.Calculate(
                    sources,  new []{target});
                if (routes == null) throw new Exception("Could not calculate routes.") ;

                var manyToOne = new Result<Path>[sources.Count];
                for (var s = 0; s < manyToOne.Length; s++)
                {
                    manyToOne[s] = routes[s][0];
                }
                return manyToOne;
            }
            else
            {
                var routes = routerOneToMany.Calculate(sourcesUndirected, new [] {target.sp});
                if (routes == null) throw new Exception("Could not calculate routes.");

                var manyToOne = new Result<Path>[sources.Count];
                for (var s = 0; s < manyToOne.Length; s++)
                {
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
        public static IReadOnlyList<Result<Route>> Calculate(this IRouterManyToOne routerManyToOne)
        {
            return routerManyToOne.Paths().Select(x => RouteBuilder.Default.Build(routerManyToOne.Network,
                routerManyToOne.Settings.Profile, x)).ToArray();
        }
        
        /// <summary>
        /// Calculates the weights.
        /// </summary>
        /// <param name="routerManyToOne">The router.</param>
        /// <returns>The weights.</returns>
        public static Result<IReadOnlyList<double?>> Calculate(this IRouterWeights<IRouterManyToOne> routerManyToOne)
        {
            return null;
            //
            // var profileHandler = routerManyToOne.Router.Network.GetCostFunctionFor(
            //     routerManyToOne.Router.Settings.Profile);
            // return routerManyToOne.Router.Paths().Select(x => x.Weight(profileHandler.GetForwardWeight)).ToArray();
        }
    }
}