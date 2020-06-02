using Itinero.Algorithms;
using Itinero.Profiles;

namespace Itinero.Tests.Functional
{
    internal class SnappingTest : FunctionalTest<SnapPoint, (RouterDbInstance routerDb, double longitude, double latitude, Profile profile)>
    {
        protected override SnapPoint Execute((RouterDbInstance routerDb, double longitude, double latitude, Profile profile) input)
        {
            var result = input.routerDb.Snap((input.longitude, input.latitude), input.profile);

            return result.Value;
        }
        
        public static readonly SnappingTest Default = new SnappingTest();
    }
}