using System;
using System.Collections.Generic;
using Itinero.Instructions.Instructions;

namespace Itinero.Instructions.ToText
{
    public class ConditionalToText: IInstructionToText
    {
        public readonly List<(Predicate<BaseInstruction> predicate, IInstructionToText toText)> _options;

        public ConditionalToText(
            List<(Predicate<BaseInstruction>, IInstructionToText)> options
            )
        {
            _options = options;
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

            throw new ArgumentException("Fallthrough on the predicates for instruction " + instruction);
        }
    }
}