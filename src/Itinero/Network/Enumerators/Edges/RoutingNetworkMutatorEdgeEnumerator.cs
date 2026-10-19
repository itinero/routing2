using Itinero.Network.Mutation;

namespace Itinero.Network.Enumerators.Edges;

internal class RoutingNetworkMutatorEdgeEnumerator : EdgeEnumerator<RoutingNetworkMutator>
{
    public RoutingNetworkMutatorEdgeEnumerator(RoutingNetworkMutator snapshot)
        : base(snapshot) { }
}
