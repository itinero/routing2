using System.Linq;
using Itinero.Algorithms;
using Itinero.Algorithms.DataStructures;
using Itinero.Algorithms.Routes;

namespace Itinero.Routers
{
    /// <summary>
    /// One to one extensions.
    /// </summary>
    public static class IRouterOneToOneExtensions
    {
        /// <summary>
        /// Calculates the path.
        /// </summary>
        /// <param name="oneToOneRouter">The router.</param>
        /// <returns>The path.</returns>
        public static Result<Path> Path(this IRouterOneToOne oneToOneRouter)
        {
            var sources = new[] {oneToOneRouter.Source};
            var targets = new[] {oneToOneRouter.Target};

            if (!sources.TryToUndirected(out var sourcesUndirected) ||
                !targets.TryToUndirected(out var targetsUndirected))
            {
                return oneToOneRouter.Calculate(sources, targets).First().First();
            }

            return oneToOneRouter.Calculate(sourcesUndirected, targetsUndirected).First().First();
        }

        /// <summary>
        /// Calculates the route.
        /// </summary>
        /// <param name="oneToOneRouter">The router.</param>
        /// <returns>The route.</returns>
        public static Result<Route> Calculate(this IRouterOneToOne oneToOneRouter)
        {
            return RouteBuilder.Default.Build(oneToOneRouter.Network, oneToOneRouter.Settings.Profile,
                oneToOneRouter.Path(), true);
        }
        
        /// <summary>
        /// Calculates the weights.
        /// </summary>
        /// <param name="oneToOneWeightRouter">The router.</param>
        /// <returns>The weight</returns>
        public static Result<double?> Calculate(this IRouterWeights<IRouterOneToOne> oneToOneWeightRouter)
        {
            var profileHandler = oneToOneWeightRouter.Router.Network.GetProfileHandler(
                oneToOneWeightRouter.Router.Settings.Profile);
            return oneToOneWeightRouter.Router.Path().Weight(profileHandler.GetForwardWeight);
        }
    }
}