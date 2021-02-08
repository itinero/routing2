using System;
using Itinero.Instructions.Types.Generators;
using Itinero.Network.Attributes;

namespace Itinero.Instructions.Types
{
    internal class FollowAlongGenerator : IInstructionGenerator
    {
        public BaseInstruction Generate(IndexedRoute route, int offset)
        {
            if (offset == 0 || offset == route.Last) {
                // We never a follow along as first or as last...
                return null;
            }

            var usedShapes = 1;
            var totalDistance = 0.0;
            route.Meta[offset].Attributes.TryGetValue("name", out var name);
            while (offset + usedShapes < route.Last) {
                var dAngle = route.DirectionChangeAt(offset + usedShapes);
                if (Math.Abs(dAngle) > 35) {
                    // To much turn for a follow along...
                    break;
                }

                route.Meta[offset + usedShapes].Attributes.TryGetValue("name", out var newName);
                if (name != newName) {
                    // Different street!
                    break;
                }

                var distance = route.DistanceToNextPoint(offset + usedShapes);
                totalDistance += distance;

                usedShapes++;
            }

            if (usedShapes <= 2) {
                // To short for a follow along
                return null;
            }

            var totalChange =
                (route.ArrivingDirectionAt(offset + usedShapes) - route.ArrivingDirectionAt(offset)).NormalizeDegrees();

            // A gentle bend also does turn, at least a few degrees per meter
            if (Math.Abs(totalChange) < 45) {
                // THere is little change - does it at least turn a bit?
                if (Math.Abs(totalChange) / totalDistance >= 2.5) {
                    // Nope, we turn more then 2.5°/m, this is 'followBend'-material
                    return null;
                }
            }

            return new FollowAlongInstruction(
                route,
                offset,
                offset + usedShapes - 1,
                totalChange
            );
        }
    }
}