namespace Itinero.Instructions.Types;

public class TurnInstruction : BaseInstruction
{
    /// <summary>
    ///     A generic turn instruction.
    /// 
    /// </summary>
    public TurnInstruction(IndexedRoute route, int shapeIndex, int shapeIndexEnd, int turnDegrees) : base(
        route, shapeIndex, shapeIndexEnd, turnDegrees)
    {
    }
}
