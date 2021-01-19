namespace Itinero.Instructions.Generators
{
    /***
     * The 'startInstruction' represents the projection from the actual startpoint (e.g. an adress) to the snapped point on the road.
     * It doesn't really have an associated segment.
     */
    internal class StartInstruction : BaseInstruction
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

        public readonly BaseInstruction then;

        public StartInstruction(IndexedRoute route, int turnDegrees, int absoluteStartingDegrees, uint projectionDistance, BaseInstruction contained = null) :
            base(route, 0, 0, turnDegrees)
        {
            StartDegrees = absoluteStartingDegrees;
            ProjectionDistance = projectionDistance;
            then = contained;
        }


        public StartInstruction(IndexedRoute route, BaseInstruction contained = null) : this(route, 
            0,
            route.DepartingDirectionAt(0).NormalizeDegrees(), 
         0, contained)
        {
        }

        public override string ToString()
        {
            return
                $"Start by going {ProjectionDistance}m towards the road, then turn {TurnDegrees}° to start a {StartDegrees}° journey";
        }
    }

    internal class StartInstructionGenerator : IInstructionGenerator
    {
        public BaseInstruction Generate(IndexedRoute route, int offset)
        {
            if (offset == 0)
            {
                return new StartInstruction(route);
            }

            return null;
        }
    }
}