using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Itinero.Instructions.Config;
using Itinero.Instructions.Types.Generators;

namespace Itinero.Instructions
{
    /// <summary>
    ///     An object that can construct instructions
    /// </summary>
    public class InstructionsGenerator
    {
        internal LinearInstructionGenerator Generator { get; }
        internal  Dictionary<string, IInstructionToText> ToText { get; }

        private InstructionsGenerator(LinearInstructionGenerator generator,
            Dictionary<string, IInstructionToText> toText)
        {
            Generator = generator;
            ToText = toText;
        }

        public static InstructionsGenerator FromConfigFile(string path,
            IEnumerable<IInstructionGenerator> extraGenerators = null)
        {
            return FromConfigFile(JsonDocument.Parse(File.ReadAllText(path)).RootElement, extraGenerators);
        }

        public static InstructionsGenerator FromConfigFile(JsonElement element,
            IEnumerable<IInstructionGenerator> extraGenerators = null)
        {
            var (generator, translations) = ConfigurationParser.ParseRouteToInstructions(element,
                AllGenerators.GetDict(extraGenerators));
            return new InstructionsGenerator(generator, translations);
        }
    }
}