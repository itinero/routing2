using Itinero.Instructions.Generators;

namespace Itinero.Instructions.ToText
{
    internal interface IInstructionToText
    {
        string ToText(BaseInstruction instruction);
    }
}