using System.Collections.Generic;

namespace Itinero.Instructions.Types;

/// <summary>
///     The crossroad-instructions is an instruction that helps travellers cross intersections.
///     When multiple streets are coming together on nearly the same point (e.g. only a few meters apart), then the
///     traveller can get confused.
///     And lets be honest, 'turn right' isn't all that clear when there is a road slightly right, right and sharp right.
/// </summary>
internal class IntersectionInstruction : BaseInstruction
{
    public readonly uint ActualIndex;

    /// <summary>
    ///     The list with all the branches and properties of all the roads at this crossroads, *except* the one we just came
    ///     from.
    ///     They are sorted by their relativeDegrees (where 0° is straight on the direction we came from)
    /// </summary>
    public readonly List<(int relativeDegrees, IEnumerable<(string, string)> tags)> AllRoads;


    public IntersectionInstruction(IndexedRoute route, int shapeIndex, int shapeIndexEnd, int turnDegrees,
        List<(int relativeDegrees, IEnumerable<(string, string)> tags)> allRoads, uint actualIndex) : base(
        route, shapeIndex, shapeIndexEnd, turnDegrees)
    {
        AllRoads = allRoads;
        ActualIndex = actualIndex;
    }


    public override string ToString()
    {
        return
            $"On the crossing: turn {this.TurnDegrees}° (road {ActualIndex + 1}/{AllRoads.Count} if left to right indexed) ({base.ToString()})";
    }
}
