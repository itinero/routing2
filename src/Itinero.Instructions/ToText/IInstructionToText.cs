using Itinero.Instructions.Types;

namespace Itinero.Instructions.ToText;

/// <summary>
/// Abstract representation of a class to translate instructions to text.
/// </summary>
public interface IInstructionToText
{
    /// <summary>
    /// Translates the given instruction to text.
    /// </summary>
    /// <param name="instruction">The instruction to translate.</param>
    /// <returns>The instruction text.</returns>
    string ToText(BaseInstruction instruction);
}
