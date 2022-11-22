namespace Itinero.Instructions.Types;

public class EndInstruction : BaseInstruction
{
    /// <summary>
    ///     The distance between the actual start point and the snapped start point on the road
    /// </summary>
    public readonly uint ProjectionDistance;

    /// <summary>
    /// The 'endInstruction' represents the projection from the actual endpoint (e.g. an adress) to the snapped point on the road.
    /// It doesn't really have an associated segment.
    /// </summary>
    /// <param name="route"></param>
    /// <param name="turnDegrees"></param>
    /// <param name="projectionDistance"></param>
    /// <param name="index"></param>
    /// <param name="indexEnd"></param>
    public EndInstruction(IndexedRoute route, int turnDegrees, uint projectionDistance, int index, int indexEnd) :
        base(route, index, indexEnd, turnDegrees)
    {
        ProjectionDistance = projectionDistance;
    }


    public EndInstruction(IndexedRoute route) : this(
        route,
        0,
        0,
        route.Shape.Count - 2,
        route.Shape.Count - 1)
    { }

    public override string ToString()
    {
        return $"Your destination lies {ProjectionDistance}m away from the road";
    }
}
