using System;

namespace Itinero.Instructions.Instructions {
    /**
     * A bend in the road is there if:
     * - Within the first 100m, the road turns at least 25°
     * - The road feels continuous:
     * *  All the branches are on the non-turning side (e.g. the turn direction is left, but there are no branches on the left)
     * * OR the branches on the turn-to side are all service roads or tracks and the current one isn't that too
     * - There are no major bends in the other direction (thus: every individual bend is at least 0°)
     */
    public class FollowBendInstruction : BaseInstruction {
        public FollowBendInstruction(IndexedRoute route, int shapeIndex, int shapeIndexEnd, int turnDegrees) : base(
            route, shapeIndex, shapeIndexEnd, turnDegrees) { }
    }

    public class FollowBendGenerator : IInstructionGenerator {
        public BaseInstruction Generate(IndexedRoute route, int offset) {
            if (offset == 0 || offset == route.Last) {
                // We never have a bend at first or as last...
                return null;
            }
            // Okay folks!
            // We will be walking forward - as long as we are turning in one direction, it is fine!


            var angleDiff = route.DirectionChangeAt(offset);
            var angleSign = Math.Sign(angleDiff);
            var usedShapes = 0;


            var totalDistance = 0.0;
            // We walk forward and detect a true gentle bend:
            while (true) {
                var distance = route.DistanceToNextPoint(offset + usedShapes);
                if (distance > 35) {
                    // a gentle bent must have pieces that are not too long at a time
                    break;
                }

                var dAngle = route.DirectionChangeAt(offset + usedShapes);
                if (Math.Sign(route.DirectionChangeAt(offset + usedShapes)) != angleSign) {
                    // The gentle bend should turn in the same direction as the first angle
                    // Here, it doesn't have that...
                    break;
                }
                totalDistance += distance;
                angleDiff += dAngle;
                // We keep the total angle too; as it might turn more then 180°
                // We do NOT normalize the angle
                usedShapes++;
            }


            if (usedShapes <= 1) {
                // A 'bend' isn't a bend if there is only one point, otherwise it is a turn...
                return null;
            }

            var totalChange =
                (route.DepartingDirectionAt(offset+ usedShapes) - route.ArrivingDirectionAt(offset )).NormalizeDegrees();

            // A gentle bend also does turn, at least a few degrees per meter
            if (Math.Abs(totalChange) < 45) {
                // THere is little change - does it at least turn a bit?
                if (Math.Abs(totalChange) / totalDistance < 2.5) {
                    // Nope, we turn only 2.5 per meter - that isn't a lot
                    return null;
                }
            }

            return new FollowBendInstruction(
                route,
                offset,
                offset + usedShapes,
                totalChange
            );
        }
    }
}