using Itinero.Data.Providers;

namespace Itinero.Network.Enumerators.Edges;

/// <summary>
/// Walks the edges written so far by a <see cref="NetworkBuilder"/>.
/// </summary>
public class NetworkBuilderEdgeEnumerator : EdgeEnumerator<NetworkBuilder>
{
    internal NetworkBuilderEdgeEnumerator(NetworkBuilder builder)
        : base(builder) { }
}
