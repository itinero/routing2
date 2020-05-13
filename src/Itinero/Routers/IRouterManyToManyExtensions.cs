using System;
using System.Collections.Generic;
using Itinero.Algorithms;

namespace Itinero.Routers
{
    /// <summary>
    /// Many to many extensions.
    /// </summary>
    public static class IRouterManyToManyExtensions
    {
        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="manyToManyRouter">The router.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IReadOnlyList<IReadOnlyList<Result<Route>>> Calculate(this IRouterManyToMany manyToManyRouter)
        {
            var sources = manyToManyRouter.Sources;
            var targets = manyToManyRouter.Targets;

            if (!sources.TryToUndirected(out var sourcesUndirected) ||
                !targets.TryToUndirected(out var targetsUndirected))
            {
                return manyToManyRouter.Calculate(sources, targets);
            }
            else
            {
                return manyToManyRouter.Calculate(sourcesUndirected, targetsUndirected);
            }
        }
        
        /// <summary>
        /// Calculates the weights.
        /// </summary>
        /// <param name="manyToManyWeightRouter">The router.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Result<double?[][]> Calculate(this IRouterWeights<IRouterManyToMany> manyToManyWeightRouter)
        {
            var manyToManyRouter = manyToManyWeightRouter.Router;
            
            var sources = manyToManyRouter.Sources;
            var targets = manyToManyRouter.Targets;

            if (!sources.TryToUndirected(out var sourcesUndirected) ||
                !targets.TryToUndirected(out var targetsUndirected))
            {
                return manyToManyWeightRouter.Calculate(sources, targets);
            }
            else
            {
                return manyToManyWeightRouter.Calculate(sourcesUndirected, targetsUndirected);
            }
        }
    }
}