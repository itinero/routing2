using System.Collections.Generic;
using System.Linq;
using Itinero.Instructions.Instructions;
using Itinero.Network.Attributes;

namespace Itinero.Instructions.ToText {
    /***
     * Instruction to text changes an instruction object into text based on simple substitution.
     * It uses reflection to create a dictionary of all available fields, which are substituted
     */
    public class SubstituteText : IInstructionToText {
        private readonly bool _crashOnMissingKey;
        private readonly IEnumerable<(string textOrVarName, bool substitute)> _text;

        private readonly Dictionary<char, int> indices = new Dictionary<char, int> {
            {'.', 0},
            {'-', -1},
            {'+', 1}
        };

        public SubstituteText(
            IEnumerable<(string textOrVarName, bool substitute)> text,
            bool crashOnMissingKey = true
        ) {
            var allTexts = new List<(string textOrVarName, bool substitute)>();
            foreach (var (txt, subs) in text) {
                if (subs) {
                    allTexts.Add((txt.ToLower(), true));
                }
                else {
                    allTexts.Add((txt, false));
                }
            }

            _text = allTexts;
            _crashOnMissingKey = crashOnMissingKey;
        }


        public string ToText(BaseInstruction instruction) {
            var subsValues = new Dictionary<string, string>();

            foreach (var f in instruction.GetType().GetFields()) {
                if (!f.IsPublic) {
                    continue;
                }

                subsValues[f.Name.ToLower()] = "" + f.GetValue(instruction);
            }

            var resultText = "";
            foreach (var (text, substitute) in _text) {
                if (substitute) {
                    var firstChar = text.ToCharArray()[0];
                    if (indices.Keys.Contains(firstChar)) {
                        var key = text.Substring(1);
                        var segment = instruction.Route.Meta[instruction.ShapeIndex + indices[firstChar]]?.Attributes;
                        if (segment == null || !segment.TryGetValue(key, out var v)) {
                            if (_crashOnMissingKey) {
                                throw new KeyNotFoundException("The segment does not contain a key  " + text);
                            }

                            return null;
                        }

                        resultText += v;
                    }
                    else if (subsValues.TryGetValue(text, out var newValue)) {
                        resultText += newValue;
                    }
                    else if (_crashOnMissingKey) {
                        throw new KeyNotFoundException("The instruction does not contain a field with name " + text +
                                                       "; try one of " + string.Join(", ", subsValues.Keys));
                    }
                    else {
                        return null;
                    }
                }
                else {
                    resultText += text;
                }
            }

            return resultText;
        }
    }
}