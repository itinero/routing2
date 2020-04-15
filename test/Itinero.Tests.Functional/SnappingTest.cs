using Itinero.Algorithms;
using Itinero.Profiles;

namespace Itinero.Tests.Functional
{
    internal class SnappingTest : FunctionalTest<SnapPoint, (RouterDb routerDb, double longitude, double latitude, Profile profile)>
    {
        protected override SnapPoint Execute((RouterDb routerDb, double longitude, double latitude, Profile profile) input)
        {
            var result = input.routerDb.Snap((input.longitude, input.latitude), profile: input.profile);

            return result.Value;
        }
        
        public static readonly SnappingTest Default = new SnappingTest();
    }
}