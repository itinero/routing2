namespace Itinero.Instructions.Generators
{
    /***
     * The 'startInstruction' represents the projection from the actual startpoint (e.g. an adress) to the snapped point on the road.
     * It doesn't really have an associated segment.
     */
    internal class EndInstruction : BaseInstruction
    {
        /// <summary>
        /// The distance between the actual start point and the snapped start point on the road
        /// </summary>
        public readonly uint ProjectionDistance;

        public EndInstruction(IndexedRoute route, int turnDegrees, uint projectionDistance, int index) :
            base(route, index, index, turnDegrees)
        {
            ProjectionDistance = projectionDistance;
        }


        public EndInstruction(IndexedRoute route) : this(
            route,
            0,
            (uint) 0,
            route.Shape.Count - 1)
        {
        }

        public override string ToString()
        {
            return $"Your destination lies {ProjectionDistance}m away from the road";
        }
    }

    internal class EndInstructionGenerator : IInstructionGenerator
    {
        public BaseInstruction Generate(IndexedRoute route, int offset)
        {
            if (route.Route.Shape.Count -1 != offset ) {
                return null;
            }

            return new EndInstruction(route);

        }
    }
}