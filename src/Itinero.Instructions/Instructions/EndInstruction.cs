namespace Itinero.Instructions.Instructions
{
    /***
     * The 'startInstruction' represents the projection from the actual startpoint (e.g. an adress) to the snapped point on the road.
     * It doesn't really have an associated segment.
     */
    public class EndInstruction : BaseInstruction
    {
        /// <summary>
        /// The distance between the actual start point and the snapped start point on the road
        /// </summary>
        public readonly uint ProjectionDistance;

        public EndInstruction(int turnDegrees, uint projectionDistance, int index) :
            base(index, index, turnDegrees)
        {
            ProjectionDistance = projectionDistance;
        }


        public EndInstruction(IndexedRoute route) : this(
            0,
            (uint) route.DistanceToNextPoint(route.Last),
            route.Shape.Count - 1)
        {
        }

        public override string ToString()
        {
            return $"Your destination lies {ProjectionDistance}m away from the road";
        }
    }

    public class EndInstructionGenerator : IInstructionGenerator
    {
        public BaseInstruction Generate(IndexedRoute route, int offset, out int usedInstructions)
        {
            if (route.Route.Shape.Count == offset)
            {
                usedInstructions = 1;
                return new EndInstruction(offset, 0, 0);
            }

            usedInstructions = 0;
            return null;
        }
    }
}