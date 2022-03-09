namespace Itinero.Instructions.Types.Generators
{
    internal class EndInstructionGenerator : IInstructionGenerator
    {
        public string Name { get; } = "end";
        
        public BaseInstruction Generate(IndexedRoute route, int offset)
        {
            return route.Last - 1 == offset ? new EndInstruction(route) : null;
        }
    }
}