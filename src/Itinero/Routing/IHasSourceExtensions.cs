using System.Collections.Generic;
using Itinero.Geo.Directions;
using Itinero.Snapping;

namespace Itinero.Routing;

/// <summary>
/// Has source extensions.
/// </summary>
public static class IHasSourceExtensions
{
    /// <summary>
    /// Configures the router to route to the given point.
    /// </summary>
    /// <param name="hasSource">The hasSource.</param>
    /// <param name="target">The target.</param>
    /// <returns>A configured router.</returns>
    public static IRouterOneToOne To(this IHasSource hasSource, SnapPoint target)
    {
        return new Router(hasSource.Network, hasSource.Settings)
        {
            Source = hasSource.Source,
            Target = (target, null)
        };
    }

    /// <summary>
    /// Configures the router to route to the given point.
    /// </summary>
    /// <param name="hasSource">The hasSource.</param>
    /// <param name="target">The target.</param>
    /// <returns>A configured router.</returns>
    public static IRouterOneToOne To(this IHasSource hasSource,
        (SnapPoint snapPoint, DirectionEnum? direction) target)
    {
        return new Router(hasSource.Network, hasSource.Settings)
        {
            Source = hasSource.Source,
            Target = target.ToDirected(hasSource.Network)
        };
    }

    /// <summary>
    /// Configures the router to route to the given point.
    /// </summary>
    /// <param name="hasSource">The hasSource.</param>
    /// <param name="target">The target.</param>
    /// <returns>A configured router.</returns>
    public static IRouterOneToOne To(this IHasSource hasSource, (SnapPoint snapPoint, bool? direction) target)
    {
        return new Router(hasSource.Network, hasSource.Settings)
        {
            Source = hasSource.Source,
            Target = target
        };
    }

    /// <summary>
    /// Configures the router to route to the given points.
    /// </summary>
    /// <param name="hasSource">The hasSource.</param>
    /// <param name="targets">The targets.</param>
    /// <returns>A configured router.</returns>
    public static IRouterOneToMany To(this IHasSource hasSource, IReadOnlyList<SnapPoint> targets)
    {
        return hasSource.To(targets.ToDirected());
    }

    /// <summary>
    /// Configures the router to route from the given point.
    /// </summary>
    /// <param name="hasSource">The hasSource.</param>
    /// <param name="directedSnapPoints">The points to route from.</param>
    /// <returns>A configured router.</returns>
    public static IRouterOneToMany To(this IHasSource hasSource,
        IReadOnlyList<(SnapPoint snapPoint, DirectionEnum? direction)> directedSnapPoints)
    {
        return hasSource.To(directedSnapPoints.ToDirected(hasSource.Network));
    }

    /// <summary>
    /// Configures the router to route to the given points.
    /// </summary>
    /// <param name="hasSource">The hasSource.</param>
    /// <param name="targets">The targets.</param>
    /// <returns>A configured router.</returns>
    public static IRouterOneToMany To(this IHasSource hasSource,
        IReadOnlyList<(SnapPoint snapPoint, bool? direction)> targets)
    {
        return new Router(hasSource.Network, hasSource.Settings)
        {
            Source = hasSource.Source,
            Targets = targets
        };
    }
}
