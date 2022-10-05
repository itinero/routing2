namespace Itinero.Routing
{
    /// <summary>
    /// Contains extension methods for weight routers.
    /// </summary>
    public static class IRouterWeightsExtensions
    {
        /// <summary>
        /// Configures the router to calculate weights.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <returns>A weights configured router.</returns>
        public static IRouterWeights<IRouterOneToOne> Weights(this IRouterOneToOne router)
        {
            return new RouterWeights<IRouterOneToOne>(router);
        }

        /// <summary>
        /// Configures the router to calculate weights.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <returns>A weights configured router.</returns>
        public static IRouterWeights<IRouterManyToOne> Weights(this IRouterManyToOne router)
        {
            return new RouterWeights<IRouterManyToOne>(router);
        }

        /// <summary>
        /// Configures the router to calculate weights.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <returns>A weights configured router.</returns>
        public static IRouterWeights<IRouterOneToMany> Weights(this IRouterOneToMany router)
        {
            return new RouterWeights<IRouterOneToMany>(router);
        }

        /// <summary>
        /// Configures the router to calculate weights.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <returns>A weights configured router.</returns>
        public static IRouterWeights<IRouterManyToMany> Weights(this IRouterManyToMany router)
        {
            return new RouterWeights<IRouterManyToMany>(router);
        }
    }
}
