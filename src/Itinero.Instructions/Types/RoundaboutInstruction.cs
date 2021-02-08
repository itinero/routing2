namespace Itinero.Instructions.Types
{
    internal class RoundaboutInstruction : BaseInstruction
    {
        /**
         * This boolean is here for cases as:  https://www.openstreetmap.org/directions?engine=graphhopper_car&route=50.94569%2C3.15129%3B50.94636%2C3.15186#map=19/50.94623/3.15189
         */
        public  bool ExitIsOnTheInside { get; }

        /// <summary>
        ///     The number of the exit to take (one-based)
        /// </summary>
        public int ExitNumber { get; }

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
}