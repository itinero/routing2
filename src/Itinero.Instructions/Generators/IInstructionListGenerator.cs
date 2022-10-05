using System.Collections.Generic;
using Itinero.Instructions.Types;
using Itinero.Routes;

namespace Itinero.Instructions.Generators;

/// <summary>
/// An instruction generator.
/// </summary>
public interface IInstructionListGenerator
{
    /// <summary>
    /// Generates base instructions for the given route.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <returns>The instructions.</returns>
    IReadOnlyList<BaseInstruction> GenerateInstructions(Route route);
}
