namespace Itinero.Instructions.Types
{
    /***
     * The 'startInstruction' represents the projection from the actual startpoint (e.g. an adress) to the snapped point on the road.
     * It doesn't really have an associated segment.
     */
    public class EndInstruction : BaseInstruction
    {
        /// <summary>
        ///     The distance between the actual start point and the snapped start point on the road
        /// </summary>
        public readonly uint ProjectionDistance;

        internal EndInstruction(IndexedRoute route, int turnDegrees, uint projectionDistance, int index, int indexEnd) :
            base(route, index, indexEnd, turnDegrees)
        {
            ProjectionDistance = projectionDistance;
        }


        internal EndInstruction(IndexedRoute route) : this(
            route,
            0,
            0,
            route.Shape.Count - 2,
            route.Shape.Count - 1) { }

        public override string ToString()
        {
            return $"Your destination lies {ProjectionDistance}m away from the road";
        }
    }
}