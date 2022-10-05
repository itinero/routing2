using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Profiles
{
    public static class ProfileExtensions
    {
        internal static EdgeFactor FactorInEdgeDirection(this Profile profile,
            IEdgeEnumerator<RoutingNetwork> enumerator)
        {
            var factor = profile.Factor(enumerator.Attributes);
            if (!enumerator.Forward)
            {
                factor = factor.Reverse;
            }

            return factor;
        }
    }
}
