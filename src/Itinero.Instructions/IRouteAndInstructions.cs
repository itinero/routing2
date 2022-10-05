using System.Collections.Generic;
using Itinero.Routes;

namespace Itinero.Instructions;

/// <summary>
/// Abstract representation of a route and generated instructions.
/// </summary>
public interface IRouteAndInstructions
{
    /// <summary>
    /// The route.
    /// </summary>
    Route Route { get; }

    /// <summary>
    /// The instructions per language.
    /// </summary>
    IReadOnlyList<Instruction> Instructions { get; }
}

internal class RouteAndInstructions : IRouteAndInstructions
{
    internal RouteAndInstructions(Route route, IReadOnlyList<Instruction> instructions)
    {
        this.Route = route;
        this.Instructions = instructions;
    }
    public Route Route { get; }

    public IReadOnlyList<Instruction> Instructions { get; }
}
