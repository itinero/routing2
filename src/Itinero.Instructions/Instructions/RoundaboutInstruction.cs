namespace Itinero.Instructions.Instructions
{
    public class RoundaboutInstruction : BaseInstruction
    {
        public static RoundaboutInstructionGenerator Constructor = new RoundaboutInstructionGenerator();

        /**
         * This boolean is here for cases as:  https://www.openstreetmap.org/directions?engine=graphhopper_car&route=50.94569%2C3.15129%3B50.94636%2C3.15186#map=19/50.94623/3.15189
         */
        public readonly bool ExitIsOnTheInside;

        /// <summary>
        ///     The number of the exit to take (zero-based)
        /// </summary>
        public readonly int ExitNumber;

        public RoundaboutInstruction(
            IndexedRoute route,
            int shapeIndex,
            int shapeIndexEnd,
            int turnDegrees,
            int exitNumber,
            bool exitIsOnTheInside = false) : base(route, shapeIndex, shapeIndexEnd, turnDegrees)
        {
            ExitNumber = exitNumber;
            ExitIsOnTheInside = exitIsOnTheInside;
        }

        public override string ToString()
        {
            return $"Take the roundabout to go {TurnDegrees.DegreesToText()} via the {ExitNumber + 1}th exit."
                   + (ExitIsOnTheInside ? "WARNING: this exit is on the inner side of the roundabout!" : "");
        }
    }

    public class RoundaboutInstructionGenerator : IInstructionGenerator
    {
        public string Name { get; } = "Roundabout";

        public BaseInstruction Generate(IndexedRoute route, int offset)
        {
            // The roundabout instruction starts when the next segment is on the roundabout ("Go on the roundabout...")
            // and ends when the person leaves the roundabout ("... and take the n'th exit")

            if (route.Last == offset)
                // No next entries
                return null;

            var inDegrees = route.DepartingDirectionAt(offset); // Offset is still on the rampup
            var usedInstructions = 1;
            var exitCount = 0;
            while (route.Meta[offset + usedInstructions].GetAttributeOrNull("junction") == "roundabout")
            {
                exitCount += route.Branches[offset + usedInstructions].Count;
                usedInstructions++;
            }


            var outDegrees = route.DepartingDirectionAt(offset + usedInstructions);

            return new RoundaboutInstruction(route,
                offset,
                offset + usedInstructions,
                (outDegrees - inDegrees).NormalizeDegrees(),
                exitCount
            );
        }
    }
}