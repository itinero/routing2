using System.Collections.Generic;
using System.Linq;

namespace Itinero.Instructions.Instructions
{
    /// <summary>
    /// The crossroad-instructions is an instruction that helps travellers cross intersections.
    ///
    /// When multiple streets are coming together on nearly the same point (e.g. only a few meters apart), then the traveller can get confused.
    /// And lets be honest, 'turn right' isn't all that clear when there is a road slightly right, right and sharp right.
    /// 
    /// </summary>
    public class IntersectionInstruction : BaseInstruction
    {
        public static IInstructionConstructor Constructor = new IntersectionInstructionGenerator();

        /// <summary>
        /// The list with all the branches and properties of all the roads at this crossroads, *except* the one we just came from.
        /// They are sorted by their relativeDegrees (where 0° is straight on the direction we came from)
        /// </summary>
        public readonly List<(int relativeDegrees, IEnumerable<(string, string)> tags)> AllRoads;

        public readonly uint ActualIndex;


        public IntersectionInstruction(int shapeIndex, int shapeIndexEnd, int turnDegrees,
            List<(int relativeDegrees, IEnumerable<(string, string)> tags)> allRoads, uint actualIndex) : base(
            shapeIndex, shapeIndexEnd, turnDegrees)
        {
            AllRoads = allRoads;
            ActualIndex = actualIndex;
        }


        public override string ToString()
        {
            return "On the crossing, take road " + ActualIndex + ", " + base.ToString();
        }

        internal class IntersectionInstructionGenerator : IInstructionConstructor
        {
            public string Name { get; }

            public BaseInstruction Construct(IndexedRoute route, int offset, out int usedInstructions)
            {
                usedInstructions = 0;


                if (route.Last == offset + 1)
                {
                    // The next maneuver is 'arrive', no need to emit a complicated intersection-instruction
                    return null;
                }

                var branches = route.Branches[offset];
                if (branches.Count == 0)
                {
                    return null;
                }

                var incomingStreets = new List<(int relativeDegrees, IEnumerable<(string, string)> tags)>();


                var incomingDirection = route.ArrivingDirectionAt(offset);
                foreach (var branch in branches)
                {
                    var branchAbsDirection = Utils.AngleBetween(route.Shape[offset], branch.Coordinate);
                    var branchRelDirection = branchAbsDirection - incomingDirection;
                    incomingStreets.Add((branchRelDirection.NormalizeDegrees(), branch.Attributes));
                }

                incomingStreets = incomingStreets.OrderBy(br => br.relativeDegrees).ToList();


                var directionChange = route.DirectionChangeAt(offset);
                var actualIndex = 0;
                while (incomingStreets[actualIndex].relativeDegrees < directionChange)
                {
                    actualIndex++;
                }

                incomingStreets.Insert(actualIndex, (directionChange, route.Meta[offset].Attributes));


                usedInstructions = 1;
                var instruction = new IntersectionInstruction(
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