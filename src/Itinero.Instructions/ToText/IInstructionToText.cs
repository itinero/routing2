using Itinero.Instructions.Instructions;

namespace Itinero.Instructions.ToText
{
    public interface IInstructionToText
    {
        string ToText(BaseInstruction instruction);
    }
}