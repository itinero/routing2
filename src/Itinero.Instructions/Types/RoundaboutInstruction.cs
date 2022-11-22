// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Itinero.Instructions.Types;

/// <summary>
/// A roundabout instruction.
/// </summary>
public class RoundaboutInstruction : BaseInstruction
{
    public RoundaboutInstruction(
        IndexedRoute route,
        int shapeIndex,
        int shapeIndexEnd,
        int turnDegrees,
        int exitNumber,
        bool exitIsOnTheInside = false) : base(route, shapeIndex, shapeIndexEnd, turnDegrees)
    {
        this.ExitNumber = exitNumber;
        this.ExitIsOnTheInside = exitIsOnTheInside;
    }

    /// <summary>
    /// True when the exit leads to the inside of the roundabout.
    /// </summary>
    /// <remarks>
    /// Example: https://www.openstreetmap.org/directions?engine=graphhopper_car&route=50.94569%2C3.15129%3B50.94636%2C3.15186#map=19/50.94623/3.15189
    /// </remarks>
    public bool ExitIsOnTheInside { get; }

    /// <summary>
    /// The number of the exit to take (one-based).
    /// </summary>
    public int ExitNumber { get; }

    //
    // public override string ToString()
    // {
    //     return $"Take the roundabout to go {TurnDegrees.DegreesToText()} via the {ExitNumber + 1}th exit."
    //            + (ExitIsOnTheInside ? "WARNING: this exit is on the inner side of the roundabout!" : "");
    // }
}
