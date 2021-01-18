using System;

namespace Itinero.Instructions.Generators {
    /**
     * The follow along is more or less the 'continue along this road' instruction.
     * It is issued if no single bend is more then 35° (incl) at a time
     * 
     */
    internal class FollowAlongInstruction : BaseInstruction {
        public FollowAlongInstruction(IndexedRoute route, int shapeIndex, int shapeIndexEnd, int turnDegrees) : base(
            route, shapeIndex, shapeIndexEnd, turnDegrees) { }
    }

    internal class FollowAllowGenerator : IInstructionGenerator {
        public BaseInstruction Generate(IndexedRoute route, int offset) {
            if (offset == 0 || offset == route.Last) {
                // We never a follow along as first or as last...
                return null;
            }
            var usedShapes = 0;
            var totalDistance = 0.0;
            // We walk forward and detect a true gentle bend:
            while (offset + usedShapes < route.Last) {
                var distance = route.DistanceToNextPoint(offset + usedShapes);
                
                var dAngle = route.DirectionChangeAt(offset + usedShapes);
                if (dAngle > 35) {
                    // To much turn for a follow along...
                    break;
                }

                totalDistance += distance;
                // We keep the total angle too; as it might turn more then 180°
                // We do NOT normalize the angle
                usedShapes++;
            }


            if (usedShapes <= 1) {
                // To short for a follow along
                return null;
            }

            var totalChange =
                (route.ArrivingDirectionAt(offset+ usedShapes) - route.ArrivingDirectionAt(offset )).NormalizeDegrees();

            // A gentle bend also does turn, at least a few degrees per meter
            if (Math.Abs(totalChange) < 45) {
                // THere is little change - does it at least turn a bit?
                if (Math.Abs(totalChange) / totalDistance < 2.5) {
                    // Nope, we turn only 2.5 per meter - that isn't a lot
                    return null;
                }
            }

            return new FollowAlongInstruction(
                route,
                offset,
                offset + usedShapes,
                totalChange
            );
        }
    }
}