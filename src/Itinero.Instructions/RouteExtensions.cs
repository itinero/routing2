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
}