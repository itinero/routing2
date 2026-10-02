namespace Itinero.Network.Search.Reachability;

/// <summary>
/// How a bounded reachability expansion ended.
/// </summary>
/// <remarks>
/// Two outcomes only. An empty frontier means the component is closed, and closed is an island
/// whatever its size — how far the walk got before running out says nothing about a way out.
/// </remarks>
internal enum ReachabilityVerdict
{
    /// <summary>
    /// The expansion ran out of network. Everything reachable from here has been seen and it
    /// does not leave, so this is an island.
    /// </summary>
    Island,

    /// <summary>
    /// The maximum stopped it while there was still network ahead. The component is at least
    /// that big, which is all the caller asked.
    /// </summary>
    BeyondBound,
}
