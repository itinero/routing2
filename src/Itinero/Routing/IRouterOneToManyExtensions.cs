using System;
using System.Collections.Generic;
using System.Linq;
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
        public static IReadOnlyList<Result<Path>> Paths(this IRouterOneToMany routerOneToMany)
        {            
            var source = routerOneToMany.Source;
            var targets = routerOneToMany.Targets;

            if (source.direction.HasValue ||
                !targets.TryToUndirected(out var targetsUndirected))
            {
                var paths = routerOneToMany.Calculate(
                    new (SnapPoint snapPoint, bool? direction)[]{ source }, targets);
                
                if (paths == null) throw new Exception("Could not calculate routes.") ;
                if (paths.Count < 1)throw new Exception("Could not calculate routes.") ;

                return paths[0];
            }
            else
            {
                var paths = routerOneToMany.Calculate(new []{ source.sp }, targetsUndirected);
                if (paths == null) throw new Exception("Could not calculate routes.") ;
                if (paths.Count < 1)throw new Exception("Could not calculate routes.") ;

                return paths[0];
            }
        }
        
        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="routerOneToMany">The router.</param>
        /// <returns>The routes.</returns>
        public static IReadOnlyList<Result<Route>> Calculate(this IRouterOneToMany routerOneToMany)
        {
            return routerOneToMany.Paths().Select(x => RouteBuilder.Default.Build(routerOneToMany.Network,
                routerOneToMany.Settings.Profile, x)).ToArray();
        }
        
        /// <summary>
        /// Calculates the weights.
        /// </summary>
        /// <param name="routerOneToMany">The router.</param>
        /// <returns>The weights.</returns>
        public static Result<IReadOnlyList<double?>> Calculate(this IRouterWeights<IRouterOneToMany> routerOneToMany)
        {
            return null;
            
            // var profileHandler = routerOneToMany.Router.Network.GetCostFunctionFor(
            //     routerOneToMany.Router.Settings.Profile);
            // return routerOneToMany.Router.Paths().Select(x => x.Weight(profileHandler.GetForwardWeight)).ToArray();
        }
    }
}