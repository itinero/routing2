namespace Itinero.Instructions.Types.Generators;

internal class StartInstructionGenerator : IInstructionGenerator
{
    public string Name { get; } = "start";

    public BaseInstruction Generate(IndexedRoute route, int offset)
    {
        if (offset == 0)
        {
            return new StartInstruction(route);
        }

        return null;
    }
}
