using System.Collections.Generic;
using Itinero.Instructions.Instructions;
using Itinero.Routes;

namespace Itinero.Instructions
{
    public interface IInstructionGenerator
    {
        IEnumerable<BaseInstruction> GenerateInstructions(Route route);

    }
}