using System.Collections.Generic;
using Itinero.Instructions.Instructions;

namespace Itinero.Instructions.ToText
{
    /***
     * Instruction to text changes an instruction object into text based on simple substitution.
     * It uses reflection to create a dictionary of all available fields, which are substituted
     */
    public class SubstituteText : IInstructionToText
    {
        private readonly IEnumerable<(string textOrVarName, bool substitute)> _text;
        private readonly bool _crashOnMissingKey;

        public SubstituteText(
            IEnumerable<(string textOrVarName, bool substitute)> text,
            bool crashOnMissingKey = true
        )
        {
            _text = text;
            _crashOnMissingKey = crashOnMissingKey;
        }

        public string ToText(BaseInstruction instruction)
        {
            var subsValues = new Reminiscence.Collections.Dictionary<string, string>();

            foreach (var f in instruction.GetType().GetFields())
            {
                if (!f.IsPublic) continue;
                subsValues[f.Name.ToLower()] = "" + f.GetValue(instruction);
            }

            var resultText = "";
            foreach (var (text, substitute) in _text)
                if (substitute)
                {
                    if (subsValues.TryGetValue(text.ToLower(), out var newValue))
                    {
                        resultText += newValue;
                    }
                    else if(_crashOnMissingKey)
                    {
                        throw new KeyNotFoundException("The instruction does not contain a field with name " + text +
                                                       "; try one of " + string.Join(", ", subsValues.Keys));
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    resultText += text;
                }

            return resultText;
        }
    }
}