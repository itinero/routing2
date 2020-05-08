using System;
using System.Collections.Generic;
using Itinero.Algorithms;

namespace Itinero.Routers
{
    public static class IRouterOneToManyExtensions
    {
        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="routerOneToMany">The router.</param>
        /// <returns></returns>
        public static IReadOnlyList<Result<Route>> Calculate(this IRouterOneToMany routerOneToMany)
        {
            var source = routerOneToMany.Source;
            var targets = routerOneToMany.Targets;

            if (source.direction.HasValue ||
                !targets.TryToUndirected(out var targetsUndirected))
            {
                var routes = routerOneToMany.Calculate(
                    new (SnapPoint snapPoint, bool? direction)[]{ source }, targets);
                
                if (routes == null) throw new Exception("Could not calculate routes.") ;
                if (routes.Count < 1)throw new Exception("Could not calculate routes.") ;

                return routes[0];
            }
            else
            {
                var routes = routerOneToMany.Calculate(new []{ source.sp }, targetsUndirected);
                if (routes == null) throw new Exception("Could not calculate routes.") ;
                if (routes.Count < 1)throw new Exception("Could not calculate routes.") ;

                return routes[0];
            }
        }
    }
}