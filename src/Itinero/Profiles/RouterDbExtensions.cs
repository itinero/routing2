namespace Itinero.Profiles
{
    public static class RouterDbExtensions
    {
        internal static EdgeFactor FactorInEdgeDirection(this RouterDbEdgeEnumerator enumerator, Profile profile)
        {
            var factor = profile.Factor(enumerator.GetAttributes());
            if (!enumerator.Forward) factor = factor.Reverse;
            return factor;
        }
    }
}