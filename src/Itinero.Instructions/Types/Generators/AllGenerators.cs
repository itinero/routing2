using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Instructions.Types.Generators;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]

namespace Itinero.Instructions.Types
{
    internal static class AllGenerators
    {
        private static readonly List<IInstructionGenerator> Generators = new List<IInstructionGenerator> {
            new BaseInstructionGenerator(),
            new EndInstructionGenerator(),
            new StartInstructionGenerator(),
            new IntersectionInstructionGenerator(),
            new RoundaboutInstructionGenerator(),
            new FollowAlongGenerator(),
            new FollowBendGenerator()
        };

        public static readonly Dictionary<string, IInstructionGenerator> AllGeneratorsDict = GetDict();

        private static Dictionary<string, IInstructionGenerator> GetDict()
        {
            var dict = new Dictionary<string, IInstructionGenerator>();

            foreach (var generator in Generators) {
                var name = generator.GetType().Name.ToLower();
                if (name.EndsWith("generator")) {
                    dict[name] = generator;
                    name = name.Substring(0, name.Length - "generator".Length);
                }

                if (name.EndsWith("instruction")) {
                    dict[name] = generator;
                    name = name.Substring(0, name.Length - "instruction".Length);
                }

                dict[name] = generator;
            }

            return dict;
        }
    }
}