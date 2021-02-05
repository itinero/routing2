using System.Collections.Generic;
using System.Linq;
using Itinero.Instructions.Generators;
using Itinero.Network.Attributes;

namespace Itinero.Instructions.ToText
{
    /***
     * Instruction to text changes an instruction object into text based on simple substitution.
     * It uses reflection to create a dictionary of all available fields, which are substituted
     */
    internal class SubstituteText : IInstructionToText
    {
        private readonly string _context;
        private readonly bool _crashOnMissingKey;

        /**
         * Extra "fields" to convert this into a string
         */
        private readonly Dictionary<string, IInstructionToText> _extensions;

        private readonly Box<IInstructionToText> _nestedToText;

        private readonly IEnumerable<(string textOrVarName, bool substitute)> _text;

        /**
         * Only used for testing
         */
        internal SubstituteText(params string[] text) : this(text.Select(t => (t.TrimStart('$'), t.StartsWith("$"))),
            null,
            "a unit test") { }

        public SubstituteText(
            IEnumerable<(string textOrVarName, bool substitute)> text,
            Box<IInstructionToText> nestedToText = null,
            string context = "context not set",
            Dictionary<string, IInstructionToText> extensions = null,
            bool crashOnMissingKey = true
        )
        {
            var allTexts = new List<(string textOrVarName, bool substitute)>();
            foreach (var (txt, subs) in text) {
                allTexts.Add((subs ? txt.ToLower() : txt, subs));
            }

            _text = allTexts;
            _nestedToText = nestedToText;
            _context = context;
            _extensions = extensions;
            _crashOnMissingKey = crashOnMissingKey;
        }


        public string ToText(BaseInstruction instruction)
        {
            var subsValues = new Dictionary<string, object>();

            foreach (var f in instruction.GetType().GetFields()) {
                if (!f.IsPublic) {
                    continue;
                }

                subsValues[f.Name.ToLower()] = f.GetValue(instruction);
            }

            var resultText = "";
            foreach (var (text, substitute) in _text) {
                if (substitute) {
                    var firstChar = text.ToCharArray()[0];
                    if (firstChar == '.' || firstChar == '+' || firstChar == '-') {
                        var key = text.Substring(1);
                        var index = 0;
                        switch (firstChar) {
                            case '+':
                                index = instruction.ShapeIndexEnd;
                                break;
                            case '-':
                                index = instruction.ShapeIndex - 1;
                                break;
                            case '.':
                                index = instruction.ShapeIndex;
                                break;
                        }

                        if (index >= instruction.Route?.Meta?.Count) {
                            return null;
                        }

                        var segment = instruction.Route?.Meta[index]?.Attributes;
                        if (segment == null || !segment.TryGetValue(key, out var v)) {
                            if (_crashOnMissingKey) {
                                throw new KeyNotFoundException("The segment does not contain a key  " + text +
                                                               ". The context is " + _context);
                            }

                            return null;
                        }

                        resultText += v;
                    }
                    else if (subsValues.TryGetValue(text, out var newValue)) {
                        if (newValue is BaseInstruction instr) {
                            resultText += _nestedToText.Content.ToText(instr);
                        }
                        else {
                            resultText += newValue;
                        }
                    }
                    else if (_extensions != null && _extensions.TryGetValue(text, out var subs)) {
                        resultText += subs.ToText(instruction);
                    }
                    else if (_crashOnMissingKey) {
                        throw new KeyNotFoundException(
                            $"The instruction does not contain a field or extension with name {text}; try one of {string.Join(", ", subsValues.Keys)} or an extensions such as {string.Join(", ", _extensions?.Keys.ToList() ?? new List<string>())}. Did you intend to use a segment property? Use $.{text} instead. This happened at {_context}");
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

        public override string ToString()
        {
            return string.Join("", _text.Select(txt => txt.textOrVarName));
        }

        public int SubstitutedValueCount()
        {
            return _text.Count(v => v.substitute);
        }
    }
}