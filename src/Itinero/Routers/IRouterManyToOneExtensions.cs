using System;
using System.Collections.Generic;
using Itinero.Algorithms;
using Itinero.Algorithms.Dijkstra;
using Itinero.Algorithms.Routes;
using Itinero.Data.Graphs;
using Itinero.Geo;

namespace Itinero.Routers
{
    /// <summary>
    /// Many to one extensions.
    /// </summary>
    public static class IRouterManyToOneExtensions
    {
        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="routerOneToMany">The router.</param>
        /// <returns></returns>
        public static IReadOnlyList<Result<Route>> Calculate(this IRouterManyToOne routerOneToMany)
        {
            var sources = routerOneToMany.Sources;
            var target = routerOneToMany.Target;

            if (target.direction.HasValue ||
                !sources.TryToUndirected(out var sourcesUndirected))
            {
                var routes = routerOneToMany.Calculate(
                    sources,  new []{target});
                if (routes == null) throw new Exception("Could not calculate routes.") ;

                var manyToOne = new Result<Route>[sources.Count];
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

                var manyToOne = new Result<Route>[sources.Count];
                for (var s = 0; s < manyToOne.Length; s++)
                {
                    manyToOne[s] = routes[s][0];
                }
                return manyToOne;
            }
        }
    }
}