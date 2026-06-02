namespace Itinero.Network.Search.Islands;

/// <summary>
/// Which subgraph an island classification considers.
///
/// <list type="bullet">
/// <item><see cref="Full"/> — every edge the profile considers routable.
/// Used by snapping. Answers "can this edge reach the main routable
/// network at all?".</item>
/// <item><see cref="NonLocal"/> — every edge the profile considers routable
/// EXCEPT those tagged <c>access=destination</c> (or mode equivalent).
/// Used by the access-aware routing rule. Answers "is this edge in the
/// N-only mainland?".</item>
/// </list>
/// </summary>
public enum IslandKind
{
    /// <summary>
    /// The full routable subgraph. L-edges (local-access) included.
    /// </summary>
    Full = 0,

    /// <summary>
    /// The N-only subgraph. L-edges masked off via
    /// <see cref="Itinero.Routing.Costs.NonLocalCostFunction"/>.
    /// </summary>
    NonLocal = 1,
}
