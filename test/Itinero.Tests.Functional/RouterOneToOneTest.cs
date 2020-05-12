using Itinero.Algorithms;
using Itinero.Profiles;
using Itinero.Routers;

namespace Itinero.Tests.Functional
{
    /// <summary>
    /// A simple point-to-point routing test.
    /// </summary>
    public class RouterOneToOneTest : FunctionalTest<Route, (RouterDb routerDb, SnapPoint sp1, SnapPoint sp2, Profile profile)>
    {
        protected override Route Execute((RouterDb routerDb, SnapPoint sp1, SnapPoint sp2, Profile profile) input)
        {
            var (routerDb, sp1, sp2, profile) = input;

            var route = routerDb.Route(new RoutingSettings()
                {
                    Profile = profile, MaxDistance = double.MaxValue
                })
                .From(sp1)
                .To(sp2)
                .Calculate();
            return route.Value;
        }
        
        /// <summary>
        /// The default point-to-point routing test.
        /// </summary>
        public static readonly RouterOneToOneTest Default = new RouterOneToOneTest();
    }
}