using Itinero.Algorithms;
using Itinero.Profiles;

namespace Itinero.Tests.Functional
{
    public class SnappingTest : FunctionalTest<Result<SnapPoint>, (RouterDb routerDb, double longitude, double latitude, Profile profile)>
    {
        protected override Result<SnapPoint> Execute((RouterDb routerDb, double longitude, double latitude, Profile profile) input)
        {
            return input.routerDb.Snap(input.longitude, input.latitude, profile: input.profile);
        }
        
        /// <summary>
        /// The default snapping test.
        /// </summary>
        public static readonly SnappingTest Default = new SnappingTest();
    }
}