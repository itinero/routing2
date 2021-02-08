using Itinero.Instructions.Types.Generators;

namespace Itinero.Instructions.Types
{
    internal class StartInstructionGenerator : IInstructionGenerator
    {
        public BaseInstruction Generate(IndexedRoute route, int offset)
        {
            if (offset == 0) {
                return new StartInstruction(route);
            }

            return null;
        }
    }
}