namespace Itinero.Instructions.Types.Generators;

internal class BaseInstructionGenerator : IInstructionGenerator
{
    public string Name { get; } = "base";

    public BaseInstruction Generate(IndexedRoute route, int offset)
    {
        if (offset + 1 >= route.Last)
        {
            // The current offset is already the last index of the shape; this is the endpoint
            return null;
        }

        // We are on the end of segment 'offset', what should we do next?
        return new BaseInstruction(route, offset, route.DirectionChangeAt(offset + 1));
    }
}
