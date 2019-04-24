namespace Itinero.Profiles
{
    public static class RouterDbExtensions
    {
        public static uint EdgeWeight(this RouterDb routerDb, Profile profile, uint edgeId, bool forward = true)
        {
            var attributes = routerDb.GetAttributes(edgeId);
            var factor = profile.Factor(attributes);
            var length = routerDb.EdgeLength(edgeId);
            
            if (forward)
            {
                return length * factor.FactorForward;
            }

            return length * factor.FactorBackward;
        }
    }
}