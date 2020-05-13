using System.Linq;
using Itinero.Algorithms;

namespace Itinero.Routers
{
    /// <summary>
    /// One to one extensions.
    /// </summary>
    public static class IRouterOneToOneExtensions
    {
        /// <summary>
        /// Calculates the routes.
        /// </summary>
        /// <param name="oneToOneRouter">The router.</param>
        /// <returns>The route.</returns>
        public static Result<Route> Calculate(this IRouterOneToOne oneToOneRouter)
        {
            var sources = new []  { oneToOneRouter.Source };
            var targets = new [] { oneToOneRouter.Target };

            if (!sources.TryToUndirected(out var sourcesUndirected) ||
                !targets.TryToUndirected(out var targetsUndirected))
            {
                return oneToOneRouter.Calculate(sources, targets).First().First();
            }
            else
            {
                return oneToOneRouter.Calculate(sourcesUndirected, targetsUndirected).First().First();;
            }
        }
        
        /// <summary>
        /// Calculates the weights.
        /// </summary>
        /// <param name="oneToOneWeightRouter">The router.</param>
        /// <returns>The weight</returns>
        public static Result<double?[][]> Calculate(this IRouterWeights<IRouterOneToOne> oneToOneWeightRouter)
        {
            var oneToOneRouter = oneToOneWeightRouter.Router;
            
            var sources = new []  { oneToOneRouter.Source };
            var targets = new [] { oneToOneRouter.Target };

            if (!sources.TryToUndirected(out var sourcesUndirected) ||
                !targets.TryToUndirected(out var targetsUndirected))
            {
                return oneToOneWeightRouter.Calculate(sources, targets);
            }
            else
            {
                return oneToOneWeightRouter.Calculate(sourcesUndirected, targetsUndirected);
            }
        }

    }
}