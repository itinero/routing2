namespace Itinero.Instructions.Types;

public class FollowAlongInstruction : BaseInstruction
{
    /// <summary>
    /// The 'follow along'-instruction means (more or less) 'continue along this road'.
    /// It is issued if no single bend is more then 35° (incl) at a time
    /// </summary>
    /// <param name="route"></param>
    /// <param name="shapeIndex"></param>
    /// <param name="shapeIndexEnd"></param>
    /// <param name="turnDegrees"></param>
    public FollowAlongInstruction(IndexedRoute route, int shapeIndex, int shapeIndexEnd, int turnDegrees) : base(
        route, shapeIndex, shapeIndexEnd, turnDegrees)
    { }
}
