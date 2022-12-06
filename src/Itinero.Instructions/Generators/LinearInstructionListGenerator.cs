using System;
using System.Collections.Generic;
using Itinero.Instructions.Types;
using Itinero.Routes;

namespace Itinero.Instructions.Generators;

/// <summary>
/// Constructs instructions using the instruction constructors and adds an end-instruction.
/// </summary>
/// <remarks>
/// Given a list of instruction constructors, this will construct a route in the following way:
///  - Try to generate a base instruction with the first instruction in the list. If this fails, try the next one
///  - Check the shape-index of the generated instruction and move the index of the route forward
///  - Try to generate a new instruction as described above
/// 
///  This means that the list of 'baseInstructionConstructors' should go from "very specialized" to "very generic", e.g.
///  the roundabout-instruction-constructor should be at the first position as that instruction will only trigger in specific circumstances;
///  whereas the 'follow the road/go left/go right' instruction will always trigger but is not very informative.
///
/// An 'end'-instruction will always an automatically be appended. Adding the 'endInstructionGenerator' via the constructor is thus unnecessary in might result in
/// _two_ end instructions
/// </remarks>
public class LinearInstructionListGenerator : IInstructionListGenerator
{
    private readonly IReadOnlyList<Types.Generators.IInstructionGenerator> _constructors;

    internal LinearInstructionListGenerator(IReadOnlyList<Types.Generators.IInstructionGenerator> constructors)
    {
        _constructors = constructors;
    }

    /// <inhertdoc/>
    public IReadOnlyList<BaseInstruction> GenerateInstructions(Route route)
    {
        var indexedRoute = new IndexedRoute(route);
        var instructions = new List<BaseInstruction>();

        var currentIndex = 0;
        while (currentIndex < indexedRoute.Last)
        {
            var instruction = this.ConstructNext(indexedRoute, currentIndex);
            instructions.Add(instruction);
            if (instruction.ShapeIndexEnd == currentIndex)
            {
                currentIndex++;
            }
            else
            {
                currentIndex = instruction.ShapeIndexEnd;
            }
        }

        instructions.Add(new EndInstruction(indexedRoute));

        return instructions;
    }

    private BaseInstruction ConstructNext(IndexedRoute r, int currentOffset)
    {
        foreach (var constructor in _constructors)
        {
            var instruction = constructor.Generate(r, currentOffset);
            if (instruction != null)
            {
                return instruction;
            }
        }

        throw new Exception("Could not generate instruction for offset "+currentOffset);
    }
}
