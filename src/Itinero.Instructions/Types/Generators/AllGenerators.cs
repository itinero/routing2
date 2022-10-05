using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]

namespace Itinero.Instructions.Types.Generators
{
    internal static class AllGenerators
    {
        public static readonly IReadOnlyList<IInstructionGenerator> Generators = new List<IInstructionGenerator>() {
            new BaseInstructionGenerator(),
            new EndInstructionGenerator(),
            new StartInstructionGenerator(),
            new IntersectionInstructionGenerator(),
            new RoundaboutInstructionGenerator(),
            new FollowAlongGenerator(),
            new FollowBendGenerator()
        };
    }
}
