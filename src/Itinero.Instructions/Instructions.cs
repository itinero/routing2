using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Itinero.Instructions.ToText;
using Itinero.Routes;

namespace Itinero.Instructions
{
    /// <summary>
    /// Generates instructions.
    /// </summary>
    public class Instructions
    {
        private readonly LinearInstructionGenerator _generator;
        private readonly Dictionary<string, IInstructionToText> _instructionToTexts;

        private Instructions(LinearInstructionGenerator generator,
            Dictionary<string, IInstructionToText> instructionToTexts)
        {
            _generator = generator;
            _instructionToTexts = instructionToTexts;
        }

        /// <summary>
        /// Gets the default instruction generator.
        /// </summary>
        /// <returns>The instruction generator.</returns>
        public static Instructions Default()
        {
            var stream =
                typeof(Instructions).Assembly.GetManifestResourceStream("Itinero.Instructions.en-GB.json");
            if (stream == null)
                throw new Exception("Default instructions not available, embedded resource not found.");
            return ReadFrom(stream);
        }
        
        public static Instructions ReadFrom(Stream stream)
        {
            var jobj = JsonDocument.Parse(stream);
            var (generator, languages) = FromJson.ParseRouteToInstructions(jobj.RootElement);

            return new Instructions(generator, languages);
        }
        
        
        public static Instructions FromFile(string path)
        {
            return Instructions.ReadFrom(File.OpenRead(path));
        }

        /// <summary>
        /// Generates instructions.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="language">The language, for example 'en'.</param>
        /// <returns>An array of instructions and where they apply.</returns>
        public (int shapeIndex, int shapeEnd, string)[] Generate(Route route, string language)
        {
            if (!_instructionToTexts.TryGetValue(language, out var instructionToText)) {
                throw new ArgumentException($"Language not supported:  {language}");
            }

            var instructions = _generator.GenerateInstructions(route);
            var texts = instructions.Select(i => (i.ShapeIndex, i.ShapeIndexEnd, instructionToText.ToText(i)));
            return texts.ToArray();
        }
    }
}