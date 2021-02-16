using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]

namespace Itinero.Instructions.Types.Generators
{
    internal static class AllGenerators
    {
        private static readonly List<IInstructionGenerator> Generators = new() {
            new BaseInstructionGenerator(),
            new EndInstructionGenerator(),
            new StartInstructionGenerator(),
            new IntersectionInstructionGenerator(),
            new RoundaboutInstructionGenerator(),
            new FollowAlongGenerator(),
            new FollowBendGenerator()
        };

        public static readonly Dictionary<string, IInstructionGenerator> AllGeneratorsDict = GetDict();

        private static string Name(this IInstructionGenerator generator)
        {
            var name = generator.GetType().Name.ToLower();
            if (name.EndsWith("generator")) {
                name = name.Substring(0, name.Length - "generator".Length);
            }

            if (name.EndsWith("instruction")) {
                name = name.Substring(0, name.Length - "instruction".Length);
            }

            return name;
        }

        public static Dictionary<string, IInstructionGenerator> GetDict(
            IEnumerable<IInstructionGenerator>? extraGenerators = null)
        {
            var dict = new Dictionary<string, IInstructionGenerator>();

            foreach (var generator in Generators) {
                dict[generator.Name()] = generator;
            }

            if (extraGenerators != null) {
                foreach (var generator in extraGenerators) {
                    dict[generator.Name()] = generator;
                }
            }

            return dict;
        }
    }
}