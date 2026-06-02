using Itinero.Profiles;
using Itinero.Routing.Costs;

namespace Itinero.Network.Search.Islands;

/// <summary>
/// Picks the cost function the classifier should use for a given
/// <see cref="IslandKind"/>. <see cref="IslandKind.Full"/> uses the profile's
/// own cost function unchanged; <see cref="IslandKind.NonLocal"/> wraps it
/// in <see cref="NonLocalCostFunction"/> so L-edges are masked off.
/// </summary>
internal static class IslandKindCostFunctions
{
    public static ICostFunction GetFor(RoutingNetwork network, Profile profile, IslandKind kind)
    {
        var inner = network.GetCostFunctionFor(profile);
        return kind switch
        {
            IslandKind.NonLocal => new NonLocalCostFunction(inner),
            _ => inner,
        };
    }
}
