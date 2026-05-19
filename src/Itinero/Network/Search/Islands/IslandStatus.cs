namespace Itinero.Network.Search.Islands;

/// <summary>
/// Classification of an edge relative to the routable main network.
/// </summary>
public enum IslandStatus
{
    /// <summary>
    /// No determination yet — caller has no information about this edge.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The edge is part of the main routable network (not an island).
    /// </summary>
    NotIsland = 1,

    /// <summary>
    /// The edge belongs to an island that is disconnected (or smaller than
    /// MaxIslandSize) from the main routable network.
    /// </summary>
    Island = 2
}
