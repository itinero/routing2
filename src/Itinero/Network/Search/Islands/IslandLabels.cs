using Reminiscence.Collections;

namespace Itinero.Network.Search.Islands;

/// <summary>
/// Keeps an island label for edges.
/// </summary>
public class IslandLabels
{
    /// <summary>
    /// Keeps a label for each edge.
    /// - Starting each edge starts with it's own unique Id.
    /// - When edges are bidirectionally connected they get take on the lowest Id of their neighbour.
    /// </summary>
    private readonly Dictionary<EdgeId, uint> _labels = new();
    private readonly uint _nextId = 0;

}
