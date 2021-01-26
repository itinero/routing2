using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Instructions.ToText;
using Itinero.Routes;
using Newtonsoft.Json.Linq;

namespace Itinero.Instructions {
    /**
     * With this class, one can create an object to convert routes into a list of textual instructions
     */
    public class Instructions {
        private readonly LinearInstructionGenerator _generator;
        private readonly Dictionary<string, IInstructionToText> _instructionToTexts;

        private Instructions(LinearInstructionGenerator generator,
            Dictionary<string, IInstructionToText> instructionToTexts) {
            _generator = generator;
            _instructionToTexts = instructionToTexts;
        }


        public static Instructions FromFile(string path) {
            var jobj = JObject.Parse(File.ReadAllText(path));
            var (generator, languages) = FromJson.ParseRouteToInstructions(jobj);
            return new Instructions(generator, languages);
        }

        public (int shapeIndex, int shapeEnd, string)[] Generate(Route r, string language) {
            if (!_instructionToTexts.TryGetValue(language, out var instructionToText)) {
                throw new ArgumentException("Language " + language + " not supported; only supported languages are" +
                                            string.Join(", ", _instructionToTexts.Keys));
            }

            var instructions = _generator.GenerateInstructions(r);
            var texts = instructions.Select(i => (i.ShapeIndex, i.ShapeIndexEnd, instructionToText.ToText(i)));
            return texts.ToArray();
        }
    }
}