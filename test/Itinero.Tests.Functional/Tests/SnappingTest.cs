using Itinero.Network;
using Itinero.Profiles;
using Itinero.Snapping;

namespace Itinero.Tests.Functional.Tests
{
    internal class SnappingTest : FunctionalTest<SnapPoint, (RoutingNetwork routerDb, double longitude, double latitude, Profile profile)>
    {
        protected override SnapPoint Execute((RoutingNetwork routerDb, double longitude, double latitude, Profile profile) input)
        {
            var result = input.routerDb.Snap().Using(input.profile).To((input.longitude, input.latitude));
            
            return result.Value;
        }
        
        public static readonly SnappingTest Default = new SnappingTest();
    }
}