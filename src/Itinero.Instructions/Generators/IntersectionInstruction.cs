using System.Collections.Generic;
using System.Linq;

namespace Itinero.Instructions.Generators
{
    /// <summary>
    /// The crossroad-instructions is an instruction that helps travellers cross intersections.
    ///
    /// When multiple streets are coming together on nearly the same point (e.g. only a few meters apart), then the traveller can get confused.
    /// And lets be honest, 'turn right' isn't all that clear when there is a road slightly right, right and sharp right.
    /// 
    /// </summary>
    internal class IntersectionInstruction : BaseInstruction
    {
        /// <summary>
        /// The list with all the branches and properties of all the roads at this crossroads, *except* the one we just came from.
        /// They are sorted by their relativeDegrees (where 0° is straight on the direction we came from)
        /// </summary>
        public readonly List<(int relativeDegrees, IEnumerable<(string, string)> tags)> AllRoads;

        public readonly uint ActualIndex;


        public IntersectionInstruction(IndexedRoute route, int shapeIndex, int shapeIndexEnd, int turnDegrees,
            List<(int relativeDegrees, IEnumerable<(string, string)> tags)> allRoads, uint actualIndex) : base(
            route, shapeIndex, shapeIndexEnd, turnDegrees)
        {
            AllRoads = allRoads;
            ActualIndex = actualIndex;
        }

        public string Mode()
        {
            var turnCutoff = 30;

            if (ActualIndex == 1 && AllRoads.Count == 3
            ) {
                if (TurnDegrees > turnCutoff
                ) {
                    if (TurnDegrees < 60) {
                        // We are going left, but this is not the leftmost street
                        return "slightly left";
                    }
                    else {
                        return "left, but not leftmost";
                    }
                }

                if (TurnDegrees < -turnCutoff) {
                    if (TurnDegrees < -60) {
                        // We are going left, but this is not the leftmost street
                        return "slightly right";
                    }
                    else {
                        return "right, but not rightmost";
                    }
                }

                if (TurnDegrees < turnCutoff && TurnDegrees > -turnCutoff) {
                    // The most boring crossroads in existence
                    return "cross the road";
                }
            }

            if (ActualIndex == 0) {
                // We take the leftmost road
                if (180 - TurnDegrees < turnCutoff) {
                    return "sharp left";
                }

                if (TurnDegrees > turnCutoff) {
                    if (AllRoads[1].relativeDegrees > turnCutoff) {
                        // The 'next' street is quite 'left' as well, we should not confuse our users
                        return "leftmost";
                    }

                    return "left";
                }

                return "keep left";
            }

            if (ActualIndex == AllRoads.Count - 1) {
                // We take the rightmost road
                if (180 + TurnDegrees < turnCutoff) {
                    return "sharp right";
                }

                if (TurnDegrees < turnCutoff) {
                    if (-AllRoads[AllRoads.Count - 2].relativeDegrees < turnCutoff) {
                        // The 'next' street is quite 'right' as well, we should not confuse our users
                        return "rightmost";
                    }

                    return "left";
                }

                return "keep left";
            }

            return "??";
        }


        public override string ToString()
        {
            return
                $"On the crossing: {Mode()} (road {ActualIndex + 1}/{AllRoads.Count} if left to right indexed) ({base.ToString()})";
        }

        public class IntersectionInstructionGenerator : IInstructionGenerator
        {
            public BaseInstruction Generate(IndexedRoute route, int offset)
            {
                if (route.Last == offset + 1) {
                    // The next maneuver is 'arrive', no need to emit a complicated intersection-instruction
                    return null;
                }

                var branches = route.Branches[offset];
                if (branches.Count == 0) {
                    return null;
                }

                var incomingStreets = new List<(int relativeDegrees, IEnumerable<(string, string)> tags)>();


                var incomingDirection = route.ArrivingDirectionAt(offset);
                foreach (var branch in branches) {
                    var branchAbsDirection = Utils.AngleBetween(route.Shape[offset], branch.Coordinate);
                    var branchRelDirection = branchAbsDirection - incomingDirection;
                    incomingStreets.Add((branchRelDirection.NormalizeDegrees(), branch.Attributes));
                }

                var directionChange = route.DirectionChangeAt(offset);
                var nextStep = (directionChange, route.Meta[offset].Attributes);
                incomingStreets.Add(nextStep);

                incomingStreets = incomingStreets.OrderByDescending(br => br.relativeDegrees).ToList();

                var actualIndex = incomingStreets.IndexOf(nextStep);


                var instruction = new IntersectionInstruction(route,
                    offset,
                    offset + 1,
                    directionChange,
                    incomingStreets,
                    (uint) actualIndex);

                return instruction;
            }
        }
    }
}