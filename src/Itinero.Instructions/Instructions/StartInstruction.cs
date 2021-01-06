namespace Itinero.Instructions.Instructions
{
    /***
     * The 'startInstruction' represents the projection from the actual startpoint (e.g. an adress) to the snapped point on the road.
     * It doesn't really have an associated segment.
     */
    public class StartInstruction : BaseInstruction
    {
        /// <summary>
        ///     
        ///  The compass degrees to start the trip, with 0° being 'go to the north'
        ///  <remarks>
        /// The 'turnDegrees' is relative to the actual startpoint, thus if walking from the startpoint to the snappedpoint, the amount of degrees to turn then
        /// </remarks>
        /// </summary>
        public readonly int StartDegrees;

        /// <summary>
        /// The distance between the actual start point and the snapped start point on the road
        /// </summary>
        public readonly uint ProjectionDistance;

        public StartInstruction(int turnDegrees, int absoluteStartingDegrees, uint projectionDistance) :
            base(0, 0, turnDegrees)
        {
            StartDegrees = absoluteStartingDegrees;
            ProjectionDistance = projectionDistance;
        }


        public StartInstruction(IndexedRoute route) : this(
            route.DirectionChangeAt(0),
            route.DepartingDirectionAt(0).NormalizeDegrees(), 
            (uint) route.DistanceToNextPoint(-1))
        {
        }

        public override string ToString()
        {
            return
                $"Start by going {ProjectionDistance}m towards the road, then turn {TurnDegrees}° to start a {StartDegrees}° journey";
        }
    }

    public class StartInstructionGenerator : IInstructionGenerator
    {
        public BaseInstruction Generate(IndexedRoute route, int offset, out int usedInstructions)
        {
            if (offset == 0)
            {
                usedInstructions = 1;
                return new StartInstruction(0, 0, 0);
            }

            usedInstructions = 0;
            return null;
        }
    }
}