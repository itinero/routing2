namespace Itinero.Network.Search.Reachability;

/// <summary>
/// How much network has to be found before a candidate counts as connected, and how much work
/// may be spent looking.
/// </summary>
/// <remarks>
/// The threshold is the judgement and the analogue of MaxIslandSize; the ceiling is the work
/// limit, and only bites where the threshold cannot end a search on its own.
/// </remarks>
/// <remarks>
/// Counted in expanded states rather than edges, and an edge has two of those, so a comparable
/// threshold is roughly twice the MaxIslandSize value.
/// </remarks>
public readonly record struct ReachabilityBounds
{
    /// <summary>
    /// Creates bounds.
    /// </summary>
    /// <param name="threshold">States that count as connected.</param>
    /// <param name="ceiling">States that may be spent before giving up on the question.</param>
    public ReachabilityBounds(int threshold)
    {
        this.Threshold = threshold;
    }

    /// <summary>
    /// How much network has to be reached before a candidate counts as connected.
    /// </summary>
    public int Threshold { get; init; }

}
