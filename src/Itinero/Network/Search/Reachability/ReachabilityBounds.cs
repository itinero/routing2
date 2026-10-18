namespace Itinero.Network.Search.Reachability;

/// <summary>
/// How much network has to be found before a candidate counts as connected, and how much work
/// may be spent looking.
/// </summary>
/// <remarks>
/// The threshold is the judgement; the ceiling is the work limit, and only bites where the
/// threshold cannot end a search on its own.
/// </remarks>
/// <remarks>
/// Counted in expanded states rather than edges, and an edge is reached over two arrivals, so a
/// threshold is roughly twice the equivalent edge count.
/// </remarks>
public readonly record struct ReachabilityBounds
{
    /// <summary>
    /// Creates bounds.
    /// </summary>
    /// <param name="threshold">States that count as connected.</param>
    /// <param name="pocketBound">Edges a local-access region may hold before it is not a pocket.</param>
    public ReachabilityBounds(int threshold, int pocketBound = DefaultPocketBound)
    {
        this.Threshold = threshold;
        this.PocketBound = pocketBound;
    }

    /// <summary>
    /// The value this bound ran at before it moved here, so behaviour is unchanged by default.
    /// </summary>
    public const int DefaultPocketBound = 4096;

    /// <summary>
    /// How much network has to be reached before a candidate counts as connected.
    /// </summary>
    public int Threshold { get; init; }

    /// <summary>
    /// Edges a local-access region may hold before it is too big to be a pocket. Counted in
    /// members, not in the expanded states <see cref="Threshold"/> counts.
    /// </summary>
    public int PocketBound { get; init; }

}
