using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Itinero.Network;
using Itinero.Network.Search;

namespace Itinero.Search;

internal class RoutingNetworkQuery : IRoutingNetworkQuery
{
    private readonly RoutingNetwork _network;

    public RoutingNetworkQuery(RoutingNetwork network)
    {
        _network = network;
    }
    public async IAsyncEnumerable<VertexId> Vertices(
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e) bottomRight)
            box, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _network.UsageNotifier.NotifyBox(_network, box, cancellationToken);
        if (cancellationToken.IsCancellationRequested) yield break;
        
        var vertices = _network.SearchVerticesInBox(box);
        foreach (var (vertex, _) in vertices)
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            yield return vertex;
        }
    }
}
