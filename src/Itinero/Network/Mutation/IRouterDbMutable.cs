using Itinero.Profiles;

namespace Itinero.Network.Mutation
{
    internal interface IRouterDbMutable
    {
        internal void Finish(RoutingNetwork network, RouterDbProfileConfiguration profileConfiguration);
    }
}