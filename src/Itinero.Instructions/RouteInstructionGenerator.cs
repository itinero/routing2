using System.Runtime.CompilerServices;
using Itinero.Instructions.Generators;
using Itinero.Routes;
[assembly: InternalsVisibleTo("Itinero.Tests.Instructions")]

namespace Itinero.Instructions;

/// <summary>
/// A route which has the capabilities to generate instructions.
/// </summary>
internal class RouteInstructionGenerator
{
    private readonly RouteInstructionGeneratorSettings _settings;
    private readonly Route _route;
    private readonly IInstructionListGenerator _generator;

    internal RouteInstructionGenerator(Route route, RouteInstructionGeneratorSettings settings)
    {
        _route = route;
        _settings = settings;

        _generator = new LinearInstructionListGenerator(_settings.Generators);
    }

    /// <summary>
    /// Generates turn-by-turn instructions.
    /// </summary>
    /// <returns>The route and the instructions.</returns>
    public IRouteAndBaseInstructions Generate()
    {
        return new RouteAndBaseInstructions(_route,
            _generator.GenerateInstructions(_route), _settings.Languages);
    }
}
