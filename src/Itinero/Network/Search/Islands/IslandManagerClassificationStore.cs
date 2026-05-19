namespace Itinero.Network.Search.Islands;

/// <summary>
/// Adapter that exposes the per-profile <see cref="Islands"/> +
/// <see cref="IslandDirectedGraph"/> pair as an
/// <see cref="IIslandClassificationStore"/>. Lets <see cref="IslandClassifier"/>
/// share results with the snap fast-path (which reads
/// <c>Islands.IsEdgeOnIsland</c> / <c>dg.IsNotIsland</c>) and across calls
/// against the same network. Both backing structures take their own locks, so
/// this adapter is safe for concurrent use.
/// </summary>
internal sealed class IslandManagerClassificationStore : IIslandClassificationStore
{
    private readonly Islands _islands;
    private readonly IslandDirectedGraph _dg;

    public IslandManagerClassificationStore(Islands islands, IslandDirectedGraph dg)
    {
        _islands = islands;
        _dg = dg;
    }

    public IslandStatus Get(EdgeId edgeId)
    {
        if (_islands.IsEdgeOnIsland(edgeId)) return IslandStatus.Island;
        if (_dg.IsNotIsland(edgeId)) return IslandStatus.NotIsland;
        return IslandStatus.Unknown;
    }

    public void Set(EdgeId edgeId, IslandStatus status)
    {
        switch (status)
        {
            case IslandStatus.Island:
                _islands.SetEdgeOnIsland(edgeId);
                break;
            case IslandStatus.NotIsland:
                _dg.AddVertex(edgeId);
                _dg.CollapseToMainNetwork(edgeId);
                break;
        }
    }
}
