namespace Itinero.Network.Search.Islands;

/// <summary>
/// Read/write callback interface for the pure island classification algorithm.
///
/// The algorithm queries <see cref="Get"/> when it encounters an edge to learn
/// what the caller already knows (cached from a previous classification or a
/// pre-built tile). It calls <see cref="Set"/> exactly once for each edge whose
/// classification it definitively determines, so the caller can persist the
/// result however it wants — in-memory dictionary, tile-based store, etc.
///
/// The algorithm itself is stateless: between calls it owns nothing. All
/// caching/persistence lives in the implementation of this interface.
/// </summary>
public interface IIslandClassificationStore
{
    /// <summary>
    /// Returns the cached classification for an edge, or
    /// <see cref="IslandStatus.Unknown"/> if the caller has nothing to share.
    /// </summary>
    IslandStatus Get(EdgeId edgeId);

    /// <summary>
    /// Records the definitive classification for an edge. Called at most once
    /// per edge per <see cref="IslandClassifier.ClassifyAsync"/> invocation.
    /// </summary>
    void Set(EdgeId edgeId, IslandStatus status);
}
