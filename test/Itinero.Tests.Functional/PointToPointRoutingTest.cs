using Itinero.Algorithms;
using Itinero.Profiles;

namespace Itinero.Tests.Functional
{
    /// <summary>
    /// A simple point-to-point routing test.
    /// </summary>
    public class PointToPointRoutingTest : FunctionalTest<Result<Route>, (RouterDb routerDb, SnapPoint sp1, SnapPoint sp2, Profile profile)>
    {
        protected override Result<Route> Execute((RouterDb routerDb, SnapPoint sp1, SnapPoint sp2, Profile profile) input)
        {
            var (routerDb, sp1, sp2, profile) = input;
            return routerDb.Calculate(profile, sp1, sp2);
        }
        
        /// <summary>
        /// The default point-to-point routing test.
        /// </summary>
        public static readonly PointToPointRoutingTest Default = new PointToPointRoutingTest();
    }
}