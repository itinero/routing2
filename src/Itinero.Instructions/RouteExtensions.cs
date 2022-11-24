using System.Collections.Generic;
using Itinero.Routes;

namespace Itinero.Instructions;

/// <summary>
/// Extensions for route to generate turn-by-turn instructions.
/// </summary>
public static class RouteExtensions
{
    /// <summary>
    /// Configures an instruction generator to generate turn-by-turn instructions.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>A route and associated instructions.</returns>
    public static IRouteAndBaseInstructions Instructions(this Route route,
        RouteInstructionGeneratorSettings? settings = null)
    {
        settings ??= RouteInstructionGeneratorSettings.Default;

        var generator = new RouteInstructionGenerator(route, settings);

        return generator.Generate();
    }
    
    /// <summary>
    ///     Checks the `ShapeMeta` of the route and removes shapeMeta's where multiple metadata point to the same Shape
    /// </summary>
    /// <param name="route">The route to clean up. This will be cleaned inline and thus change the object</param>
    /// <param name="keepFirst">
    ///     The strategy. If a duplicate is encountered, the first will be kept if true, the last if set to
    ///     false
    /// </param>
    /// <returns>The original reference is returned</returns>
    public static Route RemoveDuplicateShapeMeta(this Route route,
        bool keepFirst = true)
    {
        var oldShapeMeta = route.ShapeMeta;
        var newShapeMeta = new List<Route.Meta>();
        var lastIndex = -1;
        foreach (var meta in oldShapeMeta)
        {
            if (lastIndex != meta.Shape)
            {
                newShapeMeta.Add(meta);
                lastIndex = meta.Shape;
                continue;
            }

            if (keepFirst)
            {
                continue;
            }

            newShapeMeta[^1] = meta;
            lastIndex = meta.Shape;
        }

        route.ShapeMeta = newShapeMeta;

        return route;
    }

}
