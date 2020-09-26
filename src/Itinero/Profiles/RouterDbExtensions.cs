using Itinero.Data.Graphs;

namespace Itinero.Profiles
{
    public static class RouterDbExtensions
    {
        internal static EdgeFactor FactorInEdgeDirection<T>(this T enumerator, Profile profile)
            where T : IGraphEdge<Graph>
        {
            var factor = profile.Factor(enumerator.Attributes);
            if (!enumerator.Forward) factor = factor.Reverse;
            return factor;
        }
    }
}