using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Instructions.Types;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]

namespace Itinero.Instructions.ToText;

/// <summary>
/// The conditional to text simply contains a list of conditions (predicates) and appropriate 'toText' to generate;
/// It basically implements a 'switch case'-instruction
/// </summary>
internal class ConditionalToText : IInstructionToText
{
    internal readonly List<(Predicate<BaseInstruction> predicate, IInstructionToText toText)> _options;
    private readonly string _context;

    public ConditionalToText(
        List<(Predicate<BaseInstruction>, IInstructionToText)> options,
        string context = "context not set"
    )
    {
        _options = options;
        _context = context;
    }

    public string ToText(BaseInstruction instruction)
    {
        foreach (var option in _options)
        {
            if (option.predicate(instruction))
            {
                return option.toText.ToText(instruction);
            }
        }

        throw new ArgumentException("Fallthrough on the predicates for instruction " + instruction + " during " +
                                    _context);
    }
}
