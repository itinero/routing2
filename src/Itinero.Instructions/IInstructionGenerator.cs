using System;
using System.Collections.Generic;

namespace Itinero.Instructions
{
    public interface IInstructionGenerator
    {
        IEnumerable<Instruction> GenerateInstructions(Route route);

    }
}