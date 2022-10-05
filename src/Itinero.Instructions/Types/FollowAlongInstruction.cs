namespace Itinero.Instructions.Types
{
    /**
     * The follow along is more or less the 'continue along this road' instruction.
     * It is issued if no single bend is more then 35° (incl) at a time
     * 
     */
    internal class FollowAlongInstruction : BaseInstruction
    {
        public FollowAlongInstruction(IndexedRoute route, int shapeIndex, int shapeIndexEnd, int turnDegrees) : base(
            route, shapeIndex, shapeIndexEnd, turnDegrees)
        { }
    }
}
