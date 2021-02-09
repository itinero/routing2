using System.Collections.Generic;
using System.Linq;

namespace Itinero.Instructions.Types.Generators
{
    internal class IntersectionInstructionGenerator : IInstructionGenerator
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