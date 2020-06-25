using System.Collections.Generic;
using Itinero.Instructions.Instructions;

namespace Itinero.Instructions
{
    public interface IInstructionGenerator
    {
        IEnumerable<BaseInstruction> GenerateInstructions(Route route);

    }
}