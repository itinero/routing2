namespace Itinero.Routing.Alternatives;

/// <summary>
/// The alternative route settings.
/// </summary>
public class AlternativeRouteSettings
{
    /// <summary>
    /// The maximum number of routes returned, the fastest route included. 1 asks for no
    /// alternatives.
    /// </summary>
    /// <remarks>
    /// Fewer may come back: on a short trip where every path uses the same approach roads, the
    /// overlap limit correctly admits none.
    /// </remarks>
    public int MaxNumberOfAlternativeRoutes { get; set; } = 3;

    /// <summary>
    /// How much of its own length an alternative may share with the routes already returned, 0 to 1.
    /// </summary>
    /// <remarks>
    /// Measured against the union of the fastest route and every alternative already accepted, so
    /// a second alternative differs from the first one too.
    /// </remarks>
    public double MaxOverlap { get; set; } = 0.2;

    /// <summary>
    /// The budget as a multiple of the fastest route's cost. Nothing costing more is considered.
    /// </summary>
    public double MaxCostFactor { get; set; } = 1.5;

    /// <summary>
    /// How much more a stretch that leaves the fastest route may cost than the stretch it replaces.
    /// </summary>
    /// <remarks>
    /// This is what rejects a detour that saves nothing: leaving a motorway for a roundabout and
    /// rejoining it stays within budget overall but costs several times the stretch it skips.
    /// </remarks>
    public double LocalDetourFactor { get; set; } = 2.0;
}
