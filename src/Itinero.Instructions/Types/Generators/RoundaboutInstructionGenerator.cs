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

        var inDegrees = route.ArrivingDirectionAt(offset); // Offset is still on the rampup
        var outDegrees = route.DepartingDirectionAt(offset + usedInstructions);

        var shapeIndexEnd = offset + usedInstructions;
        if (shapeIndexEnd + 1 < route.Last)
        {
            // We add an extra index, as the first segment after the roundabout doesn't need an instruction for it;
            // This is always a right turn anyway 
            shapeIndexEnd++;
        }

        return new RoundaboutInstruction(route,
            offset,
             shapeIndexEnd,
            (inDegrees - outDegrees).NormalizeDegrees(),
            exitCount + 1
        );
    }
}
