using System.Collections.Generic;
using System.Threading;
using Itinero.Network;

namespace Itinero.Search;

/// <summary>
/// A query on a routing network.
/// </summary>
public interface IRoutingNetworkQuery
{
    /// <summary>
    /// Searches vertices.
    /// </summary>
    /// <param name="box">The bbox to search.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An enumerable with the vertices.</returns>
    public IAsyncEnumerable<VertexId> Vertices(
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) box,
                CancellationToken cancellationToken = default);
}
