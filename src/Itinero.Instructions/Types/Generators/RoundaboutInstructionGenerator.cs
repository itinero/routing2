namespace Itinero.Instructions.Types.Generators;

internal class RoundaboutInstructionGenerator : IInstructionGenerator
{
    public string Name { get; } = "roundabout";

    public BaseInstruction? Generate(IndexedRoute route, int offset)
    {
        // The roundabout instruction starts when the next segment is on the roundabout ("Go on the roundabout...")
        // and ends when the person leaves the roundabout ("... and take the n'th exit")

        if (offset >= route.Last - 1)
        { // No next entries
            return null;
        }

        var inDegrees = route.DepartingDirectionAt(offset); // Offset is still on the rampup
        var usedInstructions = 1;
        var exitCount = 0;
        while (route.Meta[offset + usedInstructions].GetAttributeOrNull("junction") == "roundabout")
        {
            if (route.Branches.Count > offset + usedInstructions)
            {
                exitCount += route.Branches[offset + usedInstructions].Count;
            }

            usedInstructions++;
        }

        if (usedInstructions == 1)
        {
            // We didn't find a roundabout in the end
            return null;
        }


        var outDegrees = route.DepartingDirectionAt(offset + usedInstructions);

        return new RoundaboutInstruction(route,
            offset,
            offset + usedInstructions,
            (outDegrees - inDegrees).NormalizeDegrees(),
            exitCount + 1
        );
    }
}
