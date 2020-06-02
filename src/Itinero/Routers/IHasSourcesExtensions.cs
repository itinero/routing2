using System.Collections.Generic;
using Itinero.Geo.Directions;

namespace Itinero.Routers
{
    /// <summary>
    /// Has sources extensions.
    /// </summary>
    public static class IHasSourcesExtensions
    {
        /// <summary>
        /// Configures the router to route to the given point.
        /// </summary>
        /// <param name="hasSources">The router with sources.</param>
        /// <param name="target">The target.</param>
        /// <returns>A configured router.</returns>
        public static IRouterManyToOne To(this IHasSources hasSources, SnapPoint target)
        {
            return new Router(hasSources.Network, hasSources.Settings)
            {
                Sources = hasSources.Sources,
                Target = (target, null)
            };
        }
        
        /// <summary>
        /// Configures the router to route to the given point.
        /// </summary>
        /// <param name="hasSources">The router with sources.</param>
        /// <param name="target">The target.</param>
        /// <returns>A configured router.</returns>
        public static IRouterManyToOne To(this IHasSources hasSources,  (SnapPoint snapPoint, DirectionEnum? direction) target)
        {
            return new Router(hasSources.Network, hasSources.Settings)
            {
                Sources = hasSources.Sources,
                Target = target.ToDirected(hasSources.Network)
            };
        }
        
        /// <summary>
        /// Configures the router to route to the given point.
        /// </summary>
        /// <param name="hasSources">The router with sources.</param>
        /// <param name="target">The target.</param>
        /// <returns>A configured router.</returns>
        public static IRouterManyToOne To(this IHasSources hasSources, (SnapPoint snapPoint, bool? direction) target)
        {
            return new Router(hasSources.Network, hasSources.Settings)
            {
                Sources = hasSources.Sources,
                Target = target
            };
        }
        
        /// <summary>
        /// Configures the router to route to the given point.
        /// </summary>
        /// <param name="hasSources">The router with sources.</param>
        /// <param name="targets">The targets.</param>
        /// <returns>A configured router.</returns>
        public static IRouterManyToMany To(this IHasSources hasSources, IReadOnlyList<SnapPoint> targets)
        {
            return new Router(hasSources.Network, hasSources.Settings)
            {
                Sources = hasSources.Sources,
                Targets = targets.ToDirected()
            };
        }
        
        /// <summary>
        /// Configures the router to route to the given point.
        /// </summary>
        /// <param name="hasSources">The router with sources.</param>
        /// <param name="targets">The targets.</param>
        /// <returns>A configured router.</returns>
        public static IRouterManyToMany To(this IHasSources hasSources, IReadOnlyList<(SnapPoint snapPoint, DirectionEnum? directed)> targets)
        {
            return new Router(hasSources.Network, hasSources.Settings)
            {
                Sources = hasSources.Sources,
                Targets = targets.ToDirected(hasSources.Network)
            };
        }
        
        /// <summary>
        /// Configures the router to route to the given point.
        /// </summary>
        /// <param name="hasSources">The router with sources.</param>
        /// <param name="targets">The targets.</param>
        /// <returns>A configured router.</returns>
        public static IRouterManyToMany To(this IHasSources hasSources, IReadOnlyList<(SnapPoint snapPoint, bool? directed)> targets)
        {
            return new Router(hasSources.Network, hasSources.Settings)
            {
                Sources = hasSources.Sources,
                Targets = targets
            };
        }
    }
}