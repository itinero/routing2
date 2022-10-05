using System.Collections.Generic;
using Itinero.Instructions.ToText;
using Itinero.Instructions.Types;
using Itinero.Routes;

namespace Itinero.Instructions;

/// <summary>
/// Abstract representation of a route and associated base instructions.
/// </summary>
public interface IRouteAndBaseInstructions
{
    /// <summary>
    /// The route.
    /// </summary>
    Route Route { get; }

    /// <summary>
    /// The base instructions.
    /// </summary>
    IReadOnlyList<BaseInstruction> BaseInstructions { get; }

    /// <summary>
    /// The supported languages.
    /// </summary>
    IReadOnlyDictionary<string, IInstructionToText> Languages { get; }
}

internal class RouteAndBaseInstructions : IRouteAndBaseInstructions
{
    internal RouteAndBaseInstructions(Route route, IReadOnlyList<BaseInstruction> baseInstructions,
        IReadOnlyDictionary<string, IInstructionToText> languages)
    {
        this.Route = route;
        this.BaseInstructions = baseInstructions;
        this.Languages = languages;
    }

    public Route Route { get; }

    public IReadOnlyList<BaseInstruction> BaseInstructions { get; }

    public IReadOnlyDictionary<string, IInstructionToText> Languages { get; }
}
