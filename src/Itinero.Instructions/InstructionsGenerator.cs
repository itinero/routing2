using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Itinero.Instructions.ToText;
using Itinero.Instructions.Types.Generators;

namespace Itinero.Instructions
{
    /// <summary>
    ///     An object that can construct instructions
    /// </summary>
    public class InstructionsGenerator
    {
        private static Lazy<InstructionsGenerator> defaultLazy = new Lazy<InstructionsGenerator>(() => {
            using var stream =
                typeof(InstructionsGenerator).Assembly.GetManifestResourceStream(
                    "Itinero.Instructions.ToText.default.json");
            return FromConfigStream(stream ?? throw new InvalidOperationException("Default not found"));
        });
        
        public static  InstructionsGenerator Default => defaultLazy.Value;
        internal LinearInstructionGenerator Generator { get; }
        internal  Dictionary<string, IInstructionToText> ToText { get; }

        private InstructionsGenerator(LinearInstructionGenerator generator,
            Dictionary<string, IInstructionToText> toText)
        {
            Generator = generator;
            ToText = toText;
        }

        public static InstructionsGenerator FromConfigFile(string path,
            IEnumerable<IInstructionGenerator>? extraGenerators = null)
        {
            return FromConfig(File.ReadAllText(path), extraGenerators);
        }
        
        public static InstructionsGenerator FromConfig(string configContents,
            IEnumerable<IInstructionGenerator>? extraGenerators = null)
        {
            return FromConfigFile(JsonDocument.Parse(configContents).RootElement, extraGenerators);
        }

        public static InstructionsGenerator FromConfigStream(Stream stream,  IEnumerable<IInstructionGenerator>? extraGenerators = null)
        {
            return FromConfigFile(JsonDocument.Parse(stream).RootElement, extraGenerators);
        }


        public static InstructionsGenerator FromConfigFile(JsonElement element,
            IEnumerable<IInstructionGenerator>? extraGenerators = null)
        {
            var (generator, translations) = ConfigurationParser.ParseRouteToInstructions(element,
                AllGenerators.GetDict(extraGenerators));
            return new InstructionsGenerator(generator, translations);
        }
    }
}