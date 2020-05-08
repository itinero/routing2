using System.Collections.Generic;

namespace Itinero.Routers
{
    /// <summary>
    /// Contains extensions for the IRouter interface.
    /// </summary>
    public static class IRouterExtensions
    {
        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="snapPoint">The point to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSource From(this IRouter router, SnapPoint snapPoint)
        {
            return router.From((snapPoint, null));
        }

        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="directedSnapPoint">The point to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSource From(this IRouter router, (SnapPoint snapPoint, bool? direction) directedSnapPoint)
        {
            return new Router(router.RouterDb, router.Settings)
            {
                Source =directedSnapPoint
            };
        }
        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="snapPoints">The points to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSources From(this IRouter router, IReadOnlyList<SnapPoint> snapPoints)
        {
            return router.From(snapPoints.ToDirected());
        }

        /// <summary>
        /// Configures the router to route from the given point.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="directedSnapPoints">The points to route from.</param>
        /// <returns>A configured router.</returns>
        public static IHasSources From(this IRouter router, IReadOnlyList<(SnapPoint snapPoint, bool? direction)> directedSnapPoints)
        {
            return new Router(router.RouterDb, router.Settings)
            {
                Sources = directedSnapPoints
            };
        }
    }
}