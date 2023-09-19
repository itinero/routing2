using System.Threading.Tasks;

namespace Itinero.Network.Search.Islands;

/// <summary>
/// Abstract definition of the island builder.
/// </summary>
public interface IIslandBuilder
{
    /// <summary>
    /// Returns true if the edge is on an island.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <returns>True if the edge is on an island, false otherwise.</returns>
    public Task<bool> IsOnIslandAsync(EdgeId edge);
}
