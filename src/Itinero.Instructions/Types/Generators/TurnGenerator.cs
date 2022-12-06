using System;

namespace Itinero.Instructions.Types.Generators;

internal class TurnGenerator : IInstructionGenerator
{
    public string Name { get; } = "turn";

    public BaseInstruction? Generate(IndexedRoute route, int offset)
    {
        if (offset == 0 || offset == route.Last)
        {
            // We never have a bend at first or as last...
            return null;
        }
        // Okay folks!
        // We will be walking forward - as long as we are turning in one direction, it is fine!


        var angleDiff = route.DirectionChangeAt(offset);
        var angleSign = Math.Sign(angleDiff);
        var usedShapes = 1;

        var totalDistance = route.DistanceToNextPoint(offset);
        // We walk forward and detect a true gentle bend:
        while (offset + usedShapes < route.Last)
        {
            var distance = route.DistanceToNextPoint(offset + usedShapes);
            if (distance > 5)
            {
                // a sharp bend must have pieces that are short
                break;
            }

            var dAngle = route.DirectionChangeAt(offset + usedShapes);
            if (Math.Sign(route.DirectionChangeAt(offset + usedShapes)) != angleSign)
            {
                // A turn should only go in the same direction as the first angle
                // Here, it doesn't have that...
                break;
            }

            totalDistance += distance;
            angleDiff += dAngle;
            // We keep the total angle too; as it might turn more then 180°
            // We do NOT normalize the angle
            usedShapes++;
        }


        // A turn does turn, at least a few degrees per meter
        if (Math.Abs(angleDiff) < 45)
        {
            // There is little change - does it at least turn a bit?
            if (Math.Abs(angleDiff) / totalDistance < 2.5)
            {
                // Nope, we turn only 2.5° per meter - that isn't a lot
                return null;
            }
        }

        return new TurnInstruction(
            route,
            offset,
            offset + usedShapes - 1,
            angleDiff
        );
    }
}
