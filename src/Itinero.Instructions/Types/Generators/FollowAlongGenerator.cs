using System;
using Itinero.Network.Attributes;

namespace Itinero.Instructions.Types.Generators;

internal class FollowAlongGenerator : IInstructionGenerator
{
    public string Name { get; } = "followalong";

    public BaseInstruction? Generate(IndexedRoute route, int offset)
    {
        if (offset == 0 || offset == route.Last)
        {
            // We can have never a follow along as first or as last locations...
            return null;
        }

        var usedShapes = 1;
        var totalDistance = route.DistanceToNextPoint(offset);
        route.Meta[offset].Attributes.TryGetValue("name", out var name);
        while (offset + usedShapes < route.Last)
        {
            var dAngle = route.DirectionChangeAt(offset + usedShapes);
            if (Math.Abs(dAngle) > 35)
            {
                // To much turn for a follow along...
                break;
            }

            route.Meta[offset + usedShapes].Attributes.TryGetValue("name", out var newName);
            if (name != newName)
            {
                // Different street!
                break;
            }

            var distance = route.DistanceToNextPoint(offset + usedShapes);
            totalDistance += distance;

            usedShapes++;
        }
        // In degrees. Difference in bearing between start- and end
        var totalChange =
            (route.ArrivingDirectionAt(offset + usedShapes) - route.ArrivingDirectionAt(offset)).NormalizeDegrees();

        // A follow along is not allowed to turn more then 45 degrees in total; otherwise this is a 'followBend'
        if (Math.Abs(totalChange) >= 45)
        {
            return null;
        }

        // THere is little directional change - does it turn at most a little bit?
        if (Math.Abs(totalChange) / totalDistance >= 2.5)
        {
            // Nope, we turn more then 2.5°/m, this is 'followBend'-material
            return null;
        }


        return new FollowAlongInstruction(
            route,
            offset,
            offset + usedShapes - 1,
            totalChange
        );
    }
}
