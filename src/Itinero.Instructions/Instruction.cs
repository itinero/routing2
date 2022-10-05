using System.Collections.Generic;
using Itinero.Instructions.Types;

namespace Itinero.Instructions;

/// <summary>
/// Represents a single turn-by-turn instructions and it's textual representation in one or more languages.
/// </summary>
public class Instruction
{
    internal Instruction(BaseInstruction baseInstruction, IReadOnlyDictionary<string, string> text)
    {
        this.BaseInstruction = baseInstruction;
        this.Text = text;
    }

    /// <summary>
    /// The base instruction, the details about the type manoeuvre to make.
    /// </summary>
    public BaseInstruction BaseInstruction { get; }

    /// <summary>
    /// The textual representation of the instruction for a user to follow.
    /// </summary>
    public IReadOnlyDictionary<string, string> Text { get; }
}
