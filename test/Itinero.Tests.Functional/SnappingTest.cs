using Itinero.Algorithms;
using Itinero.Profiles;

namespace Itinero.Tests.Functional
{
    public class SnappingTest : FunctionalTest<SnapPoint, (RouterDb routerDb, double longitude, double latitude, Profile profile)>
    {
        protected override SnapPoint Execute((RouterDb routerDb, double longitude, double latitude, Profile profile) input)
        {
            var result = input.routerDb.Snap(input.longitude, input.latitude, profile: input.profile);

            return result.Value;
        }
        
        /// <summary>
        /// The default snapping test.
        /// </summary>
        public static readonly SnappingTest Default = new SnappingTest();
    }
}